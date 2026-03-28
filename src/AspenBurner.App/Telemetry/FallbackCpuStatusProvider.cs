using System.Diagnostics;
using System.Globalization;
using System.Management;

namespace AspenBurner.App.Telemetry;

/// <summary>
/// Provides a lightweight best-effort CPU status snapshot without vendor integrations.
/// </summary>
public sealed class FallbackCpuStatusProvider : ICpuStatusProvider
{
    private readonly PerformanceCounter? performanceCounter;
    private readonly int baseClockMHz;

    /// <summary>
    /// Initializes a new fallback provider.
    /// </summary>
    public FallbackCpuStatusProvider()
    {
        this.baseClockMHz = ReadBaseClockMHz() ?? 2100;

        try
        {
            this.performanceCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
            _ = this.performanceCounter.NextValue();
        }
        catch
        {
            this.performanceCounter = null;
        }
    }

    /// <inheritdoc />
    public CpuStatusSnapshot Capture()
    {
        try
        {
            float performancePercent = this.performanceCounter?.NextValue() ?? 0;
            int frequencyMHz = performancePercent > 0
                ? (int)Math.Round((this.baseClockMHz * performancePercent) / 100.0, MidpointRounding.AwayFromZero)
                : this.baseClockMHz;

            return new CpuStatusSnapshot(frequencyMHz, null, false, "Fallback", DateTimeOffset.Now);
        }
        catch
        {
            return new CpuStatusSnapshot(this.baseClockMHz, null, false, "Unavailable", DateTimeOffset.Now);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.performanceCounter?.Dispose();
    }

    private static int? ReadBaseClockMHz()
    {
        try
        {
            using ManagementObjectSearcher searcher = new("SELECT MaxClockSpeed FROM Win32_Processor");
            ManagementObjectCollection collection = searcher.Get();

            double average = collection
                .Cast<ManagementObject>()
                .Select(static processor => processor["MaxClockSpeed"])
                .Where(static value => value is not null)
                .Select(static value => Convert.ToDouble(value, CultureInfo.InvariantCulture))
                .DefaultIfEmpty()
                .Average();

            return average > 0
                ? (int)Math.Round(average, MidpointRounding.AwayFromZero)
                : null;
        }
        catch
        {
            return null;
        }
    }
}
