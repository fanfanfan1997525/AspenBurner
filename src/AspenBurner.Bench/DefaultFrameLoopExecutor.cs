namespace AspenBurner.Bench;

/// <summary>
/// Uses the built-in frame-loop scenario as the runtime executor.
/// </summary>
public sealed class DefaultFrameLoopExecutor : IFrameLoopExecutor
{
    /// <inheritdoc />
    public Task<FrameLoopResult> RunAsync(int durationSeconds, int targetFps, int workerCount, CancellationToken cancellationToken)
    {
        return FrameLoopScenario.RunAsync(durationSeconds, targetFps, workerCount, cancellationToken);
    }
}
