# 技术上下文

版本: v0.5.0

- 运行环境: Windows 10/11 + Windows PowerShell 5.1 + .NET SDK 8.0.419
- 现有工具链: PowerShell + WinForms + `System.Drawing`
- 新增桌面运行时: C# `net8.0-windows` WinForms 应用 `src/AspenBurner.App`
- 核心脚本: `src/CrosshairOverlay.Core.psm1`
- 新核心迁移方向: `src/AspenBurner.App/Configuration` + `src/AspenBurner.App/Core`
- 测试框架: Pester 3.4.0 + MSTest (`tests/AspenBurner.App.Tests`)
- 依赖策略: 本轮优先使用 .NET 自带能力，不引入额外 UI 框架
