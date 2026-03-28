namespace AspenBurner.App.Runtime;

/// <summary>
/// Implements the legacy target-visibility debounce behavior.
/// </summary>
public static class OverlayVisibilityStateMachine
{
    /// <summary>
    /// Computes the next visibility state.
    /// </summary>
    public static OverlayVisibilityState Next(bool wasVisible, bool shouldShowTarget, int missCount, int hideAfterMisses)
    {
        if (hideAfterMisses < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(hideAfterMisses), "HideAfterMisses must be at least 1.");
        }

        if (shouldShowTarget)
        {
            return new OverlayVisibilityState(true, 0);
        }

        if (!wasVisible)
        {
            return new OverlayVisibilityState(false, 0);
        }

        int nextMissCount = missCount + 1;
        return new OverlayVisibilityState(nextMissCount < hideAfterMisses, nextMissCount);
    }
}
