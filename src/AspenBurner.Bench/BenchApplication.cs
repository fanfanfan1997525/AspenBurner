namespace AspenBurner.Bench;

/// <summary>
/// Coordinates argument parsing, bench execution and report output.
/// </summary>
public sealed class BenchApplication
{
    private readonly IBenchRunner runner;

    /// <summary>
    /// Initializes a new application instance.
    /// </summary>
    public BenchApplication(IBenchRunner runner)
    {
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <summary>
    /// Runs the application and writes the report to the provided writer.
    /// </summary>
    public async Task<int> RunAsync(string[] args, TextWriter writer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(writer);

        try
        {
            BenchOptions options = BenchOptions.Parse(args);
            BenchReport report = await this.runner.RunAsync(options, cancellationToken);
            await writer.WriteLineAsync(ReportFormatter.Format(report));
            return 0;
        }
        catch (ArgumentException exception)
        {
            await writer.WriteLineAsync($"Invalid arguments: {exception.Message}");
            return 1;
        }
    }
}
