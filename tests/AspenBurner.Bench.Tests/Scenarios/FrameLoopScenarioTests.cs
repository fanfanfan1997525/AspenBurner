namespace AspenBurner.Bench.Tests.Scenarios;

[TestClass]
public sealed class FrameLoopScenarioTests
{
    [TestMethod]
    public void Summarize_ComputesPercentilesMissRateAndThroughput()
    {
        FrameLoopResult result = FrameLoopScenario.Summarize(
            [6.0, 7.0, 8.0, 9.0, 12.0],
            frameBudgetMs: 8.33,
            durationSeconds: 1,
            totalWorkUnits: 5000);

        Assert.AreEqual(5, result.FrameCount);
        Assert.AreEqual(8.40, result.AverageFrameMs, 0.01);
        Assert.AreEqual(12.00, result.P95FrameMs, 0.01);
        Assert.AreEqual(12.00, result.P99FrameMs, 0.01);
        Assert.AreEqual(0.40, result.MissRate, 0.001);
        Assert.AreEqual(5000, result.WorkUnitsPerSecond);
    }

    [TestMethod]
    public void Summarize_RejectsEmptySamples()
    {
        Assert.ThrowsException<ArgumentException>(() => FrameLoopScenario.Summarize([], 8.33, 1, 0));
    }
}
