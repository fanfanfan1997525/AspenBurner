namespace AspenBurner.Bench;

/// <summary>
/// Executes the full CPU bench and returns one report.
/// </summary>
public interface IBenchRunner
{
    /// <summary>
    /// Runs one complete bench pass.
    /// </summary>
    Task<BenchReport> RunAsync(BenchOptions options, CancellationToken cancellationToken);
}
