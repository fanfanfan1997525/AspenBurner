namespace AspenBurner.Bench;

/// <summary>
/// Represents metrics produced by the frame-loop scenario.
/// </summary>
public sealed record FrameLoopResult(
    int FrameCount,
    double AverageFrameMs,
    double P95FrameMs,
    double P99FrameMs,
    double MissRate,
    long WorkUnitsPerSecond);
