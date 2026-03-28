using System.Diagnostics;

namespace AspenBurner.Bench;

/// <summary>
/// Runs a frame-budgeted workload that approximates a game-like main loop.
/// </summary>
public static class FrameLoopScenario
{
    /// <summary>
    /// Summarizes frame-loop samples into stable metrics.
    /// </summary>
    public static FrameLoopResult Summarize(
        IReadOnlyList<double> frameDurationsMs,
        double frameBudgetMs,
        double durationSeconds,
        long totalWorkUnits)
    {
        ArgumentNullException.ThrowIfNull(frameDurationsMs);
        if (frameDurationsMs.Count == 0)
        {
            throw new ArgumentException("Frame samples cannot be empty.", nameof(frameDurationsMs));
        }

        List<double> ordered = frameDurationsMs.OrderBy(static value => value).ToList();
        double averageFrameMs = frameDurationsMs.Average();
        double p95FrameMs = Percentile(ordered, 0.95);
        double p99FrameMs = Percentile(ordered, 0.99);
        double missRate = frameDurationsMs.Count(value => value > frameBudgetMs) / (double)frameDurationsMs.Count;
        long workUnitsPerSecond = durationSeconds <= 0
            ? 0
            : (long)Math.Round(totalWorkUnits / durationSeconds, MidpointRounding.AwayFromZero);

        return new FrameLoopResult(frameDurationsMs.Count, averageFrameMs, p95FrameMs, p99FrameMs, missRate, workUnitsPerSecond);
    }

    /// <summary>
    /// Runs the frame-loop workload for the requested duration.
    /// </summary>
    public static async Task<FrameLoopResult> RunAsync(int durationSeconds, int targetFps, int workerCount, CancellationToken cancellationToken)
    {
        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }

        if (targetFps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetFps));
        }

        double frameBudgetMs = 1000.0 / targetFps;
        Stopwatch totalStopwatch = Stopwatch.StartNew();
        List<double> frameDurationsMs = new();
        long totalWorkUnits = 0;

        while (totalStopwatch.Elapsed < TimeSpan.FromSeconds(durationSeconds))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Stopwatch frameStopwatch = Stopwatch.StartNew();
            totalWorkUnits += BenchWorkloads.RunFrameWorkload(workerCount);
            double elapsedMs = frameStopwatch.Elapsed.TotalMilliseconds;
            frameDurationsMs.Add(elapsedMs);

            if (elapsedMs < frameBudgetMs)
            {
                int delayMs = (int)Math.Max(1, Math.Round(frameBudgetMs - elapsedMs, MidpointRounding.AwayFromZero));
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        return Summarize(frameDurationsMs, frameBudgetMs, totalStopwatch.Elapsed.TotalSeconds, totalWorkUnits);
    }

    private static double Percentile(IReadOnlyList<double> ordered, double percentile)
    {
        int index = (int)Math.Ceiling(ordered.Count * percentile) - 1;
        index = Math.Clamp(index, 0, ordered.Count - 1);
        return ordered[index];
    }
}
