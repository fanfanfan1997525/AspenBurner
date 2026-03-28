using AspenBurner.App.Telemetry;

namespace AspenBurner.Bench;

/// <summary>
/// Abstracts CPU telemetry capture for deterministic tests and runtime reuse.
/// </summary>
public interface ITelemetrySource : IDisposable
{
    /// <summary>
    /// Captures one CPU telemetry snapshot.
    /// </summary>
    CpuStatusSnapshot Capture();
}
