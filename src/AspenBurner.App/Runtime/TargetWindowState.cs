namespace AspenBurner.App.Runtime;

/// <summary>
/// Represents whether the configured target window is currently visible to the runtime.
/// </summary>
public enum TargetWindowState
{
    /// <summary>
    /// Runtime is waiting for the target window.
    /// </summary>
    WaitingForTarget,

    /// <summary>
    /// Target window is matched and overlay can display.
    /// </summary>
    TargetMatched,

    /// <summary>
    /// A temporary desktop preview is active.
    /// </summary>
    DesktopPreview,
}
