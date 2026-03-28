# AspenBurner CPU 验证工具

版本: v0.6.0  
日期: 2026-03-29

## 目标

提供一个可直接运行的 CPU 临时验证工具，用来判断当前机器是否存在明显锁频、固件限速或异常调度行为。

## 组成

- `src\AspenBurner.Bench\`
- `tests\AspenBurner.Bench.Tests\`

## 负载模型

- `FrameLoop`
  - 类游戏主线程帧循环
  - 统计平均帧耗时、P95、P99、预算失守比例
- `SustainedParallel`
  - 持续多核吞吐
  - 统计总吞吐与线程负载均衡

## 输出

- `AverageFrequencyMHz`
- `MaxFrequencyMHz`
- `PeakTemperatureC`
- `Event37Delta`
- `正常 / 疑似锁频 / 明显异常`

## 运行方式

```powershell
dotnet run --project .\src\AspenBurner.Bench\AspenBurner.Bench.csproj -- --duration-seconds 75 --warmup-seconds 5
```

## 2026-03-29 本机正式验证

```text
AverageFrequencyMHz = 3534
MaxFrequencyMHz = 4518
PeakTemperatureC = 87
Event37Delta = 0
结论 = 正常：未发现明显锁频证据
```
