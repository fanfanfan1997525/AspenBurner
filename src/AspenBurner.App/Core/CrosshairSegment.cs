namespace AspenBurner.App.Core;

/// <summary>
/// Represents one line segment of the rendered crosshair.
/// </summary>
public readonly record struct CrosshairSegment(string Name, int X1, int Y1, int X2, int Y2);
