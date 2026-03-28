using System.Diagnostics;

namespace AspenBurner.Bench;

/// <summary>
/// Runs a fixed-duration multi-core throughput workload.
/// </summary>
public static class SustainedParallelScenario
{
    /// <summary>
    /// Summarizes per-thread work counts into stable throughput metrics.
    /// </summary>
    public static SustainedParallelResult Summarize(IReadOnlyList<long> perThreadOperations, double durationSeconds)
    {
        ArgumentNullException.ThrowIfNull(perThreadOperations);
        if (durationSeconds <= 0)
        {
            throw new ArgumentException("Duration must be positive.", nameof(durationSeconds));
        }

        long totalOperations = perThreadOperations.Sum();
        double operationsPerSecond = totalOperations / durationSeconds;
        double averagePerThread = perThreadOperations.Count == 0 ? 0 : perThreadOperations.Average();
        double minPerThread = perThreadOperations.Count == 0 ? 0 : perThreadOperations.Min();
        double balanceRatio = averagePerThread <= 0 ? 0 : minPerThread / averagePerThread;

        return new SustainedParallelResult(totalOperations, operationsPerSecond, balanceRatio);
    }

    /// <summary>
    /// Runs the sustained parallel workload.
    /// </summary>
    public static async Task<SustainedParallelResult> RunAsync(int durationSeconds, int workerCount, CancellationToken cancellationToken)
    {
        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }

        if (workerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workerCount));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        Task<long>[] tasks = Enumerable.Range(0, workerCount)
            .Select(workerIndex => Task.Run(() => RunWorker(stopwatch, durationSeconds, workerIndex, cancellationToken), cancellationToken))
            .ToArray();

        long[] counts = await Task.WhenAll(tasks);
        return Summarize(counts, stopwatch.Elapsed.TotalSeconds);
    }

    private static long RunWorker(Stopwatch stopwatch, int durationSeconds, int workerIndex, CancellationToken cancellationToken)
    {
        long operations = 0;
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(durationSeconds))
        {
            cancellationToken.ThrowIfCancellationRequested();
            operations += BenchWorkloads.RunParallelWorkload(workerIndex);
        }

        return operations;
    }
}
