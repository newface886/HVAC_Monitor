# Playwright 与 Chrome 浏览器配置指导文档

## 1. 项目概述

本项目使用 **Playwright** 作为浏览器自动化工具，调用 **Chromium** 浏览器进行网页图片爬取。

---

## 2. Playwright 安装信息

### 2.1 Python 包信息
| 属性 | 值 |
|------|-----|
| 包名 | `playwright` |
| 版本 | `1.57.0` |
| 安装位置 | `C:\Users\fang rui\AppData\Roaming\Python\Python313\site-packages` |
| 依赖包 | `greenlet`, `pyee` |
| 官方仓库 | https://github.com/Microsoft/playwright-python |

### 2.2 安装命令
```bash
pip install playwright>=1.40.0
```

### 2.3 安装浏览器（如未安装）
```bash
# 安装 Chromium 浏览器
playwright install chromium

# 或安装所有浏览器
playwright install
```

---

## 3. Chrome/Chromium 浏览器位置

### 3.1 浏览器可执行文件路径
```
C:\Users\fang rui\AppData\Local\ms-playwright\chromium-1200\chrome-win64\chrome.exe
```

### 3.2 Playwright 浏览器目录结构
```
C:\Users\fang rui\AppData\Local\ms-playwright\
├── .links/                          # 链接文件
├── chromium-1200/                   # Chromium 浏览器主目录
│   └── chrome-win64/
│       └── chrome.exe               # 浏览器可执行文件
├── chromium_headless_shell-1200/    # 无头模式专用浏览器
├── ffmpeg-1011/                     # 视频处理工具
├── winldd-1007/                     # Windows 依赖工具
└── __dirlock                        # 目录锁文件
```

---

## 4. 项目中 Playwright 的使用

### 4.1 主要使用文件

| 文件 | 用途 | 实现方式 |
|------|------|----------|
| `cli.py` | 命令行版本爬虫 | 异步 API (`async_playwright`) |
| `image_crawler_playwright.py` | GUI 版本爬虫 | 同步 API (`sync_playwright`) |

### 4.2 浏览器启动配置

#### 同步版本（GUI）
```python
from playwright.sync_api import sync_playwright

self._playwright = sync_playwright().start()

self.browser = self._playwright.chromium.launch(
    headless=self.headless,
    args=[
        '--no-sandbox',
        '--disable-dev-shm-usage',
        '--disable-gpu',
        '--disable-extensions',
        '--disable-software-rasterizer',
        '--disable-webgl',
        '--disable-background-timer-throttling',
        '--disable-renderer-backgrounding',
        '--disable-backgrounding-occluded-windows',
        '--disable-features=VizDisplayCompositor',
        '--memory-pressure-off',
        '--window-size=1920,1080',
        '--start-maximized'
    ]
)
```

#### 异步版本（CLI）
```python
from playwright.async_api import async_playwright

async with async_playwright() as p:
    browser = await p.chromium.launch(
        headless=self.headless,
        args=[
            '--no-sandbox',
            '--disable-dev-shm-usage',
            '--disable-gpu',
            '--disable-extensions',
            '--window-size=1920,1080'
        ]
    )
```

---

## 5. 依赖文件

### 5.1 requirements.txt
```
# 核心依赖
playwright>=1.40.0
aiofiles>=23.0.0
psutil>=5.9.0
```

---

## 6. 运行前检查清单

### 6.1 安装依赖
```bash
pip install -r requirements.txt
```

### 6.2 安装浏览器（首次使用）
```bash
playwright install chromium
```

### 6.3 验证安装
```python
python -c "from playwright.sync_api import sync_playwright; p = sync_playwright().start(); print(p.chromium.executable_path); p.stop()"
```

预期输出：
```
C:\Users\fang rui\AppData\Local\ms-playwright\chromium-1200\chrome-win64\chrome.exe
```

---

## 7. 常见问题

### 7.1 浏览器未找到
如果提示浏览器未安装，运行：
```bash
playwright install chromium
```

### 7.2 版本不匹配
如果 Playwright 版本与浏览器版本不匹配，尝试：
```bash
pip install --upgrade playwright
playwright install chromium
```

### 7.3 手动指定浏览器路径（可选）
如需手动指定浏览器路径，可在代码中设置：
```python
browser = p.chromium.launch(
    executable_path=r"C:\Users\fang rui\AppData\Local\ms-playwright\chromium-1200\chrome-win64\chrome.exe",
    headless=False
)
```

---

## 8. 项目文件结构

```
d:\study\lingshi\
├── cli.py                          # 命令行版本（异步 Playwright）
├── image_crawler_playwright.py     # GUI 版本（同步 Playwright）
├── requirements.txt                # Python 依赖
├── config_playwright.json          # 爬虫配置文件
└── PLAYWRIGHT_GUIDE.md             # 本指导文档
```

---

## 9. 使用示例

### 9.1 启动 GUI 版本
```bash
python image_crawler_playwright.py
```

### 9.2 启动命令行版本
```bash
# 单个 AID
python cli.py --aid 342901

# 多个 AID
python cli.py --aid 342901,342902,342903

# 从文件读取 AID
python cli.py --file aids.txt

# 无头模式
python cli.py --aid 342901 --headless
```

---

## 10. 参考链接

- [Playwright Python 文档](https://playwright.dev/python/)
- [Playwright GitHub](https://github.com/Microsoft/playwright-python)
- [Chromium 项目](https://www.chromium.org/)
