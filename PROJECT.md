# HVAC 能源监控平台 — 项目说明文档

> 版本：v1.0 · 2026-07-04
> 适用源码：`HVAC.EnergyMonitor.sln`（.NET 8 WPF + Prism 9）
> 文档面向：项目维护者、协作者、评审者、新加入工程师

---

## 一、项目概览

### 1.1 项目定位

**HVAC Energy Monitor** 是一款面向**暖通空调（HVAC）系统**的**工业级能源监控桌面平台**。
基于 .NET 8 WPF 构建，采用 **Prism 9 模块化框架**与 **MVVM 设计模式**，提供以下核心能力：

- 工业级监控仪表盘（数字看板 + 实时趋势曲线）
- Modbus TCP / Modbus RTU 工业通信，内置**仿真模式（Simulator）** 无需真实 PLC 即可演示
- 实时数据采集、缓存与多类型寄存器解析
- SQLite + EF Core 8 本地历史数据持久化
- ScottPlot 趋势曲线分析
- 高低限报警与报警记录管理
- 设备、测点、能源报表等配置与分析视图

### 1.2 设计目标

| 目标 | 说明 |
|------|------|
| **工业级 UI** | 深色科技风、丰富监控元素（数字仪表、状态指示灯、实时曲线） |
| **MVVM 纯净** | View 不含业务逻辑，ViewModel 不持有 UI 控件，命令替代 Click 事件 |
| **高稳定性** | 0 编译警告、关键路径全 try-catch、async void 异常保护、无 sync-over-async |
| **可扩展** | Prism 区域导航 + 工厂模式 + 接口抽象，扩展新协议/模块无需修改核心代码 |
| **可发布** | Release 模式一键发布，本地 SQLite，无需任何外部服务依赖 |

---

## 二、技术栈

| 层级 | 选型 | 版本 |
|------|------|------|
| UI 框架 | .NET WPF | .NET 8 |
| MVVM / 模块化 | Prism.Unity | 9.0.537 |
| ORM | Microsoft.EntityFrameworkCore.Sqlite | 8.0.6 |
| 工业通信 | NModbus | 4.0.0-alpha010 |
| 趋势图表 | ScottPlot.WPF | 5.0.35 |
| 日志 | NLog.Extensions.Logging | 5.3.11 |
| 图标 | MahApps.Metro.IconPacks | 5.1.0 |
| 配置 | Microsoft.Extensions.Configuration.Json | 8.0.0 |

> 项目目标框架：`net8.0-windows` · `<UseWPF>true</UseWPF>` · 启用 `<Nullable>enable</Nullable>`

---

## 三、目录结构

```text
623WPFstudy/
├── HVAC.EnergyMonitor.sln              # 解决方案文件
├── README.md                            # GitHub 风格快速入门
├── CHANGELOG.md                         # 完整变更日志
├── hvac_energy_monitor.db               # SQLite 数据库（运行后生成）
├── docs/                                # 设计文档与计划
│   ├── HVAC-项目教学指导.md
│   └── superpowers/
│       ├── plans/                       # 实施计划
│       └── specs/                       # 设计规格
└── HVAC.EnergyMonitor/                  # 主项目
    ├── HVAC.EnergyMonitor.csproj
    ├── App.xaml / App.xaml.cs           # 应用入口
    ├── Bootstrapper.cs                  # Prism 引导（注册服务、读取配置）
    ├── NLog.config                      # 日志配置
    ├── appsettings.json                 # 外部配置（连接串、采集/刷盘间隔）
    │
    ├── Modules/
    │   └── CoreModule.cs                # 核心模块（种子数据 + 初始化容错）
    │
    ├── Views/                           # XAML 视图层（仅 UI，不含业务逻辑）
    │   ├── MainWindow.xaml              # 主窗口与导航
    │   ├── DashboardView.xaml           # 实时监控仪表盘
    │   ├── HistoryTrendView.xaml        # 历史趋势分析
    │   ├── EnergyReportView.xaml        # 能源报表
    │   ├── AlarmView.xaml               # 报警管理
    │   ├── DeviceConfigView.xaml        # 设备配置
    │   └── PointConfigView.xaml         # 测点配置
    │
    ├── ViewModels/                      # MVVM 视图模型
    │   ├── ViewModelBase.cs             # 基类（BindableBase + IRegionMemberLifetime + INavigationAware）
    │   ├── MainWindowViewModel.cs
    │   ├── DashboardViewModel.cs
    │   ├── HistoryTrendViewModel.cs
    │   ├── EnergyReportViewModel.cs
    │   ├── AlarmViewModel.cs
    │   ├── DeviceConfigViewModel.cs
    │   └── PointConfigViewModel.cs
    │
    ├── Services/                        # 业务服务层
    │   ├── Communication/               # Modbus 通信（含仿真）
    │   │   ├── ICommunicationService.cs
    │   │   ├── ICommunicationServiceFactory.cs
    │   │   ├── CommunicationServiceFactory.cs
    │   │   ├── ModbusTcpCommunicationService.cs
    │   │   └── SimulatorCommunicationService.cs
    │   ├── Acquisition/                 # 采集调度
    │   │   ├── IDataAcquisitionService.cs
    │   │   └── DataAcquisitionService.cs
    │   ├── Cache/                       # 实时测点缓存
    │   │   ├── IPointValueCache.cs
    │   │   └── PointValueCache.cs
    │   ├── Storage/                     # 历史数据存储
    │   │   ├── IDataStorageService.cs
    │   │   └── DataStorageService.cs
    │   ├── Alarm/                       # 报警服务
    │   │   ├── IAlarmService.cs
    │   │   └── AlarmService.cs
    │   ├── Report/                      # 能源报表
    │   │   ├── IEnergyReportService.cs
    │   │   └── EnergyReportService.cs
    │   ├── Dialog/                      # 对话框服务
    │   │   ├── IDialogService.cs
    │   │   └── DialogService.cs
    │   └── Common/                      # 通用服务（Dispatcher 抽象）
    │       ├── IDispatcherService.cs
    │       └── DispatcherService.cs
    │
    ├── Infrastructure/                  # 基础设施层
    │   ├── DbContext/
    │   │   ├── AppDbContext.cs
    │   │   └── AppDbContextFactory.cs   # DbContext 工厂
    │   ├── Repository/
    │   │   ├── IUnitOfWork.cs
    │   │   └── UnitOfWork.cs            # 注入 IDbContextFactory
    │   └── Helpers/                     # 工具类
    │
    ├── Models/                          # 模型层
    │   ├── Entities/                    # 数据库实体
    │   ├── DTOs/                        # 数据传输对象
    │   ├── Events/                      # Prism 事件聚合
    │   └── Enums/                       # 枚举定义
    │
    ├── Constants/                       # 常量
    │   └── NavigationKeys.cs            # NavigationKeys + RegionNames
    │
    ├── Converters/                      # XAML 值转换器
    ├── Design/                          # 工业级 UI 样式资源
    │   └── Styles.xaml
    └── Infrastructure/Helpers/          # 通用助手
```

---

## 四、核心架构

### 4.1 分层架构图

```text
┌──────────────────────────────────────────────────┐
│           WPF Views（XAML + 极简 code-behind）   │  ← UI 层
├──────────────────────────────────────────────────┤
│       ViewModels（MVVM / Prism 导航）            │  ← 表现层
├──────────────────────────────────────────────────┤
│  Services: Communication / Acquisition / Cache   │  ← 业务服务层
│            Storage / Alarm / Report / Dialog     │
├──────────────────────────────────────────────────┤
│  Infrastructure: DbContext / Repository / UoW    │  ← 基础设施层
├──────────────────────────────────────────────────┤
│  Models: Entities / DTOs / Events / Enums        │  ← 模型层
└──────────────────────────────────────────────────┘
```

### 4.2 数据流

```text
  [Simulator / Modbus 设备]
            │ 寄存器原始值
            ▼
  ICommunicationService ──► IDataAcquisitionService（按扫描周期）
            │                     │
            │                     ├─► IPointValueCache（实时缓存）
            │                     └─► IAlarmService（高低限判定）
            │                               │
            ▼                               ▼
  IDataStorageService（按刷盘间隔）   UI 通过事件聚合 / DispatcherService
            │                     更新
            ▼
       SQLite (hvac_energy_monitor.db)
            │
            ▼
  IEnergyReportService / HistoryTrendViewModel（查询）
            │
            ▼
       ScottPlot 趋势曲线 + 报表 UI
```

### 4.3 关键设计模式

| 模式 | 应用 |
|------|------|
| **MVVM** | View ↔ ViewModel ↔ Model 严格分离，绑定优先于赋值 |
| **Prism 模块化** | `Bootstrapper` + `CoreModule`，Region 导航 |
| **工厂模式** | `ICommunicationServiceFactory` 创建 Simulator / ModbusTCP 实例 |
| **仓储 + 工作单元** | `IUnitOfWork` 聚合 `IDbContextFactory<AppDbContext>` |
| **事件聚合** | Prism `IEventAggregator` 解耦采集 → 报警 → UI |
| **依赖注入** | Unity 容器（Prism 内置），服务按 Singleton / Transient 注册 |
| **抽象 UI 线程** | `IDispatcherService` 隔离 `Application.Current.Dispatcher` |

---

## 五、模块说明

| 模块 / 视图 | 主要职责 | 关键文件 |
|-------------|---------|----------|
| **MainWindow** | 主窗口壳，承载 Region 导航 | [MainWindow.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/MainWindow.xaml) · [MainWindowViewModel.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/MainWindowViewModel.cs) |
| **Dashboard** | 实时数字看板 + ScottPlot 实时趋势 | [DashboardView.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/DashboardView.xaml) · [DashboardViewModel.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs) |
| **HistoryTrend** | 历史趋势查询（ScottPlot） | [HistoryTrendView.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/HistoryTrendView.xaml) · [HistoryTrendViewModel.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/HistoryTrendViewModel.cs) |
| **EnergyReport** | 能源报表（日/周/月聚合） | [EnergyReportView.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/EnergyReportView.xaml) · [EnergyReportViewModel.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/EnergyReportViewModel.cs) |
| **Alarm** | 报警列表、确认、清除 | [AlarmView.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/AlarmView.xaml) · [AlarmViewModel.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/AlarmViewModel.cs) |
| **DeviceConfig** | 设备参数、协议切换 | [DeviceConfigView.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/DeviceConfigView.xaml) · [DeviceConfigViewModel.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/DeviceConfigViewModel.cs) |
| **PointConfig** | 测点地址、量程、单位、报警阈值 | [PointConfigView.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/PointConfigView.xaml) · [PointConfigViewModel.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/PointConfigViewModel.cs) |

### 5.1 核心服务

| 服务 | 接口 | 实现 | 作用 |
|------|------|------|------|
| 通信 | [ICommunicationService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Communication/ICommunicationService.cs) | `SimulatorCommunicationService` / `ModbusTcpCommunicationService` | 读写 Modbus 寄存器 / 仿真生成数据 |
| 通信工厂 | [ICommunicationServiceFactory](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Communication/ICommunicationServiceFactory.cs) | [CommunicationServiceFactory](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Communication/CommunicationServiceFactory.cs) | 按协议类型创建对应通信服务 |
| 采集 | [IDataAcquisitionService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/IDataAcquisitionService.cs) | [DataAcquisitionService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs) | 按周期调度采集，更新缓存并触发存储/报警 |
| 缓存 | [IPointValueCache](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Cache/IPointValueCache.cs) | [PointValueCache](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Cache/PointValueCache.cs) | 线程安全的实时测点值 |
| 存储 | [IDataStorageService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Storage/IDataStorageService.cs) | [DataStorageService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Storage/DataStorageService.cs) | 批量写入 SQLite，实现 `IAsyncDisposable` |
| 报警 | [IAlarmService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Alarm/IAlarmService.cs) | [AlarmService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Alarm/AlarmService.cs) | 高低限判定 + `ConcurrentDictionary` 活跃集合 |
| 报表 | [IEnergyReportService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Report/IEnergyReportService.cs) | [EnergyReportService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Report/EnergyReportService.cs) | 能源聚合查询 |
| 对话框 | `HVAC.EnergyMonitor.Services.Dialog.IDialogService` | [DialogService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Dialog/DialogService.cs) | 模态对话框（与 Prism 自带 IDialogService 区分） |
| Dispatcher | [IDispatcherService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Common/IDispatcherService.cs) | [DispatcherService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Common/DispatcherService.cs) | 抽象 UI 线程访问，便于测试 |

---

## 六、关键设计决策

### 6.1 ViewModel 生命周期管理

- `ViewModelBase` 实现 `IRegionMemberLifetime`（`KeepAlive => false`）和 `INavigationAware`
- 离开 Region 时由 Prism 自动调用 `OnNavigatedFrom` → `Dispose`
- **View 不再 `Dispose` ViewModel**（已移除 6 个 View 的 `OnUnloaded` 反向调用）

### 6.2 DbContext 生命周期

- `AppDbContext` 与 `UnitOfWork` **不注册为 Singleton**（避免并发风险）
- `UnitOfWork` 构造函数注入 `IDbContextFactory<AppDbContext>`，自管 context 生命周期
- 4 个 ViewModel 重写 `Dispose` 释放 `_unitOfWork`

### 6.3 异步安全

- 所有后台 Service 的 `await` 添加 `ConfigureAwait(false)`（30 处）
- `IAsyncDisposable` 替代 `Dispose` 中的 sync-over-async
- `DataAcquisitionService.Dispose` 限 2 秒、`DataStorageService.Dispose` 限 3 秒
- `async void` 事件处理器（`AlarmViewModel.OnRefreshTimerTick`、`App.OnExit`）均包裹 `try-catch` + 日志

### 6.4 配置外置

- 数据库连接串、采集间隔（`ScanIntervalMs`）、刷盘间隔（`FlushIntervalSec`）均放在 [appsettings.json](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/appsettings.json)
- `Bootstrapper` 用 `ConfigurationBuilder` 读取并注册 `IConfiguration`

### 6.5 Magic String 治理

- 导航键、区域名集中在 [Constants/NavigationKeys.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Constants/NavigationKeys.cs)：
  - `NavigationKeys.{Dashboard, DeviceConfig, PointConfig, HistoryTrend, Alarm, EnergyReport}`
  - `RegionNames.MainRegion`

### 6.6 模块加载时序修复（关键 Bug）

- Prism 9.0.537 的 `PrismBootstrapperBase.Initialize()` 顺序为 `RegisterTypes → CreateShell → InitializeModules`
- 旧版 `CoreModule.RegisterTypes` 在 Shell 创建后执行，导致 `MainWindowViewModel` 解析 `IDialogService` 失败
- 修复：将全部服务注册迁移到 `Bootstrapper.RegisterTypes`，`CoreModule` 只保留种子数据与初始化容错

---

## 七、运行与构建

### 7.1 环境要求

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 / JetBrains Rider / VS Code（任选）

### 7.2 调试运行

```powershell
# 解决方案根目录
dotnet build HVAC.EnergyMonitor.sln
dotnet run --project HVAC.EnergyMonitor/HVAC.EnergyMonitor.csproj
```

或用 Visual Studio 打开 `HVAC.EnergyMonitor.sln`，将 `HVAC.EnergyMonitor` 设为启动项目，按 `F5` 运行。

> 首次启动自动创建 `hvac_energy_monitor.db`，并初始化一台仿真冷水机组与 4 个默认测点。

### 7.3 发布 Release

```powershell
dotnet publish HVAC.EnergyMonitor/HVAC.EnergyMonitor.csproj -c Release -o ./publish
```

输出位于 `./publish/HVAC.EnergyMonitor.exe`，含 `hvac_energy_monitor.db`、`NLog.config`、`appsettings.json`。

### 7.4 默认仿真测点

| 测点名称 | 寄存器地址 | 数据类型 | 单位 | 量程缩放 |
|----------|------------|----------|------|----------|
| 冷冻水供水温度 | 0 | UShort | °C | 0.1 |
| 冷冻水回水温度 | 1 | UShort | °C | 0.1 |
| 冷机功率 | 2 | UShort | kW | 1 |
| 冷却塔风机频率 | 3 | UShort | Hz | 0.1 |

---

## 八、外部配置（appsettings.json）

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=hvac_energy_monitor.db"
  },
  "AppSettings": {
    "ScanIntervalMs": 1000,
    "FlushIntervalSec": 5
  }
}
```

| 键 | 含义 | 默认值 |
|----|------|--------|
| `ConnectionStrings:DefaultConnection` | SQLite 数据库文件路径 | `hvac_energy_monitor.db` |
| `AppSettings:ScanIntervalMs` | 数据采集周期（毫秒） | `1000` |
| `AppSettings:FlushIntervalSec` | 内存缓冲刷盘到 SQLite 周期（秒） | `5` |

---

## 九、扩展指南

### 9.1 新增一个 Modbus 协议变体

1. 实现 [ICommunicationService](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Communication/ICommunicationService.cs)
2. 在 [CommunicationServiceFactory](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Communication/CommunicationServiceFactory.cs) 注册新 `case`
3. 在 `Bootstrapper.RegisterTypes` 注册新实现

### 9.2 新增一个导航页面

1. 在 [Constants/NavigationKeys.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Constants/NavigationKeys.cs) 增加常量
2. 创建 `Views/<Name>View.xaml` + `<Name>View.xaml.cs`（最小 code-behind）
3. 创建 `ViewModels/<Name>ViewModel.cs`（继承 [ViewModelBase](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/ViewModelBase.cs)）
4. 在 `Bootstrapper.RegisterTypes` 注册 View / ViewModel
5. 在 `MainWindowViewModel` 增加导航命令

### 9.3 新增一个数据库实体

1. 在 `Models/Entities/` 增加实体类
2. 在 [AppDbContext](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/DbContext/AppDbContext.cs) 增加 `DbSet<>` 与映射
3. 必要时扩展 `IUnitOfWork` 与 `UnitOfWork`

---

## 十、代码质量基线

| 维度 | 基线要求 |
|------|---------|
| 编译 | `dotnet build` 0 错误 0 警告 |
| MVVM | View code-behind 无业务逻辑、无 ViewModel.Dispose 调用 |
| 异步 | 后台 `await` 必须 `ConfigureAwait(false)`，禁用 `GetAwaiter().GetResult()` |
| 资源 | `IDisposable` / `IAsyncDisposable` 必须显式释放，DI 容器不注册 Singleton `DbContext` |
| 异常 | `async void` 必须 try-catch + 日志，silent catch 必须有日志 |
| 线程安全 | 共享可变状态使用 `ConcurrentDictionary` / `SemaphoreSlim` |
| 配置 | 连接串、扫描周期等放 `appsettings.json`，不硬编码 |
| 字符串 | 导航键、区域名用 `Constants`，禁止 magic string |

---

## 十一、版本与变更

完整变更历史见 [CHANGELOG.md](file:///d:/study/623WPFstudy/CHANGELOG.md)。

最近一次大规模优化（2026-07-03）按 **P0 关键修复 → P1 架构改进 → P2 质量打磨** 实施 14 项改进 + 3 项关键 Bug 修复，涵盖稳定性、可维护性、代码质量三大维度。

---

## 十二、后续可扩展方向

- [ ] 接入真实 Modbus RTU 串口（增加 `ModbusRtuCommunicationService`）
- [ ] 增加 OPC UA / BACnet 协议驱动
- [ ] 多语言国际化（i18n）
- [ ] 能源报表导出（Excel / PDF）
- [ ] 用户权限与登录模块
- [ ] 云端数据上传接口（MQTT / HTTP）
- [ ] 单元测试与集成测试覆盖

---

> 本文档随项目演进持续更新；如发现与代码不一致，以代码为准并提交 PR 修改本文档。
