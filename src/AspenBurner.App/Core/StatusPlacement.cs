namespace AspenBurner.App.Core;

/// <summary>
/// Represents an anchored status overlay placement.
/// </summary>
public readonly record struct StatusPlacement(string Position, int OffsetX, int OffsetY);
