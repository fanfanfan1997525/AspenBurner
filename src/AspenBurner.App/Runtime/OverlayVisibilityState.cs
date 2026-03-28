namespace AspenBurner.App.Runtime;

/// <summary>
/// Represents the next overlay visibility decision and miss counter.
/// </summary>
public readonly record struct OverlayVisibilityState(bool ShouldShow, int MissCount);
