namespace AspenBurner.App.Telemetry;

/// <summary>
/// Provides CPU status snapshots for the overlay.
/// </summary>
public interface ICpuStatusProvider : IDisposable
{
    /// <summary>
    /// Captures one telemetry snapshot.
    /// </summary>
    CpuStatusSnapshot Capture();
}
