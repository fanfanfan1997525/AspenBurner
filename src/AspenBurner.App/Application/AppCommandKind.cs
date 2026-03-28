namespace AspenBurner.App.Application;

/// <summary>
/// Enumerates commands that secondary instances can send to the primary app instance.
/// </summary>
public enum AppCommandKind
{
    /// <summary>
    /// Opens the settings window.
    /// </summary>
    ShowSettings,

    /// <summary>
    /// Starts a temporary desktop preview.
    /// </summary>
    Preview,

    /// <summary>
    /// Requests a health snapshot.
    /// </summary>
    Health,

    /// <summary>
    /// Ensures the runtime is active and not paused.
    /// </summary>
    Resume,

    /// <summary>
    /// Requests a graceful shutdown.
    /// </summary>
    Stop,
}
