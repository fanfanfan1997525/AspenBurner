namespace AspenBurner.Bench.Tests;

[TestClass]
public sealed class BenchClassifierTests
{
    [TestMethod]
    public void Classify_ReturnsNormalWhenSignalsAreHealthy()
    {
        BenchAssessment assessment = BenchClassifier.Classify(new BenchEvidence(
            AverageFrequencyMHz: 4200,
            PeakTemperatureC: 84,
            Event37CountDelta: 0,
            FrameLoopMissRate: 0.03,
            FrameLoopP95Ms: 7.0,
            FrameBudgetMs: 8.33,
            ParallelBalanceRatio: 0.92));

        Assert.AreEqual(BenchOutcome.Normal, assessment.Outcome);
        StringAssert.Contains(assessment.Summary, "正常");
    }

    [TestMethod]
    public void Classify_ReturnsSuspectedThrottleWhenFrequencyIsPinnedLow()
    {
        BenchAssessment assessment = BenchClassifier.Classify(new BenchEvidence(
            AverageFrequencyMHz: 2100,
            PeakTemperatureC: 90,
            Event37CountDelta: 0,
            FrameLoopMissRate: 0.10,
            FrameLoopP95Ms: 8.0,
            FrameBudgetMs: 8.33,
            ParallelBalanceRatio: 0.88));

        Assert.AreEqual(BenchOutcome.SuspectedThrottle, assessment.Outcome);
        CollectionAssert.Contains(assessment.Reasons.ToArray(), "平均频率长期偏低");
    }

    [TestMethod]
    public void Classify_ReturnsSuspectedThrottleWhenEvent37Appears()
    {
        BenchAssessment assessment = BenchClassifier.Classify(new BenchEvidence(
            AverageFrequencyMHz: 3600,
            PeakTemperatureC: 88,
            Event37CountDelta: 2,
            FrameLoopMissRate: 0.06,
            FrameLoopP95Ms: 7.5,
            FrameBudgetMs: 8.33,
            ParallelBalanceRatio: 0.85));

        Assert.AreEqual(BenchOutcome.SuspectedThrottle, assessment.Outcome);
        CollectionAssert.Contains(assessment.Reasons.ToArray(), "出现新的 Event 37 固件限速事件");
    }

    [TestMethod]
    public void Classify_ReturnsClearlyAbnormalWhenLowFrequencyAndEvent37Coexist()
    {
        BenchAssessment assessment = BenchClassifier.Classify(new BenchEvidence(
            AverageFrequencyMHz: 2100,
            PeakTemperatureC: 96,
            Event37CountDelta: 4,
            FrameLoopMissRate: 0.31,
            FrameLoopP95Ms: 15.0,
            FrameBudgetMs: 8.33,
            ParallelBalanceRatio: 0.44));

        Assert.AreEqual(BenchOutcome.ClearlyAbnormal, assessment.Outcome);
        StringAssert.Contains(assessment.Summary, "明显异常");
    }

    [TestMethod]
    public void Classify_ReturnsClearlyAbnormalWhenFrameLoopAndParallelSignalsCollapseTogether()
    {
        BenchAssessment assessment = BenchClassifier.Classify(new BenchEvidence(
            AverageFrequencyMHz: 3000,
            PeakTemperatureC: 92,
            Event37CountDelta: 0,
            FrameLoopMissRate: 0.40,
            FrameLoopP95Ms: 18.0,
            FrameBudgetMs: 8.33,
            ParallelBalanceRatio: 0.35));

        Assert.AreEqual(BenchOutcome.ClearlyAbnormal, assessment.Outcome);
        CollectionAssert.Contains(assessment.Reasons.ToArray(), "主线程与多核吞吐同时异常");
    }

    [TestMethod]
    public void Classify_ExplainsUnavailableTelemetry()
    {
        BenchAssessment assessment = BenchClassifier.Classify(new BenchEvidence(
            AverageFrequencyMHz: 0,
            PeakTemperatureC: null,
            Event37CountDelta: 0,
            FrameLoopMissRate: 0.05,
            FrameLoopP95Ms: 7.0,
            FrameBudgetMs: 8.33,
            ParallelBalanceRatio: 0.87));

        Assert.AreEqual(BenchOutcome.Normal, assessment.Outcome);
        CollectionAssert.Contains(assessment.Reasons.ToArray(), "频率遥测不可用，结论可信度下降");
    }
}
