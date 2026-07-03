# HVAC EnergyMonitor P0 可靠性重构计划

## Context

当前项目是一个 .NET 8 WPF 工业能源监控应用，使用 Prism.Unity 9 + MVVM + SQLite + EF Core 8 + NModbus + ScottPlot.WPF + NLog。经过代码审查，发现项目在以下工业级要求上存在明显缺口：

- 缺少全局异常处理
- ViewModel 异步方法没有 try-catch 保护
- Dashboard 在 UI 线程上直接查询数据库，存在卡顿风险
- `AppDbContext` 与 `UnitOfWork` 注册为 Singleton，存在并发访问隐患
- `DispatcherTimer`、`CancellationTokenSource`、`TcpClient` 等资源未规范释放
- 部分按钮仍使用 `Click` 事件而非 `Command`
- 缺少统一的错误提示机制

本计划聚焦 **P0 —— 让应用不崩溃、不卡死、资源可回收**，不涉及 UI 视觉改动，也不涉及 ScottPlot 与 ViewModel 的深度解耦（P1）。

---

## Goals

1. 添加全局异常处理与 NLog 兜底日志。
2. 建立 `ViewModelBase`，统一提供 `IsBusy`、`ErrorMessage`、安全异步执行。
3. 引入 `IDialogService`，统一错误/提示弹窗，并支持后台线程调用。
4. 修正 `AppDbContext`/`UnitOfWork` 生命周期，避免 Singleton 并发问题。
5. 所有 ViewModel 的异步加载/查询方法使用 try-catch 与 `ExecuteAsync`。
6. 将 Dashboard 的数据库查询移出 UI 线程。
7. 所有工具栏按钮改为 `DelegateCommand`。
8. 规范释放 `DispatcherTimer`、`CancellationTokenSource`、`TcpClient` 等关键资源。

---

## Step-by-Step Plan

### Step 1：全局异常处理（10 分钟）

**文件：**
- `HVAC.EnergyMonitor/App.xaml.cs`

**动作：**
- 订阅 `DispatcherUnhandledException`、`TaskScheduler.UnobservedTaskException`、`AppDomain.CurrentDomain.UnhandledException`。
- 使用 NLog 记录异常；UI 线程异常弹出 MessageBox 并设置 `e.Handled = true`。
- 在 `OnExit` 中停止 `IDataAcquisitionService`，再关闭日志。

**验证：**
- `dotnet build` 通过。
- 临时抛出一个 UI 异常，确认弹窗与日志文件均正常。

---

### Step 2：创建 IDialogService（10 分钟）

**文件：**
- 新建 `HVAC.EnergyMonitor/Services/Dialog/IDialogService.cs`
- 新建 `HVAC.EnergyMonitor/Services/Dialog/DialogService.cs`
- 修改 `HVAC.EnergyMonitor/Modules/CoreModule.cs`

**动作：**
- 接口提供 `ShowError(string)`、`ShowInfo(string)`、`Ask(string) -> bool`。
- 实现通过 `Application.Current.Dispatcher.Invoke` 调用 `MessageBox`，确保后台线程安全。
- 在 CoreModule 中以 Singleton 注册。

**验证：**
- `dotnet build` 通过。

---

### Step 3：创建 ViewModelBase（15 分钟）

**文件：**
- 新建 `HVAC.EnergyMonitor/ViewModels/ViewModelBase.cs`

**动作：**
- 继承 `Prism.Mvvm.BindableBase`，实现 `IDisposable`。
- 提供 `IsBusy`、`ErrorMessage`。
- 提供 `ExecuteAsync(Func<Task>, string)` 与 `ExecuteAsync<T>(Func<Task<T>>, string)`，自动 try-catch、NLog 记录、弹窗提示、设置 `ErrorMessage`。
- 提供 `CreateAsyncCommand` 辅助方法生成安全的 `DelegateCommand`。
- 子类通过构造函数注入 `IDialogService` 与 `ILogger`。

**验证：**
- `dotnet build` 通过。

---

### Step 4：修正 DbContext / UnitOfWork 生命周期（15 分钟）

**文件：**
- 新建 `HVAC.EnergyMonitor/Infrastructure/DbContext/AppDbContextFactory.cs`
- 修改 `HVAC.EnergyMonitor/Infrastructure/Repository/UnitOfWork.cs`
- 修改 `HVAC.EnergyMonitor/Modules/CoreModule.cs`

**动作：**
- 注册 `DbContextOptions<AppDbContext>` 为 Singleton。
- 注册 `IDbContextFactory<AppDbContext>` 为 Singleton，使用 `AppDbContextFactory`。
- `IUnitOfWork` 改为 Transient，每次 resolve 创建新的 `AppDbContext`。
- `CoreModule.OnInitialized` 中使用工厂创建临时 Context 执行 `EnsureCreated` 与 `SeedData`。
- `UnitOfWork` 保持 `IDisposable`，由调用方 `using` 管理。

**验证：**
- `dotnet build` 通过。
- 删除旧 SQLite 文件后运行程序，确认数据库重新创建且种子数据正确。

---

### Step 5：服务层适配新生命周期（20 分钟）

**文件：**
- `HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs`
- `HVAC.EnergyMonitor/Services/Storage/DataStorageService.cs`
- `HVAC.EnergyMonitor/Services/Alarm/AlarmService.cs`
- `HVAC.EnergyMonitor/Services/Report/EnergyReportService.cs`
- `HVAC.EnergyMonitor/Services/Communication/ModbusTcpCommunicationService.cs`

**动作：**
- `DataAcquisitionService` 注入 `IDbContextFactory<AppDbContext>`，每轮循环 `using var context = _dbContextFactory.CreateDbContext()`。
- `DataStorageService` 注入 `IDbContextFactory<AppDbContext>`，每次 Flush 创建新的 `AppDbContext` 与 UoW；`Dispose` 中同步等待最后一次 Flush。
- `AlarmService` 注入 `IDbContextFactory<AppDbContext>`。
- `EnergyReportService` 注入 `IDbContextFactory<AppDbContext>`。
- `ModbusTcpCommunicationService` 实现 `IDisposable`，规范释放 `TcpClient`。
- `DataAcquisitionService` 实现 `IDisposable`，停止任务并释放 `_cts`。

**验证：**
- `dotnet build` 通过。
- 运行程序，确认实时采集、历史存储、报警、报表功能正常。

---

### Step 6：DashboardViewModel 后台刷新与异常处理（20 分钟）

**文件：**
- `HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs`
- `HVAC.EnergyMonitor/Views/DashboardView.xaml.cs`

**动作：**
- 继承 `ViewModelBase`。
- 注入 `IDbContextFactory<AppDbContext>` 替代 `AppDbContext`。
- 将 UI 线程的 `DispatcherTimer` 改为 `System.Threading.Timer`。
- 使用 `SemaphoreSlim` 防止刷新重叠。
- 数据库查询在后台线程完成，通过 `Application.Current.Dispatcher.InvokeAsync` 更新 `ObservableCollection`。
- 实现 `Dispose`，停止定时器、释放 `SemaphoreSlim`、停止采集服务。
- `DashboardView.xaml.cs` 在 `Unloaded` 中调用 `(DataContext as IDisposable)?.Dispose()`。

**验证：**
- `dotnet build` 通过。
- 运行程序，观察实时监控页面刷新流畅，无 UI 卡顿。

---

### Step 7：AlarmViewModel 命令化与资源释放（15 分钟）

**文件：**
- `HVAC.EnergyMonitor/ViewModels/AlarmViewModel.cs`
- `HVAC.EnergyMonitor/Views/AlarmView.xaml`
- `HVAC.EnergyMonitor/Views/AlarmView.xaml.cs`

**动作：**
- 继承 `ViewModelBase`。
- 工具栏按钮绑定 `QueryCommand` 与 `ExportCommand`。
- 确认报警使用 `CreateAsyncCommand<int?>`。
- 刷新与确认操作通过 `ExecuteAsync` 包装。
- 使用命名事件处理器订阅 `AlarmTriggered`，在 `Dispose` 中取消订阅并停止 `DispatcherTimer`。
- `AlarmView.xaml.cs` 在 `Unloaded` 中调用 `Dispose`。

**验证：**
- `dotnet build` 通过。
- 进入报警管理，测试查询与确认按钮；离开页面后无重复刷新。

---

### Step 8：HistoryTrendViewModel 查询命令化（15 分钟）

**文件：**
- `HVAC.EnergyMonitor/ViewModels/HistoryTrendViewModel.cs`
- `HVAC.EnergyMonitor/Views/HistoryTrendView.xaml`
- `HVAC.EnergyMonitor/Views/HistoryTrendView.xaml.cs`

**动作：**
- 继承 `ViewModelBase`。
- 增加 `QueryCommand`，移除 XAML 中的 `Click="QueryButton_Click"`。
- `LoadPointsAsync` 与 `QueryAsync` 通过 `ExecuteAsync` 包装。
- 代码后置改为监听 `HourlyDataPoints`/`DailyDataPoints`/`MonthlyDataPoints` 的 `CollectionChanged` 自动刷新图表。
- 简化/移除 `QueryButton_Click` 方法。

**验证：**
- `dotnet build` 通过。
- 进入历史趋势，选择点位和时间，点击查询，确认三个图表刷新。

---

### Step 9：EnergyReportViewModel 异常处理（10 分钟）

**文件：**
- `HVAC.EnergyMonitor/ViewModels/EnergyReportViewModel.cs`

**动作：**
- 继承 `ViewModelBase`。
- `QueryCommand` 与 `LoadPointsAsync` 通过 `ExecuteAsync` 包装。

**验证：**
- `dotnet build` 通过。
- 进入能耗报表，选择周期和点位，点击查询，确认表格与饼图更新。

---

### Step 10：PointConfigViewModel 命令化（15 分钟）

**文件：**
- `HVAC.EnergyMonitor/ViewModels/PointConfigViewModel.cs`
- `HVAC.EnergyMonitor/Views/PointConfigView.xaml`

**动作：**
- 继承 `ViewModelBase`。
- 注入 `IUnitOfWork`（Transient）。
- 增加 `SelectedPoint`、`AddCommand`、`SaveCommand`、`RefreshCommand`。
- XAML 工具栏按钮绑定命令，DataGrid 绑定 `SelectedItem`。

**验证：**
- `dotnet build` 通过。
- 进入点位管理，测试新增、修改、保存、刷新。

---

### Step 11：DeviceConfigViewModel 命令化（15 分钟）

**文件：**
- `HVAC.EnergyMonitor/ViewModels/DeviceConfigViewModel.cs`
- `HVAC.EnergyMonitor/Views/DeviceConfigView.xaml`

**动作：**
- 继承 `ViewModelBase`。
- 注入 `IUnitOfWork`（Transient）。
- 增加 `SelectedDevice`、`AddCommand`、`DeleteCommand`、`SaveCommand`、`RefreshCommand`。
- XAML 工具栏按钮绑定命令，DataGrid 绑定 `SelectedItem`。

**验证：**
- `dotnet build` 通过。
- 进入设备管理，测试新增、删除、保存、刷新。

---

### Step 12：MainWindowViewModel 释放定时器（5 分钟）

**文件：**
- `HVAC.EnergyMonitor/ViewModels/MainWindowViewModel.cs`

**动作：**
- 实现 `IDisposable`。
- 在 `Dispose` 中停止顶部时间 `DispatcherTimer`。

**验证：**
- `dotnet build` 通过。

---

### Step 13：最终集成验证（15 分钟）

**动作：**
1. `dotnet clean` + `dotnet build` 通过。
2. 运行程序，依次切换所有导航页面。
3. 在历史趋势执行查询，确认图表刷新。
4. 在报警管理执行确认。
5. 在设备/点位管理执行新增、保存、刷新。
6. 检查 `logs/hvac-YYYY-MM-DD.log`，确认无未处理异常。
7. 关闭程序，确认进程正常退出。

---

## Critical Files

- `HVAC.EnergyMonitor/App.xaml.cs`
- `HVAC.EnergyMonitor/Modules/CoreModule.cs`
- `HVAC.EnergyMonitor/ViewModels/ViewModelBase.cs`
- `HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs`
- `HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs`
- `HVAC.EnergyMonitor/Services/Storage/DataStorageService.cs`
- `HVAC.EnergyMonitor/Services/Communication/ModbusTcpCommunicationService.cs`
- `HVAC.EnergyMonitor/Views/HistoryTrendView.xaml`
- `HVAC.EnergyMonitor/Views/AlarmView.xaml`
- `HVAC.EnergyMonitor/Views/DeviceConfigView.xaml`
- `HVAC.EnergyMonitor/Views/PointConfigView.xaml`

---

## Out of Scope (P1 / P2)

- ScottPlot 与 ViewModel 的深度解耦（Behavior/PlotController）。
- 配置中心（appsettings.json + IOptions）。
- 通信服务工厂化（ICommunicationServiceFactory）。
- Modbus 断线重连与真实 Modbus TCP 读写实现。
- 报警恢复逻辑与数据质量可视化。
- 报表导出功能。
