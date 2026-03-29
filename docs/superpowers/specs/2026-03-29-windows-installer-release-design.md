# AspenBurner Windows 安装器与发布设计

版本: v0.6.0  
日期: 2026-03-29

## 背景

当前仓库只具备 `dotnet publish` 级别的发布产物，没有 Windows 安装器、版本化 tag，也没有可复用的 Release 资产生成链路。

## 目标

1. 生成可安装到 Windows 的原生安装包。
2. 保留现有兼容入口：`Start/Configure/Stop`。
3. 让安装目录成为一等运行场景，而不是仅支持仓库目录。
4. 固化版本号、tag、Release 资产命名。

## 非目标

1. 不引入 MSIX/WiX 这类更重的打包体系。
2. 不改应用核心运行模型。
3. 不把配置迁移到云端或注册表。

## 方案

### 1. 安装器技术

使用 Inno Setup 6 生成传统 `.exe` 安装器。

理由：
- 依赖轻。
- 脚本可读。
- 支持开始菜单、桌面快捷方式、卸载。
- 适合当前 WinForms + PowerShell 兼容入口模型。

### 2. 安装形态

发布产物分为两类：

1. `portable zip`
2. `installer exe`

安装目录采用：

- `{localappdata}\Programs\AspenBurner`

理由：
- 避免把可写配置落到 `Program Files`
- 保持兼容脚本可直接管理本地 `config\crosshair.json`
- 安装/卸载不强依赖管理员权限

### 3. 发布产物内容

安装包和 portable 均包含：

- `AspenBurner.exe`
- `config\crosshair.json`
- `Start-Crosshair.cmd/.ps1`
- `Configure-Crosshair.cmd/.ps1`
- `Stop-Crosshair.cmd/.ps1`
- `AspenBurner.Cli.cmd/.ps1`
- `AspenBurner.Common.ps1`
- 运行所需脚本与依赖

### 4. 兼容脚本修正

现有 `Get-AspenBurnerExecutablePath` 只认仓库结构。  
需要补一条安装目录路径：

- `<root>\AspenBurner.exe`

否则安装后的 `Start/Configure/Stop` 会退化为无效壳。

### 5. 版本策略

本轮版本提升到 `v0.6.0`。

同步位置：
- `src/AspenBurner.App/AspenBurner.App.csproj`
- `README.md`
- `memory-bank/*.md`

### 6. 验证

至少完成：

1. `dotnet test`
2. `dotnet build -c Release`
3. `Build-Release.ps1` 生成 portable 与 installer
4. 安装器静默安装 smoke
5. 安装目录下 `Start/Configure/Stop` 链路 smoke
6. 静默卸载 smoke

