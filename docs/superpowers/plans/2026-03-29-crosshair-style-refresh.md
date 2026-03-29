# AspenBurner 样式刷新修复实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 AspenBurner 在颜色、透明度、文字样式变化时真实 overlay 不立即更新的问题。

**Architecture:** 不改配置链路，只在 `CrosshairOverlayForm` 和 `StatusOverlayForm` 两个透明窗体里把“样式变化”提升为即时重绘。测试直接盯住可见窗体的重绘语义，避免在 runtime 层打补丁。

**Tech Stack:** .NET 8, WinForms, MSTest, STA UI tests

---

### Task 1: 固化问题边界

**Files:**
- Modify: `memory-bank/activeContext.md`
- Modify: `memory-bank/progress.md`
- Create: `docs/superpowers/specs/2026-03-29-crosshair-style-refresh-design.md`
- Create: `docs/superpowers/plans/2026-03-29-crosshair-style-refresh.md`

- [ ] **Step 1: 更新 memory-bank，写清“样式参数不生效”而不是几何参数失效**
- [ ] **Step 2: 写短 spec，固定根因和最小修复策略**
- [ ] **Step 3: 写计划，明确只改两只 overlay form**
- [ ] **Step 4: 提交文档工作项**

### Task 2: 先写 UI 红灯测试

**Files:**
- Create: `tests/AspenBurner.App.Tests/UI/CrosshairOverlayFormTests.cs`
- Create: `tests/AspenBurner.App.Tests/UI/StatusOverlayFormTests.cs`

- [ ] **Step 1: 写失败测试，覆盖可见 crosshair form 的颜色/透明度变更即时重绘**
- [ ] **Step 2: 写失败测试，覆盖可见 status form 的颜色/透明度变更即时重绘**
- [ ] **Step 3: 运行定向测试，确认红灯**

### Task 3: 以最小代码修复窗体重绘

**Files:**
- Modify: `src/AspenBurner.App/UI/CrosshairOverlayForm.cs`
- Modify: `src/AspenBurner.App/UI/StatusOverlayForm.cs`

- [ ] **Step 1: 在 crosshair form 中把样式变化升级为即时重绘**
- [ ] **Step 2: 在 status form 中把样式变化升级为即时重绘**
- [ ] **Step 3: 再跑定向测试，确认转绿**
- [ ] **Step 4: 跑 `dotnet test AspenBurner.sln`**
- [ ] **Step 5: 提交最终工作项**
