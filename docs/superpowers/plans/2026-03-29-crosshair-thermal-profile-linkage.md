# AspenBurner 准心联动热管理实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 AspenBurner 在本机上把准心 runtime 状态与 A/C 热管理档自动联动起来，并保持现有 overlay 逻辑不被污染。

**Architecture:** 新增一个纯状态机 `ThermalProfileController`，由 `AspenBurnerApplicationContext` 驱动 5 分钟定时器；真实切档由 `IThermalProfileDriver` / `ClevoThermalProfileDriver` 负责，隔离 WMI、`powercfg` 与 CC40 UI Automation。`OverlayRuntime` 继续只做显示与遥测，不承担热管理副作用。

**Tech Stack:** .NET 8, C# 12, WinForms Timer, System.Management, UI Automation, MSTest

---

### Task 1: 固化设计状态与测试边界

**Files:**
- Modify: `memory-bank/activeContext.md`
- Modify: `memory-bank/progress.md`
- Create: `docs/superpowers/specs/2026-03-29-crosshair-thermal-profile-linkage-design.md`
- Create: `docs/superpowers/plans/2026-03-29-crosshair-thermal-profile-linkage.md`

- [ ] **Step 1: 更新 memory-bank，写清 A/C 档定义与“手动关闭才回 C”的边界**
- [ ] **Step 2: 写中文 spec，固定状态机、门禁和非目标**
- [ ] **Step 3: 自查 spec 中是否还有含糊表述**
- [ ] **Step 4: 写实现计划，明确文件边界与 TDD 顺序**
- [ ] **Step 5: 提交文档工作项**

### Task 2: 先写状态机失败测试

**Files:**
- Create: `src/AspenBurner.App/Thermal/ThermalProfileKind.cs`
- Create: `src/AspenBurner.App/Thermal/ThermalTimerCommand.cs`
- Create: `src/AspenBurner.App/Thermal/ThermalProfileDecision.cs`
- Create: `src/AspenBurner.App/Thermal/ThermalProfileController.cs`
- Create: `tests/AspenBurner.App.Tests/Thermal/ThermalProfileControllerTests.cs`

- [ ] **Step 1: 写失败测试覆盖 7 个核心分支**
  - `Running + StatusEnabled=true + WaitingForTarget` 启动晋升定时器
  - `DesktopPreview` 停止晋升定时器且不切 C
  - `Paused` 立即切 C
  - `Stopped` 立即切 C
  - `StatusEnabled=false` 立即切 C
  - `CadenceTick` 到点切 A
  - `WaitingForTarget` 不会回 C
- [ ] **Step 2: 运行 `dotnet test F:\software\crosshair-overlay\AspenBurner.sln --filter ThermalProfileControllerTests`，确认红灯**
- [ ] **Step 3: 用最小实现补齐状态机和决策模型**
- [ ] **Step 4: 再跑同一组测试，确认转绿**
- [ ] **Step 5: 提交状态机工作项**

### Task 3: 先写本机驱动失败测试

**Files:**
- Create: `src/AspenBurner.App/Thermal/IThermalProfileDriver.cs`
- Create: `src/AspenBurner.App/Thermal/ClevoMachineIdentity.cs`
- Create: `src/AspenBurner.App/Thermal/ClevoThermalProfileDriver.cs`
- Create: `tests/AspenBurner.App.Tests/Thermal/ClevoMachineIdentityTests.cs`

- [ ] **Step 1: 写失败测试，覆盖本机识别和非本机拒绝两类路径**
- [ ] **Step 2: 运行 `dotnet test ... --filter ClevoMachineIdentityTests`，确认红灯**
- [ ] **Step 3: 实现最小机型判定与驱动可用性门禁**
- [ ] **Step 4: 再跑测试确认转绿**
- [ ] **Step 5: 提交驱动门禁工作项**

### Task 4: 集成 ApplicationContext 与 5 分钟 cadence

**Files:**
- Modify: `src/AspenBurner.App/Application/AspenBurnerApplicationContext.cs`
- Create: `tests/AspenBurner.App.Tests/Application/ThermalProfileIntegrationTests.cs`

- [ ] **Step 1: 写失败测试，覆盖 ApplicationContext 对状态机决策的消费**
  - 配置变更会把 `StatusEnabled` 传给控制器
  - `HealthChanged` 会更新控制器
  - 应用退出时请求 C 档
- [ ] **Step 2: 运行 `dotnet test ... --filter ThermalProfileIntegrationTests`，确认红灯**
- [ ] **Step 3: 在 `ApplicationContext` 中接入 controller、driver 和 5 分钟 WinForms Timer**
- [ ] **Step 4: 再跑目标测试确认转绿**
- [ ] **Step 5: 提交集成工作项**

### Task 5: 跑回归与本机 smoke

**Files:**
- Modify: `memory-bank/activeContext.md`
- Modify: `memory-bank/progress.md`

- [ ] **Step 1: 运行 `dotnet test F:\software\crosshair-overlay\AspenBurner.sln`**
- [ ] **Step 2: 本机 smoke：启动 AspenBurner，确认不因等待目标窗口而立刻回 C**
- [ ] **Step 3: 本机 smoke：手动 Pause 后确认请求 C**
- [ ] **Step 4: 更新 memory-bank，记录联动功能落地结果**
- [ ] **Step 5: 提交最终工作项**
