using AspenBurner.App.Telemetry;

namespace AspenBurner.Bench.Tests;

[TestClass]
public sealed class BenchRunnerTests
{
    [TestMethod]
    public async Task RunAsync_BuildsReportAndAssessmentFromInjectedDependencies()
    {
        FakeFrameLoopExecutor frameLoop = new(new FrameLoopResult(1200, 9.5, 12.0, 14.0, 0.22, 110000));
        FakeParallelExecutor parallel = new(new SustainedParallelResult(450000000, 12500000, 0.78));
        FakeEvent37Reader eventReader = new([1, 3]);
        FakeTelemetrySource telemetry = new(
        [
            new CpuStatusSnapshot(2100, 95, false, "Control Center", DateTimeOffset.Parse("2026-03-29T02:10:01+08:00")),
            new CpuStatusSnapshot(2150, 97, false, "Control Center", DateTimeOffset.Parse("2026-03-29T02:10:02+08:00")),
        ]);

        BenchRunner runner = new(
            frameLoop,
            parallel,
            () => telemetry,
            eventReader,
            () => DateTimeOffset.Parse("2026-03-29T02:10:00+08:00"),
            () => "i7-13700HX",
            TimeSpan.FromMilliseconds(1));

        BenchReport report = await runner.RunAsync(new BenchOptions(10, 120, 0, 4), CancellationToken.None);

        Assert.AreEqual("i7-13700HX", report.ProcessorName);
        Assert.AreEqual(2, report.Event37CountDelta);
        Assert.AreEqual(BenchOutcome.ClearlyAbnormal, report.Assessment.Outcome);
        Assert.AreEqual(2125, report.Telemetry.AverageFrequencyMHz);
    }

    private sealed class FakeFrameLoopExecutor : IFrameLoopExecutor
    {
        private readonly FrameLoopResult result;

        public FakeFrameLoopExecutor(FrameLoopResult result)
        {
            this.result = result;
        }

        public Task<FrameLoopResult> RunAsync(int durationSeconds, int targetFps, int workerCount, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.result);
        }
    }

    private sealed class FakeParallelExecutor : IParallelExecutor
    {
        private readonly SustainedParallelResult result;

        public FakeParallelExecutor(SustainedParallelResult result)
        {
            this.result = result;
        }

        public Task<SustainedParallelResult> RunAsync(int durationSeconds, int workerCount, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.result);
        }
    }

    private sealed class FakeTelemetrySource : ITelemetrySource
    {
        private readonly Queue<CpuStatusSnapshot> samples;

        public FakeTelemetrySource(IEnumerable<CpuStatusSnapshot> samples)
        {
            this.samples = new Queue<CpuStatusSnapshot>(samples);
        }

        public CpuStatusSnapshot Capture()
        {
            return this.samples.Count > 0
                ? this.samples.Dequeue()
                : new CpuStatusSnapshot(2150, 97, false, "Control Center", DateTimeOffset.Now);
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeEvent37Reader : IEvent37Reader
    {
        private readonly Queue<int> counts;

        public FakeEvent37Reader(IEnumerable<int> counts)
        {
            this.counts = new Queue<int>(counts);
        }

        public int CountSince(DateTimeOffset startTime)
        {
            return this.counts.Dequeue();
        }
    }
}
