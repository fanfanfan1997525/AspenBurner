namespace AspenBurner.App.Diagnostics;

/// <summary>
/// Registers best-effort global exception logging for the WinForms process.
/// </summary>
public static class ApplicationExceptionMonitor
{
    /// <summary>
    /// Hooks global exception events so unexpected crashes leave disk evidence.
    /// </summary>
    public static void Register(AppLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        global::System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        global::System.Windows.Forms.Application.ThreadException += (_, args) => logger.Error("Unhandled UI thread exception.", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            logger.Error("Unhandled AppDomain exception.", args.ExceptionObject as Exception ?? new InvalidOperationException($"Non-exception crash payload: {args.ExceptionObject?.GetType().FullName ?? "<null>"}"));
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            logger.Error("Unobserved task exception.", args.Exception);
            args.SetObserved();
        };
    }
}
