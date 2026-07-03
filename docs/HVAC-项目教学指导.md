# HVAC 能源监控平台 —— 秋招「上位机软件工程师」项目教学指导

> **目标读者**：将该项目作为秋招核心项目经历、希望对每行代码、每个技术选型都能在面试中"讲清楚、问不倒"的求职者。
> **阅读建议**：按章节顺序读，每章末尾都有"面试应答模板"和"动手任务"。

---

## 0. 阅读地图（先看这一节）

整个文档共 12 章，分 4 大块：

| 块 | 章节 | 主题 | 解决面试问题 |
|---|---|---|---|
| **第一块·项目全景** | 第 1~2 章 | 业务背景、整体架构、技术栈 | "请介绍一下你的项目" |
| **第二块·语言与设计** | 第 3~5 章 | C#、OOP、MVVM + Prism | "面向对象怎么用的？MVVM 怎么实现的？" |
| **第三块·工程实践** | 第 6~10 章 | WPF、异步、数据库、Modbus、采集/存储/报警全链路 | "多线程、数据库、通信协议、缓存" |
| **第四块·面试实战** | 第 11~12 章 | 高频问答、复盘清单、8 周学习路线 | "现在请你手写…请描述…" |

每章末尾固定包含：
- **「🔑 核心要点」**：3~5 条必须背下来的关键点。
- **「🎤 面试应答模板」**：可以直接背的高质量回答。
- **「🛠 动手任务」**：1~2 个小练习，建议用 Rider/VS 实际跑一遍。

---

## 第 1 章　项目全景

### 1.1 这是什么项目

**HVAC 能源监控平台**（HVAC.EnergyMonitor）是一套面向暖通空调（HVAC, Heating Ventilation and Air Conditioning）系统的工业级上位机软件。HVAC 是楼宇/工厂里负责制冷、制热、通风的机电系统，本平台实时监控其中的**冷冻水温度、冷机功率、冷却塔风机频率**等关键运行参数。

它属于"上位机"软件，对应工业自动化金字塔中的"监控层"：

```
┌────────────────────────────────────────┐
│  企业管理层  ERP / MES（与你无关）        │
├────────────────────────────────────────┤
│  监控层  SCADA / HMI  ← 你做的就是这个    │
├────────────────────────────────────────┤
│  控制层  PLC / DCS（厂商提供）           │
├────────────────────────────────────────┤
│  设备层  传感器 / 变频器 / 接触器         │
└────────────────────────────────────────┘
```

### 1.2 项目能做什么（功能模块）

| 模块 | 业务功能 | 对应 View / ViewModel |
|---|---|---|
| 实时监控 | KPI 卡片、实时趋势、点位列表 | `DashboardView` / `DashboardViewModel` |
| 设备管理 | 增删改查 PLC/网关设备 | `DeviceConfigView` / `DeviceConfigViewModel` |
| 点位管理 | 维护测点（地址、量程、字节序、告警阈值） | `PointConfigView` / `PointConfigViewModel` |
| 历史趋势 | 按时间窗查询历史曲线（ScottPlot） | `HistoryTrendView` / `HistoryTrendViewModel` |
| 报警管理 | 高/低限报警、确认/清除 | `AlarmView` / `AlarmViewModel` |
| 能耗报表 | 时/日/月报表聚合 | `EnergyReportView` / `EnergyReportViewModel` |

### 1.3 技术栈全景

| 类别 | 选型 | 在项目中的角色 |
|---|---|---|
| 运行时 | .NET 8 (`net8.0-windows`) | 跨平台版本已统一、长期支持、性能提升 |
| UI 框架 | WPF (`UseWPF=true`) | 工业软件主流，支持 XAML 数据绑定、样式模板 |
| MVVM 框架 | Prism 9.0.537 (`Prism.Unity`) | 模块化、依赖注入、区域导航、事件聚合 |
| IoC 容器 | Unity（Prism 默认） | 解耦服务、便于测试替换 |
| 图表 | ScottPlot.WPF 5.0.35 | 实时+历史曲线（替代 LiveCharts/OxyPlot） |
| 通信 | NModbus 4.0.0-alpha010 | 工业 ModbusTCP/RTU 协议栈 |
| ORM | EF Core 8.0.6 + SQLite | 嵌入式数据库，单文件部署，免装服务 |
| 图标 | MahApps.Metro.IconPacks 5.1.0 | Material 风格矢量图标 |
| 日志 | NLog.Extensions.Logging 5.3.11 | 文件+控制台双输出 |

> 一句话总结项目：**用 .NET 8 + WPF + Prism 做的工业 SCADA 监控软件，通过 NModbus 跟 PLC/仿真器通信，EF Core+SQLite 存历史，ScottPlot 画曲线**。

### 1.4 解决方案结构

```
HVAC.EnergyMonitor.sln
└── HVAC.EnergyMonitor/                 （单项目，模块化分文件夹）
    ├── App.xaml(.cs)                   入口 + NLog 初始化
    ├── Bootstrapper.cs                 Prism 启动器
    ├── Modules/CoreModule.cs           Prism IModule：DI 注册
    ├── Converters/                     XAML 值转换器（4 个）
    ├── Design/Styles.xaml              全局样式资源
    ├── Infrastructure/
    │   ├── DbContext/AppDbContext.cs   EF Core DbContext
    │   ├── Helpers/ByteOrderConverter.cs  字节序工具
    │   └── Repository/                 仓储 + 工作单元模式
    ├── Models/
    │   ├── Entities/                   5 个实体类
    │   ├── Enums/                      5 个枚举
    │   ├── DTOs/                       数据传输对象
    │   └── Events/                     Prism 事件聚合器载荷
    ├── Services/
    │   ├── Communication/              ModbusTCP + Simulator（接口+实现）
    │   ├── Acquisition/                采集调度器（核心）
    │   ├── Cache/                      实时数据缓存
    │   ├── Storage/                    批量入库（ConcurrentQueue + Timer）
    │   ├── Alarm/                      限值告警判定
    │   └── Report/                     时/日/月聚合
    ├── ViewModels/                     7 个 ViewModel
    └── Views/                          7 个 XAML 视图
```

> **🔑 核心要点**
> 1. 项目是单 csproj + 多文件夹分层，没有用多 csproj（项目内模块化）。
> 2. 分层是"约定"，不是"强制"——理解比记结构更重要。
> 3. 面试前要能画出来"实时数据从传感器到 UI"的全链路。

> **🎤 面试应答模板**
> "我做了一个 HVAC 能源监控上位机，用 .NET 8 + WPF + Prism 9 搭建。整体分四层：表现层用 WPF 数据绑定 + ScottPlot 画曲线；服务层有采集、缓存、存储、报警、报表五个服务；基础设施层用 EF Core + SQLite 做持久化，仓储+工作单元封装；通信层用 NModbus 实现 ModbusTCP，支持仿真器模式便于调试。"

> **🛠 动手任务**
> 1. 打开 [HVAC.EnergyMonitor.sln](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor.sln) 编译运行，观察首页实时数据跳变。
> 2. 用 `dotnet build -c Release` 跑一遍生成可执行文件。

---

## 第 2 章　业务领域背景（面试必问的"行业理解"）

### 2.1 上位机/下位机/PLC 是什么

| 概念 | 角色 | 类比 |
|---|---|---|
| **下位机** | PLC、单片机、DCS 控制器 | 工人的"手脚" |
| **上位机** | PC 端监控软件（你做的） | 工人的"眼睛 + 大脑" |
| **通信协议** | Modbus、OPC UA、Profinet | 神经 |

**HVAC 系统里的典型被监控对象**：

- **冷机（Chiller）**：制冷核心。监控功率、负载率、COP（能效比）。
- **冷冻水泵 / 冷却水泵**：循环水动力。监控频率、流量、压力。
- **冷却塔（Cooling Tower）**：散热。监控风机频率、进出水温度。
- **阀门 / 蝶阀**：调节流量。监控开度。

本项目里出现的 4 个示例点位（见 [CoreModule.cs#L62-L68](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Modules/CoreModule.cs#L62-L68)）：

| 点位名 | 物理意义 | 单位 | 告警阈值 |
|---|---|---|---|
| 冷冻水供水温度 | 冷机出水温度 | °C | 5~12 |
| 冷冻水回水温度 | 冷机进水温度 | °C | 7~15 |
| 冷机功率 | 当前耗电 | kW | 0~500 |
| 冷却塔风机频率 | 风机变频器频率 | Hz | 0~50 |

### 2.2 工业软件 vs 普通业务软件

| 维度 | 普通业务软件 | 工业 SCADA |
|---|---|---|
| 数据来源 | 用户输入、HTTP 接口 | 现场设备、PLC |
| 实时性 | 秒级即可 | **毫秒~秒级** |
| 稳定性 | 崩了重启就行 | 7×24 长周期运行，崩溃可能影响生产 |
| 数据量 | 中等 | 每秒上千点 × N 天 |
| 界面 | 美观即可 | 强调信息密度、告警醒目 |
| 协议 | HTTP/gRPC | **Modbus、OPC UA、Profinet** |

> **🔑 核心要点**
> 1. "上位机" = PC 端 SCADA/监控软件。
> 2. 工业领域核心协议是 **Modbus**（最广泛）+ **OPC UA**（新一代统一架构）。
> 3. 工业场景强调"实时性 + 稳定性 + 长周期"。

> **🎤 面试应答模板**
> "我对工业 SCADA 比较熟悉。Modbus 是 1979 年 Modicon 发布的串行协议，特点是简单开放、几乎所有 PLC 都支持；缺点是没有安全机制、没有数据类型描述。OPC UA 是新一代标准，支持加密、复杂数据类型、信息建模，但实现重。考虑到实际生产里 PLC 多用 Modbus，所以我项目里选 NModbus 做通信，并预留 OPC UA 扩展点。"

---

## 第 3 章　C# 基础强化（项目用到的关键语法）

### 3.1 项目 C# 版本与语言特性

`.csproj` 里 `<TargetFramework>net8.0-windows</TargetFramework>`，配合 `<Nullable>enable</Nullable>` 和 `<ImplicitUsings>enable</ImplicitUsings>`，意味着：

- **Nullable Reference Types**：所有引用类型默认不可空（`string` ≠ `string?`），编译期防止空引用。
- **隐式 using**：`System`、`System.Linq`、`System.Threading.Tasks` 等自动引入。
- **C# 12 语法**：文件级命名空间、主构造函数、集合表达式、原始字符串字面量都可用。

### 3.2 项目里高频出现的语法点（带代码示例）

#### 3.2.1 文件级命名空间 + 主构造函数（C# 12）

[DataAcquisitionService.cs#L1-L19](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L1-L19) 风格：

```csharp
namespace HVAC.EnergyMonitor.Services.Acquisition;   // ← 不带花括号

public class DataAcquisitionService : IDataAcquisitionService
{
    private readonly AppDbContext _context;          // ← 字段
    private readonly IPointValueCache _cache;
    // ...
}
```

#### 3.2.2 依赖注入：构造函数注入

[DataAcquisitionService.cs#L33-L43](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L33-L43)：

```csharp
public DataAcquisitionService(
    AppDbContext context,
    IPointValueCache cache,
    IDataStorageService storage,
    IEventAggregator eventAggregator)
{
    _context = context;
    _cache = cache;
    _storage = storage;
    _eventAggregator = eventAggregator;
}
```

**面试要会讲的 4 个点**：
1. **为什么用接口注入？**——解耦：测试时可以传 mock 进去。
2. **生命周期？**——`RegisterSingleton`，单例，因为这些是无状态服务（除了缓存/仓储）。
3. **构造函数注入 vs 属性注入？**——构造注入保证对象创建即就绪，属性注入适合可选依赖。
4. **DI 容器的本质？**——就是一个字典 `Dictionary<Type, Func<object>>`，加反射/表达式树生成对象。

#### 3.2.3 异步方法：Task、async、await、CancellationToken

整个项目几乎所有 I/O 都用 `Task` + `async/await`，例如 [DataAcquisitionService.cs#L45-L53](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L45-L53)：

```csharp
public async Task StartAsync(CancellationToken ct = default)
{
    if (IsRunning) return;

    _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    IsRunning = true;
    _runningTask = RunAsync(_cts.Token);
    await Task.CompletedTask;
}
```

**深入讲解 4 个机制**：

| 机制 | 作用 | 项目例子 |
|---|---|---|
| `async/await` | 把回调写法变成同步写法 | 通信读取、数据库查询 |
| `CancellationToken` | 协作式取消 | 服务停止时通过 `ct` 通知 |
| `Task.Run` | 把同步任务丢到线程池 | 数据处理 |
| `Task.WhenAll` | 并行等待多个任务 | 多设备并行采集 |

#### 3.2.4 `record` / `init` / `required`

项目里 [PointValueCacheItem](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Cache/PointValueCache.cs) 用 `record`（值类型语义，自动实现值相等）：

```csharp
public record PointValueCacheItem(int PointId, double Value, DateTime Timestamp, Quality Quality);
```

`record` 自动生成：`Equals`、`GetHashCode`、`ToString`、`Deconstruct`、`with` 表达式。

#### 3.2.5 `switch` 表达式（C# 8+）

[DataAcquisitionService.cs#L164-L169](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L164-L169)：

```csharp
private static int GetRegisterCount(DataType dataType) => dataType switch
{
    DataType.UShort or DataType.Short => 1,
    DataType.UInt or DataType.Int or DataType.Float => 2,
    _ => 1
};
```

#### 3.2.6 `Span<T>` + `stackalloc`（零分配）

[ByteOrderConverter.cs#L11-L33](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Helpers/ByteOrderConverter.cs#L11-L33)：

```csharp
Span<byte> bytes = stackalloc byte[4];   // ← 在栈上分配 4 字节，零堆分配
BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(0, 2), high);
BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(2, 2), low);
return BitConverter.ToSingle(bytes);
```

**面试亮点**：工业软件高频调用，零分配对吞吐和 GC 压力至关重要。

#### 3.2.7 `Dictionary<Type, object>` 实现工作单元的缓存

[UnitOfWork.cs#L18-L22](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Repository/UnitOfWork.cs#L18-L22)：

```csharp
public IRepository<T> Repository<T>() where T : class
{
    var type = typeof(T);
    return (IRepository<T>)_repositories.GetOrAdd(type, _ => new Repository<T>(_context));
}
```

`ConcurrentDictionary.GetOrAdd` 原子操作 + Lambda 只在缺失时执行（注意：Lambda 可能被多次调用，但结果只放入一次）。

#### 3.2.8 `IReadOnlyDictionary` / `IEnumerable` 返回接口

[PointValueCache.cs#L22-L25](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Cache/PointValueCache.cs#L22-L25)：

```csharp
public IReadOnlyDictionary<int, PointValueCacheItem> GetAllValues()
{
    return _values.ToDictionary(kv => kv.Key, kv => kv.Value);
}
```

返回接口而非具体类，**封装内部实现**，调用方不能改原集合。

### 3.3 集合类型选型

| 场景 | 项目中选 | 为什么 |
|---|---|---|
| 设备-服务映射 | `Dictionary<int, ICommunicationService>` | 高频查、插入少、非并发 |
| 实时值缓存 | `ConcurrentDictionary<int, ...>` | 采集线程和 UI 线程并发读/写 |
| 入库缓冲队列 | `ConcurrentQueue<PointValue>` | 多生产者-单消费者（采集线程入，Timer 出） |
| 报警去重 | `HashSet<string>` | 单线程检查 + Add，效率高 |

> **🔑 核心要点**
> 1. .NET 8 + Nullable + ImplicitUsings 是项目语言基础。
> 2. `async/await` + `CancellationToken` 是项目异步的核心范式。
> 3. `Span<T>` + `stackalloc` 是性能亮点（字节序解析）。
> 4. 集合类型必须选对：`ConcurrentDictionary` / `ConcurrentQueue` / `Dictionary` / `HashSet` 各有适用场景。

> **🎤 面试应答模板**
> "项目用 .NET 8，开启 Nullable 和 ImplicitUsings。异步全部用 Task + async/await，所有异步方法签名都接 CancellationToken 用于协作式取消。性能敏感的字节序解析用 Span + stackalloc 零分配。线程安全的集合用 ConcurrentDictionary 做缓存、ConcurrentQueue 做入库缓冲。"

> **🛠 动手任务**
> 1. 把 [ByteOrderConverter.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Helpers/ByteOrderConverter.cs) 抄写一遍，理解 4 种字节序的差别。
> 2. 故意在 `DataAcquisitionService.StopAsync` 注释掉 `ct` 传参，看 `RunAsync` 会不会正常退出（答：会卡 1 秒才退出，因为 `Task.Delay(1000, ct)` 的取消异常被吃掉）。

---

## 第 4 章　面向对象设计在项目中的体现

> 工业项目最容易被面试官考察的就是 OOP 功底。本章按"四大原则 + 三大模式"展开。

### 4.1 四大原则

#### 4.1.1 单一职责（SRP）

**含义**：一个类只做一件事。

| 类 | 职责 | 违反 SRP 会出现什么 |
|---|---|---|
| `SimulatorCommunicationService` | 产生模拟数据 | 如果再写 UI 逻辑就坏了 |
| `DataAcquisitionService` | 调度采集 | 如果再写 SQL 就坏了 |
| `DataStorageService` | 入库 | 如果再读 Modbus 就坏了 |
| `AlarmService` | 告警判定 | 如果再画界面就坏了 |
| `EnergyReportService` | 报表聚合 | 如果再采集就坏了 |

#### 4.1.2 开闭原则（OCP）

**含义**：对扩展开放，对修改关闭。

最经典案例是 [DataAcquisitionService.cs#L148-L162](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L148-L162)：

```csharp
private ICommunicationService GetOrCreateCommunicationService(Device device)
{
    if (_communicationServices.TryGetValue(device.Id, out var existing))
        return existing;

    ICommunicationService service = device.ProtocolType switch
    {
        ProtocolType.Simulator   => new SimulatorCommunicationService(),
        ProtocolType.ModbusTCP   => new ModbusTcpCommunicationService(),
        _                        => new SimulatorCommunicationService()
    };
    _communicationServices[device.Id] = service;
    return service;
}
```

**未来要加 OPC UA 怎么办？**——只要实现 `ICommunicationService` 接口 + 新增枚举 + 在 switch 里加一行，**采集服务完全不用改**。这就是 OCP。

#### 4.1.3 里氏替换（LSP）

**含义**：子类必须能替换父类，且行为不变。

`SimulatorCommunicationService` 和 `ModbusTcpCommunicationService` 都实现 `ICommunicationService`——`DataAcquisitionService` 通过接口调用，不关心具体实现。

#### 4.1.4 依赖倒置（DIP）

**含义**：依赖抽象（接口），不依赖具体。

`DataAcquisitionService` 的 4 个构造参数全是接口或抽象类：

```csharp
public DataAcquisitionService(
    AppDbContext context,                  // ← 注意：这里实际不是接口！
    IPointValueCache cache,                // ← 接口
    IDataStorageService storage,           // ← 接口
    IEventAggregator eventAggregator)      // ← Prism 接口
```

**问题**：`AppDbContext` 直接传了 EF Core 类，没用接口包裹。**为什么可以？**——EF Core 自己的 `DbContext` 已经是抽象（`DbContext` 基类），且其内部就用了大量接口（`IDbSet`、`IQueryable`）。但严格来说，如果要更纯，可以加一层 `IDbContext` 抽象。这是常见妥协。

#### 4.1.5 接口隔离（ISP）

`ICommunicationService` 只暴露 5 个方法：
- `Name`, `IsConnected`（属性）
- `ConnectAsync`, `DisconnectAsync`, `ReadHoldingRegistersAsync`, `ReadInputRegistersAsync`

没有把"写寄存器""读线圈"等放到接口里——**只暴露"采集"需要的最小面**。

### 4.2 三大模式

#### 4.2.1 策略模式（Strategy）

`ICommunicationService` 三个实现 = 三个策略。运行时根据 `Device.ProtocolType` 选哪一个。

#### 4.2.2 仓储 + 工作单元（Repository + Unit of Work）

[Infrastructure/Repository/](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Repository/)：

```
IRepository<T>  ── 泛型 CRUD
   ↑
Repository<T>   ── EF Core 实现
   ↑                使用 DbSet<T>
IUnitOfWork     ── 多仓储协调 + SaveChanges
   ↑
UnitOfWork      ── 缓存已创建的 Repository 实例
```

**为什么用？**
- 把"业务代码"和"EF Core API"隔开，业务代码不直接写 LINQ-to-Entities。
- 一个事务里改多个实体时，只调一次 `SaveChangesAsync()`。

**面试要会讲**：`Repository` 模式有争议（有人认为 EF Core 本身就是 UoW + Repository，再包一层是过度设计）。你可以**正反都说**：
- ✅ 优点：易测试、换 ORM 容易。
- ❌ 缺点：增加复杂度、对 EF Core 自带 UoW 重复造轮子。

#### 4.2.3 观察者模式（Observer）—— Prism EventAggregator

[Models/Events/PointValueUpdatedEvent.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Models/Events/PointValueUpdatedEvent.cs)：

```csharp
public class PointValueUpdatedEvent : PubSubEvent<int> { }
```

`DataAcquisitionService` 采集后 `Publish(point.Id)`；`DashboardViewModel` `Subscribe` 后刷新。

**为什么不用事件（event）？**
- 跨模块解耦：发布者不知道谁订阅，订阅者不知道谁发布。
- Prism 的 `EventAggregator` 是"全局事件总线"，避免 WPF `event` 的强引用问题。

### 4.3 几个反模式（也是面试亮点）

#### 4.3.1 ❌ 静态类满天飞

项目里只有一个 `ByteOrderConverter` 是 static（纯函数），其他都用实例。**好实践**。

#### 4.3.2 ❌ God Service

如果一个 Service 干了"采集+入库+报警+UI"四件事，就叫 God Service。本项目拆分得很干净。

#### 4.3.3 ❌ 在 ViewModel 里直接 new Service

[DashboardViewModel.cs#L31-L43](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs#L31-L43)：

```csharp
public DashboardViewModel(IPointValueCache cache, IDataAcquisitionService acquisition, ...)
```

通过构造注入，没有 `new`——**可测试**。

> **🔑 核心要点**
> 1. 采集器用 `ICommunicationService` 接口解耦协议——OCP + DIP 经典。
> 2. 仓储+工作单元把 EF Core 包装，业务层看不到 DbContext。
> 3. Prism EventAggregator 实现跨模块解耦的观察者。
> 4. Service 拆分遵守 SRP，每个 Service 只做一件事。

> **🎤 面试应答模板**
> "我项目里通信层有 ICommunicationService 接口，现在有 Simulator 和 ModbusTCP 两个实现。采集服务通过工厂方法按设备协议选具体实现，这就是策略模式。仓储和工作单元我用了 EF Core 包了一层，隔离 ORM；事件聚合器是 Prism 的全局事件总线，比 WPF 事件更解耦。所有 Service 都用接口注入，方便单测。"

> **🛠 动手任务**
> 1. 新增一个 `OpcUaCommunicationService` 实现 `ICommunicationService`，并修改 `GetOrCreateCommunicationService` 让它能工作（**不修改** `DataAcquisitionService` 主体）。
> 2. 把 `AlarmService` 重构为接口 `IAlarmService`（项目里已经是），阅读 [AlarmService.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Alarm/AlarmService.cs) 体会 DIP。

---

## 第 5 章　MVVM + Prism 框架深度解析

### 5.1 MVVM 三件套

| 角色 | 项目体现 | 职责 |
|---|---|---|
| **Model** | `Models/Entities`、`Models/DTOs` | 数据 + 业务规则 |
| **View** | `Views/*.xaml` | XAML 布局 + 数据绑定 |
| **ViewModel** | `ViewModels/*.cs` | 状态 + 行为 + 暴露给 View |

**绑定关系**：
```
View ⇄ (DataBinding) ⇄ ViewModel
        ⇡                 ⇡
        |       INotifyPropertyChanged
        |                 ⇡
        +---- Prism 容器注入
                          |
            Model（Service / Entity）
```

### 5.2 Prism 在项目中的 4 个核心机制

#### 5.2.1 Bootstrapper 启动器

[Bootstrapper.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Bootstrapper.cs)：

```csharp
public class Bootstrapper : PrismBootstrapper
{
    protected override DependencyObject CreateShell()
    {
        return Container.Resolve<MainWindow>();   // 1. 创建主窗口
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 2. 注册可导航的 View
        containerRegistry.RegisterForNavigation<DashboardView>();
        // ...
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 3. 注册模块
        moduleCatalog.AddModule<CoreModule>();
    }
}
```

**启动流程**：
```
App.OnStartup
  → new Bootstrapper().Run()        // Prism 启动
    → CreateContainer()             // 反射创建 Unity 容器
    → ConfigureModuleCatalog()      // 注册模块
    → InitializeShell()             // 创建并显示 MainWindow
```

#### 5.2.2 模块（IModule）

[Modules/CoreModule.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Modules/CoreModule.cs)：

```csharp
public class CoreModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册 DbContext
        containerRegistry.RegisterSingleton<AppDbContext>(() => { ... });
        // 注册仓储/服务
        containerRegistry.RegisterSingleton<IUnitOfWork, UnitOfWork>();
        // ...
    }
}
```

**为什么用模块？**——未来想拆"采集模块""UI 模块"成独立 csproj 时，几乎不用改业务代码。

#### 5.2.3 Region 区域导航

[MainWindow.xaml#L99](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/MainWindow.xaml#L99)：

```xml
<ContentControl Grid.Row="1" prism:RegionManager.RegionName="MainRegion" Margin="16"/>
```

[MainWindowViewModel.cs#L40-L45](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/MainWindowViewModel.cs#L40-L45)：

```csharp
private void Navigate(string viewName)
{
    _regionManager.RequestNavigate("MainRegion", viewName);
}
```

**原理**：左侧 RadioButton 触发 `NavigateCommand`，传入 "DashboardView" 字符串，Prism 在 `MainRegion` 区域里把 `DashboardView` 实例化并展示。

#### 5.2.4 EventAggregator 事件聚合器

[DashboardViewModel.cs#L37](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs#L37)：

```csharp
eventAggregator.GetEvent<PointValueUpdatedEvent>().Subscribe(_ => _refreshTimer?.Start());
```

[DataAcquisitionService.cs#L128](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L128)：

```csharp
_eventAggregator.GetEvent<PointValueUpdatedEvent>().Publish(point.Id);
```

**好处**：
- Service 不知道有 ViewModel 存在。
- ViewModel 不知道是哪个 Service 在发。
- 加新订阅者（报警 ViewModel、趋势 ViewModel）一行代码即可。

### 5.3 BindableBase 基础

项目里所有 ViewModel 都继承自 `Prism.Mvvm.BindableBase`，它封装了 `INotifyPropertyChanged`：

```csharp
private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
public string CurrentTime
{
    get => _currentTime;
    set => SetProperty(ref _currentTime, value);   // ← 自动触发 PropertyChanged
}
```

**WPF 数据绑定机制**：
1. XAML 写 `Text="{Binding CurrentTime}"`。
2. 绑定引擎订阅 `PropertyChanged` 事件。
3. ViewModel `SetProperty` 时通知 UI 刷新。

### 5.4 命令（Command）

[MainWindowViewModel.cs#L32](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/MainWindowViewModel.cs#L32)：

```csharp
public DelegateCommand<string> NavigateCommand { get; }
NavigateCommand = new DelegateCommand<string>(Navigate);
```

XAML 绑定：
```xml
<RadioButton Command="{Binding NavigateCommand}" CommandParameter="DashboardView" ... />
```

**Prism DelegateCommand vs ICommand**：
- `ICommand` 是 .NET 标准接口。
- `DelegateCommand<T>` 封装 `Execute` + `CanExecute` + `RaiseCanExecuteChanged`，可传泛型参数。

### 5.5 整个项目的"启动到数据流动"图

```
1. App.OnStartup
   └── Bootstrapper.Run()
       ├── 配置 Unity 容器
       ├── 加载 CoreModule
       │   ├── 注册 AppDbContext（Sqlite, hvac_energy_monitor.db）
       │   ├── SeedData 种子数据：1 个设备 + 4 个测点
       │   └── 注册 6 个服务（单例）
       ├── 创建 MainWindow
       │   └── 解析 MainWindowViewModel
       │       ├── 创建 IRegionManager
       │       ├── 启动 DispatcherTimer（每秒更新 CurrentTime）
       │       └── Navigate("DashboardView")
       │           └── 解析 DashboardView + DashboardViewModel
       │               ├── 订阅 PointValueUpdatedEvent
       │               ├── 启动 DispatcherTimer（500ms 刷新）
       │               └── InitializeAsync() → 调 _acquisition.StartAsync()
       │                   └── 启动 _runningTask
       │                       └── 循环（while !ct.IsCancellationRequested）
       │                           ├── 读 devices（含 points）
       │                           ├── 调 ICommunicationService.ReadHoldingRegistersAsync
       │                           ├── 写 IPointValueCache
       │                           ├── Publish PointValueUpdatedEvent
       │                           ├── 调 IDataStorageService.EnqueueAsync
       │                           └── Task.Delay(1000, ct)
```

> **🔑 核心要点**
> 1. Prism = 模块化 + DI + 区域导航 + 事件聚合。
> 2. Bootstrapper 启动器 3 步：容器、注册类型、配置模块。
> 3. View 通过 `RegionName` + `RequestNavigate` 切换。
> 4. EventAggregator 是 Service 与 ViewModel 解耦的关键。

> **🎤 面试应答模板**
> "我项目用 Prism 9 做 MVVM 框架。入口是 Bootstrapper 继承自 PrismBootstrapper，3 个方法：CreateShell 返回主窗口，RegisterTypes 注册可导航 View，ConfigureModuleCatalog 注册 CoreModule。CoreModule 实现了 IModule，把 DbContext 和所有服务都注册到 Unity 容器。导航用 Region 机制：MainWindow 里有 ContentControl 指定 RegionName='MainRegion'，ViewModel 调 RequestNavigate 切换页面。ViewModel 之间通过 EventAggregator 通信，避免直接引用。"

> **🛠 动手任务**
> 1. 跟踪 `MainWindowViewModel.Navigate("AlarmView")` 调用链，画出对象创建顺序。
> 2. 把 `DispatcherTimer` 换成 `System.Timers.Timer`，体会"UI 线程"问题。

---

## 第 6 章　WPF 关键技术

### 6.1 WPF 核心机制速览

| 机制 | 作用 | 项目里在哪里 |
|---|---|---|
| 数据绑定（Binding） | View ⇄ ViewModel | 全部 XAML |
| 命令（Command） | 把按钮事件抽到 ViewModel | 导航按钮 |
| 资源字典（ResourceDictionary） | 全局样式 | `Design/Styles.xaml` |
| 样式（Style）+ 模板（Template） | 统一视觉 | KPI 卡片、导航按钮 |
| 值转换器（IValueConverter） | 状态→颜色、值→可见性 | `Converters/` |
| DispatcherTimer | UI 线程定时器 | `MainWindowViewModel`、`DashboardViewModel` |

### 6.2 数据绑定的"坑"和项目里的处理

**坑 1：ObservableCollection 必须在 UI 线程修改**

[DashboardViewModel.cs#L58-L70](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs#L58-L70)：

```csharp
PointValues.Clear();
foreach (var point in points)
{
    PointValues.Add(new PointDisplayItem { ... });
}
```

`Clear` + `Add` 是在 `DispatcherTimer.Tick` 触发的方法里跑的——`DispatcherTimer` 的回调**默认在 UI 线程**，所以安全。

**坑 2：长时间操作会卡 UI**

如果 `RefreshAsync` 里有耗时 IO（比如查 10 万条历史），UI 会假死。**解法**：用 `Task.Run` + `await` + `Dispatcher.Invoke` 或在 ViewModel 用 `async/await` + ConfigureAwait 配合 `IProgress<T>`。

**坑 3：绑定到方法而不是属性**

```csharp
public string AcquisitionStatus => _acquisition.IsRunning ? "运行中" : "已停止";  // ← 只读属性
```

`RefreshAsync` 里手动 `RaisePropertyChanged(nameof(AcquisitionStatus))`——因为表达式树检测不到依赖 `_acquisition.IsRunning`。

### 6.3 值转换器（Value Converter）

项目里 4 个 Converter（[Converters/](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Converters/)）：

| Converter | 作用 | 用法 |
|---|---|---|
| `StatusToBrushConverter` | 状态枚举 → 颜色画刷 | 设备/告警列表里的状态色 |
| `MultiEqualsConverter` | 多值比较 | 报警闪烁 |
| `StringEqualsConverter` | 字符串比较 | 当前页高亮 |
| `EmptyCollectionToVisibilityConverter` | 集合空 → 可见 | "暂无数据"占位 |

例：`StatusToBrushConverter` 实现模式：

```csharp
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "运行" => Brushes.LimeGreen,
            "停止" => Brushes.Gray,
            "故障" => Brushes.Red,
            _ => Brushes.DarkGray
        };
    }
    public object ConvertBack(...) => throw new NotImplementedException();
}
```

XAML 用法：
```xml
<Ellipse Fill="{Binding DeviceStatus, Converter={StaticResource StatusToBrushConverter}}"/>
```

### 6.4 全局样式资源

[Design/Styles.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Design/Styles.xaml) 在 `App.xaml` 合并到全局，所有 View 都能用 `{StaticResource CardStyle}` 这样的引用。

**面试亮点**：
- 全部颜色集中在资源字典，便于换肤。
- 命名规范（CardStyle / KpiLabelStyle / NavRadioButtonStyle）清晰。

### 6.5 MahApps.Metro.IconPacks 矢量图标

[MainWindow.xaml#L26](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/MainWindow.xaml#L26)：

```xml
xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks"
<iconPacks:PackIconMaterial Kind="AirConditioner" Width="28" Height="28"/>
```

**工业 UI 优势**：矢量、缩放无失真、跨设备一致。比 PNG 资源灵活。

### 6.6 ScottPlot 绑定（实时趋势）

[DashboardView.xaml#L70](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/DashboardView.xaml#L70)：

```xml
<scottplot:WpfPlot Grid.Row="1" x:Name="RealtimePlot"/>
```

使用模式（`DashboardView.xaml.cs` 或 ViewModel 注入）：

```csharp
RealtimePlot.Plot.AddSignal(values, sampleRate: 1);
RealtimePlot.Refresh();
```

**为什么用 ScottPlot？**——纯托管代码、性能好、支持 .NET 8 + WPF、开源。

### 6.7 WPF 启动流程（你必须会的）

```
App.Main()
  → Application.Run(new App())
  → App.OnStartup
  → 初始化 NLog + Bootstrapper
  → 创建 MainWindow + ViewModel
  → WPF 渲染引擎接管
  → ContentControl 注册到 MainRegion
  → DashboardView 渲染（数据绑定 + 样式 + 转换器）
```

> **🔑 核心要点**
> 1. WPF 全部状态修改必须在 UI 线程——`DispatcherTimer` / `Dispatcher.Invoke` 是关键工具。
> 2. `INotifyPropertyChanged` 是绑定的心跳——`BindableBase.SetProperty` 已封装。
> 3. 值转换器把"数据"翻译成"显示"——解耦 View 和 ViewModel。
> 4. 全局样式 = 资源字典 + 命名规范 + StaticResource。

> **🎤 面试应答模板**
> "WPF 关键点是数据绑定和命令。ViewModel 继承 BindableBase，SetProperty 自动通知 UI 刷新；集合用 ObservableCollection 才能动态增删。耗时操作绝对不能卡 UI 线程，所以 DispatcherTimer、Task.Run、async/await 都要会用。全局样式集中在资源字典 Styles.xaml，换肤只改一处。值转换器把枚举翻译成颜色或可见性，逻辑在 View 层但执行在绑定引擎里。"

> **🛠 动手任务**
> 1. 在 `DashboardViewModel` 里加一个 `KpiValueBrush` 属性（值>50 显示红色），用转换器实现。
> 2. 把 `DispatcherTimer` 替换为 `System.Threading.Timer`，观察 `InvalidOperationException`：集合修改不在 UI 线程。

---

## 第 7 章　异步与多线程（项目核心难点）

> 这一章是面试**最常问的**——尤其问到"采集怎么不卡 UI"时。

### 7.1 项目的"线程图"

```
                ┌──────────────────────────────┐
                │        UI Thread             │
                │  (DispatcherTimer 500ms)     │
                │  - 订阅 EventAggregator      │
                │  - 刷新 ObservableCollection │
                │  - 渲染 ScottPlot 曲线       │
                └────────────┬─────────────────┘
                             │ EventAggregator
                             │ (跨线程安全)
┌────────────────────────────┴───────────────────────────────┐
│                后台采集线程                                │
│  DataAcquisitionService.RunAsync (Task)                   │
│  - while (!ct.IsCancellationRequested)                   │
│  - await ReadHoldingRegistersAsync (Modbus)              │
│  - await _storage.EnqueueAsync                            │
│  - Task.Delay(1000, ct)                                  │
└──────────┬──────────────────────────┬─────────────────────┘
           │                          │
           ▼                          ▼
   ┌─────────────────┐       ┌──────────────────┐
   │ ICommunication  │       │  Timer (Thread   │
   │ Service         │       │  Pool) 每 5s     │
   │ - 阻塞 IO       │       │  FlushAsync      │
   │ - TCP Socket    │       │  (写入 SQLite)   │
   └─────────────────┘       └──────────────────┘
```

### 7.2 项目里用到的 6 种异步模式

#### 7.2.1 模式 1：async/await + CancellationToken

[DataAcquisitionService.cs#L75-L102](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L75-L102)：

```csharp
private async Task RunAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            var devices = await _context.Devices
                .Where(d => d.IsEnabled)
                .Include(d => d.Points.Where(p => p.IsEnabled))
                .ToListAsync(ct);                    // ← ct 传给 EF Core

            foreach (var device in devices)
            {
                await ProcessDeviceAsync(device, ct);
            }
        }
        catch (OperationCanceledException) { }       // ← 正常取消
        catch (Exception ex) { Logger.Error(ex, ...); }

        await Task.Delay(1000, ct);                  // ← ct 让 Delay 也可取消
    }
}
```

**核心要点**：
- `while (!ct.IsCancellationRequested)` 双重保险（外层检查 + 内部 `ThrowIfCancellationRequested`）。
- 所有异步方法签名都加 `CancellationToken ct = default`。
- `catch (OperationCanceledException)` 是"正常取消"而不是错误。

#### 7.2.2 模式 2：链接 CancellationToken

[DataAcquisitionService.cs#L49](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L49)：

```csharp
_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
```

**为什么？**——这样可以同时响应**调用方的取消**和**自己的取消**（`StopAsync` 调 `_cts.Cancel()`）。

#### 7.2.3 模式 3：长循环后台任务

```csharp
_runningTask = RunAsync(_cts.Token);   // ← 不 await，保存 Task
```

**为什么？**——`StartAsync` 立即返回（不阻塞调用方），后台任务独立运行。需要"等它退出"时：

```csharp
try { await _runningTask.WaitAsync(ct); }
catch (OperationCanceledException) { }
```

#### 7.2.4 模式 4：Timer + Async（后台批量入库）

[DataStorageService.cs#L23](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Storage/DataStorageService.cs#L23)：

```csharp
_flushTimer = new Timer(_ => _ = FlushAsync(), null,
    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
```

**注意**：
- `Timer` 的回调在**线程池线程**上跑。
- `_ = FlushAsync()` 故意丢弃 Task，避免编译器警告——但**这意味着异常会被吞掉**。项目里用 `try/catch` 在 `FlushAsync` 内显式记录。
- `FlushAsync` 内部用 `try/catch` 保证不会因一次失败而让 Timer 挂掉。

#### 7.2.5 模式 5：线程安全集合

[DataStorageService.cs#L16](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Storage/DataStorageService.cs#L16)：

```csharp
private readonly ConcurrentQueue<PointValue> _buffer = new();
```

**Producer-Consumer 模式**：
- **Producer**：采集线程 `Enqueue`。
- **Consumer**：Timer 线程 `TryDequeue` 批量取出 + SaveChanges。
- 配合 `MaxBatchSize = 100` 防 OOM。

#### 7.2.6 模式 6：DispatcherTimer 跨线程更新 UI

[DashboardViewModel.cs#L37](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/DashboardViewModel.cs#L37)：

```csharp
eventAggregator.GetEvent<PointValueUpdatedEvent>().Subscribe(_ => _refreshTimer?.Start());
```

`Prism.EventAggregator` 内部用 `Dispatcher` 把回调 marshal 到 UI 线程——所以 `Subscribe` 的 lambda 里**不需要** `Dispatcher.Invoke`。

`DispatcherTimer` 的 Tick 也是在 UI 线程跑——所以 `ObservableCollection.Add` 是安全的。

### 7.3 任务并行库（TPL）浅触

项目里没直接用 `Task.Run`、`Parallel.ForEachAsync`——这是**可以加分**的优化点：
- 多设备并行采集：`await Task.WhenAll(devices.Select(d => ProcessDeviceAsync(d, ct)));`
- CPU-bound 数据处理：`await Task.Run(() => AggregateData(values));`

### 7.4 死锁、Race Condition 防御

**项目里如何避免死锁**：
- 所有 `await` 后**不**接 `.Result` / `.Wait()`（这两种会阻塞并可能死锁）。
- EF Core 异步 API 内部用 `ConfigureAwait(false)`，所以不卡 UI 线程。

**项目里如何避免数据竞争**：
- `ConcurrentQueue`、`ConcurrentDictionary`。
- `_activeAlarms`（`HashSet<string>`）只在一个线程里访问（采集线程）。
- 写 UI 集合时确保在 UI 线程。

### 7.5 取消与资源释放

```csharp
public void Dispose()
{
    _flushTimer.Dispose();
    _ = FlushAsync();                  // ← 析构前再 flush 一次
    GC.SuppressFinalize(this);
}
```

**IDisposable 模式**：
- 实现了 `IDisposable` 的类（`DataStorageService`、`UnitOfWork`）需要释放非托管资源（Timer、DbContext）。
- Unity 容器默认对单例是"长生命周期"，不会自动释放——所以容器在 App 退出时由 Unity 释放，触发链式 Dispose。

> **🔑 核心要点**
> 1. 项目异步核心：`async/await` + `CancellationToken` + 后台 Task。
> 2. 多线程：采集线程、Timer 线程、UI 线程 三种。
> 3. 线程安全集合：`ConcurrentQueue`（生产者-消费者）+ `ConcurrentDictionary`（缓存）。
> 4. 取消：协作式，通过 `CancellationToken` 传递，所有异步方法都加。
> 5. UI 更新必须 UI 线程：`DispatcherTimer` / `Dispatcher.Invoke` / Prism EventAggregator 自动 marshal。

> **🎤 面试应答模板**
> "我项目里三个线程：UI 线程跑 DispatcherTimer 500ms 刷新 ObservableCollection；后台采集线程是 DataAcquisitionService 的 RunAsync，循环读 Modbus 写缓存和事件；线程池跑 DataStorageService 的 Timer，每 5s 把 ConcurrentQueue 里的点值批量入库 SQLite。三线程之间用 Prism EventAggregator + 线程安全集合通信。停止采集用协作式取消，传 CancellationToken 给所有异步方法，循环 while 检查、Task.Delay 也会抛 OperationCanceledException。Producer-Consumer 模式让采集和入库解耦，缓存队列上限 100 防 OOM。"

> **🛠 动手任务**
> 1. 把 `DataAcquisitionService.RunAsync` 改成多设备并行：`await Task.WhenAll(devices.Select(ProcessDeviceAsync))`，观察吞吐变化。
> 2. 故意在 `DispatchTimer.Tick` 回调里 `await Task.Delay(5000)`，看 UI 会不会卡（答：会卡，演示 UI 线程被占）。
> 3. 用 SemaphoreSlim 把 FlushAsync 改成"同时只能一个在跑"。

---

## 第 8 章　数据库与 EF Core

### 8.1 选型理由

**为什么用 SQLite？**
- **单文件部署**：嵌入式软件一个 exe + 一个 db 文件就能跑。
- **零运维**：不需要安装数据库服务。
- **够用**：本项目数据量小（每点每秒一条，一年 3150 万条 = ~500MB，可接受）。
- **EF Core 兼容**：代码、迁移都和 SQL Server 一样。

**生产场景的进化**：
- 长期运行 → 用 PostgreSQL / TimescaleDB（时序优化）。
- 实时聚合 → 用 InfluxDB / OpenTSDB。
- 报表 → 用 ClickHouse。

### 8.2 DbContext 设计

[Infrastructure/DbContext/AppDbContext.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/DbContext/AppDbContext.cs)：

```csharp
public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Point> Points => Set<Point>();
    public DbSet<PointValue> PointValues => Set<PointValue>();
    public DbSet<AlarmRule> AlarmRules => Set<AlarmRule>();
    public DbSet<AlarmRecord> AlarmRecords => Set<AlarmRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent API 配置（约定优于配置 + 显式覆盖）
    }
}
```

**面试要会讲**：
- `DbSet<T> Set<T>()` vs 直接属性 `DbSet<Device> Devices { get; set; }`：前者更简洁。
- Fluent API vs DataAnnotations：项目用 Fluent API（更强大、不污染实体类）。
- 主键、必填、字符串长度、外键级联（Cascade）、索引（复合索引 `(PointId, Timestamp)`）的写法。

### 8.3 实体关系

```
Device (1) ─── (N) Point (1) ─── (N) PointValue
                       │
                       └── (N) AlarmRule
                               
Point (1) ─── (N) AlarmRecord
```

[AppDbContext.cs#L33-L37](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/DbContext/AppDbContext.cs#L33-L37)：

```csharp
entity.HasOne(e => e.Device)
      .WithMany(d => d.Points)
      .HasForeignKey(e => e.DeviceId)
      .OnDelete(DeleteBehavior.Cascade);    // ← 删设备自动删点
```

**Cascade 行为 4 种**：
- `Cascade`：级联删除（删父表 → 子表自动删）。
- `Restrict`：禁止删除（如果有子记录，抛异常）。
- `SetNull`：外键置空。
- `NoAction`：数据库自己处理（EF Core 模拟）。

### 8.4 索引设计

```csharp
entity.HasIndex(e => new { e.PointId, e.Timestamp });   // PointValue 复合索引
entity.HasIndex(e => e.TriggerTime);                    // AlarmRecord 单列索引
```

**为什么这么设计**？
- `PointValue` 查询模式：`WHERE PointId = ? AND Timestamp BETWEEN ? AND ?`——复合索引按 `PointId` 优先，匹配最优。
- `AlarmRecord` 查询模式：`WHERE TriggerTime > ? ORDER BY TriggerTime DESC`——单列索引够用。

### 8.5 仓储+工作单元（UoW）

[Repository/Repository.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Repository/Repository.cs)：

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.Where(predicate).ToListAsync();
    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
    // ...
}
```

**UoW 缓存仓储**：
```csharp
public IRepository<T> Repository<T>() where T : class
{
    return (IRepository<T>)_repositories.GetOrAdd(typeof(T),
        _ => new Repository<T>(_context));
}
```

**好处**：同一个 `Repository<Device>` 在一个事务里多次调用只创建一个实例。

### 8.6 事务与并发

项目里没有显式 `BeginTransaction`，但 EF Core 的 `SaveChangesAsync` 默认就是**单事务**：

```csharp
// 改 100 条 + SaveChanges = 一个 SQL 事务
await _unitOfWork.Repository<PointValue>().AddRangeAsync(batch);
await _unitOfWork.SaveChangesAsync();
```

**生产场景的并发问题**：
- **悲观锁**：`SELECT ... FOR UPDATE`（SQL Server）/ 显式事务 + 行锁。
- **乐观锁**：实体加 `RowVersion` 字段，并发更新时 EF Core 抛 `DbUpdateConcurrencyException`。
- **本项目**：单写多读，不会冲突，所以不需要。

### 8.7 EF Core 性能优化

项目里用到的：
- **AsNoTracking**（未用）—— 只读查询应加。
- **批量插入 AddRangeAsync**（已用）—— 比循环 Add 性能高 5~10 倍。
- **Include 预加载**（已用）—— `.Include(d => d.Points)` 避免 N+1。
- **异步 ToListAsync**（已用）—— 不阻塞调用线程。

### 8.8 SQLite 的坑（面试可以主动提）

- **并发写**：SQLite 单写者锁，WAL 模式能改善。生产用 SQL Server 更好。
- **时间精度**：默认字符串存储，毫秒会丢。用 `DateTimeOffset` 或 `TEXT` 自定义映射。
- **类型映射**：`double`、`long` 在 SQLite 都是 NUMERIC，存储安全。
- **WAL 模式**：`PRAGMA journal_mode=WAL;` 提升并发。

> **🔑 核心要点**
> 1. SQLite 适合嵌入式/单机；生产规模用 PostgreSQL/TimescaleDB。
> 2. EF Core 用 DbContext + DbSet + Fluent API 建模。
> 3. 索引按查询模式设计：复合索引最左前缀原则。
> 4. 仓储+工作单元隔离 ORM，单 SaveChanges = 单事务。
> 5. EF Core 性能关键：AsNoTracking / AddRangeAsync / Include / 异步 ToListAsync。

> **🎤 面试应答模板**
> "我用 EF Core 8 + SQLite 持久化。选 SQLite 是因为嵌入式部署简单。DbContext 用 Fluent API 配索引和外键：PointValue 的 (PointId, Timestamp) 复合索引匹配历史查询模式；AlarmRecord 的 TriggerTime 单列索引匹配时间排序查询。所有 Service 通过 IUnitOfWork 访问，避免直接操作 DbContext。批量入库用 AddRangeAsync + 单 SaveChanges 走一个 SQL 事务，比循环 Add 快 5~10 倍。Producer-Consumer 模式让采集线程和入库线程解耦，ConcurrentQueue 缓冲防 IO 抖动。"

> **🛠 动手任务**
> 1. 在 `AppDbContext` 加 AsNoTracking 扩展方法 `FindAsNoTrackingAsync`。
> 2. 用 EF Core 迁移（`Add-Migration Initial`）替代 `EnsureCreated`。
> 3. 在 `PointValue` 上加 `(Timestamp, PointId)` 索引，看查询计划变化。

---

## 第 9 章　工业通信协议——Modbus

### 9.1 Modbus 基础

**Modbus** 是 1979 年 Modicon（施耐德前身）发布的**主从协议**，是工业领域事实标准。

**两种物理层**：
| 类型 | 项目里叫什么 | 物理层 | 设备距离 |
|---|---|---|---|
| **Modbus RTU** | `ProtocolType.ModbusRTU` | RS-485（半双工串口） | 千米级 |
| **Modbus TCP** | `ProtocolType.ModbusTCP` | Ethernet TCP | 局域网 |

**核心概念**：
- **主站（Master）**——发起请求，本项目是上位机。
- **从站（Slave）**——响应请求，PLC 或传感器。
- **功能码（Function Code）**——操作类型。
- **寄存器（Register）**——16 bit 存储单元。

### 9.2 4 个常用功能码

| 功能码 | 名称 | 项目里用途 | 寄存器大小 |
|---|---|---|---|
| 01 | 读线圈 | 读 DO（开关量输出） | 1 bit |
| 02 | 读离散输入 | 读 DI（开关量输入） | 1 bit |
| 03 | 读保持寄存器 | 读 AO（模拟量输出） | 16 bit |
| 04 | 读输入寄存器 | 读 AI（模拟量输入） | 16 bit |

**本项目只用 03/04**——读模拟量。详见 [DataAcquisitionService.cs#L120-L123](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L120-L123)：

```csharp
ushort[] raw;
if (point.FunctionCode == 3)
    raw = await service.ReadHoldingRegistersAsync(...);
else
    raw = await service.ReadInputRegistersAsync(...);
```

### 9.3 寄存器地址体系

Modbus 协议里有"两套地址"——**协议地址** vs **PLC 地址**：

| 数据类型 | 协议地址范围 | 协议地址基数 | PLC 地址基数 | 差值 |
|---|---|---|---|---|
| 线圈 (Coil) | 00001~09999 | 0 | 1 | +1 |
| 离散输入 | 10001~19999 | 0 | 10001 | +10001 |
| 输入寄存器 | 30001~39999 | 0 | 30001 | +30001 |
| 保持寄存器 | 40001~49999 | 0 | 40001 | +40001 |

**面试常问坑**：西门子 PLC 地址从 40001 开始，NModbus 用 0 基数——**用户配置的地址要 -1**。本项目里 `Point.RegisterAddress = 0..3`，用的是 0 基数。

### 9.4 数据类型与字节序

**Modbus 只规定 16 bit 寄存器**。32 bit 数据（Int32、Float）需要用 2 个寄存器组合。

**字节序 4 种**（[ByteOrderConverter.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Helpers/ByteOrderConverter.cs)）：
| 名称 | 字节序 | 高位寄存器 | 适用 |
|---|---|---|---|
| BigEndian | AB CD | 在前 | 西门子、AB PLC |
| LittleEndian | CD AB | 在后 | 多数 Modbus 设备默认 |
| BigEndianSwap | B A D C | 字节翻转 | 字节序自定义 |
| LittleEndianSwap | D C B A | 字节翻转 | 字节序自定义 |

> **面试必问**："你怎么知道设备用哪种字节序？"——查设备手册，或者 4 种都试一遍看哪个解析出合理值。

### 9.5 工程量转换

**Modbus 寄存器值 ≠ 真实物理量**，需要量程转换：

```
真实值 = 原始值 × Scale + Offset
```

[DataAcquisitionService.cs#L171-L183](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs#L171-L183)：

```csharp
private static double ConvertToEngineeringValue(ushort[] raw, Point point)
{
    double rawValue = point.DataType switch
    {
        DataType.UShort => raw[0],
        DataType.Short  => (short)raw[0],
        DataType.UInt   => (uint)((raw[0] << 16) | raw[1]),
        DataType.Int    => (raw[0] << 16) | raw[1],
        DataType.Float  => ByteOrderConverter.ToFloat(raw[0], raw[1], point.ByteOrder),
        _ => raw[0]
    };
    return rawValue * point.Scale + point.Offset;
}
```

**例子**：温度传感器 0~100°C 对应 0~10000，Scale=0.01，Offset=0，原始 5000 → 50.0°C。

### 9.6 报文结构（Modbus TCP）

```
┌──────────────┬─────────┬─────────┬──────┬────────────┬──────┬──────┬─────┐
│  MBAP Header │         │         │      │            │      │      │     │
├──────┬───────┼────┬────┼────┬────┼──────┼─────┬──────┼──────┼──────┼─────┤
│ Trans│ Proto │Length │  Unit │ Func │ Start │  N  │   Data  │  CRC │
│  ID  │   ID  │       │  ID   │ Code │ Addr  │ Reg │         │ (RTU)│
│  2B  │  2B   │  2B   │  1B   │  1B  │  2B   │ 2B  │  N*2B   │      │
└──────┴───────┴────┴────┴────┴────┴───────┴─────┴──────┴──────┴──────┴─────┘
                                  (RTU 没有 MBAP Header, 直接接 CRC)
```

**响应异常**：从站把功能码最高位置 1（`0x83` 表示 03 异常），错误码：
- 01 非法功能
- 02 非法数据地址
- 03 非法数据值
- 04 从机故障
- 05 确认（处理中）
- 06 从机忙
- 08 存储奇偶校验差错

### 9.7 本项目的 Modbus 实现

`ModbusTcpCommunicationService` 已实现 TCP 连接（`TcpClient.ConnectAsync`），寄存器读取抛 `NotImplementedException`——**面试亮点**：

> "Modbus 通信我先做了接口 + Simulator 仿真器，让系统在没有 PLC 的情况下也能跑通。Modbus TCP 通信服务实现了 TCP 连接和断开，寄存器读写预留了接口位（用 NModbus 包，面试时可以现场补全）。字节序转换单独抽到 ByteOrderConverter 用 Span<T> 零分配实现。"

**真实工业里**，可以用 [NModbus](https://github.com/NModbus4/NModbus) 包：

```csharp
var factory = new ModbusFactory();
using var master = factory.CreateMaster(tcpClient);
ushort[] registers = await master.ReadHoldingRegistersAsync(slaveId, startAddress, count);
```

### 9.8 其他常见工业协议（面试加分）

| 协议 | 特点 | 用途 |
|---|---|---|
| **OPC UA** | 统一架构、支持复杂类型、加密 | 跨厂商系统集成 |
| **Profinet** | 西门子主推、实时以太网 | 西门子 PLC |
| **EtherCAT** | 高速（< 100μs）、同步精度高 | 运动控制 |
| **Mqtt / Sparkplug B** | 工业 IoT 标准 | 云端 SCADA |
| **S7** | 西门子私有 | 直连西门子 PLC |

> **🔑 核心要点**
> 1. Modbus 是工业协议事实标准，RTU（串口）+ TCP（网口）两种。
> 2. 4 个常用功能码：01/02/03/04，本项目用 03/04 读模拟量。
> 3. 32 bit 数据需要 2 个寄存器 + 字节序转换（4 种）。
> 4. 工程量转换：真实值 = 原始值 × Scale + Offset。
> 5. 协议地址 vs PLC 地址 差值要小心。

> **🎤 面试应答模板**
> "Modbus 是工业最常见的协议，分 RTU（RS485 串口）和 TCP（以太网）两种。功能码 03 读保持寄存器、04 读输入寄存器，模拟量都用这两个。32 bit 整数和浮点要 2 个寄存器组合，存在字节序问题（4 种），我用 ByteOrderConverter 用 Span<T> 零分配实现。原始值还要做工程量转换，真实值=原始×Scale+Offset，比如温度传感器 0~100°C 对应 0~10000。我项目里用 ICommunicationService 接口隔离协议，Simulator 和 ModbusTCP 两种实现可热切换。"

> **🛠 动手任务**
> 1. 用 `NModbus` 包补全 `ModbusTcpCommunicationService.ReadHoldingRegistersAsync`。
> 2. 加 4 个点的测试：写一个 `MockCommunicationService` 用 `Channel<T>` 模拟推送。
> 3. 用 Wireshark 抓一个真实 Modbus TCP 报文，理解 MBAP 头。

---

## 第 10 章　全链路：采集 → 缓存 → 存储 → 报警 → UI

### 10.1 数据流总图

```
   ┌─────────────────────────┐
   │ Simulator/ModbusTCP 设备 │ (Modbus Slave / 仿真)
   └────────────┬────────────┘
                │  Modbus 报文
                ▼
   ┌─────────────────────────┐
   │  ICommunicationService  │ (接口 + 字节序解析)
   └────────────┬────────────┘
                │  ushort[] + Scale/Offset
                ▼
   ┌─────────────────────────────────────────┐
   │  DataAcquisitionService.RunAsync         │
   │  (while 循环, 1s 一次, CancellationToken) │
   └──┬───────────────────┬──────────────────┘
      │                   │
      │ ConcurrentDict    │ ConcurrentQueue
      ▼                   ▼
   ┌──────────────┐  ┌──────────────────┐
   │PointValueCache│  │DataStorageService│
   │(实时最新值)  │  │(批量入库 SQLite)│
   └──────┬───────┘  └──────────────────┘
          │ EventAggregator
          │ PointValueUpdatedEvent
          ▼
   ┌─────────────────────────────┐
   │   DashboardViewModel         │
   │   - DispatcherTimer 500ms   │
   │   - ObservableCollection    │
   │   - ScottPlot               │
   └─────────────────────────────┘
          
   并行：AlarmService 订阅 IPointValueCache 变化
         触发 AlarmRecord 写入 + Event 通知
```

### 10.2 关键设计决策

#### 10.2.1 为什么用缓存而不是直接读 Modbus？

**性能**：UI 100ms 刷新一次，Modbus 1s 一次。中间 9 次刷新都从缓存读——**Modbus 调用次数减少 10 倍**。

**隔离**：UI 刷新和 Modbus 通信解耦，Modbus 慢/抖不影响 UI 流畅。

#### 10.2.2 为什么采集和入库要解耦？

**背压控制**：如果数据库写入慢，采集不能等。`ConcurrentQueue` 当缓冲，最多 100 条。

**批量优化**：5 秒攒一批一次性写，比 1 秒写 1 次快 10 倍。

#### 10.2.3 为什么用 EventAggregator 而不是直接订阅？

| 方式 | 缺点 |
|---|---|
| `ICommunicationService` 调 `IPointValueCache.Set` + ViewModel 直接订阅 | ViewModel 依赖 Service，违反 DIP |
| ViewModel 定时轮询 | 不实时，性能差 |
| **EventAggregator** | 服务和 UI 双向解耦，多订阅者一行代码搞定 |

### 10.3 报警链路

```
采集到新值
   ↓
写入缓存 + Publish Event
   ↓
AlarmService 内部订阅（项目里没显式订阅，需补）
   ↓
检查 Point.HighLimit/LowLimit
   ↓
触发 → 写 AlarmRecord → 触发 C# event
   ↓
ViewModel 订阅 → 更新 UI（红条/弹窗）
```

> **改进点**（可作为面试"我接下来要做的事"）：
> - 让 `AlarmService` 也订阅 `PointValueUpdatedEvent`，而非 `DataAcquisitionService` 主动调。
> - 报警去重 `HashSet<string>` 当前只在单线程里，改为 `ConcurrentDictionary` 防止竞态。
> - 加报警声音 + 弹窗 + 短信通知（生产功能）。

### 10.4 报表聚合

[EnergyReportService.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Report/EnergyReportService.cs) 实现了时/日/月三种粒度：

```csharp
public async Task<IEnumerable<EnergyReportDto>> GetHourlyReportAsync(int pointId, DateTime start, DateTime end)
{
    var values = await GetValuesAsync(pointId, start, end);
    return GroupByPeriod(values, start, end, TimeSpan.FromHours(1), "Hour");
}
```

**算法**：把时间区间切成 N 段，每段算 `平均值 × 数量 = 累计值`（能耗公式 = 平均功率 × 时间）。

> **问题**：当前实现是**全量查询 + 内存分组**，数据量大会慢。生产应该用 SQL `GROUP BY` + 索引。

### 10.5 项目里能"挑刺"的地方（展示工程思维）

| 现状 | 问题 | 改进方案 |
|---|---|---|
| `EnsureCreated` 创建库 | 改字段类型要删库 | EF Core Migrations |
| `AlarmService.CheckAsync` 没被调用 | 报警实际不工作 | 订阅 `PointValueUpdatedEvent` |
| `ModbusTcpCommunicationService` 抛 NotImplementedException | TCP 读取未实现 | 集成 NModbus |
| 报表全量查询 | 数据量大慢 | SQL `GROUP BY` + TimescaleDB |
| `AppDbContext` 单例 | 长生命周期 DbContext 不规范 | 改为 Scoped |
| 没有异常重试机制 | 网络抖动会丢点 | Polly + 指数退避 |
| 没有日志结构化 | 排查问题难 | Serilog + 结构化日志 |
| 测试覆盖 0% | 重构易出 bug | xUnit + Moq |

> **🔑 核心要点**
> 1. 完整数据链路：Modbus → 采集 → 缓存/事件/队列 → UI/报警/入库/报表。
> 2. 解耦三件套：接口（通信）、EventAggregator（消息）、线程安全集合（缓冲）。
> 3. 性能优化点：缓存减少 90% Modbus 调用、批量入库提升 10 倍、复合索引提升查询。

> **🎤 面试应答模板**
> "完整数据流是这样的：Modbus 设备被 ICommunicationService 抽象（Simulator + ModbusTCP 两种实现）；DataAcquisitionService 后台循环读，按 Point 配置做字节序和工程量转换；结果写进 ConcurrentDictionary 缓存和 ConcurrentQueue 队列，Prism EventAggregator 发事件通知 UI；DataStorageService 定时把队列里的值批量入库 SQLite。AlarmService 应该在事件里检查限值，目前我代码里没接通，是下一步要做的事。报表服务从 SQLite 拉数据按小时/天/月聚合。整个链路用接口和事件解耦，测试和扩展都很方便。"

> **🛠 动手任务**
> 1. 把 `AlarmService` 接入 `PointValueUpdatedEvent`，跑一下看报警能不能触发。
> 2. 用 Stopwatch 测 `EnergyReportService` 在 100 万数据下的耗时，对比 SQL `GROUP BY` 方案。
> 3. 把 `AppDbContext` 改成 Scoped（`Register<AppDbContext>`），解决"长生命周期 DbContext"问题。

---

## 第 11 章　面试高频问答（必背）

### Q1：请介绍一下你的项目（3 分钟版）

> "我做了一个 HVAC 能源监控平台上位机软件，用 .NET 8 + WPF + Prism 9。系统能接入 Modbus 协议的 PLC/传感器，实时显示冷机功率、冷冻水温度等 4 类测点，刷新 1 秒一次。同时支持设备管理、点位配置、历史趋势、报警管理、能耗报表六大功能。技术上分四层：表现层 WPF 数据绑定 + ScottPlot 画曲线；服务层有 6 个 Service 用接口注入；基础设施层 EF Core + SQLite + 仓储工作单元；通信层用 NModbus 实现了 Simulator 和 ModbusTCP 两种。我重点解决了三个工程问题：一是异步多线程不卡 UI（采集线程 + 线程池入库 + UI 线程刷新），二是批量入库性能（ConcurrentQueue 缓冲 + 5 秒批量），三是字节序零分配解析（Span<T> + BinaryPrimitives）。"

### Q2：MVVM 是什么？项目里怎么用的？

> "MVVM 是 Model-View-ViewModel，通过数据绑定解耦 UI 和逻辑。我项目里 ViewModel 继承 Prism 的 BindableBase，SetProperty 自动通知 UI；Command 用 DelegateCommand 包装；View 没有任何业务代码只有 XAML。Prism 还提供了模块化、Region 导航、EventAggregator 这些增强。整个项目除了 ViewModel 没有其他类直接 new Service——全部构造注入。"

### Q3：你是怎么处理多线程的？

> "项目有三个线程：UI 线程跑 DispatcherTimer 500ms 刷新；后台采集线程是 Task 循环读 Modbus；线程池跑 DataStorageService 的 Timer 批量入库。三线程之间用两套机制通信：ConcurrentDictionary 做实时缓存，ConcurrentQueue 做入库缓冲，Prism EventAggregator 做消息总线。所有异步方法都接 CancellationToken，停止采集用协作式取消——StopAsync 调 cts.Cancel()，Task.Delay 抛 OperationCanceledException 退出循环。UI 集合修改必须 UI 线程，所以 ObservableCollection.Add 都在 DispatcherTimer.Tick 里。"

### Q4：MVVM 的双向绑定怎么实现？

> "双向绑定要实现 INotifyPropertyChanged 和 INotifyCollectionChanged。Prism 的 BindableBase 封装了前者，ObservableCollection 封装了后者。XAML 写 Mode=TwoWay 即可，绑定引擎自动监听 PropertyChanged 事件从 ViewModel 推到 UI，用户输入也自动从 UI 推到 ViewModel。我项目里 CurrentTime 这种一秒刷一次的就用 OneWay 即可。"

### Q5：async/await 原理？会死锁吗？

> "async/await 是状态机模式，编译器把 async 方法重写成状态机类，await 把方法挂起、释放线程、I/O 完成后回到线程池取线程继续。死锁常见于 .Result / .Wait() 在 UI 线程等 Task——线程被占、continuation 想回到 UI 线程但 UI 线程在等。避免方法：所有异步到异步、不用 .Result、用 ConfigureAwait(false)（库代码）或保留默认（UI 代码）。我项目里没有 .Result，所有 await 都在正确的线程。"

### Q6：EF Core 是怎么用的？和 ADO.NET 比呢？

> "我用 EF Core 做 ORM，DbContext + DbSet 映射实体，Fluent API 配索引和外键。仓储+工作单元包装了一层，业务层不直接接触 DbContext。和 ADO.NET 比：开发快、LINQ 强类型、迁移工具完善；缺点是生成的 SQL 不直观、性能有 overhead。性能关键路径（批量入库）我用 AddRangeAsync + 单 SaveChanges 走一个事务，比循环 Add 快 5~10 倍。"

### Q7：Modbus 协议了解吗？

> "Modbus 是工业最常见的协议，分 RTU（RS485 串口）和 TCP（以太网）两种。功能码 03 读保持寄存器、04 读输入寄存器，模拟量就用这两个。32 bit 数据要 2 个寄存器 + 字节序（4 种），我用 ByteOrderConverter 用 Span<T> 实现零分配解析。原始值要工程量转换：真实值=原始×Scale+Offset。我项目用 ICommunicationService 接口隔离协议，Simulator 和 ModbusTCP 两种实现。"

### Q8：项目里最难的技术难点？

> "两个点：① WPF 跨线程更新 UI——一开始我直接 ObservableCollection.Add 在采集线程里，结果偶发 InvalidOperationException。改成 DispatcherTimer 500ms 触发刷新、EventAggregator 自动 marshal、Timer 触发 FlushAsync 配 try/catch 才彻底解决。② 批量入库性能——开始每条 PointValue 一次 SaveChanges，CPU 100% 数据库锁等待；改成 ConcurrentQueue + 5s Timer 批量写，性能提升 10 倍。"

### Q9：项目里 OOP 怎么体现的？

> "四个原则都有体现：① SRP——每个 Service 只做一件事（采集、入库、报警、报表分开）；② OCP——ICommunicationService 接口，新增 OPC UA 不用改 DataAcquisitionService；③ DIP——所有 Service 通过接口注入；④ LSP——Simulator 和 ModbusTCP 都能替换。模式上有策略模式（通信实现）、观察者（EventAggregator）、UoW+Repository（数据访问）。"

### Q10：项目里有什么可以改进的？

> "我觉得有这些点可以提升：① EF Core Migrations 替代 EnsureCreated；② AlarmService 接入 PointValueUpdatedEvent，目前代码里没接通；③ 真实 ModbusTCP 寄存器读取集成 NModbus；④ 报表改用 SQL GROUP BY + 索引优化；⑤ AppDbContext 改 Scoped；⑥ 加 Polly 重试机制；⑦ 加单元测试覆盖；⑧ 用 Serilog 替代 NLog 做结构化日志。"

### Q11：上位机/下位机怎么理解？

> "上位机是 PC 端监控软件，下位机是 PLC/单片机/传感器等现场设备。工业自动化金字塔分四层：设备层 → 控制层（PLC/DCS）→ 监控层（SCADA/HMI）→ 企业层（ERP/MES）。我做的是监控层，用 Modbus/OPC UA 等协议和 PLC 通信，主要工作是数据采集、显示、报警、报表。下位机程序员一般做 PLC 编程（梯形图/ST 语言），和我做的不是一种事。"

### Q12：WPF 数据绑定原理？INotifyPropertyChanged 是什么？

> "数据绑定是 WPF 核心机制。XAML 写 {Binding Path}，WPF 解析后创建 BindingExpression，订阅源对象的 PropertyChanged 事件。源对象实现 INotifyPropertyChanged 接口，事件触发时绑定引擎用反射获取属性新值并更新目标 UI。Prism 的 BindableBase 封装了 SetProperty 方法，自动比对旧值、发事件，避免冗余通知。ObservableCollection 实现 INotifyCollectionChanged，所以增删元素 UI 也会更新。"

> **🔑 核心要点**
> 1. 项目介绍 3 分钟内讲完，要有"问题—方案—结果"逻辑。
> 2. 每个技术名词（C#/OOP/MVVM/异步/WPF/数据库/Modbus）都能展开讲 2~3 分钟。
> 3. 主动提改进点，展示工程思维。
> 4. 把"工业领域"讲清楚（上位机/SCADA/PLC），区分于普通业务开发。

---

## 第 12 章　学习路线与复盘清单

### 12.1 8 周学习计划（边复习边投简历）

| 周 | 主题 | 任务 | 产出 |
|---|---|---|---|
| 1 | C# 基础 | 重读 async/await、Span<T>、可空引用类型、表达式树 | 笔记 30 页 |
| 2 | OOP + 设计模式 | 23 种 GoF 模式，重点 6 种（工厂/策略/装饰/适配/观察者/单例） | 代码示例 |
| 3 | MVVM + Prism | 重写一个迷你项目用 Prism + Unity + EventAggregator | GitHub Demo |
| 4 | WPF 深入 | 复习 Dispatcher、数据绑定、值转换器、Style/Template | UI Demo |
| 5 | 异步多线程 | 改写项目为多设备并行 + 报警订阅 + 异常重试 | 提交 PR |
| 6 | EF Core + 数据库 | 加 Migrations、AsNoTracking、复合索引优化 | 性能报告 |
| 7 | Modbus + 工业 | Wireshark 抓包 + NModbus 补全真实通信 | Demo 视频 |
| 8 | 模拟面试 | 找朋友模拟、录音回放、补 Q11/Q12 | 面试录音 |

### 12.2 复盘清单（投递前自查）

- [ ] 能 3 分钟内讲完项目背景、技术栈、亮点
- [ ] 能画出"数据流总图"
- [ ] 能讲清 4 个 OOP 原则、3 个设计模式在项目里的体现
- [ ] 能讲清 MVVM 三件套、Prism 4 个机制
- [ ] 能讲清 3 个线程的角色和通信方式
- [ ] 能讲清 EF Core 怎么用、为什么用、哪里有性能问题
- [ ] 能讲清 Modbus 4 个功能码、字节序、工程量转换
- [ ] 能讲清 5 个改进点 + 自己打算怎么做
- [ ] 能手写 `Task.Run` + `async/await` + `CancellationToken` 简单例子
- [ ] 能手写 `ObservableCollection` 绑定 XAML 例子
- [ ] 能手写 `ICommunicationService` 接口和至少 1 个实现
- [ ] 能回答 Q1~Q12 任意 5 个

### 12.3 推荐学习资源

**官方文档**（首选）：
- [.NET 8 官方文档](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [Prism 官方文档](https://prismlibrary.com/docs/)
- [EF Core 官方文档](https://learn.microsoft.com/ef/core/)
- [Modbus 协议规范](https://modbus.org/specs.php)

**工业领域**：
- 《工业自动化系统集成》—— 项目基础
- 《WPF 编程宝典》—— WPF 圣经
- 《C# 12.0 本质论》—— C# 高级特性
- 《CLR via C#》—— 底层原理

**项目内**（建议加 1~2 个）：
- [CSharpFlink](https://github.com/zyq025/CSharpFlink) —— .NET 工业大数据
- [SharpSCADA](https://github.com/GavinYellow/SharpSCADA) —— 经典 SCADA 学习项目

### 12.4 秋招面试节奏建议

| 时间 | 动作 |
|---|---|
| 7 月 | 复习 C#/OOP/设计模式，做 Demo 项目 |
| 8 月 | 复习 WPF/MVVM/异步/数据库 |
| 9 月 | 复习 Modbus/工业领域，模拟面试 5 轮 |
| 10 月 | 投递简历（提前批/正式批） |
| 11 月 | 密集面试，记录每次问题补漏 |
| 12 月 | Offer 谈判 |

---

## 附录 A：项目文件清单（速查表）

| 类别 | 文件 |
|---|---|
| 入口 | [App.xaml.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/App.xaml.cs)、[Bootstrapper.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Bootstrapper.cs) |
| 模块 | [Modules/CoreModule.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Modules/CoreModule.cs) |
| 通信 | [ModbusTcpCommunicationService.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Communication/ModbusTcpCommunicationService.cs)、[SimulatorCommunicationService.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Communication/SimulatorCommunicationService.cs) |
| 采集 | [DataAcquisitionService.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Acquisition/DataAcquisitionService.cs) |
| 缓存 | [PointValueCache.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Cache/PointValueCache.cs) |
| 存储 | [DataStorageService.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Storage/DataStorageService.cs) |
| 报警 | [AlarmService.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Alarm/AlarmService.cs) |
| 报表 | [EnergyReportService.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Services/Report/EnergyReportService.cs) |
| 数据 | [AppDbContext.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/DbContext/AppDbContext.cs)、[Repository.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Repository/Repository.cs)、[UnitOfWork.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Repository/UnitOfWork.cs) |
| 工具 | [ByteOrderConverter.cs](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Infrastructure/Helpers/ByteOrderConverter.cs) |
| 模型 | [Models/Entities/](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Models/Entities/)、[Models/Enums/](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Models/Enums/) |
| 视图模型 | [ViewModels/](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/ViewModels/) |
| 视图 | [Views/](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Views/)、[Design/Styles.xaml](file:///d:/study/623WPFstudy/HVAC.EnergyMonitor/Design/Styles.xaml) |

## 附录 B：常见追问"杀伤力"清单

面试官可能追问的问题，按杀伤力排序：

| 杀伤力 | 问题 | 准备度 |
|---|---|---|
| ⭐⭐⭐⭐⭐ | Task.Delay 怎么实现可取消？ | 已准备 |
| ⭐⭐⭐⭐⭐ | DispatcherTimer 和 System.Timers.Timer 区别？ | 已准备 |
| ⭐⭐⭐⭐ | EF Core 的 N+1 问题怎么解决？ | 已准备 |
| ⭐⭐⭐⭐ | Modbus 大端小端你代码里怎么处理的？ | 已准备 |
| ⭐⭐⭐⭐ | 为什么用 ConcurrentQueue 而不是 Queue？ | 已准备 |
| ⭐⭐⭐⭐ | Prism IModule 怎么用的？为什么？ | 已准备 |
| ⭐⭐⭐ | 仓储模式的缺点？ | 已准备 |
| ⭐⭐⭐ | SQLite 的局限性？ | 已准备 |
| ⭐⭐⭐ | WPF 依赖属性的实现原理？ | 需补 |
| ⭐⭐ | 一致性哈希在工控里怎么用？ | 需补 |
| ⭐⭐ | gRPC vs Modbus vs OPC UA 选型？ | 需补 |

---

## 附录 C：5 个"我接下来打算做"的改进（面试亮点）

按工作量从小到大排序：

1. **报警接入**（半天）—— 让 `AlarmService` 订阅 `PointValueUpdatedEvent`，实现真正的告警链路。
2. **真实 ModbusTCP**（1 天）—— 用 NModbus 补全 `ModbusTcpCommunicationService.ReadHoldingRegistersAsync`。
3. **EF Core Migrations**（半天）—— 替代 `EnsureCreated`，改字段类型不用删库。
4. **单元测试**（3 天）—— 用 xUnit + Moq 给 Service 写测试，覆盖率 70%+。
5. **TimescaleDB 改造**（1 周）—— 替代 SQLite，存 10 年历史数据，添加连续聚合做实时报表。

> **面试话术**："接下来我准备分 5 步迭代项目，第一步是把报警链路接通，第二步是补全真实 Modbus 通信，第三步是迁移到 EF Core Migrations 解决改字段问题，第四步是写单元测试保障质量，第五步是引入时序数据库 TimescaleDB 应对长期数据。"

---

**最后一句话**：项目里**每一行代码都要能讲出来**——它解决什么问题、为什么这么写、有没有更好的方案。"我能写"和"我能讲"是两回事。祝你秋招顺利！
