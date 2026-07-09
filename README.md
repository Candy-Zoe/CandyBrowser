# CandyZoe 浏览器

一款基于 WPF + WebView2 的现代浏览器，采用 MVVM 架构，支持多标签页、书签管理、历史记录、密码管理等功能。

## 功能特性

- **多标签页浏览** — 每个标签页独立 WebView2 实例，切换不丢失页面状态
- **新标签页页** — 搜索框 + 快速链接，输入网址或关键词即可导航
- **地址栏搜索建议** — 输入时自动匹配历史记录和书签
- **书签管理** — 支持添加、编辑、删除书签，文件夹分类，书签栏快捷访问
- **浏览历史记录** — 按时间分组显示，支持搜索
- **密码管理器** — AES 加密存储，支持密码生成
- **下载管理器** — 下载进度追踪、暂停/继续/取消
- **扩展支持** — 基础扩展安装管理
- **阅读模式** — 多主题阅读视图
- **PDF 阅读器** — 内置 PDF 查看
- **设置面板** — 主页、搜索引擎、外观、隐私安全等配置

## 技术栈

- **框架**: .NET 8 + WPF
- **浏览器内核**: WebView2 (Edge Chromium)
- **架构**: MVVM (CommunityToolkit.Mvvm)
- **数据持久化**: Entity Framework Core + SQLite
- **依赖注入**: Microsoft.Extensions.DependencyInjection

## 项目结构

```
src/
├── CandyBrowser.Core/           # 核心模型和枚举
├── CandyBrowser.Data/           # EF Core 实体和数据库上下文
├── CandyBrowser.Services/       # 业务逻辑服务
│   ├── Bookmarks/               # 书签服务
│   ├── History/                 # 历史记录服务
│   ├── Passwords/               # 密码管理服务
│   ├── Tabs/                    # 标签页服务
│   ├── Navigation/              # 导航服务
│   ├── Settings/                # 设置服务
│   ├── Downloads/               # 下载服务
│   ├── Extensions/              # 扩展服务
│   ├── Reading/                 # 阅读模式服务
│   └── PDF/                     # PDF 服务
├── CandyBrowser.Shared.Abstractions/  # 接口定义
└── CandyBrowser.Windows/        # WPF 界面层
    ├── Views/                   # 视图
    ├── ViewModels/              # 视图模型
    ├── Themes/                  # 主题样式
    └── Services/                # 平台特定服务
```

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+T | 新建标签页 |
| Ctrl+W | 关闭当前标签页 |
| Ctrl+L | 聚焦地址栏 |
| Ctrl+D | 添加书签 |
| F5 | 刷新页面 |
| F12 | 打开开发者工具 |
| Alt+← | 后退 |
| Alt+→ | 前进 |

## 构建和运行

```bash
# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行
dotnet run --project src/CandyBrowser.Windows
```

## 运行要求

- Windows 10/11
- .NET 8 SDK
- WebView2 Runtime（Windows 10/11 通常已预装）
