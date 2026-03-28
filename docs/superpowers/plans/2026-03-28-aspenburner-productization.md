# AspenBurner 产品化实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 AspenBurner 从 PowerShell 脚本工具重构为一个可运行的 Windows 专用桌面软件，并保持现有入口兼容。

**Architecture:** 采用 `C# WinForms + 单实例应用上下文 + 托盘 + 透明 overlay + 设置窗体 + JSON 配置兼容层`。PowerShell 脚本保留为兼容入口，不再承载主运行时。

**Tech Stack:** .NET 8 SDK, C# 12, Windows Forms, Win32 P/Invoke, System.Text.Json, MSTest/xUnit 级单元测试

---

### Task 0: 建立迁移底座

**Files:**
- Modify: `memory-bank/techContext.md`
- Create: `docs/contracts/legacy-runtime-contract.md`
- Create: `docs/contracts/legacy-entrypoint-contract.md`
- Create: `docs/contracts/legacy-elevation-contract.md`

- [ ] **Step 1: 安装并验证 .NET SDK 可用**
- [ ] **Step 2: 记录当前仓库技术基线与新增运行时基线**
- [ ] **Step 3: 写清旧入口、UAC、退出语义、配置路径、目标进程显示规则等兼容契约**
- [ ] **Step 3.1: 明确 `.ps1` 与 `.cmd` 都是正式兼容入口，不降级为临时内部脚本**
- [ ] **Step 4: 提交**

### Task 1: 建立桌面应用工程与测试工程

**Files:**
- Create: `src/AspenBurner.App/AspenBurner.App.csproj`
- Create: `src/AspenBurner.App/Program.cs`
- Create: `src/AspenBurner.App/App.manifest`
- Create: `tests/AspenBurner.App.Tests/AspenBurner.App.Tests.csproj`
- Create: `tests/AspenBurner.App.Tests/Config/CrosshairConfigTests.cs`

- [ ] **Step 1: 先写配置基础测试**
- [ ] **Step 2: 运行测试，确认缺失实现导致失败**
- [ ] **Step 3: 建立 WinForms 主工程和测试工程**
- [ ] **Step 3.1: 在 manifest 中明确 `asInvoker`，把 legacy wrapper 的提权策略作为兼容层契约实现**
- [ ] **Step 4: 让最小测试通过**
- [ ] **Step 5: 运行 `dotnet test` 与 `dotnet build`**
- [ ] **Step 6: 提交**

### Task 2: 建立兼容护栏与契约测试

**Files:**
- Create: `tests/AspenBurner.App.Tests/Compatibility/LegacyConfigContractTests.cs`
- Create: `tests/AspenBurner.App.Tests/Compatibility/LegacyEntryContractTests.cs`
- Create: `tests/AspenBurner.App.Tests/Compatibility/LegacyFormattingContractTests.cs`

- [ ] **Step 1: 基于当前 PowerShell 行为补兼容契约测试**
- [ ] **Step 2: 跑当前基线并固化预期**
- [ ] **Step 3: 确认后续 C# 实现必须过这些护栏**
- [ ] **Step 4: 提交**

### Task 3: 迁移配置、几何和格式化纯逻辑

**Files:**
- Create: `src/AspenBurner.App/Configuration/CrosshairConfig.cs`
- Create: `src/AspenBurner.App/Configuration/CrosshairConfigService.cs`
- Create: `src/AspenBurner.App/Configuration/CrosshairConfigMigrator.cs`
- Create: `src/AspenBurner.App/Core/CrosshairGeometry.cs`
- Create: `src/AspenBurner.App/Core/StatusOverlayPlacement.cs`
- Create: `src/AspenBurner.App/Core/StatusTextFormatter.cs`
- Test: `tests/AspenBurner.App.Tests/Configuration/CrosshairConfigServiceTests.cs`
- Test: `tests/AspenBurner.App.Tests/Core/CrosshairGeometryTests.cs`
- Test: `tests/AspenBurner.App.Tests/Core/StatusOverlayPlacementTests.cs`
- Test: `tests/AspenBurner.App.Tests/Core/StatusTextFormatterTests.cs`

- [ ] **Step 1: 先补 10+ 个失败测试，覆盖默认值、校验、几何、文本格式化，以及设置器不会夹断合法旧配置**
- [ ] **Step 1.1: 补 `ConfigVersion` 与旧配置迁移回放测试**
- [ ] **Step 2: 运行测试，确认红灯**
- [ ] **Step 3: 写最小实现**
- [ ] **Step 4: 运行测试转绿**
- [ ] **Step 5: 重构并保持全绿**
- [ ] **Step 6: 提交**

### Task 4: 实现 overlay 运行时与目标窗口检测

**Files:**
- Create: `src/AspenBurner.App/Native/NativeMethods.cs`
- Create: `src/AspenBurner.App/Runtime/TargetWindowMonitor.cs`
- Create: `src/AspenBurner.App/Runtime/OverlayRuntime.cs`
- Create: `src/AspenBurner.App/Runtime/IForegroundWindowSource.cs`
- Create: `src/AspenBurner.App/Runtime/ITickSource.cs`
- Create: `src/AspenBurner.App/UI/CrosshairOverlayForm.cs`
- Create: `src/AspenBurner.App/UI/StatusOverlayForm.cs`
- Test: `tests/AspenBurner.App.Tests/Runtime/TargetWindowMonitorTests.cs`
- Test: `tests/AspenBurner.App.Tests/Runtime/OverlayVisibilityStateTests.cs`

- [ ] **Step 1: 先写显示状态、防抖、目标进程匹配失败测试**
- [ ] **Step 2: 运行测试确认失败**
- [ ] **Step 3: 实现 overlay 运行时与透明窗体**
- [ ] **Step 3.1: Win32 前台窗口和定时器通过 seam 注入，保证可测**
- [ ] **Step 4: 运行测试**
- [ ] **Step 5: 做一次本地桌面 smoke run**
- [ ] **Step 6: 提交**

### Task 5: 实现设置窗体、托盘、单实例与命令转发

**Files:**
- Create: `src/AspenBurner.App/Application/AspenBurnerApplicationContext.cs`
- Create: `src/AspenBurner.App/Application/AppCommandServer.cs`
- Create: `src/AspenBurner.App/Application/AppCommandClient.cs`
- Create: `src/AspenBurner.App/Diagnostics/AppLogger.cs`
- Create: `src/AspenBurner.App/Diagnostics/HealthSnapshot.cs`
- Create: `src/AspenBurner.App/UI/SettingsForm.cs`
- Create: `src/AspenBurner.App/UI/PreviewCanvas.cs`
- Test: `tests/AspenBurner.App.Tests/Application/AppCommandTests.cs`

- [ ] **Step 1: 先写单实例命令与配置实时应用测试**
- [ ] **Step 2: 运行测试确认失败**
- [ ] **Step 3: 实现托盘、设置窗体、桌面预览命令**
- [ ] **Step 3.1: 命令转发使用命名管道，不再依赖外部强杀或文件轮询**
- [ ] **Step 3.2: 明确定义并实现 `运行中/等待目标窗口/已暂停/桌面预览/遥测异常` 的托盘状态文案与 tooltip**
- [ ] **Step 3.3: 日志落盘与健康状态查询在这一任务接通，不再允许静默空 `catch`**
- [ ] **Step 4: 运行测试**
- [ ] **Step 5: 本地手工验证“启动 -> 托盘 -> 设置 -> 预览 -> 退出”路径**
- [ ] **Step 6: 提交**

### Task 6: 实现 CPU 遥测与数据源状态

**Files:**
- Create: `src/AspenBurner.App/Telemetry/CpuStatusSnapshot.cs`
- Create: `src/AspenBurner.App/Telemetry/ICpuStatusProvider.cs`
- Create: `src/AspenBurner.App/Telemetry/ControlCenterCpuProvider.cs`
- Create: `src/AspenBurner.App/Telemetry/FallbackCpuProvider.cs`
- Create: `src/AspenBurner.App/Telemetry/CpuStatusService.cs`
- Test: `tests/AspenBurner.App.Tests/Telemetry/CpuStatusServiceTests.cs`

- [ ] **Step 1: 先写 provider 选择、回退、文本状态测试**
- [ ] **Step 2: 运行测试确认失败**
- [ ] **Step 3: 先实现 fallback 与数据新鲜度，再接 `Control Center` provider**
- [ ] **Step 4: 运行测试**
- [ ] **Step 5: 在本机做一次真实读取 smoke 验证**
- [ ] **Step 6: 提交**

### Task 7: 接通兼容脚本、构建发布与文档

**Files:**
- Modify: `Start-Crosshair.ps1`
- Modify: `Stop-Crosshair.ps1`
- Modify: `Configure-Crosshair.ps1`
- Modify: `README.md`
- Modify: `memory-bank/techContext.md`
- Modify: `memory-bank/systemPatterns.md`
- Test: `tests/AspenBurner.App.Tests/Compatibility/LegacyEntryTests.cs`
- Test: `tests/AspenBurner.App.Tests/Compatibility/LegacyElevationTests.cs`

- [ ] **Step 1: 先写旧入口兼容测试**
- [ ] **Step 2: 运行测试确认失败**
- [ ] **Step 3: 修改脚本壳层接入 `AspenBurner.exe`**
- [ ] **Step 3.1: 验证 `关闭窗口 != 退出程序`，`暂停显示 != stop/exit`，并覆盖对应测试**
- [ ] **Step 3.2: 验证 `.ps1/.cmd` 两套入口都保持兼容语义与提权行为**
- [ ] **Step 4: `dotnet publish` 生成可运行产物**
- [ ] **Step 5: 更新 README 与 memory-bank**
- [ ] **Step 6: 运行全量测试与最终 smoke 验证**
- [ ] **Step 7: 提交**
