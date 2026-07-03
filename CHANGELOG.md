# 更新日志（Changelog）

本项目所有重要变更均会记录于此文件。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [0.2.0] - 2026-07-03

本轮提交围绕"工业级 C# 软件开发核心思想"对 HVAC 能源监控平台进行全量优化，
按优先级分为 **P0 关键修复**（稳定性）、**P1 架构改进**（可维护性）、**P2 质量打磨**（代码质量）三大阶段，
共 14 项改进 + 3 项实施过程中发现的关键 Bug 修复。

### 🚨 关键 Bug 修复（实施过程发现）

- **修复 Prism 9 模块加载时机导致的 MainWindowViewModel 解析失败**
  - 现象：启动弹窗 "An unexpected error occurred while resolving 'MainWindowViewModel'"
  - 根因：Prism 9.0.537 的 `PrismBootstrapperBase.Initialize()` 执行顺序为 `RegisterTypes → CreateShell → InitializeModules`，`CoreModule.RegisterTypes` 在 Shell 创建之后才执行，但 Shell 创建时就需要解析 `MainWindowViewModel`（依赖 `IDialogService`），此时服务尚未注册
  - 修复：将 `CoreModule.RegisterTypes` 中的全部服务注册迁移至 `Bootstrapper.RegisterTypes`
- **修复 `IDialogService` 命名冲突**
  - 现象：编译错误 "IDialogService 是 HVAC.EnergyMonitor.Services.Dialog.IDialogService 和 Prism.Dialogs.IDialogService 之间的不明确的引用"
  - 修复：在 `Bootstrapper.cs` 使用别名 `using DialogServiceInterface = HVAC.EnergyMonitor.Services.Dialog.IDialogService;`
- **`App.OnExit` 改为 `async void` 并加诊断日志**，支持异步资源清理

### P0 - 关键修复（稳定性）

#### P0-1：移除 View 反向 Dispose ViewModel 反模式
- **问题**：6 个 View 的 `OnUnloaded` 直接调用 `ViewModel.Dispose()`，让 View 反向控制 ViewModel 生命周期，违反 MVVM 原则
- **修复**：
  - 移除 `AlarmView`、`PointConfigView`、`DeviceConfigView`、`EnergyReportView`、`HistoryTrendView`、`DashboardView` 共 6 个 View 的 `OnUnloaded` 中的 `Dispose` 调用
  - `ViewModelBase` 实现 `IRegionMemberLifetime`（`KeepAlive => false`）与 `INavigationAware`（`OnNavigatedFrom` 调用 `Dispose`）
  - 由 Prism Region 自动管理 ViewModel 生命周期

#### P0-2：修复 sync-over-async 死锁风险
- **问题**：`DataAcquisitionService` 和 `DataStorageService` 的同步 `Dispose()` 调用异步方法并无限等待，可能引发死锁
- **修复**：
  - 两个 Service 实现 `IAsyncDisposable`
  - 同步 `Dispose` 路径改为有限超时等待（`DataAcquisitionService` 2 秒、`DataStorageService` 3 秒），超时则记录告警并放弃未刷盘数据，避免无限阻塞

#### P0-3：CoreModule.OnInitialized 加 try-catch 降级
- **问题**：模块初始化异常会导致整个应用崩溃
- **修复**：`CoreModule.OnInitialized` 包裹 `try-catch`，异常时记录日志但不向上抛出

### P1 - 架构改进（可维护性）

#### P1-A：AlarmService 线程安全
- **问题**：`HashSet<string> _activeAlarms` 在多线程访问下不安全（采集线程触发告警、UI 线程确认告警）
- **修复**：改为 `ConcurrentDictionary<string, byte>`，`Contains` → `ContainsKey`、`Add` → `TryAdd`、`Remove` → `TryRemove`

#### P1-B：async void 异常保护
- **问题**：`AlarmViewModel.OnRefreshTimerTick` 是 `async void`，未捕获异常会直接崩溃应用
- **修复**：包裹 `try-catch`，异常通过 `Logger.Error` 记录

#### P1-C：引入 ICommunicationServiceFactory 替代硬编码 new
- **问题**：`DataAcquisitionService` 用 `switch + new` 创建通信服务，违反开闭原则，扩展新协议需修改服务类
- **修复**：
  - 新建 `ICommunicationServiceFactory` 接口与 `CommunicationServiceFactory` 实现
  - `DataAcquisitionService` 构造函数注入工厂
  - `GetOrCreateCommunicationService` 用工厂替代 `switch + new`

#### P1-D：ScottPlot 图表逻辑封装评估
- **问题**：考虑是否引入 `IPlotAdapter` 抽象 ScottPlot 控件以解耦 ViewModel 与 UI 控件
- **决策**：**不引入** — `ScottPlot.WPF` 是 UI 控件，按 WPF 规约必须留在 View 中；图表数据已通过 `ObservableCollection<HistoryDataPoint>` 在 ViewModel 中解耦，进一步抽象属于过度设计

#### P1-E：UnitOfWork 资源泄漏
- **问题**：`UnitOfWork` 持有 `AppDbContext` 但未实现 `IDisposable`；4 个 ViewModel 创建 `UnitOfWork` 后未释放，DbContext 长期堆积
- **修复**：
  - `IUnitOfWork` 继承 `IDisposable`
  - `UnitOfWork` 构造函数改为注入 `IDbContextFactory<AppDbContext>`，自管 context 生命周期
  - `DeviceConfigViewModel`、`PointConfigViewModel`、`EnergyReportViewModel`、`HistoryTrendViewModel` 重写 `Dispose` 释放 `_unitOfWork`

### P2 - 质量打磨（代码质量）

#### P2-1：silent catch 加日志
- **问题**：`DataAcquisitionService` 有 4 处空 `catch` 块，吞掉异常无法诊断
- **修复**：4 处补 `Logger.Warn` / `Logger.Error`，含上下文信息

#### P2-2：配置外置到 appsettings.json
- **问题**：数据库连接字符串硬编码在代码中，部署环境切换困难
- **修复**：
  - 新建 `appsettings.json`（含 `ConnectionStrings` 与 `AppSettings.ScanIntervalMs/FlushIntervalSec`）
  - `Bootstrapper` 用 `ConfigurationBuilder` 读取并注册 `IConfiguration`
  - `.csproj` 添加 `Microsoft.Extensions.Configuration.Json 8.0.0` 包与 `CopyToOutputDirectory` 配置

#### P2-3：Magic string 替换为常量
- **问题**：导航键、区域名以字符串字面量散落在代码中，重构易漏
- **修复**：
  - 新建 `Constants/NavigationKeys.cs`（含 `NavigationKeys` 与 `RegionNames` 静态类）
  - `MainWindowViewModel` 引用常量替代字面量

#### P2-4：IDispatcherService 抽象 Application.Current.Dispatcher
- **问题**：ViewModel 直接调用 `Application.Current.Dispatcher.InvokeAsync`，难以单元测试，且耦合静态状态
- **修复**：
  - 新建 `IDispatcherService` 接口与 `DispatcherService` 实现
  - `DashboardViewModel`、`AlarmViewModel`、`EnergyReportViewModel` 注入 `IDispatcherService` 替代直接调用
  - 在 `Bootstrapper` 注册为 Singleton

#### P2-5：后台 Service 加 ConfigureAwait(false)
- **问题**：后台 Service 的 `await` 不必要地捕获同步上下文，造成线程切换开销
- **修复**：5 个 Service 文件共 30 处 `await` 全部添加 `ConfigureAwait(false)`
  - `DataAcquisitionService`：12 处
  - `AlarmService`：10 处
  - `DataStorageService`：3 处
  - `EnergyReportService`：4 处
  - `ModbusTcpCommunicationService`：1 处
  - **注**：ViewModel 中的 `await` 不加（需要返回 UI 上下文）

### 新增文件

- `HVAC.EnergyMonitor/appsettings.json` — 应用配置（连接串、采集/刷盘间隔）
- `HVAC.EnergyMonitor/Constants/NavigationKeys.cs` — 导航键与区域名常量
- `HVAC.EnergyMonitor/Services/Common/IDispatcherService.cs` — Dispatcher 抽象接口
- `HVAC.EnergyMonitor/Services/Common/DispatcherService.cs` — Dispatcher 实现
- `HVAC.EnergyMonitor/Services/Communication/ICommunicationServiceFactory.cs` — 通信服务工厂接口
- `HVAC.EnergyMonitor/Services/Communication/CommunicationServiceFactory.cs` — 通信服务工厂实现
- `HVAC.EnergyMonitor/Infrastructure/DbContext/AppDbContextFactory.cs` — DbContext 工厂

### 变更文件（主要）

- `HVAC.EnergyMonitor/Bootstrapper.cs` — 接管服务注册、读取配置、注册工厂与 Dispatcher
- `HVAC.EnergyMonitor/Modules/CoreModule.cs` — 移除服务注册（迁至 Bootstrapper）、`OnInitialized` 加 try-catch
- `HVAC.EnergyMonitor/App.xaml.cs` — `OnExit` 改 `async void` + 诊断日志
- `HVAC.EnergyMonitor/ViewModels/ViewModelBase.cs` — 实现 `IRegionMemberLifetime` + `INavigationAware`
- `HVAC.EnergyMonitor/ViewModels/MainWindowViewModel.cs` — 引用 `NavigationKeys`/`RegionNames` 常量
- `HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs` — 注入 `IDispatcherService`
- `HVAC.EnergyMonitor/ViewModels/AlarmViewModel.cs` — `async void` 加 try-catch、注入 `IDispatcherService`
- `HVAC.EnergyMonitor/ViewModels/EnergyReportViewModel.cs` — 注入 `IDispatcherService`、重写 `Dispose`
- `HVAC.EnergyMonitor/ViewModels/DeviceConfigViewModel.cs` — 重写 `Dispose` 释放 `_unitOfWork`
- `HVAC.EnergyMonitor/ViewModels/PointConfigViewModel.cs` — 重写 `Dispose` 释放 `_unitOfWork`
- `HVAC.EnergyMonitor/ViewModels/HistoryTrendViewModel.cs` — 重写 `Dispose` 释放 `_unitOfWork`
- `HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs` — `IAsyncDisposable`、工厂注入、silent catch 加日志、12 处 `ConfigureAwait(false)`
- `HVAC.EnergyMonitor/Services/Alarm/AlarmService.cs` — `ConcurrentDictionary`、10 处 `ConfigureAwait(false)`
- `HVAC.EnergyMonitor/Services/Storage/DataStorageService.cs` — `IAsyncDisposable`、3 处 `ConfigureAwait(false)`
- `HVAC.EnergyMonitor/Services/Report/EnergyReportService.cs` — 4 处 `ConfigureAwait(false)`
- `HVAC.EnergyMonitor/Services/Communication/ModbusTcpCommunicationService.cs` — 1 处 `ConfigureAwait(false)`
- `HVAC.EnergyMonitor/Infrastructure/Repository/IUnitOfWork.cs` — 继承 `IDisposable`
- `HVAC.EnergyMonitor/Infrastructure/Repository/UnitOfWork.cs` — 改用 `IDbContextFactory<AppDbContext>`
- 6 个 View（`*.xaml.cs`）— 移除 `OnUnloaded` 中的 `Dispose` 调用
- `HVAC.EnergyMonitor/HVAC.EnergyMonitor.csproj` — 添加 `Microsoft.Extensions.Configuration.Json` 包引用

### 验证

- `dotnet build` 编译通过：0 错误、0 警告
- 应用启动正常：主窗口正常显示，日志输出 "Bootstrapper.Run() 完成，主窗口应已显示"
- 资源释放验证：`IAsyncDisposable` 路径在 `App.OnExit` 异步清理

---

## 历史版本

此前版本未维护 CHANGELOG，可参考 Git 提交历史：

- `feat: configure NLog for application logging`
- `feat: add main window with Prism region navigation`
- `feat: register all core services and seed default simulator device`
- `feat: add energy report service`
- `feat: add alarm service with high/low limit checking`
