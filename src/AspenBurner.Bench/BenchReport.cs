namespace AspenBurner.Bench;

/// <summary>
/// Represents one complete CPU bench report.
/// </summary>
public sealed record BenchReport(
    DateTimeOffset StartedAt,
    int DurationSeconds,
    string ProcessorName,
    int LogicalCoreCount,
    FrameLoopResult FrameLoop,
    SustainedParallelResult Parallel,
    TelemetrySummary Telemetry,
    int Event37CountDelta,
    BenchAssessment Assessment);
