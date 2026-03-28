namespace AspenBurner.Bench;

/// <summary>
/// Represents the high-level bench conclusion.
/// </summary>
public enum BenchOutcome
{
    Normal,
    SuspectedThrottle,
    ClearlyAbnormal,
}
