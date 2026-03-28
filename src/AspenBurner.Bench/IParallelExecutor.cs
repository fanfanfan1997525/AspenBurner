namespace AspenBurner.Bench;

/// <summary>
/// Abstracts sustained parallel execution for deterministic tests.
/// </summary>
public interface IParallelExecutor
{
    /// <summary>
    /// Runs the sustained multi-core workload.
    /// </summary>
    Task<SustainedParallelResult> RunAsync(int durationSeconds, int workerCount, CancellationToken cancellationToken);
}
