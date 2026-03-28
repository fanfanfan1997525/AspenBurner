namespace AspenBurner.Bench;

/// <summary>
/// Abstracts frame-loop execution for deterministic tests.
/// </summary>
public interface IFrameLoopExecutor
{
    /// <summary>
    /// Runs the frame-loop workload.
    /// </summary>
    Task<FrameLoopResult> RunAsync(int durationSeconds, int targetFps, int workerCount, CancellationToken cancellationToken);
}
