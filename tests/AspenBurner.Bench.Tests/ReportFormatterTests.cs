namespace AspenBurner.Bench.Tests;

[TestClass]
public sealed class ReportFormatterTests
{
    [TestMethod]
    public void Format_IncludesOutcomeReasonsAndKeyMetrics()
    {
        BenchReport report = new(
            StartedAt: new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.FromHours(8)),
            DurationSeconds: 75,
            ProcessorName: "i7-13700HX",
            LogicalCoreCount: 24,
            FrameLoop: new FrameLoopResult(3600, 8.10, 9.20, 11.40, 0.08, 125000),
            Parallel: new SustainedParallelResult(840000000, 21000000, 0.86),
            Telemetry: new TelemetrySummary(2280, 2360, 96, 12, "Control Center"),
            Event37CountDelta: 3,
            Assessment: new BenchAssessment(
                BenchOutcome.ClearlyAbnormal,
                "明显异常：固件限速与低频同时出现。",
                ["平均频率长期偏低", "出现新的 Event 37 固件限速事件"]));

        string text = ReportFormatter.Format(report);

        StringAssert.Contains(text, "CPU 验证报告");
        StringAssert.Contains(text, "明显异常");
        StringAssert.Contains(text, "平均频率长期偏低");
        StringAssert.Contains(text, "FrameLoop");
        StringAssert.Contains(text, "SustainedParallel");
        StringAssert.Contains(text, "Event37Delta: 3");
    }
}
