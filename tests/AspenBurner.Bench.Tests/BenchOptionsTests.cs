namespace AspenBurner.Bench.Tests;

[TestClass]
public sealed class BenchOptionsTests
{
    [TestMethod]
    public void Parse_UsesExpectedDefaults()
    {
        BenchOptions options = BenchOptions.Parse([]);

        Assert.AreEqual(75, options.DurationSeconds);
        Assert.AreEqual(120, options.FrameLoopTargetFps);
        Assert.AreEqual(5, options.WarmupSeconds);
        Assert.IsTrue(options.WorkerCount >= 1);
    }

    [TestMethod]
    public void Parse_AcceptsDurationOverride()
    {
        BenchOptions options = BenchOptions.Parse(["--duration-seconds", "60"]);

        Assert.AreEqual(60, options.DurationSeconds);
        Assert.AreEqual(120, options.FrameLoopTargetFps);
    }

    [TestMethod]
    public void Parse_AcceptsFrameRateOverride()
    {
        BenchOptions options = BenchOptions.Parse(["--frame-loop-target-fps", "144"]);

        Assert.AreEqual(144, options.FrameLoopTargetFps);
        Assert.AreEqual(75, options.DurationSeconds);
    }

    [TestMethod]
    public void Parse_AcceptsWarmupOverride()
    {
        BenchOptions options = BenchOptions.Parse(["--warmup-seconds", "3"]);

        Assert.AreEqual(3, options.WarmupSeconds);
        Assert.AreEqual(75, options.DurationSeconds);
    }

    [TestMethod]
    public void Parse_AcceptsWorkerCountOverride()
    {
        BenchOptions options = BenchOptions.Parse(["--worker-count", "8"]);

        Assert.AreEqual(8, options.WorkerCount);
        Assert.AreEqual(120, options.FrameLoopTargetFps);
    }

    [TestMethod]
    public void Parse_RejectsZeroDuration()
    {
        ArgumentException exception = Assert.ThrowsException<ArgumentException>(() => BenchOptions.Parse(["--duration-seconds", "0"]));

        StringAssert.Contains(exception.Message, "duration");
    }

    [TestMethod]
    public void Parse_RejectsNegativeWarmup()
    {
        ArgumentException exception = Assert.ThrowsException<ArgumentException>(() => BenchOptions.Parse(["--warmup-seconds", "-1"]));

        StringAssert.Contains(exception.Message, "warmup");
    }

    [TestMethod]
    public void Parse_RejectsUnknownArgument()
    {
        ArgumentException exception = Assert.ThrowsException<ArgumentException>(() => BenchOptions.Parse(["--mystery", "1"]));

        StringAssert.Contains(exception.Message, "mystery");
    }

    [TestMethod]
    public void Parse_RejectsMissingValue()
    {
        ArgumentException exception = Assert.ThrowsException<ArgumentException>(() => BenchOptions.Parse(["--worker-count"]));

        StringAssert.Contains(exception.Message, "worker-count");
    }
}
