namespace AspenBurner.App.Runtime;

/// <summary>
/// Represents freshness of CPU telemetry data.
/// </summary>
public enum TelemetryFreshnessState
{
    /// <summary>
    /// Fresh recent data is available.
    /// </summary>
    Fresh,

    /// <summary>
    /// Last known value is stale.
    /// </summary>
    Stale,

    /// <summary>
    /// No usable data is available.
    /// </summary>
    Unavailable,
}
