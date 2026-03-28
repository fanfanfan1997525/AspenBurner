namespace AspenBurner.App.Runtime;

/// <summary>
/// Abstraction over the foreground window query source.
/// </summary>
public interface IForegroundWindowSource
{
    /// <summary>
    /// Tries to capture the current foreground window.
    /// </summary>
    TargetWindowInfo? TryGetForegroundWindow();
}
