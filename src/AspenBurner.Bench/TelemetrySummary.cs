namespace AspenBurner.Bench;

/// <summary>
/// Represents aggregated CPU telemetry samples collected during one run.
/// </summary>
public sealed record TelemetrySummary(
    int AverageFrequencyMHz,
    int MaxFrequencyMHz,
    double? AverageTemperatureC,
    double? PeakTemperatureC,
    int SampleCount,
    string Source);
