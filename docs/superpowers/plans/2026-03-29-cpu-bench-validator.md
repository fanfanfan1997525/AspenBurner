# AspenBurner CPU 验证工具实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增一个可自动运行的 CPU 验证命令行工具，模拟类游戏与持续多核两类负载，并输出原始指标与异常结论。

**Architecture:** 新增 `AspenBurner.Bench` 与 `AspenBurner.Bench.Tests` 两个项目。核心逻辑拆成参数解析、两个场景、遥测采样、事件探针、分类器和报告格式化器，避免把系统依赖和纯逻辑混在一起。

**Tech Stack:** .NET 8, C# 12, MSTest, 现有 AspenBurner 遥测模块复用

---

### Task 1: 建立项目骨架与解空间接入

**Files:**
- Modify: `AspenBurner.sln`
- Create: `src/AspenBurner.Bench/AspenBurner.Bench.csproj`
- Create: `src/AspenBurner.Bench/Program.cs`
- Create: `tests/AspenBurner.Bench.Tests/AspenBurner.Bench.Tests.csproj`

- [ ] **Step 1: 先建立测试项目骨架**
- [ ] **Step 2: 运行空测试，确认测试工程可被发现**
- [ ] **Step 3: 建立 bench 命令行项目并接入 solution**
- [ ] **Step 4: 运行 `dotnet test AspenBurner.sln`**
- [ ] **Step 5: 提交**

### Task 2: 用 TDD 固化参数解析与分类规则

**Files:**
- Create: `src/AspenBurner.Bench/BenchOptions.cs`
- Create: `src/AspenBurner.Bench/BenchOutcome.cs`
- Create: `src/AspenBurner.Bench/BenchClassifier.cs`
- Test: `tests/AspenBurner.Bench.Tests/BenchOptionsTests.cs`
- Test: `tests/AspenBurner.Bench.Tests/BenchClassifierTests.cs`

- [ ] **Step 1: 先写 10+ 个失败测试，覆盖默认值、非法值、三档结论**
- [ ] **Step 2: 跑测试确认红灯**
- [ ] **Step 3: 写最小实现让测试变绿**
- [ ] **Step 4: 运行测试并重构**
- [ ] **Step 5: 提交**

### Task 3: 用 TDD 实现场景结果模型与报告格式

**Files:**
- Create: `src/AspenBurner.Bench/BenchReport.cs`
- Create: `src/AspenBurner.Bench/ReportFormatter.cs`
- Create: `src/AspenBurner.Bench/TelemetrySample.cs`
- Test: `tests/AspenBurner.Bench.Tests/ReportFormatterTests.cs`

- [ ] **Step 1: 先写报告文本格式测试**
- [ ] **Step 2: 跑测试确认失败**
- [ ] **Step 3: 实现结果模型与格式化器**
- [ ] **Step 4: 跑测试确认通过**
- [ ] **Step 5: 提交**

### Task 4: 用 TDD 实现 FrameLoop 场景

**Files:**
- Create: `src/AspenBurner.Bench/Scenarios/FrameLoopScenario.cs`
- Create: `src/AspenBurner.Bench/Scenarios/FrameLoopResult.cs`
- Test: `tests/AspenBurner.Bench.Tests/Scenarios/FrameLoopScenarioTests.cs`

- [ ] **Step 1: 先写失败测试，覆盖帧预算、miss rate、吞吐统计**
- [ ] **Step 2: 跑测试确认失败**
- [ ] **Step 3: 实现最小帧循环与指标聚合**
- [ ] **Step 4: 跑测试确认通过**
- [ ] **Step 5: 提交**

### Task 5: 用 TDD 实现 SustainedParallel 场景

**Files:**
- Create: `src/AspenBurner.Bench/Scenarios/SustainedParallelScenario.cs`
- Create: `src/AspenBurner.Bench/Scenarios/SustainedParallelResult.cs`
- Test: `tests/AspenBurner.Bench.Tests/Scenarios/SustainedParallelScenarioTests.cs`

- [ ] **Step 1: 先写失败测试，覆盖总吞吐、线程负载均衡、确定性下限**
- [ ] **Step 2: 跑测试确认失败**
- [ ] **Step 3: 实现最小并行场景**
- [ ] **Step 4: 跑测试确认通过**
- [ ] **Step 5: 提交**

### Task 6: 接入系统遥测与 Event 37 探针

**Files:**
- Create: `src/AspenBurner.Bench/System/Event37Probe.cs`
- Create: `src/AspenBurner.Bench/System/TelemetrySampler.cs`
- Create: `src/AspenBurner.Bench/System/ITelemetrySource.cs`
- Test: `tests/AspenBurner.Bench.Tests/System/Event37ProbeTests.cs`
- Test: `tests/AspenBurner.Bench.Tests/System/TelemetrySamplerTests.cs`

- [ ] **Step 1: 先写失败测试，覆盖事件差值与采样聚合**
- [ ] **Step 2: 跑测试确认失败**
- [ ] **Step 3: 实现探针与遥测采样，必要时复用现有 AspenBurner 遥测**
- [ ] **Step 4: 跑测试确认通过**
- [ ] **Step 5: 提交**

### Task 7: 集成主程序入口并完成实测

**Files:**
- Modify: `src/AspenBurner.Bench/Program.cs`
- Modify: `README.md`
- Modify: `memory-bank/activeContext.md`
- Modify: `memory-bank/progress.md`

- [ ] **Step 1: 先写端到端失败测试，覆盖命令行返回与报告内容**
- [ ] **Step 2: 跑测试确认失败**
- [ ] **Step 3: 接好完整执行链路**
- [ ] **Step 4: 运行 `dotnet test AspenBurner.sln`**
- [ ] **Step 5: 运行 bench 工具做一次本机实测**
- [ ] **Step 6: 更新 README 与 memory-bank**
- [ ] **Step 7: 提交**
