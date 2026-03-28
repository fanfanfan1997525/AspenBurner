using AspenBurner.App.Telemetry;

namespace AspenBurner.Bench;

/// <summary>
/// Reuses AspenBurner telemetry providers inside the bench tool.
/// </summary>
public sealed class AspenTelemetrySource : ITelemetrySource
{
    private readonly CpuStatusService service;

    /// <summary>
    /// Initializes a new telemetry source.
    /// </summary>
    public AspenTelemetrySource()
    {
        this.service = new CpuStatusService(new ControlCenterCpuStatusProvider(), new FallbackCpuStatusProvider());
    }

    /// <inheritdoc />
    public CpuStatusSnapshot Capture()
    {
        return this.service.Capture();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.service.Dispose();
    }
}
