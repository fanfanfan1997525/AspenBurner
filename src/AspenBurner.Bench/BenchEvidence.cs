namespace AspenBurner.Bench;

/// <summary>
/// Represents the aggregated evidence used for CPU health classification.
/// </summary>
public sealed record BenchEvidence(
    int AverageFrequencyMHz,
    double? PeakTemperatureC,
    int Event37CountDelta,
    double FrameLoopMissRate,
    double FrameLoopP95Ms,
    double FrameBudgetMs,
    double ParallelBalanceRatio);
