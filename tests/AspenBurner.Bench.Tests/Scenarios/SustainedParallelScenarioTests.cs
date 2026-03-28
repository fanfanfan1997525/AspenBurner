namespace AspenBurner.Bench.Tests.Scenarios;

[TestClass]
public sealed class SustainedParallelScenarioTests
{
    [TestMethod]
    public void Summarize_ComputesOperationsPerSecondAndBalance()
    {
        SustainedParallelResult result = SustainedParallelScenario.Summarize(
            perThreadOperations: [100, 90, 80, 70],
            durationSeconds: 2);

        Assert.AreEqual(340, result.TotalOperations);
        Assert.AreEqual(170, result.OperationsPerSecond, 0.001);
        Assert.AreEqual(0.82, result.PerThreadBalanceRatio, 0.01);
    }

    [TestMethod]
    public void Summarize_RejectsZeroDuration()
    {
        Assert.ThrowsException<ArgumentException>(() => SustainedParallelScenario.Summarize([10, 10], 0));
    }
}
