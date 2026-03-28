namespace AspenBurner.Bench;

/// <summary>
/// Provides deterministic CPU-heavy workloads for the bench scenarios.
/// </summary>
public static class BenchWorkloads
{
    private static double sink;

    /// <summary>
    /// Executes one frame worth of mixed main-thread and worker-thread work.
    /// </summary>
    public static long RunFrameWorkload(int workerCount)
    {
        int frameWorkers = Math.Min(Math.Max(workerCount, 1), 6);
        long total = RunKernel(180_000, 1);
        if (frameWorkers > 1)
        {
            object gate = new();
            Parallel.For(0, frameWorkers - 1, new ParallelOptions { MaxDegreeOfParallelism = frameWorkers - 1 }, workerIndex =>
            {
                long local = RunKernel(50_000, workerIndex + 10);
                lock (gate)
                {
                    total += local;
                }
            });
        }

        return total;
    }

    /// <summary>
    /// Executes one chunk of parallel throughput work.
    /// </summary>
    public static long RunParallelWorkload(int workerIndex)
    {
        return RunKernel(30_000, workerIndex + 100);
    }

    private static long RunKernel(int iterations, int seed)
    {
        uint state = unchecked((uint)(seed * 2654435761u)) | 1u;
        double accumulator = 0;
        int[] buffer = new int[128];

        for (int index = 0; index < iterations; index++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;

            int slot = (int)(state & 127);
            buffer[slot] = unchecked(buffer[slot] + (int)(state & 1023));
            accumulator += Math.Sqrt((buffer[slot] & 1023) + 1) + Math.Sin(buffer[(slot + 1) & 127]);
        }

        sink = accumulator;
        return iterations;
    }
}
