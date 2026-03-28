namespace AspenBurner.App.Telemetry;

/// <summary>
/// Represents one CPU telemetry sample.
/// </summary>
public sealed record CpuStatusSnapshot(
    int FrequencyMHz,
    double? TemperatureC,
    bool ApproximateTemperature,
    string Source,
    DateTimeOffset CapturedAt);
