using AspenBurner.App.Runtime;

namespace AspenBurner.App.Telemetry;

/// <summary>
/// Merges preferred and fallback CPU providers and tracks freshness.
/// </summary>
public sealed class CpuStatusService : IDisposable
{
    private readonly ICpuStatusProvider preferredProvider;
    private readonly ICpuStatusProvider fallbackProvider;
    private readonly TimeSpan staleAfter;
    private CpuStatusSnapshot? lastSnapshot;

    /// <summary>
    /// Initializes a new telemetry service.
    /// </summary>
    public CpuStatusService(ICpuStatusProvider preferredProvider, ICpuStatusProvider fallbackProvider, TimeSpan? staleAfter = null)
    {
        this.preferredProvider = preferredProvider ?? throw new ArgumentNullException(nameof(preferredProvider));
        this.fallbackProvider = fallbackProvider ?? throw new ArgumentNullException(nameof(fallbackProvider));
        this.staleAfter = staleAfter ?? TimeSpan.FromSeconds(3);
    }

    /// <summary>
    /// Gets the latest merged snapshot, if any.
    /// </summary>
    public CpuStatusSnapshot? LastSnapshot => this.lastSnapshot;

    /// <summary>
    /// Captures a fresh merged snapshot.
    /// </summary>
    public CpuStatusSnapshot Capture()
    {
        CpuStatusSnapshot fallbackSnapshot = CaptureSafely(this.fallbackProvider);
        CpuStatusSnapshot preferredSnapshot = CaptureSafely(this.preferredProvider);

        bool preferredHasSignal = preferredSnapshot.FrequencyMHz > 0 || preferredSnapshot.TemperatureC.HasValue;
        bool fallbackHasSignal = fallbackSnapshot.FrequencyMHz > 0 || fallbackSnapshot.TemperatureC.HasValue;
        int frequencyMHz = preferredSnapshot.FrequencyMHz > 0
            ? preferredSnapshot.FrequencyMHz
            : fallbackSnapshot.FrequencyMHz;
        double? temperatureC = preferredSnapshot.TemperatureC;
        bool approximateTemperature = preferredHasSignal && preferredSnapshot.ApproximateTemperature;
        string source = preferredHasSignal
            ? preferredSnapshot.Source
            : fallbackHasSignal ? fallbackSnapshot.Source : "Unavailable";
        CpuStatusSnapshot merged = new(frequencyMHz, temperatureC, approximateTemperature, source, DateTimeOffset.Now);

        if (merged.FrequencyMHz > 0 || merged.TemperatureC.HasValue)
        {
            this.lastSnapshot = merged;
        }

        return merged;
    }

    /// <summary>
    /// Returns freshness state for the latest successful sample.
    /// </summary>
    public TelemetryFreshnessState GetFreshness(DateTimeOffset now)
    {
        if (this.lastSnapshot is null)
        {
            return TelemetryFreshnessState.Unavailable;
        }

        return now - this.lastSnapshot.CapturedAt <= this.staleAfter
            ? TelemetryFreshnessState.Fresh
            : TelemetryFreshnessState.Stale;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.preferredProvider.Dispose();
        this.fallbackProvider.Dispose();
    }

    private static CpuStatusSnapshot CaptureSafely(ICpuStatusProvider provider)
    {
        try
        {
            return provider.Capture();
        }
        catch
        {
            return new CpuStatusSnapshot(0, null, false, "Unavailable", DateTimeOffset.Now);
        }
    }
}
