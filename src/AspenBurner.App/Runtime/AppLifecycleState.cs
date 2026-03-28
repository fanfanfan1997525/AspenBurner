namespace AspenBurner.App.Runtime;

/// <summary>
/// Represents the lifecycle state of the main application runtime.
/// </summary>
public enum AppLifecycleState
{
    /// <summary>
    /// Runtime is active.
    /// </summary>
    Running,

    /// <summary>
    /// Overlay display is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Runtime is stopping.
    /// </summary>
    Stopped,
}
