using System.Drawing;

namespace AspenBurner.App.Core;

/// <summary>
/// Computes crosshair segment geometry and compact overlay bounds.
/// </summary>
public static class CrosshairGeometry
{
    /// <summary>
    /// Returns line segments for the requested crosshair shape.
    /// </summary>
    public static IReadOnlyList<CrosshairSegment> GetSegments(
        int centerX,
        int centerY,
        int length,
        int gap,
        bool showLeftArm = true,
        bool showRightArm = true,
        bool showTopArm = true,
        bool showBottomArm = true)
    {
        List<CrosshairSegment> segments = new();

        if (showLeftArm)
        {
            segments.Add(new CrosshairSegment("Left", centerX - gap - length, centerY, centerX - gap, centerY));
        }

        if (showRightArm)
        {
            segments.Add(new CrosshairSegment("Right", centerX + gap, centerY, centerX + gap + length, centerY));
        }

        if (showTopArm)
        {
            segments.Add(new CrosshairSegment("Top", centerX, centerY - gap - length, centerX, centerY - gap));
        }

        if (showBottomArm)
        {
            segments.Add(new CrosshairSegment("Bottom", centerX, centerY + gap, centerX, centerY + gap + length));
        }

        return segments;
    }

    /// <summary>
    /// Computes the smallest compact overlay window that can contain the crosshair.
    /// </summary>
    public static Rectangle GetOverlayBounds(
        int areaLeft,
        int areaTop,
        int areaWidth,
        int areaHeight,
        int length,
        int gap,
        int thickness,
        int outlineThickness,
        int offsetX,
        int offsetY)
    {
        if (areaWidth <= 0 || areaHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaWidth), "Area dimensions must be positive.");
        }

        int strokeWidth = thickness + (outlineThickness * 2);
        int padding = (int)Math.Ceiling(strokeWidth / 2.0) + 2;
        int halfSpan = gap + length + padding;
        int windowSize = (halfSpan * 2) + 1;
        int targetCenterX = areaLeft + (areaWidth / 2) + offsetX;
        int targetCenterY = areaTop + (areaHeight / 2) + offsetY;
        int localCenter = windowSize / 2;

        return new Rectangle(targetCenterX - localCenter, targetCenterY - localCenter, windowSize, windowSize);
    }
}
