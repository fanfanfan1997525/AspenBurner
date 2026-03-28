namespace AspenBurner.Bench;

/// <summary>
/// Classifies CPU bench evidence into a human-readable health verdict.
/// </summary>
public static class BenchClassifier
{
    /// <summary>
    /// Converts raw evidence into a stable outcome.
    /// </summary>
    public static BenchAssessment Classify(BenchEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        List<string> reasons = [];

        if (evidence.AverageFrequencyMHz <= 0)
        {
            reasons.Add("频率遥测不可用，结论可信度下降");
        }
        else if (evidence.AverageFrequencyMHz <= 2400)
        {
            reasons.Add("平均频率长期偏低");
        }

        if (evidence.Event37CountDelta > 0)
        {
            reasons.Add("出现新的 Event 37 固件限速事件");
        }

        bool frameLoopCollapsed = evidence.FrameLoopMissRate >= 0.30 || evidence.FrameLoopP95Ms > evidence.FrameBudgetMs * 1.5;
        bool parallelCollapsed = evidence.ParallelBalanceRatio < 0.50;
        if (frameLoopCollapsed && parallelCollapsed)
        {
            reasons.Add("主线程与多核吞吐同时异常");
        }

        BenchOutcome outcome;
        string summary;
        if (evidence.Event37CountDelta > 0 && evidence.AverageFrequencyMHz is > 0 and <= 2400)
        {
            outcome = BenchOutcome.ClearlyAbnormal;
            summary = "明显异常：固件限速与低频同时出现。";
        }
        else if (frameLoopCollapsed && parallelCollapsed)
        {
            outcome = BenchOutcome.ClearlyAbnormal;
            summary = "明显异常：主线程帧循环和多核吞吐同时失常。";
        }
        else if (evidence.Event37CountDelta > 0 || evidence.AverageFrequencyMHz is > 0 and <= 2400)
        {
            outcome = BenchOutcome.SuspectedThrottle;
            summary = "疑似锁频：频率或固件限速信号异常。";
        }
        else
        {
            outcome = BenchOutcome.Normal;
            summary = "正常：未发现明显锁频证据。";
        }

        return new BenchAssessment(outcome, summary, reasons);
    }
}
