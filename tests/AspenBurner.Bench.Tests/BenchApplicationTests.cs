namespace AspenBurner.Bench.Tests;

[TestClass]
public sealed class BenchApplicationTests
{
    [TestMethod]
    public async Task RunAsync_WritesFormattedReport()
    {
        BenchReport report = new(
            DateTimeOffset.Parse("2026-03-29T02:00:00+08:00"),
            75,
            "i7-13700HX",
            24,
            new FrameLoopResult(3600, 8.0, 9.0, 10.0, 0.04, 120000),
            new SustainedParallelResult(800000000, 20000000, 0.90),
            new TelemetrySummary(4200, 4500, 90, 10, "Control Center"),
            0,
            new BenchAssessment(BenchOutcome.Normal, "正常：未发现明显锁频证据。", []));

        BenchApplication application = new(new FakeBenchRunner(report));
        StringWriter writer = new();

        int exitCode = await application.RunAsync(["--duration-seconds", "60"], writer, CancellationToken.None);
        string text = writer.ToString();

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(text, "CPU 验证报告");
        StringAssert.Contains(text, "正常");
    }

    [TestMethod]
    public async Task RunAsync_ReturnsErrorForInvalidArgs()
    {
        BenchApplication application = new(new FakeBenchRunner(null));
        StringWriter writer = new();

        int exitCode = await application.RunAsync(["--duration-seconds", "0"], writer, CancellationToken.None);

        Assert.AreEqual(1, exitCode);
        StringAssert.Contains(writer.ToString(), "Invalid");
    }

    private sealed class FakeBenchRunner : IBenchRunner
    {
        private readonly BenchReport? report;

        public FakeBenchRunner(BenchReport? report)
        {
            this.report = report;
        }

        public Task<BenchReport> RunAsync(BenchOptions options, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.report ?? throw new InvalidOperationException("No report configured."));
        }
    }
}
