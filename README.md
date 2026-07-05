# HVAC Energy Monitor

一个基于 .NET 8 WPF 的暖通空调（HVAC）能源监控平台，采用 Prism 9 模块化架构与 MVVM 设计模式，支持 Modbus 通信（含仿真模式）、实时数据采集、SQLite 历史存储、趋势曲线展示与报警管理。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows%20Presentation%20Foundation-0078D4?logo=windows)
![Prism](https://img.shields.io/badge/Prism-9.0-3EABBE)
![SQLite](https://img.shields.io/badge/SQLite-EF%20Core%208-003B57?logo=sqlite)
![License](https://img.shields.io/badge/License-MIT-green.svg)

---

## 功能特性

- **工业级监控仪表盘**  
  实时展示测点数值、设备状态、采集状态与在线状态，支持数字看板与实时趋势曲线。

- **多协议通信支持**  
  内置 Modbus TCP / Modbus RTU 通信能力，并提供**仿真模式（Simulator）**，无需真实 PLC 即可运行与演示。

- **实时数据采集与缓存**  
  可配置的扫描周期，支持 ushort / short / uint / int / float 等多种数据类型，带量程缩放与偏移计算。

- **SQLite 历史数据存储**  
  基于 Entity Framework Core 8 的本地数据持久化，支持历史趋势查询与能源报表生成。

- **趋势曲线分析**  
  使用 ScottPlot.WPF 绘制冷机功率、冷冻水供回水温度等关键参数的历史曲线。

- **报警管理**  
  支持测点高低限报警规则配置与报警记录管理。

- **设备与测点配置**  
  提供设备参数、通信参数、测点地址、量程、单位、存储策略等配置视图。

- **模块化架构**  
  基于 Prism 的 Region 导航与依赖注入，便于后续扩展更多功能模块。

---

## 技术栈

| 层级 / 能力 | 技术选型 |
|-------------|----------|
| UI 框架 | .NET 8 WPF |
| MVVM 框架 | Prism.Unity 9 |
| 数据库 | SQLite + Entity Framework Core 8（主库）<br>SQL Server 2019+（可选热备镜像） |
| 工业通信 | NModbus 4 |
| 趋势图表 | ScottPlot.WPF 5 |
| 日志记录 | NLog |
| 图标资源 | MahApps.Metro.IconPacks |

---

## 项目结构

```text
HVAC.EnergyMonitor/
├── App.xaml / App.xaml.cs          # 应用程序入口
├── Bootstrapper.cs                 # Prism 引导与模块初始化
├── Modules/
│   └── CoreModule.cs               # 核心依赖注册与种子数据
├── Views/
│   ├── MainWindow.xaml             # 主窗口与导航菜单
│   ├── DashboardView.xaml          # 实时监控仪表盘
│   ├── HistoryTrendView.xaml       # 历史趋势分析
│   ├── EnergyReportView.xaml       # 能源报表
│   ├── AlarmView.xaml              # 报警管理
│   ├── DeviceConfigView.xaml       # 设备配置
│   └── PointConfigView.xaml        # 测点配置
├── ViewModels/                     # MVVM 视图模型
├── Services/
│   ├── Communication/              # Modbus / 仿真通信服务
│   ├── Acquisition/                # 数据采集调度服务
│   ├── Cache/                      # 实时测点缓存
│   ├── Storage/                    # 历史数据存储服务
│   ├── Alarm/                      # 报警服务
│   └── Report/                     # 能源报表服务
├── Infrastructure/
│   ├── DbContext/                  # EF Core 数据库上下文
│   ├── Repository/                 # 仓储与工作单元
│   └── Helpers/                    # 工具类
├── Models/
│   ├── Entities/                   # 数据库实体
│   ├── DTOs/                       # 数据传输对象
│   ├── Events/                     # Prism 事件聚合
│   └── Enums/                      # 枚举定义
├── Design/
│   └── Styles.xaml                 # 工业级 UI 样式资源
└── Converters/                     # XAML 值转换器
```

---

## 快速开始

### 环境要求

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 或 JetBrains Rider / VS Code

### 克隆与运行

```bash
git clone https://github.com/yourusername/HVAC.EnergyMonitor.git
cd HVAC.EnergyMonitor
```

使用 Visual Studio 打开 `HVAC.EnergyMonitor.sln`，将 `HVAC.EnergyMonitor` 设为启动项目后按 `F5` 运行；或在项目目录下执行：

```bash
dotnet build HVAC.EnergyMonitor.sln
dotnet run --project HVAC.EnergyMonitor/HVAC.EnergyMonitor.csproj
```

> 首次启动会自动创建 `hvac_energy_monitor.db` SQLite 数据库，并初始化一台仿真冷水机组与 4 个默认测点，可直接查看实时数据与趋势。

### 发布 Release 版本

```bash
dotnet publish HVAC.EnergyMonitor/HVAC.EnergyMonitor.csproj -c Release -o ./publish
```

---

## 默认仿真数据

程序首次运行时会自动写入以下默认测点（可通过测点配置视图修改）：

| 测点名称 | 寄存器地址 | 数据类型 | 单位 | 说明 |
|----------|------------|----------|------|------|
| 冷冻水供水温度 | 0 | UShort | °C | 量程缩放 0.1 |
| 冷冻水回水温度 | 1 | UShort | °C | 量程缩放 0.1 |
| 冷机功率 | 2 | UShort | kW | 量程缩放 1 |
| 冷却塔风机频率 | 3 | UShort | Hz | 量程缩放 0.1 |

---

## 配置说明

- **数据库连接字符串**：位于 `HVAC.EnergyMonitor/appsettings.json`。
  - `DefaultConnection`：SQLite 本地文件路径（默认 `hvac_energy_monitor.db`）
  - `SqlServerConnection`：SQL Server 连接串（可选，未配置则禁用镜像）
- **同步行为配置**（位于 `appsettings.json` 的 `AppSettings` 节）：
  - `SyncEnabled`：`true`/`false` 是否启用 SQL Server 镜像
  - `SyncIntervalSec`：同步轮询间隔（秒，默认 2）
  - `SyncBatchSize`：每批同步最大行数（默认 500）
- **通信模式切换**：在设备配置视图中，可将协议类型在 `Simulator`（仿真）与 `ModbusTCP`/`ModbusRTU` 之间切换。
- **扫描周期**：按设备配置，默认 1000 ms。
- **日志配置**：通过 `NLog.config` 控制文件与控制台输出级别。

---

## SQL Server 镜像

应用支持将本地 SQLite 数据异步同步到 SQL Server 作为**只写热备镜像**。SQLite 仍是主库，应用不读 SQL Server；SQL Server 不可达时业务功能完全不受影响。

### 启用步骤

1. 确保本机或网络内有可用的 SQL Server 实例（开发环境推荐 `SQLEXPRESS`）
2. 编辑 `HVAC.EnergyMonitor/appsettings.json`，填写 `ConnectionStrings.SqlServerConnection`，例如：
   ```json
   "SqlServerConnection": "Server=LAPTOP-BFI60JL1\\SQLEXPRESS;Database=hvacm_data;User Id=sa;Password=xxx;TrustServerCertificate=True;Encrypt=True;Connection Timeout=10;"
   ```
3. 确认 `AppSettings.SyncEnabled` 为 `true`（默认即 `true`）
4. 重新构建并启动应用：
   ```bash
   dotnet build
   dotnet run --project HVAC.EnergyMonitor
   ```
5. 启动时自动校验/建表 + 全量同步参考数据 + 启动增量同步

### 同步范围

| 表 | 同步 | 说明 |
|---|------|------|
| `PointValues` | ✅ | 实时采集数据（每 2 秒增量） |
| `AlarmRecords` | ✅ | 报警事件 |
| `Devices` | ✅ | 启动时全量一次 |
| `Points` | ✅ | 启动时全量一次 |
| `AlarmRules` | ✅ | 启动时全量一次 |
| `SyncStates` | ❌ | 本地水位线，不同步 |

### 工作原理

- **水位线机制**：本地 `SyncStates` 表记录 `PointValues` / `AlarmRecords` 已同步的最大 RowId
- **轮询 + 批量推送**：每 2 秒检查一次新行，按 `SyncBatchSize` 批量推送到 SQL Server
- **保留 RowId**：使用 `SET IDENTITY_INSERT` 显式保留 SQLite 端 RowId，重启后无重复行
- **事务包裹**：每次推送用 EF Core 事务保证 IDENTITY_INSERT + INSERT 原子性

### 故障降级

| 场景 | 应用行为 |
|------|----------|
| SQL Server 启动时不可达 | UI 正常打开，模拟数据正常滚动，日志显示 `SQL Server 不可达，跳过 schema 校验` |
| SQL Server 运行时不可达 | UI 正常，每 2 秒日志 `Tick 失败 (连续 N 次)`，SQLite 数据继续累积 |
| SQL Server 恢复 | 下一次 Tick 自动追上积压数据，无需重启 |
| 删除 SQL Server 表 | 日志 `Schema 验证失败，缺失表: XXX`，需手动重建表（`EnsureCreated` 不支持增量建表） |
| `SyncEnabled=false` | 跳过所有同步，业务功能与改造前一致 |

---

## 核心架构

```text
┌─────────────────────────────────────────────┐
│              WPF Views (XAML)               │
├─────────────────────────────────────────────┤
│           ViewModels (MVVM / Prism)         │
├─────────────────────────────────────────────┤
│  Services: Communication | Acquisition     │
│            Cache | Storage | Alarm | Report │
├─────────────────────────────────────────────┤
│        Infrastructure (EF Core / SQLite)    │
├─────────────────────────────────────────────┤
│        Models (Entities / DTOs / Events)    │
└─────────────────────────────────────────────┘
```

---

## 后续可扩展方向

- [ ] 接入真实 Modbus 设备，支持串口/RTU 参数配置
- [ ] 增加 OPC UA / BACnet 协议驱动
- [ ] 多语言国际化（i18n）
- [ ] 能源报表导出（Excel / PDF）
- [ ] 用户权限与登录模块
- [ ] 云端数据上传接口

---

## 贡献指南

1. Fork 本仓库
2. 创建功能分支：`git checkout -b feature/your-feature`
3. 提交变更：`git commit -m "feat: add your feature"`
4. 推送分支：`git push origin feature/your-feature`
5. 创建 Pull Request

---

## 许可证

本项目基于 [MIT License](LICENSE) 开源。

---

> 本项目为 HVAC 能源监控领域的学习与实践项目，UI 采用工业级科技风设计，适合作为 WPF + Prism + Modbus + SQLite 的完整参考示例。
