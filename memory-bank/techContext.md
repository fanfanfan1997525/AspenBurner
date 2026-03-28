# 技术上下文
版本: v0.5.0

- 运行环境: Windows 10/11, Windows PowerShell 5.1, .NET SDK 8.0.419
- 主运行时: C# 12, `net8.0-windows`, WinForms
- 兼容壳层: PowerShell 脚本 + `.cmd`
- 遥测相关:
  - `System.Diagnostics.PerformanceCounter`
  - `System.Management`
  - `Control Center` 反射接入
- 配置: `System.Text.Json`, 原子保存 `config\crosshair.json`
- 自动化测试: MSTest
- 发布物: `dist\AspenBurner\AspenBurner.exe`
