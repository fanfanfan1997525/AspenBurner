using System.Collections.Concurrent;
using AspenBurner.App.Telemetry;

namespace AspenBurner.Bench;

/// <summary>
/// Collects and summarizes CPU telemetry samples during the bench run.
/// </summary>
public static class TelemetrySampler
{
    /// <summary>
    /// Summarizes captured telemetry samples.
    /// </summary>
    public static TelemetrySummary Summarize(IReadOnlyList<CpuStatusSnapshot> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (samples.Count == 0)
        {
            return new TelemetrySummary(0, 0, null, 0, "Unavailable");
        }

        List<int> frequencies = samples
            .Select(static sample => sample.FrequencyMHz)
            .Where(static value => value > 0)
            .ToList();

        List<double> temperatures = samples
            .Select(static sample => sample.TemperatureC)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .ToList();

        int averageFrequency = frequencies.Count == 0
            ? 0
            : (int)Math.Round(frequencies.Average(), MidpointRounding.AwayFromZero);
        int maxFrequency = frequencies.Count == 0 ? 0 : frequencies.Max();
        double? peakTemperature = temperatures.Count == 0 ? null : temperatures.Max();
        string source = samples
            .Select(static sample => sample.Source)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))
            ?? "Unavailable";

        return new TelemetrySummary(averageFrequency, maxFrequency, peakTemperature, samples.Count, source);
    }

    /// <summary>
    /// Captures telemetry samples until the provided token is canceled.
    /// </summary>
    public static async Task<IReadOnlyList<CpuStatusSnapshot>> CollectAsync(
        ITelemetrySource source,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        ConcurrentQueue<CpuStatusSnapshot> samples = new();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                samples.Enqueue(source.Capture());
            }
            catch
            {
                // Sampling failure degrades report confidence but should not kill the bench.
            }

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return samples.ToArray();
    }
}
