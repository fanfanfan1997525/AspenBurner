# AspenBurner CPU 验证工具设计

版本: v0.6.0  
日期: 2026-03-29

## 1. 目标

在 AspenBurner 仓库内新增一个可直接运行的 CPU 验证工具，用固定、可重复的方式模拟两类负载：

1. 类游戏的主线程帧循环负载
2. 持续多核吞吐负载

工具需要自动输出原始指标与结论，用于判断当前机器是否存在明显锁频、固件限速或异常调度行为。

## 2. 非目标

- 不做通用跑分软件
- 不做 GPU 压力测试
- 不做长期烤机工具
- 不做带 GUI 的交互面板
- 不把结果上传到外部服务

## 3. 设计原则

### 3.1 真实问题

当前机器已经出现以下强信号：

- `Kernel-Processor-Power Event 37`
- `Win32_Processor CurrentClockSpeed/MaxClockSpeed = 2100`
- 游戏场景 GPU 吃不满，CPU 疑似被固件层限速

所以工具不能只输出 CPU 占用率，而必须把负载结果与系统遥测、事件日志绑定到一份报告里。

### 3.2 好品味

工具只做一件事：给出“像游戏”和“像持续并行计算”两段固定负载下的证据。  
不引入复杂插件、数据库、脚本依赖或在线基线下载。

### 3.3 不破坏现有用户空间

验证工具作为独立命令行项目存在，不影响现有 AspenBurner 主程序、兼容脚本和 UI 行为。

## 4. 方案比较

### 方案 A：PowerShell 压测脚本

- 优点：实现快
- 缺点：时钟精度差、噪声大、结果不稳定、很难做到可靠结论
- 结论：不选

### 方案 B：独立 .NET 命令行 Bench 工具

- 优点：与现有技术栈一致；易于测试；易于自动运行；可直接采样现有遥测与事件日志
- 缺点：需要新增项目与测试
- 结论：主推

### 方案 C：原生 C/C++ 基准程序

- 优点：开销最低
- 缺点：工程复杂度明显上升，与现有仓库不一致
- 结论：当前阶段不选

## 5. 最终设计

新增 `AspenBurner.Bench` 命令行项目，提供一次性验证入口：

```powershell
dotnet run --project .\src\AspenBurner.Bench\AspenBurner.Bench.csproj -- --duration-seconds 75
```

输出分三部分：

1. 环境信息
2. 原始指标
3. 结论与理由

## 6. 组件划分

### 6.1 `BenchOptions`

负责解析命令行参数，提供默认值：

- `DurationSeconds`
- `FrameLoopTargetFps`
- `WorkerCount`
- `WarmupSeconds`

### 6.2 `FrameLoopScenario`

模拟类游戏主线程负载：

- 固定帧预算，默认 `120 FPS`
- 主线程每帧执行确定性计算、少量内存访问和任务分发
- 少量工作线程执行固定工作量
- 输出：
  - `AverageFrameMs`
  - `P95FrameMs`
  - `P99FrameMs`
  - `MissRate`
  - `WorkUnitsPerSecond`

### 6.3 `SustainedParallelScenario`

模拟持续多核吞吐：

- 固定时长并行执行整数/浮点混合计算
- 每个线程独立循环，避免锁竞争主导结果
- 输出：
  - `TotalOperations`
  - `OperationsPerSecond`
  - `PerThreadBalance`

### 6.4 `TelemetrySampler`

在基准期间按固定间隔采样：

- CPU 当前频率
- CPU 温度
- 样本新鲜度

优先使用现有 `Control Center` provider，失败时回退到现有频率估算。

### 6.5 `Event37Probe`

记录运行前后的 `Kernel-Processor-Power Event 37` 数量差值，判断负载期间是否新增固件限速事件。

### 6.6 `BenchClassifier`

根据场景结果与遥测信号给出结论：

- `正常`
- `疑似锁频`
- `明显异常`

## 7. 判定规则

### 正常

满足以下条件：

- 负载期间未出现新的 `Event 37`
- 遥测频率明显高于 `2.1GHz`
- `FrameLoop` 没有出现大面积预算失守

### 疑似锁频

满足任一组合：

- 负载期间新增 `Event 37`
- 遥测频率长期接近 `2.1GHz`
- `FrameLoop` 与多核吞吐同时偏低

### 明显异常

满足以下组合之一：

- 新增 `Event 37` 且负载期间主频持续接近 `2.1GHz`
- `FrameLoop` 大幅失守且多核吞吐无法随核心数扩展

## 8. 数据流

1. 解析参数
2. 记录开始时间与起始 `Event 37` 数量
3. 预热
4. 运行 `FrameLoopScenario`
5. 运行 `SustainedParallelScenario`
6. 汇总遥测样本
7. 查询结束时 `Event 37`
8. 分类并打印报告

## 9. 错误处理

- 遥测不可用时继续跑基准，但在报告中标记 `TelemetryUnavailable`
- 事件日志不可读时不终止基准，结论降级并注明依据不足
- 单个场景失败时返回非零退出码并打印明确错误

## 10. 测试策略

至少覆盖：

1. 参数默认值
2. 参数校验
3. `FrameLoop` 指标汇总
4. `SustainedParallel` 指标汇总
5. `Event37` 差值计算
6. `正常` 判定
7. `疑似锁频` 判定
8. `明显异常` 判定
9. 遥测不可用降级
10. 文本报告格式

## 11. 验收标准

- 仓库新增独立 bench 项目与测试项目
- `dotnet test` 全绿
- bench 工具可在本机直接运行
- 输出同时包含原始指标和结论
- 我能够用它在当前机器上给出一次实测判断
