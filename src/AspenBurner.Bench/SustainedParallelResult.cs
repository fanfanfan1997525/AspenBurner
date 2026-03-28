namespace AspenBurner.Bench;

/// <summary>
/// Represents metrics produced by the sustained parallel scenario.
/// </summary>
public sealed record SustainedParallelResult(
    long TotalOperations,
    double OperationsPerSecond,
    double PerThreadBalanceRatio);
