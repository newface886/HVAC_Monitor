# 项目自动化规则 v3.0

> 本文档定义了各类开发场景下的自动化技能调用链和关键要求，确保高效、高质量的交付。

---

## 一、工作流基础技能

### 1.1 技能发现与调用
**触发条件：** 开始任何对话或任务时

**技能链：**
```
1. using-superpowers → 检查并发现相关技能（必须首先调用）
```

**关键要求：**
- 如果有哪怕1%的可能性某个技能适用，必须调用该技能
- 技能调用优先级：流程技能 → 实现技能
- 用户指令优先级最高，技能次之，系统默认最低

### 1.2 创意设计探索
**触发条件：** 任何创意工作之前 - 创建功能、构建组件、添加功能、修改行为

**技能链：**
```
1. brainstorming → 探索需求、UI/UX设计、技术栈选择（必须首先调用）
2. writing-plans → 创建详细实施计划
```

**关键要求：**
- **硬性约束：** 在呈现设计并获得用户批准之前，不得调用任何实现技能、编写代码、搭建项目
- 必须遵循：探索项目上下文 → 提出澄清问题 → 提出2-3种方案 → 呈现设计 → 获取批准
- 设计文档保存到 `docs/superpowers/specs/YYYY-MM-DD-<topic>-design.md`

### 1.3 计划编写与执行
**触发条件：** 有规格或需求的多步骤任务，在编写代码之前

**技能链：**
```
1. writing-plans → 创建实施计划（在brainstorming之后）
2. [根据子代理可用性选择执行方式]
   - 有子代理 → subagent-driven-development
   - 无子代理 → executing-plans
```

**关键要求：**
- 计划保存到 `docs/superpowers/plans/YYYY-MM-DD-<feature-name>.md`
- 每个步骤是一个动作（2-5分钟）
- 遵循DRY、YAGNI、TDD原则，频繁提交

---

## 二、前端开发场景

### 2.1 网页/网站创建
**触发条件：** 用户要求创建网页、网站、前端应用、落地页、仪表盘等

**技能链：**
```
1. using-superpowers → 检查并发现相关技能
2. brainstorming → 确认需求、UI/UX设计、技术栈选择
3. frontend-dev → 创建高质量界面
4. webapp-testing → 全面测试前端功能
5. verification-before-completion → 验证所有功能正常
6. finishing-a-development-branch → 完成交付
```

**关键要求：**
- 必须使用 `frontend-dev` 技能确保界面质量
- 响应式设计，支持移动端适配
- **禁止使用占位符URL**（unsplash、picsum等）
- 所有媒体资源必须生成本地文件
- 交付前必须通过 `webapp-testing` 验证

### 2.2 前端组件开发
**触发条件：** 用户要求开发单个组件、UI库、设计系统

**技能链：**
```
1. brainstorming → 确认组件API、交互行为
2. frontend-dev → 设计组件样式和交互
3. playwright-expert → 编写组件E2E测试
4. code-reviewer → 代码质量审查
```

**关键要求：**
- 组件必须支持主题定制
- 提供完整的Props类型定义
- 包含使用示例和文档

---

## 三、后端开发场景

### 3.1 Python API/服务开发
**触发条件：** 用户要求开发Python后端、API、微服务、脚本

**技能链：**
```
1. using-superpowers → 发现Python相关技能
2. brainstorming → 确认架构设计、API规范
3. python-pro → 实现类型安全、异步代码
4. systematic-debugging → 处理开发中的错误
5. code-reviewer → 代码审查
6. verification-before-completion → 验证功能
```

**关键要求：**
- 必须使用类型注解（Type Hints）
- 配置 mypy 严格模式检查
- 实现完整的错误处理和日志记录
- 编写 pytest 测试套件
- 使用 black 和 ruff 格式化代码

### 3.2 C#/.NET 开发
**触发条件：** 用户要求开发C#应用、ASP.NET Core API、Blazor应用

**技能链：**
```
1. csharp-developer → 自动应用.NET最佳实践
2. brainstorming → 确认架构模式（CQRS、Clean Architecture等）
3. webapp-testing → API端点测试
4. code-reviewer → 代码审查
```

**关键要求：**
- 遵循 .NET 8+ 最佳实践
- 使用依赖注入和配置模式
- 实现异步编程模式，始终接受和转发 `CancellationToken`
- 配置 Entity Framework Core 数据访问
- 编写单元测试和集成测试

---

## 四、数据科学场景

### 4.1 数据分析/处理
**触发条件：** 用户要求数据分析、数据清洗、数据转换、报表生成

**技能链：**
```
1. using-superpowers → 发现数据处理技能
2. brainstorming → 确认数据源、分析目标、输出格式
3. pandas-pro → 执行DataFrame操作
4. scientific-figure-pro → 生成可视化图表（如需要）
5. verification-before-completion → 验证数据准确性
```

**关键要求：**
- 处理大数据集时注意内存优化
- 保留数据处理日志和中间结果
- 输出数据质量报告
- 使用类型安全的 pandas 操作

### 4.2 机器学习实验
**触发条件：** 用户要求ML模型训练、特征工程、模型评估、实验

**技能链：**
```
1. using-superpowers → 发现ML相关技能
2. brainstorming → 确认数据集、模型类型、评估指标、实验目标
3. ml-pipeline → 配置实验跟踪和管道
4. pandas-pro → 数据预处理和特征工程
5. subagent-driven-development → 并行执行实验任务
6. systematic-debugging → 处理训练错误
7. verification-before-completion → 验证模型性能
8. finishing-a-development-branch → 整理实验报告
```

**关键要求：**
- **必须设置随机种子**保证可复现性
- 自动检查数据泄露问题
- 使用 MLflow 或 W&B 跟踪实验
- 保存模型、预处理管道和完整配置
- 生成实验报告包含：数据分布、特征重要性、模型性能

### 4.3 大数据处理（Spark）
**触发条件：** 用户要求处理大规模数据、分布式计算、Spark作业

**技能链：**
```
1. spark-engineer → 自动应用Spark最佳实践
2. brainstorming → 确认数据处理逻辑、性能要求
3. verification-before-completion → 验证处理结果
```

**关键要求：**
- 优化 DataFrame API 使用
- 配置合理的分区策略
- 实现容错和检查点机制
- 监控资源使用和性能

---

## 五、测试与质量场景

### 5.1 E2E测试开发
**触发条件：** 用户要求编写E2E测试、UI自动化测试、浏览器测试

**技能链：**
```
1. playwright-expert → 自动应用Playwright最佳实践
2. brainstorming → 确认测试覆盖范围、关键用户流程
3. webapp-testing → 执行测试验证
```

**关键要求：**
- 使用 Page Object Model 模式
- 配置测试报告和截图
- 处理异步操作和等待策略
- 配置 CI/CD 集成

### 5.2 Web应用测试
**触发条件：** 需要测试本地Web应用、验证前端功能、调试UI行为

**技能链：**
```
1. webapp-testing → 使用Playwright进行自动化测试
```

**关键要求：**
- 静态HTML：直接读取文件识别选择器
- 动态应用：等待 `networkidle` 后再检查DOM
- 使用 `scripts/with_server.py` 管理服务器生命周期

### 5.3 Bug修复/调试
**触发条件：** 用户报告bug、测试失败、意外行为

**技能链：**
```
1. systematic-debugging → 系统化诊断问题
2. [根据问题类型调用相应开发技能]
3. verification-before-completion → 验证修复有效
4. code-reviewer → 审查修复代码
```

**关键要求：**
- **铁律：没有根本原因调查就不能提出修复方案**
- 先诊断根因再修复
- 添加回归测试防止问题复发
- 记录问题原因和解决方案

### 5.4 代码审查
**触发条件：** 用户要求审查代码、PR审查、代码质量检查

**技能链：**
```
1. code-reviewer → 执行全面代码审查
2. receiving-code-review → 处理审查反馈（如用户是被审查方）
```

**审查维度：**
- 正确性：逻辑错误、边界条件
- 安全性：SQL注入、XSS、敏感数据处理
- 性能：N+1查询、内存泄漏
- 可维护性：命名、代码结构、重复代码
- 测试覆盖：单元测试、集成测试

### 5.5 请求代码审查
**触发条件：** 完成任务、实现主要功能、或合并前验证工作

**技能链：**
```
1. requesting-code-review → 调度code-reviewer子代理
```

**关键要求：**
- 在子代理驱动开发中每个任务后必须审查
- 完成主要功能后必须审查
- 合并到main前必须审查

---

## 六、文档生成场景

### 6.1 Word文档生成
**触发条件：** 用户要求生成报告、文档、信函、.docx文件

**技能链：**
```
1. docx → 自动处理Word文档
2. theme-factory → 应用文档主题样式（如需要）
```

**关键要求：**
- 使用专业格式和排版
- 支持目录、页码、页眉页脚
- 处理图片、表格、图表

### 6.2 演示文稿生成
**触发条件：** 用户要求生成PPT、演示文稿、幻灯片、.pptx文件

**技能链：**
```
1. pptx → 自动处理PowerPoint文件
2. pptx-generator → 从头创建演示文稿
3. theme-factory → 应用演示主题样式
```

**关键要求：**
- 统一的视觉风格
- 合理的内容布局
- 支持动画和过渡效果
- 必须进行QA验证

### 6.3 科学图表生成
**触发条件：** 用户要求生成论文图表、学术图表、出版级图表

**技能链：**
```
1. scientific-figure-pro → 生成出版级图表
2. theme-factory → 应用图表主题
```

**关键要求：**
- 遵循学术出版标准
- 高分辨率输出（300 DPI+）
- 一致的字体和配色方案

### 6.4 内容编辑审查
**触发条件：** 用户要求审查博客文章、营销文案、技术文档、邮件等内容的风格和质量

**技能链：**
```
1. every-style-editor → 执行四阶段编辑审查
```

**关键要求：**
- 遵循四阶段审查：初步评估 → 详细行编辑 → 机械审查 → 建议
- 检查语法、标点、风格指南合规性
- 提供具体可操作的改进建议

---

## 七、流程管理场景

### 7.1 复杂多步骤任务
**触发条件：** 用户要求执行包含多个独立步骤的任务

**技能链：**
```
1. brainstorming → 拆解任务、确认依赖关系
2. dispatching-parallel-agents → 并行执行独立任务
3. verification-before-completion → 验证所有任务完成
```

**关键要求：**
- 识别可并行化的任务
- 正确处理任务间依赖
- 汇总并报告所有结果

### 7.2 子代理驱动开发
**触发条件：** 在当前会话中执行有独立任务的实施计划

**技能链：**
```
1. subagent-driven-development → 每个任务调度新子代理
2. [两阶段审查：规格合规 → 代码质量]
3. finishing-a-development-branch → 完成开发
```

**关键要求：**
- 每个任务一个新子代理
- 两阶段审查：规格合规审查 → 代码质量审查
- 审查发现问题必须修复后重新审查

### 7.3 实施计划执行
**触发条件：** 用户提供了详细的实施计划需要执行

**技能链：**
```
1. executing-plans → 在独立会话中执行计划
2. [根据计划步骤调用相应技能]
3. verification-before-completion → 验证计划完成
```

**关键要求：**
- 按顺序执行计划步骤
- 在检查点进行验证
- 报告执行进度和结果

### 7.4 开发完成交付
**触发条件：** 开发工作完成，需要整合交付

**技能链：**
```
1. verification-before-completion → 最终验证
2. code-reviewer → 最终代码审查
3. finishing-a-development-branch → 整理并交付
```

**关键要求：**
- 所有测试必须通过
- 代码审查无阻塞性问题
- 更新相关文档
- 生成变更日志

---

## 八、技能开发场景

### 8.1 创建新技能
**触发条件：** 用户要求创建新技能、添加技能

**技能链：**
```
1. skill-creator → 必须首先调用
2. brainstorming → 设计技能功能和触发条件
3. writing-skills → 编写技能定义（TDD方式）
4. verification-before-completion → 验证技能可用
```

**关键要求：**
- **铁律：没有失败的测试就不能创建技能**
- 遵循RED-GREEN-REFACTOR循环
- 明确的触发条件描述
- 完整的功能说明
- 包含使用示例

---

## 九、通用规则

### 9.1 技能调用优先级
1. **首先检查** - 使用 `using-superpowers` 发现相关技能
2. **创意任务** - 必须先使用 `brainstorming` 探索需求
3. **开发任务** - 使用对应的语言/框架技能
4. **质量保证** - 使用 `code-reviewer` 和 `verification-before-completion`
5. **完成交付** - 使用 `finishing-a-development-branch`

### 9.2 强制验证点
以下场景必须调用 `verification-before-completion`：
- 功能开发完成时
- Bug修复后
- 代码重构后
- 提交/合并前
- 任何声称完成/成功之前

**铁律：没有新鲜验证证据就不能声称完成**

### 9.3 调试规则
遇到以下情况必须使用 `systematic-debugging`：
- 测试失败
- 运行时错误
- 意外行为
- 性能问题

**铁律：没有根本原因调查就不能提出修复方案**

### 9.4 代码审查规则
- **主动审查**：完成重要功能后自动调用 `code-reviewer`
- **接收反馈**：收到审查意见时使用 `receiving-code-review` 处理
- **请求审查**：用户要求审查时使用 `requesting-code-review`

### 9.5 接收审查反馈规则
使用 `receiving-code-review` 时：
- **禁止**：表演性同意（"你说得对！"、"很好的观点！"）
- **必须**：技术验证 → 评估 → 技术确认或合理反驳
- 不清楚的项目必须先澄清再实施

---

## 十、场景决策树

```
用户请求
    │
    ├─ 开始任何任务 → [using-superpowers]
    │
    ├─ 创意工作/新功能 → [brainstorming] → [writing-plans]
    │
    ├─ 创建网页/前端应用 → [前端开发场景]
    │
    ├─ 开发API/后端服务
    │   ├─ Python → [Python API开发]
    │   └─ C#/.NET → [C#/.NET开发]
    │
    ├─ 数据相关
    │   ├─ 分析/处理 → [数据分析场景]
    │   ├─ ML/模型 → [ML实验场景]
    │   └─ 大数据/Spark → [大数据处理场景]
    │
    ├─ 测试相关
    │   ├─ E2E测试 → [E2E测试开发]
    │   ├─ Web应用测试 → [Web应用测试]
    │   └─ Bug修复 → [Bug修复场景]
    │
    ├─ 文档相关
    │   ├─ Word → [Word文档生成]
    │   ├─ PPT → [演示文稿生成]
    │   ├─ 科学图表 → [科学图表生成]
    │   └─ 内容审查 → [内容编辑审查]
    │
    ├─ 代码审查 → [代码审查场景]
    │
    ├─ 创建技能 → [技能开发场景]
    │
    ├─ 多步骤任务 → [流程管理场景]
    │
    └─ 完成工作 → [verification-before-completion] → [finishing-a-development-branch]
```

---

## 十一、技能完整清单

| 技能名称 | 类型 | 触发条件 |
|---------|------|---------|
| using-superpowers | 流程 | 开始任何对话或任务 |
| brainstorming | 流程 | 任何创意工作之前 |
| writing-plans | 流程 | 有规格的多步骤任务 |
| executing-plans | 流程 | 执行实施计划（无子代理） |
| subagent-driven-development | 流程 | 执行实施计划（有子代理） |
| dispatching-parallel-agents | 流程 | 2+独立任务可并行 |
| finishing-a-development-branch | 流程 | 开发完成需要交付 |
| verification-before-completion | 质量 | 声称完成之前 |
| systematic-debugging | 质量 | Bug、测试失败、意外行为 |
| code-reviewer | 质量 | 审查代码、PR审查 |
| requesting-code-review | 质量 | 完成任务后请求审查 |
| receiving-code-review | 质量 | 接收审查反馈 |
| python-pro | 开发 | Python 3.11+应用开发 |
| csharp-developer | 开发 | C#/.NET应用开发 |
| frontend-dev | 开发 | 前端网页开发 |
| pandas-pro | 数据 | DataFrame操作 |
| spark-engineer | 数据 | Spark大数据处理 |
| ml-pipeline | 数据 | ML管道基础设施 |
| scientific-figure-pro | 数据 | 科学图表生成 |
| playwright-expert | 测试 | Playwright E2E测试 |
| webapp-testing | 测试 | Web应用测试 |
| docx | 文档 | Word文档处理 |
| pptx | 文档 | PowerPoint处理 |
| pptx-generator | 文档 | PowerPoint生成 |
| every-style-editor | 文档 | 内容风格编辑 |
| writing-skills | 元 | 创建/编辑技能 |

---

## 十二、版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v3.0 | 2026-03-24 | 全面重构，整合26个技能，新增工作流基础技能、内容编辑审查、Web应用测试等场景 |
| v2.0 | 2026-03-18 | 全面重构，新增8大场景，覆盖25个技能 |
| v1.0 | - | 初始版本，包含网页创建和ML实验场景 |

---

## 十三、本机环境配置

### 13.1 Playwright 及 Chromium 浏览器位置

> 以下为本机已安装的 Playwright 及 Chromium 浏览器路径，后续涉及浏览器自动化的任务可直接使用，无需重复安装。

| 组件 | 路径 |
|------|------|
| **Playwright Python 包** | `C:\Users\fang rui\AppData\Roaming\Python\Python313\site-packages\playwright` |
| **Chromium 浏览器主目录** | `C:\Users\fang rui\AppData\Local\ms-playwright\chromium-1200\` |
| **Chromium 可执行文件** | `C:\Users\fang rui\AppData\Local\ms-playwright\chromium-1200\chrome-win64\chrome.exe` |
| **Headless Shell** | `C:\Users\fang rui\AppData\Local\ms-playwright\chromium_headless_shell-1200\` |
| **Playwright 浏览器根目录** | `C:\Users\fang rui\AppData\Local\ms-playwright\` |

#### 完整目录结构

```
C:\Users\fang rui\AppData\Local\ms-playwright\
├── .links/                          # 链接文件
├── chromium-1200/                   # Chromium 浏览器主目录
│   └── chrome-win64/
│       └── chrome.exe               # 浏览器可执行文件
├── chromium_headless_shell-1200/    # 无头模式专用
├── ffmpeg-1011/                     # 视频处理
├── winldd-1007/                     # Windows 依赖
└── __dirlock                        # 目录锁文件
```

#### 验证命令

```bash
python -c "from playwright.sync_api import sync_playwright; p = sync_playwright().start(); print(p.chromium.executable_path); p.stop()"
```

#### 注意事项
- 本环境配置最后更新：2026-04-19
- 如遇浏览器版本不匹配，执行 `pip install --upgrade playwright && playwright install chromium`
- 代码中可通过 `executable_path` 参数显式指定浏览器路径

### 13.2 OfficeCLI 工具位置

> 以下为本机已安装的 OfficeCLI 工具路径，用于 .docx/.xlsx/.pptx 文档处理，无需重复安装。

| 组件 | 路径 |
|------|------|
| **OfficeCLI 可执行文件** | `C:\Users\fang rui\AppData\Local\OfficeCli\officecli.exe` |
| **OfficeCLI 目录** | `C:\Users\fang rui\AppData\Local\OfficeCli` |

#### 使用方式

```bash
# PowerShell 中临时添加到 PATH
$env:Path += ";C:\Users\fang rui\AppData\Local\OfficeCli"

# 验证安装
officecli --version
```

#### 注意事项
- 本环境配置最后更新：2026-05-10
- 如未找到命令，请检查上述路径是否存在 officecli.exe
- 涉及 Word/Excel/PowerPoint 自动化处理时优先使用此工具

### 13.3 GPU / CUDA / PyTorch 深度学习环境

> 以下为本机 GPU 加速深度学习环境配置，涉及 PyTorch 模型训练、RL 训练等任务时直接使用，无需重复查询。

#### 硬件信息

| 组件 | 信息 |
|------|------|
| **GPU** | NVIDIA GeForce RTX 4060 Laptop GPU |
| **VRAM** | 8.0 GB (8188 MiB) |
| **Compute Capability** | 8.9 (Ada Lovelace) |
| **NVIDIA Driver** | 566.26 |
| **CUDA Toolkit (系统)** | 12.7 |

#### Conda 环境

| 项目 | 值 |
|------|-----|
| **环境名称** | `pytorch_env` |
| **Python 路径** | `C:\Users\fang rui\.conda\envs\pytorch_env\python.exe` |
| **Python 版本** | 3.10.20 (Anaconda) |
| **Conda 版本** | 25.5.1 |

#### 核心依赖版本

| 包 | 版本 | 备注 |
|---|------|------|
| **PyTorch** | 2.6.0+cu124 | CUDA 12.4 编译版 |
| **CUDA (PyTorch 内置)** | 12.4 | 无需单独安装 CUDA Toolkit |
| **cuDNN** | 90100 (9.1.0) | PyTorch 内置 |
| **Gymnasium** | 1.2.3 | RL 环境接口 |
| **Stable-Baselines3** | 2.7.1 | RL 算法库 |
| **Pandas** | 2.3.3 | 数据处理 |
| **NumPy** | 2.2.6 | 数值计算 |
| **scikit-learn** | 1.7.2 | 机器学习 |
| **Matplotlib** | 3.10.8 | 可视化 |
| **Optuna** | 4.8.0 | 超参数调优 |
| **TensorBoard** | 2.20.0 | 训练监控 |
| **pytest** | 9.0.3 | 测试框架 |

#### PowerShell 中使用此环境

```powershell
# 方式1：直接调用 Python（推荐，避免 conda run 路径空格问题）
$pypath = 'C:\Users\fang rui\.conda\envs\pytorch_env\python.exe'
& $pypath -c "import torch; print(torch.cuda.is_available())"

# 方式2：运行脚本
& $pypath scripts/train_dynamics.py

# 方式3：运行 pytest
& $pypath -m pytest tests/ -v

# 方式4：激活 conda 环境后使用
conda activate pytorch_env
python -c "import torch; print(torch.cuda.is_available())"
```

#### Python 代码中 GPU 使用模式

```python
import torch

# 自动检测设备（本项目 src/utils.py 中 get_device() 已封装）
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# 模型移至 GPU
model = model.to(device)

# 数据移至 GPU
x_batch = x_batch.to(device)
y_batch = y_batch.to(device)

# 推理时使用 no_grad
with torch.no_grad():
    pred = model(x)

# 加载模型权重到 GPU
model.load_state_dict(torch.load("model.pth", map_location=device, weights_only=True))
```

#### VRAM 使用注意事项

- **8GB VRAM 限制**：训练 LSTM (hidden_dim=256, 3层) 约占 0.5GB，SAC (net_arch=[256,256]) 约占 1-2GB
- **batch_size 建议**：LSTM ≤ 512，SAC ≤ 256，PPO ≤ 64 (n_steps=2048)
- **如遇 OOM**：降低 batch_size、hidden_dim 或 net_arch 维度
- **监控 VRAM**：`nvidia-smi` 或 `torch.cuda.memory_allocated() / 1024**2` (MB)

#### 验证命令

```powershell
# 一键验证 GPU 环境
$pypath = 'C:\Users\fang rui\.conda\envs\pytorch_env\python.exe'
& $pypath -c "import torch; print(f'PyTorch: {torch.__version__}'); print(f'CUDA: {torch.cuda.is_available()}'); print(f'GPU: {torch.cuda.get_device_name(0)}'); print(f'VRAM: {torch.cuda.get_device_properties(0).total_memory / 1024**3:.1f} GB')"
```

Expected output:
```
PyTorch: 2.6.0+cu124
CUDA: True
GPU: NVIDIA GeForce RTX 4060 Laptop GPU
VRAM: 8.0 GB
```

#### 注意事项
- 本环境配置最后更新：2026-05-25
- **路径含空格**：`C:\Users\fang rui\...` 在 PowerShell 中必须用引号包裹或赋值给变量后用 `&` 调用
- **conda run 不可靠**：路径含空格时 `conda run -n pytorch_env` 可能失败，优先使用直接调用 python.exe 的方式
- **CUDA 版本对应**：PyTorch 2.6.0+cu124 内置 CUDA 12.4 运行时，无需系统安装 CUDA Toolkit（系统有 12.7 但不影响）
- **如需升级 PyTorch**：`& $pypath -m pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu126`
