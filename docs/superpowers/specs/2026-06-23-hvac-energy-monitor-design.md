# HVAC 能源监控平台 — 设计规格书

## 1. 项目目标

为初学者设计并开发一套完整的 **HVAC（暖通空调）能源监控平台**桌面应用。项目重点体现以下技术能力：

- WPF 桌面客户端开发
- MVVM 设计模式
- Prism 框架（模块化、依赖注入、Region 导航、EventAggregator）
- Modbus 工业通信（TCP / RTU / 模拟器）
- SQLite + Entity Framework Core 数据持久化
- 趋势曲线（历史数据可视化）
- 异步编程、日志、断线重连等工业软件常见能力

项目遵循 `command.md` 中强调的**服务化架构思想**，拒绝“读取数据 → 显示数据”的学生项目模式，体现：

- 数据采集服务
- 通信服务
- 缓存服务
- 数据库服务
- 报警服务
- 可视化服务

---

## 2. 方案选择

### 2.1 候选方案

| 方案 | 描述 | 优点 | 缺点 | 推荐度 |
|---|---|---|---|---|
| 方案1：一体化 WPF 应用 | 采集、存储、展示全部运行在一个 WPF 进程内，后台使用托管服务做轮询 | 适合学习、部署简单、调试方便、能完整覆盖所有技术点 | 不适合超大规模产线部署 | ⭐⭐⭐⭐⭐ |
| 方案2：WPF + 独立 Windows Service | 数据采集拆分为独立 Windows 服务，WPF 仅负责展示 | 贴近真实工业架构、可独立扩展采集端 | 对初学者复杂、跨进程通信和部署成本高 | ⭐⭐⭐ |

### 2.2 最终方案

**采用方案1：一体化 WPF 应用**。

理由：

- 学习者需要在一个项目中完整理解 WPF、MVVM、Prism、Modbus、数据库、趋势曲线的协同工作。
- 通过接口抽象（`ICommunicationService`、`IDataAcquisitionService` 等），未来可平滑拆分为独立服务或接入真实 PLC。
- 便于单步调试和快速迭代。

---

## 3. 技术栈

| 层级 | 技术选型 | 版本 |
|---|---|---|
| 运行时 | .NET | 8.0 |
| UI 框架 | WPF | .NET 8 WPF |
| MVVM / DI / 模块化 | Prism | 9.x |
| ORM | Entity Framework Core | 8.x |
| 数据库 | SQLite | 通过 `Microsoft.EntityFrameworkCore.Sqlite` |
| Modbus 库 | NModbus4 / NModbus | 稳定版 |
| 趋势曲线 | ScottPlot.WPF | 最新稳定版 |
| 日志 | NLog.Extensions.Logging | 稳定版 |
| UI 风格 | 自定义工业风控件 + XAML 样式 | - |

---

## 4. 整体架构

### 4.1 分层架构图

```text
┌─────────────────────────────────────────┐
│           WPF Dashboard                 │
│  (Views + ViewModels + Prism Modules)   │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│         Business Logic Layer            │
│  AlarmService / EnergyReportService     │
│  PointValueService / DeviceService      │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│         Data Storage Layer              │
│  EF Core + SQLite + Repository/UoW      │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│         Message Queue / Cache           │
│  System.Threading.Channels + MemoryCache│
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│         Data Acquisition Layer          │
│  DataCollector + ICommunicationService  │
│  ModbusTcpService / SimulatorService    │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│              PLC / Simulator            │
│         Modbus TCP / Modbus RTU         │
└─────────────────────────────────────────┘
```

### 4.2 与 command.md 服务化思想的映射

| command.md 描述 | 本项目的对应实现 |
|---|---|
| 数据采集服务 | `DataAcquisitionService` |
| 通信服务 | `ICommunicationService`、`ModbusTcpCommunicationService`、`SimulatorCommunicationService` |
| 缓存服务 | `PointValueCache` |
| 数据库服务 | `AppDbContext` + `IRepository<T>` + `UnitOfWork` |
| 报警服务 | `AlarmService` |
| 可视化服务 | WPF Views + ViewModels + Prism Region 导航 |

---

## 5. 数据流

```text
Simulator / 真实 PLC
        ↓
ICommunicationService.ReadAsync()
        ↓
RawValue (ushort / float)
        ↓
DataAcquisitionService 解析为工程值
        ↓
PointValueCache（内存最新值）
        ↓
├─→ Dashboard 实时刷新（UI 绑定）
├─→ AlarmService 判断是否超限
└─→ DataStorageService 批量写入 SQLite
```

### 5.1 关键时序

1. 应用启动时，`AppDbContext` 自动迁移/初始化数据库。
2. `DataAcquisitionService` 作为托管后台服务启动，读取 `Devices` 和 `Points` 配置。
3. 按设备扫描周期，调用 `ICommunicationService.ReadAsync()` 读取原始值。
4. 原始值经系数、偏移量转换为工程值。
5. 工程值写入 `PointValueCache`，并通过 `EventAggregator` 发布 `PointValueUpdatedEvent`。
6. UI 订阅事件并刷新显示。
7. `DataStorageService` 按批量策略（如每 5 秒或每 100 条）写入 `PointValues` 表。
8. `AlarmService` 对比工程值与报警规则，触发报警记录。

---

## 6. 核心服务设计

### 6.1 通信服务

```csharp
public interface ICommunicationService
{
    string Name { get; }
    bool IsConnected { get; }
    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<ushort[]> ReadHoldingRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken ct = default);
    Task<ushort[]> ReadInputRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken ct = default);
}
```

实现类：

- `ModbusTcpCommunicationService`：真实 Modbus TCP。
- `ModbusRtuCommunicationService`：真实 Modbus RTU（通过 SerialPort）。
- `SimulatorCommunicationService`：模拟 PLC，生成正弦波、随机数或自定义曲线数据。

### 6.2 采集服务

```csharp
public interface IDataAcquisitionService
{
    bool IsRunning { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
```

职责：

- 加载设备和点位配置。
- 按扫描周期调度通信服务读取数据。
- 将原始值转换为工程值。
- 更新缓存、触发事件、提交存储。
- 处理断线重连。

### 6.3 缓存服务

```csharp
public interface IPointValueCache
{
    void SetValue(int pointId, double value, DateTime timestamp);
    PointValue? GetValue(int pointId);
    IReadOnlyDictionary<int, PointValue> GetAllValues();
}
```

使用 `ConcurrentDictionary` 保证线程安全，供 UI 实时读取。

### 6.4 数据库服务

- `AppDbContext`：EF Core 上下文。
- `IRepository<T>`：通用仓储接口。
- `IUnitOfWork`：工作单元，统一提交。
- `IDataStorageService`：面向采集的批量写入服务，内部使用仓储。

### 6.5 报警服务

```csharp
public interface IAlarmService
{
    event EventHandler<AlarmEventArgs>? AlarmTriggered;
    Task CheckAsync(PointValue value, CancellationToken ct = default);
    Task<IEnumerable<AlarmRecord>> GetActiveAlarmsAsync();
    Task AcknowledgeAsync(int alarmRecordId);
}
```

支持：

- 高限报警
- 低限报警
- 变化率报警（可选）

### 6.6 能耗报表服务

```csharp
public interface IEnergyReportService
{
    Task<IEnumerable<EnergyReport>> GetHourlyReportAsync(DateTime start, DateTime end, int pointId);
    Task<IEnumerable<EnergyReport>> GetDailyReportAsync(DateTime start, DateTime end, int pointId);
    Task<IEnumerable<EnergyReport>> GetMonthlyReportAsync(DateTime start, DateTime end, int pointId);
}
```

按时间维度聚合点位积分值，生成能耗统计。

---

## 7. 功能模块

### 7.1 设备管理

- 增删改查 PLC 设备。
- 配置字段：名称、协议类型（Simulator / ModbusTCP / ModbusRTU）、IP 地址、端口、串口名、波特率、扫描周期（ms）、从机地址、启用状态。
- 默认至少预置一台 Simulator 设备，方便零基础运行。

### 7.2 点位管理

- 为每个设备配置采集点位。
- 字段：点位名、所属设备、功能码（03 / 04）、寄存器地址、数据类型（ushort / short / float / int）、字节序、系数、偏移量、单位、高限、低限、是否存储历史、存储周期、启用状态。
- 支持 CSV 导入/导出（加分项）。
- 预置 HVAC 常用点位：冷冻水供水温度、回水温度、流量、冷机功率、COP、冷却塔风机频率等。

### 7.3 实时监控

- 设备在线状态指示灯（绿/红）。
- 点位实时数值表格，显示：点位名、当前值、单位、最后更新时间、质量位。
- KPI 卡片区域：总功率、系统 COP、供回水温差、当日累计能耗。
- 使用 `DispatcherTimer` 或事件驱动刷新，避免频繁 UI 重绘。

### 7.4 历史趋势

- 选择单个或多个点位。
- 选择时间范围（最近 1 小时 / 24 小时 / 7 天 / 自定义）。

- 使用 ScottPlot 绘制：
  - 折线图
  - 游标查看具体数值
  - 缩放和平移
- 支持导出当前查询数据为 CSV。

### 7.5 报警管理

- 报警规则配置（基于点位）。
- 实时报警列表：报警时间、点位、报警类型、当前值、限值、确认状态。
- 报警触发时可选声音提示和视觉闪烁。
- 历史报警查询与确认。

### 7.6 能耗报表

- 选择能耗点位（如功率、冷量）。
- 选择统计周期：小时 / 日 / 月。
- ScottPlot 柱状图或折线图展示。
- 表格展示具体数值。
- 支持导出 CSV。

---

## 8. 数据库设计

### 8.1 表结构

#### Devices

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | int PK | 自增 |
| Name | string | 设备名称 |
| ProtocolType | int | Simulator=0, ModbusTCP=1, ModbusRTU=2 |
| IpAddress | string | TCP 时使用 |
| Port | int | TCP 端口 |
| SerialPortName | string | RTU 时使用 |
| BaudRate | int | RTU 波特率 |
| SlaveAddress | byte | 从机地址 |
| ScanIntervalMs | int | 扫描周期 |
| IsEnabled | bool | 是否启用 |

#### Points

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | int PK | 自增 |
| DeviceId | int FK | 所属设备 |
| Name | string | 点位名称 |
| FunctionCode | int | 03 或 04 |
| RegisterAddress | int | 寄存器地址 |
| DataType | int | 数据类型枚举 |
| ByteOrder | int | 字节序 |
| Scale | double | 系数 |
| Offset | double | 偏移量 |
| Unit | string | 单位 |
| HighLimit | double? | 高限 |
| LowLimit | double? | 低限 |
| StoreHistory | bool | 是否存历史 |
| IsEnabled | bool | 是否启用 |

#### PointValues

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | long PK | 自增 |
| PointId | int FK | 点位 |
| Value | double | 工程值 |
| Timestamp | DateTime | 时间戳 |
| Quality | int | 质量位 |

#### AlarmRules

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | int PK | 自增 |
| PointId | int FK | 关联点位 |
| HighLimit | double? | 高限 |
| LowLimit | double? | 低限 |
| IsEnabled | bool | 是否启用 |

#### AlarmRecords

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | int PK | 自增 |
| PointId | int FK | 点位 |
| AlarmType | int | High / Low |
| TriggerValue | double | 触发值 |
| LimitValue | double | 限值 |
| TriggerTime | DateTime | 触发时间 |
| Acknowledged | bool | 是否确认 |
| AckTime | DateTime? | 确认时间 |

#### EnergyReports

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | int PK | 自增 |
| PointId | int FK | 点位 |
| PeriodStart | DateTime | 周期开始 |
| PeriodEnd | DateTime | 周期结束 |
| PeriodType | int | Hour / Day / Month |
| TotalValue | double | 累计值 |
| Unit | string | 单位 |

### 8.2 EF Core 迁移

- 使用 Code First 迁移。
- 初始化时自动执行 `EnsureCreated` 或最新迁移，便于初学者直接运行。

---

## 9. 项目结构

```text
HVAC.EnergyMonitor/
├── App.xaml
├── App.xaml.cs
├── Bootstrapper.cs              // Prism 启动器
├── Views/
│   ├── MainWindow.xaml
│   ├── DashboardView.xaml
│   ├── DeviceConfigView.xaml
│   ├── PointConfigView.xaml
│   ├── HistoryTrendView.xaml
│   ├── AlarmView.xaml
│   └── EnergyReportView.xaml
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── DeviceConfigViewModel.cs
│   ├── PointConfigViewModel.cs
│   ├── HistoryTrendViewModel.cs
│   ├── AlarmViewModel.cs
│   └── EnergyReportViewModel.cs
├── Modules/
│   ├── CoreModule/
│   │   ├── CoreModule.cs
│   │   └── ViewModels/Services 注册
│   ├── DashboardModule/
│   ├── DeviceModule/
│   ├── TrendModule/
│   ├── AlarmModule/
│   └── ReportModule/
├── Models/
│   ├── Entities/                // EF 实体
│   ├── Enums/
│   ├── DTOs/
│   └── Events/                  // Prism EventAggregator 事件
├── Services/
│   ├── Communication/
│   │   ├── ICommunicationService.cs
│   │   ├── ModbusTcpCommunicationService.cs
│   │   ├── ModbusRtuCommunicationService.cs
│   │   └── SimulatorCommunicationService.cs
│   ├── Acquisition/
│   │   ├── IDataAcquisitionService.cs
│   │   └── DataAcquisitionService.cs
│   ├── Cache/
│   │   ├── IPointValueCache.cs
│   │   └── PointValueCache.cs
│   ├── Storage/
│   │   ├── IDataStorageService.cs
│   │   └── DataStorageService.cs
│   ├── Alarm/
│   │   ├── IAlarmService.cs
│   │   └── AlarmService.cs
│   └── Report/
│       ├── IEnergyReportService.cs
│       └── EnergyReportService.cs
├── Infrastructure/
│   ├── DbContext/
│   │   └── AppDbContext.cs
│   ├── Repository/
│   │   ├── IRepository.cs
│   │   ├── Repository.cs
│   │   ├── IUnitOfWork.cs
│   │   └── UnitOfWork.cs
│   └── Helpers/
│       ├── ModbusValueConverter.cs
│       └── ByteOrderConverter.cs
├── Design/
│   ├── Styles.xaml
│   ├── Templates.xaml
│   └── Converters/
└── Tests/
    └── （可选单元测试项目）
```

---

## 10. 关键技术点覆盖

| 知识点 | 在本项目中的体现 |
|---|---|
| WPF | 自定义控件、数据绑定、样式/模板、资源字典 |
| MVVM | ViewModel 不引用 View、ICommand、INotifyPropertyChanged、数据绑定 |
| Prism | 模块化（IModule）、Region 导航、IContainerRegistry 依赖注入、EventAggregator 事件总线、DialogService |
| Modbus | NModbus 读写 Holding/Input 寄存器、从机地址、功能码、字节序处理 |
| 数据库 | EF Core + SQLite、Repository 模式、UnitOfWork、Code First 迁移、批量写入 |
| 趋势曲线 | ScottPlot.WPF 实时曲线、历史曲线、缩放游标 |
| 异步编程 | async/await、后台采集、CancellationToken、UI 线程安全（Dispatcher） |
| 日志 | NLog 记录采集日志、报警日志、异常日志 |
| 断线重连 | ICommunicationService 内部维护连接状态，异常后自动重试 |
| 工业架构思想 | 服务拆分、接口抽象、缓存解耦、数据分层 |

---

## 11. 扩展性预留

- `ICommunicationService` 接口预留，未来接入真实 PLC 只需新增实现类并在配置中选择。
- `IDataAcquisitionService` 未来可迁移为独立 `BackgroundService` 或 Windows Service。
- SQLite 可替换为 SQL Server / PostgreSQL，只需修改连接字符串和 EF Provider。
- 消息队列当前使用 `System.Threading.Channels`，未来可替换为 RabbitMQ / MQTT。
- 报警规则未来可扩展为报警升级、通知推送、短信/邮件。

---

## 12. 验收标准

- [ ] 应用能直接运行，无需真实 PLC，Simulator 能生成可观测的数据。
- [ ] 设备、点位可配置，配置持久化到 SQLite。
- [ ] 实时仪表盘能显示点位数值和设备状态。
- [ ] 历史趋势能按时间范围查询并绘制 ScottPlot 曲线。
- [ ] 报警规则生效，超限能产生报警记录并支持确认。
- [ ] 能耗报表能按小时/日/月统计并展示。
- [ ] 代码体现 WPF + MVVM + Prism 的规范结构。
- [ ] 关键服务有接口抽象，便于单元测试和替换实现。

---

## 13. 风险与注意事项

1. **UI 卡顿**：采集频率过高时，若直接绑定大量点位到 UI，可能导致卡顿。解决方案：使用 `PointValueCache` + 定时刷新或事件驱动批量刷新。
2. **数据库写入性能**：高频采集时，直接逐条写入 SQLite 会成为瓶颈。解决方案：`DataStorageService` 批量写入 + 内存缓冲。
3. **内存泄漏**：WPF 绑定和事件订阅需注意解除订阅，避免 ViewModel 无法释放。
4. **线程安全**：缓存和采集服务需使用线程安全集合（如 `ConcurrentDictionary`）。

---

## 14. 后续演进路线

1. **阶段1（当前）**：完成一体化 WPF 应用，掌握基础架构。
2. **阶段2**：将采集服务拆分为独立 Windows Service，WPF 通过 gRPC / MQTT / WebAPI 通信。
3. **阶段3**：接入真实 PLC，验证 Modbus TCP/RTU 通信。
4. **阶段4**：引入机器学习能耗预测（项目2）和故障诊断（项目3）。
