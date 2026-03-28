namespace AspenBurner.App.Diagnostics;

/// <summary>
/// Writes lightweight structured runtime diagnostics to disk.
/// </summary>
public sealed class AppLogger
{
    private readonly string logFilePath;
    private readonly object gate = new();

    /// <summary>
    /// Initializes a new logger writing into the provided directory.
    /// </summary>
    public AppLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        this.logFilePath = Path.Combine(logDirectory, $"aspenburner-{DateTime.Now:yyyyMMdd}.log");
    }

    /// <summary>
    /// Gets the current log file path.
    /// </summary>
    public string LogFilePath => this.logFilePath;

    /// <summary>
    /// Writes an informational entry.
    /// </summary>
    public void Info(string message)
    {
        this.Write("INFO", message, null);
    }

    /// <summary>
    /// Writes an error entry.
    /// </summary>
    public void Error(string message, Exception? exception = null)
    {
        this.Write("ERROR", message, exception);
    }

    private void Write(string level, string message, Exception? exception)
    {
        lock (this.gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.logFilePath) ?? AppContext.BaseDirectory);
            File.AppendAllText(this.logFilePath, $"{DateTime.Now:O} [{level}] {message}{Environment.NewLine}");
            if (exception is not null)
            {
                File.AppendAllText(this.logFilePath, $"{exception}{Environment.NewLine}");
            }
        }
    }
}
