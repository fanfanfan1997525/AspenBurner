namespace AspenBurner.Bench;

/// <summary>
/// Program entry point for the CPU validation tool.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the CPU validation tool.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        BenchApplication application = new(
            new BenchRunner(
                new DefaultFrameLoopExecutor(),
                new DefaultParallelExecutor(),
                static () => new AspenTelemetrySource(),
                new WindowsEvent37Reader()));

        return await application.RunAsync(args, Console.Out, CancellationToken.None);
    }
}
