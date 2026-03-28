namespace AspenBurner.Bench;

/// <summary>
/// Uses the built-in sustained parallel scenario as the runtime executor.
/// </summary>
public sealed class DefaultParallelExecutor : IParallelExecutor
{
    /// <inheritdoc />
    public Task<SustainedParallelResult> RunAsync(int durationSeconds, int workerCount, CancellationToken cancellationToken)
    {
        return SustainedParallelScenario.RunAsync(durationSeconds, workerCount, cancellationToken);
    }
}
