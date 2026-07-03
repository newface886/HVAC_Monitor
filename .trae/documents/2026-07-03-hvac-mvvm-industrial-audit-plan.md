# HVAC 能源监控平台 —— MVVM / 工业级可靠性重构与评估计划

## 1. Summary

本计划对 `d:\study\623WPFstudy\HVAC.EnergyMonitor` 进行全面架构审计，识别出 MVVM、依赖注入、资源管理、异步与异常处理、UI 线程安全等方面的差距，并给出可落地的修复方案。计划同时完成前一会话遗留的 **Step 11 ~ Step 13** 工作（`DeviceConfigViewModel` 命令化、`MainWindowViewModel` 资源释放、最终集成验证）。

修复后应达到以下工业场景标准：

- 所有 ViewModel 统一继承 `ViewModelBase`，使用 `ExecuteAsync` 包裹异步/IO 操作。
- 所有按钮交互使用 `Command` 绑定，无代码隐藏 `Click` 事件。
- 无直接 `DataContext = ...` 赋值，ViewModel 由 Prism 导航/注入提供。
- 所有数据访问通过 `IDbContextFactory`/`IUnitOfWork`，DbContext 不跨线程复用。
- 定时器、`CancellationTokenSource`、事件订阅、`IDisposable` 资源在 `Dispose` 中可靠释放。
- 后台线程不直接操作 `ObservableCollection`；集合更新通过 `Dispatcher` 回切 UI 线程。
- 所有公共异步入口具备 try-catch + NLog 日志 + 用户提示。

## 2. Current State Analysis

### 2.1 已完成的良好实践

| 文件/区域 | 现状 | 评价 |
|-----------|------|------|
| `ViewModelBase.cs` | 提供 `ExecuteAsync`、`CreateAsyncCommand`、`IsBusy`、`ErrorMessage`、`IDisposable` | 基础框架已建立 |
| `AlarmViewModel` / `DashboardViewModel` / `EnergyReportViewModel` / `HistoryTrendViewModel` / `PointConfigViewModel` | 均继承 `ViewModelBase`，命令化，使用 `ExecuteAsync` | 符合 MVVM 规范 |
| `DataAcquisitionService` / `DataStorageService` / `ModbusTcpCommunicationService` | 实现 `IDisposable`，使用 `IDbContextFactory` | 资源管理基本到位 |
| `App.xaml.cs` | 全局 `DispatcherUnhandledException` / `TaskScheduler.UnobservedTaskException` / `AppDomain.UnhandledException` | 异常兜底已具备 |
| `CoreModule.cs` | 注册 `IDbContextFactory`、`IUnitOfWork`（Transient）、各 Service 为 Singleton | DI 配置基本正确 |

### 2.2 关键缺陷（按优先级）

#### P0 —— 运行时崩溃 / 资源泄漏风险

1. **`AlarmViewModel` 跨线程操作集合（高概率崩溃）**
   - 文件：`HVAC.EnergyMonitor\ViewModels\AlarmViewModel.cs`
   - 问题：`_alarmService.AlarmTriggered` 由 `DataAcquisitionService` 后台线程触发；其回调直接调用 `RefreshAsync()`，内部对 `Alarms`（`ObservableCollection`）执行 `Clear()`/`Add()`。
   - 风险：工业场景下数据刷新频繁时，几乎必然抛出 `NotSupportedException`（"This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread"）。
   - 修复：在 `RefreshAsync` 内使用 `Dispatcher.InvokeAsync` 或改为 ViewModel 层只准备数据，视图层订阅变更。

2. **`MainWindowViewModel` 的 `DispatcherTimer` 无法释放（内存泄漏）**
   - 文件：`HVAC.EnergyMonitor\ViewModels\MainWindowViewModel.cs`
   - 问题：`timer` 为构造函数局部变量，未提升为字段，窗口关闭时无法 `Stop`/`Dispose`。
   - 风险：主窗口通常伴随整个应用生命周期，但严格工业级应用应确保资源可释放；同时 `MainWindowViewModel` 未继承 `ViewModelBase`，无统一错误处理。
   - 修复：改为字段，实现 `IDisposable`，在 `Dispose` 中停止定时器；继承 `ViewModelBase`。

3. **`DeviceConfigViewModel` 完全未接入现有框架**
   - 文件：`HVAC.EnergyMonitor\ViewModels\DeviceConfigViewModel.cs`
   - 问题：
     - 继承 `BindableBase` 而非 `ViewModelBase`，无 `ExecuteAsync`、无错误处理。
     - 直接注入 `AppDbContext`（生命周期不明确，且与 `IDbContextFactory` 模式不一致）。
     - `LoadAsync` 无 try-catch。
     - `DeviceConfigView.xaml` 中新增/删除/保存/刷新按钮均无 `Command` 绑定。
   - 风险：设备配置页面是工业应用核心入口，任何数据库异常都会直接抛到 UI 线程并崩溃。
   - 修复：命令化并继承 `ViewModelBase`，使用 `IUnitOfWork`/`IDbContextFactory`。

#### P1 —— MVVM / 绑定 / 命令不完整

4. **XAML 按钮未绑定命令**
   - `PointConfigView.xaml`：新增/保存/刷新按钮已存在对应 `Command`，但 XAML 未设置 `Command` 属性。
   - `DeviceConfigView.xaml`：新增/删除/保存/刷新按钮均无 `Command` 绑定。
   - `EnergyReportView.xaml` / `HistoryTrendView.xaml`：导出按钮无命令。
   - 风险：用户点击无响应，违背“命令替代 click 事件”原则。

5. **`MainWindowViewModel` 未继承 `ViewModelBase`**
   - 导致无 `IsBusy`、无统一错误处理、无 `IDisposable` 模式。

6. **ViewModel 生命周期释放不一致**
   - `DashboardView.xaml.cs` / `AlarmView.xaml.cs` / `EnergyReportView.xaml.cs` 已在 `Unloaded` 中释放 `DataContext`。
   - `PointConfigView.xaml.cs` / `DeviceConfigView.xaml.cs` / `HistoryTrendView.xaml.cs` 未在 `Unloaded` 中释放；其中 `HistoryTrendView` 使用 `DataContextChanged`，已做部分清理，但 `PointConfigView`/`DeviceConfigView` 需要补齐。

#### P2 —— 工业级健壮性增强

7. **`ViewModelBase` 需要增强**
   - `CreateAsyncCommand` 未检查 `IsDisposed`，命令在 ViewModel 释放后仍可能执行。
   - `ExecuteAsync` 不支持 `CancellationToken`，长查询在页面切换时无法取消。
   - 建议增加 `CancellationTokenSource` 字段，提供 `ExecuteAsync(Func<CancellationToken, Task>, string)` 重载，并在 `Dispose` 中取消。

8. **所有 ViewModel 异步查询缺少 `CancellationToken`**
   - 文件：`EnergyReportViewModel.cs`、`HistoryTrendViewModel.cs`、`PointConfigViewModel.cs` 等。
   - 风险：查询大量历史数据时，切换页面不会取消，浪费资源并可能竞争 DbContext。

9. **`DataAcquisitionService` 未使用设备配置中的 IP/端口**
   - `GetOrCreateCommunicationService` 对 `ModbusTCP` 使用硬编码 `127.0.0.1:502`。
   - 工业场景应调用 `Configure(device.IpAddress, device.Port)`。

10. **输入验证缺失**
    - 文件：所有配置 ViewModel。
    - 风险：用户可输入非法扫描周期、空名称、非法寄存器地址等，导致数据库或通信异常。
    - 建议：为关键实体实现 `IDataErrorInfo` 或 `INotifyDataErrorInfo`。

11. **导出功能为占位符**
    - `AlarmViewModel.ExportCommand` 仅弹出提示。
    - `EnergyReportView` / `HistoryTrendView` 导出按钮无命令绑定。
    - 建议：至少实现 CSV/文本导出骨架，避免“死按钮”。

12. **`DataStorageService.FlushAsync` 同步等待风险**
    - `Dispose` 中 `FlushAsync().GetAwaiter().GetResult()` 在 UI 线程调用可能短暂阻塞。
    - 当前为 Singleton，通常只在应用退出时释放，可接受；但建议加注释说明。

### 2.3 代码审计速览表

| 维度 | 状态 | 说明 |
|------|------|------|
| MVVM 架构 | 部分 | 5/7 ViewModel 已规范，2 个待改 |
| 绑定替代赋值 | 较好 | 无直接 `DataContext =`；部分按钮未绑定 |
| 命令替代 Click | 部分 | 主页面导航已命令化；设备/点位按钮未完全绑定 |
| 依赖注入 | 较好 | Prism Unity 配置合理；DeviceConfig 直接注入 DbContext 需修正 |
| 资源管理 | 部分 | Service 已 IDisposable；MainWindow timer 泄漏；部分 View 未释放 VM |
| UI 不卡死 | 较好 | 数据库查询已异步；Dashboard 用 SemaphoreSlim |
| try-catch | 部分 | 继承 `ViewModelBase` 的 VM 已覆盖；DeviceConfig/MainWindow 未覆盖 |
| 线程安全 | 有风险 | AlarmViewModel 跨线程操作集合 |

## 3. Proposed Changes

### Step 1 —— 修复 `AlarmViewModel` 跨线程集合更新（P0）

**文件：**
- `HVAC.EnergyMonitor\ViewModels\AlarmViewModel.cs`

**修改内容：**
- 在 `RefreshAsync` 内部将集合清空与填充的逻辑放到 `Application.Current.Dispatcher.InvokeAsync` 中执行。
- 或者改为先在线程安全列表中准备数据，再一次性 `InvokeAsync` 替换集合内容（推荐，减少 UI 线程工作量）。
- 保持 `ExecuteAsync` 包装不变。

**验证点：**
- 模拟报警触发后 UI 不抛跨线程异常。

### Step 2 —— 重构 `MainWindowViewModel`（P0/P1）

**文件：**
- `HVAC.EnergyMonitor\ViewModels\MainWindowViewModel.cs`
- `HVAC.EnergyMonitor\Views\MainWindow.xaml.cs`（如需要）

**修改内容：**
- 继承 `ViewModelBase`（需要注入 `IDialogService`）。
- 将 `_timer` 提升为只读字段。
- 在 `Dispose` 中 `Stop()`、`Tick -=`、并置空委托。
- `NavigateCommand` 改用 `CreateAsyncCommand` 或保持 `DelegateCommand<string>`，但由基类统一处理异常。
- 保持导航命令参数绑定不变。

**验证点：**
- 编译通过；窗口关闭无 `DispatcherTimer` 残留。

### Step 3 —— 命令化并升级 `DeviceConfigViewModel`（P0/P1）

**文件：**
- `HVAC.EnergyMonitor\ViewModels\DeviceConfigViewModel.cs`
- `HVAC.EnergyMonitor\Views\DeviceConfigView.xaml`
- `HVAC.EnergyMonitor\Views\DeviceConfigView.xaml.cs`

**修改内容：**
- 继承 `ViewModelBase`。
- 注入 `IUnitOfWork` 或 `IDbContextFactory<AppDbContext>`（推荐 `IUnitOfWork`，与点位配置一致）。
- 添加属性：
  - `SelectedDevice`（`Device?`）
  - `SelectedProtocolType`（用于协议下拉绑定）
- 添加命令：
  - `AddCommand` —— 新增默认设备并选中。
  - `DeleteCommand` —— 删除选中设备并刷新。
  - `SaveCommand` —— 保存变更。
  - `RefreshCommand` —— 重新加载。
- `LoadAsync` 使用 `ExecuteAsync` 包装。
- `DeviceConfigView.xaml`：
  - 为四个按钮设置 `Command`/`CommandParameter`。
  - 协议列改为 `ComboBox` 绑定 `ProtocolTypes`（可选，先保证命令可用）。
- `DeviceConfigView.xaml.cs`：增加 `Unloaded += (s, e) => (DataContext as IDisposable)?.Dispose();`。

**验证点：**
- 新增/删除/保存/刷新设备功能正常；数据库异常被捕获并弹窗提示。

### Step 4 —— 补齐 XAML 命令绑定（P1）

**文件：**
- `HVAC.EnergyMonitor\Views\PointConfigView.xaml`
- `HVAC.EnergyMonitor\Views\DeviceConfigView.xaml`
- `HVAC.EnergyMonitor\Views\EnergyReportView.xaml`
- `HVAC.EnergyMonitor\Views\HistoryTrendView.xaml`
- `HVAC.EnergyMonitor\ViewModels\EnergyReportViewModel.cs`
- `HVAC.EnergyMonitor\ViewModels\HistoryTrendViewModel.cs`

**修改内容：**
- `PointConfigView.xaml`：新增/保存/刷新按钮绑定 `AddCommand`/`SaveCommand`/`RefreshCommand`。
- `DeviceConfigView.xaml`：四个按钮绑定 Step 3 创建的命令。
- `EnergyReportView.xaml` / `HistoryTrendView.xaml`：导出按钮绑定 `ExportCommand`。
- `EnergyReportViewModel` / `HistoryTrendViewModel`：添加 `ExportCommand`，至少实现导出当前 `Reports`/`DataPoints` 到 CSV 文件的功能（使用 `SaveFileDialog` 通过 `IDialogService` 扩展或直接在 ViewModel 调用）。

**验证点：**
- 所有工具栏按钮点击后有响应；导出文件成功生成。

### Step 5 —— 统一 ViewModel 生命周期释放（P1）

**文件：**
- `HVAC.EnergyMonitor\Views\PointConfigView.xaml.cs`
- `HVAC.EnergyMonitor\Views\DeviceConfigView.xaml.cs`
- `HVAC.EnergyMonitor\Views\HistoryTrendView.xaml.cs`（检查是否已完整）

**修改内容：**
- 在 `Unloaded` 或 `DataContextChanged` 中取消事件订阅并调用 `(DataContext as IDisposable)?.Dispose()`。
- 对 `HistoryTrendView` 已订阅 `CollectionChanged` 的，确保旧 VM 取消订阅。

**验证点：**
- 多次切换页面后内存无明显增长；无重复事件触发。

### Step 6 —— 增强 `ViewModelBase`（P2）

**文件：**
- `HVAC.EnergyMonitor\ViewModels\ViewModelBase.cs`

**修改内容：**
- 增加 `CancellationTokenSource _cts` 字段。
- 增加 `ExecuteAsync(Func<CancellationToken, Task> action, string operationName)` 重载。
- 在 `Dispose()` 中调用 `_cts?.Cancel()` 与 `_cts?.Dispose()`。
- `CreateAsyncCommand` 在 `CanExecute` 中增加 `!IsDisposed` 判断（通过 `ObservesProperty(() => IsBusy)` 间接；若 `IsDisposed` 不暴露，可改为命令内部检查）。

**验证点：**
- 页面切换后长时间查询被取消；编译通过。

### Step 7 —— 为各 ViewModel 传递 `CancellationToken`（P2）

**文件：**
- `HVAC.EnergyMonitor\ViewModels\PointConfigViewModel.cs`
- `HVAC.EnergyMonitor\ViewModels\EnergyReportViewModel.cs`
- `HVAC.EnergyMonitor\ViewModels\HistoryTrendViewModel.cs`
- `HVAC.EnergyMonitor\ViewModels\AlarmViewModel.cs`

**修改内容：**
- 将 `ExecuteAsync` 的调用改为使用带 `CancellationToken` 的重载。
- 在 EF Core 查询链上追加 `.ToListAsync(_cts.Token)` 等（若使用 `IUnitOfWork`，需在接口层扩展 token 支持；优先通过 `IDbContextFactory` 直接传 token）。

**验证点：**
- 切换页面时长时间查询可被取消；无 `OperationCanceledException` 弹窗（基类已吞掉）。

### Step 8 —— `DataAcquisitionService` 使用设备真实配置（P2）

**文件：**
- `HVAC.EnergyMonitor\Services\Acquisition\DataAcquisitionService.cs`

**修改内容：**
- 在 `GetOrCreateCommunicationService` 中，对 `ModbusTCP` 调用 `service.Configure(device.IpAddress, device.Port)`。
- 若 `ProtocolType` 为 `ModbusRTU`，使用合适的串口服务（当前可保持 Simulator 兜底，但需加 TODO/日志）。

**验证点：**
- ModbusTCP 设备配置变更后，采集服务使用新 IP/端口尝试连接。

### Step 9 —— 输入验证（P2）

**文件：**
- `HVAC.EnergyMonitor\Models\Entities\Device.cs`
- `HVAC.EnergyMonitor\Models\Entities\Point.cs`
- 或新增 `HVAC.EnergyMonitor\Models\Validation\EntityValidator.cs`

**修改内容：**
- 实现 `IDataErrorInfo`（简单场景）或 `INotifyDataErrorInfo`（复杂场景）。
- 校验规则示例：
  - `Device.Name` 非空且长度 ≤ 100。
  - `Device.ScanIntervalMs` ≥ 100。
  - `Point.RegisterAddress` ≥ 0。
  - `Point.Scale` > 0。
- 在 ViewModel 的 Save 命令前调用验证，失败时提示用户。

**验证点：**
- 输入非法值时保存按钮给出明确错误提示，不写入数据库。

### Step 10 —— 最终集成验证（P0/P1/P2）

**文件：** 整个项目

**验证清单：**
1. **编译验证**
   - `dotnet build HVAC.EnergyMonitor.sln` 或 Visual Studio 生成，确保 Release/Debug 均通过。
2. **静态扫描**
   - 再次全局搜索 `Click +=`、`Click="`（应无结果）。
   - 再次全局搜索 `DataContext =`（应无结果）。
   - 检查所有 ViewModel 继承链：仅 `ViewModelBase` 可继承 `BindableBase`。
   - 检查所有 `Timer`/`DispatcherTimer` 在 `Dispose`/`Unloaded` 中释放。
3. **运行时验证**
   - 启动应用，切换所有导航页面多次，确认无跨线程异常、无内存泄漏。
   - 触发报警（可临时调低限值），确认报警列表刷新正常。
   - 设备/点位新增、保存、删除、刷新功能正常。
   - 历史趋势与能耗报表查询正常。
   - 关闭应用时无未处理异常。
4. **代码审查**
   - 使用 `code-reviewer` 技能对变更文件进行审查。

## 4. Assumptions & Decisions

1. **Prism.Unity 默认生命周期**：`Register<TFrom, TTo>()` 不指定参数时按 Unity 默认行为解析。计划保持 `IUnitOfWork` 为 Transient（每次新建 VM/操作新建 DbContext），`IDbContextFactory` 为 Singleton。
2. **ViewModel 释放责任**：由对应 View 的 `Unloaded`/`DataContextChanged` 调用 `(DataContext as IDisposable)?.Dispose()`。Prism 导航默认不会自动调用 VM 的 `Dispose`。
3. **图表更新保留在代码隐藏**：ScottPlot 的实时刷新属于“视图特定”操作，保留在 `DashboardView.xaml.cs` / `HistoryTrendView.xaml.cs` / `EnergyReportView.xaml.cs` 中，通过订阅 VM 集合/属性变更来驱动，不视为 MVVM 违规。
4. **导出功能范围**：本次先实现 CSV 文本导出（足够工业报表基础需求），不引入 Excel 库。
5. **输入验证范围**：先覆盖 `Device` 与 `Point` 实体的关键字段，后续可扩展到报警规则。
6. **不修改数据库 Schema**：当前 Schema 已满足需求，计划内变更不涉及迁移。
7. **语言与提交**：按用户偏好，代码注释与文档使用中文；完成本地验证即可，不上传 GitHub。

## 5. Verification Steps

### 5.1 编译与静态检查

```powershell
# 在仓库根目录执行
dotnet build d:\study\623WPFstudy\HVAC.EnergyMonitor.sln -c Release
```

预期：0 error，0 warning（或仅第三方库 warning）。

### 5.2 运行时功能验证

| 验证项 | 操作 | 预期结果 |
|--------|------|----------|
| 导航切换 | 连续点击侧边栏所有菜单 5 次以上 | 无跨线程异常，内存稳定 |
| 实时监控 | 观察 Dashboard 数值与曲线 | 数值刷新，曲线滚动 |
| 报警触发 | 临时将某点位高限调低 | 报警列表自动刷新，确认按钮可用 |
| 设备管理 | 新增/修改/删除/刷新设备 | 操作成功，异常被捕获提示 |
| 点位管理 | 新增/保存/刷新点位 | 操作成功，未选设备时给出警告 |
| 历史趋势 | 选择点位、时间范围、点击查询 | 三条曲线正确显示 |
| 能耗报表 | 选择点位、周期、点击查询 | 表格与饼图正确显示，导出 CSV 成功 |
| 应用退出 | 点击窗口关闭 | 无未处理异常，NLog 正常关闭 |

### 5.3 代码审查

使用 `code-reviewer` 技能对以下变更文件执行审查：

- `ViewModels\ViewModelBase.cs`
- `ViewModels\MainWindowViewModel.cs`
- `ViewModels\DeviceConfigViewModel.cs`
- `ViewModels\AlarmViewModel.cs`
- `Views\DeviceConfigView.xaml`
- `Views\DeviceConfigView.xaml.cs`
- `Views\PointConfigView.xaml`
- `Views\EnergyReportView.xaml`
- `Views\HistoryTrendView.xaml`
- `Services\Acquisition\DataAcquisitionService.cs`

### 5.4 成功标准

- 所有 P0 问题修复并验证通过。
- 所有 P1 问题修复并通过静态扫描。
- P2 问题至少完成 `ViewModelBase` 增强、`CancellationToken` 传递、输入验证骨架。
- Release 编译成功，应用可独立运行。
