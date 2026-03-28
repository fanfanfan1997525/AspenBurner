using System.Drawing;

namespace AspenBurner.App.Core;

/// <summary>
/// Computes anchored status overlay bounds and drag resolution.
/// </summary>
public static class StatusOverlayPlacement
{
    /// <summary>
    /// Gets anchored bounds for the status overlay.
    /// </summary>
    public static Rectangle GetBounds(
        int areaLeft,
        int areaTop,
        int areaWidth,
        int areaHeight,
        int overlayWidth,
        int overlayHeight,
        string position,
        int offsetX,
        int offsetY)
    {
        if (areaWidth <= 0 || areaHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaWidth), "Area dimensions must be positive.");
        }

        if (overlayWidth <= 0 || overlayHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overlayWidth), "Overlay dimensions must be positive.");
        }

        int left;
        int top;
        switch (position)
        {
            case "TopLeft":
                left = areaLeft + offsetX;
                top = areaTop + offsetY;
                break;
            case "TopRight":
                left = areaLeft + areaWidth - overlayWidth - offsetX;
                top = areaTop + offsetY;
                break;
            case "BottomLeft":
                left = areaLeft + offsetX;
                top = areaTop + areaHeight - overlayHeight - offsetY;
                break;
            case "BottomRight":
                left = areaLeft + areaWidth - overlayWidth - offsetX;
                top = areaTop + areaHeight - overlayHeight - offsetY;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(position), $"Unknown status position: {position}");
        }

        int maxLeft = areaLeft + Math.Max(areaWidth - overlayWidth, 0);
        int maxTop = areaTop + Math.Max(areaHeight - overlayHeight, 0);

        return new Rectangle(
            Math.Min(Math.Max(left, areaLeft), maxLeft),
            Math.Min(Math.Max(top, areaTop), maxTop),
            overlayWidth,
            overlayHeight);
    }

    /// <summary>
    /// Resolves a dragged overlay position into the nearest anchor and offsets.
    /// </summary>
    public static StatusPlacement Resolve(
        int areaWidth,
        int areaHeight,
        int overlayWidth,
        int overlayHeight,
        int overlayLeft,
        int overlayTop)
    {
        if (areaWidth <= 0 || areaHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaWidth), "Area dimensions must be positive.");
        }

        if (overlayWidth <= 0 || overlayHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overlayWidth), "Overlay dimensions must be positive.");
        }

        int clampedLeft = Math.Min(Math.Max(overlayLeft, 0), Math.Max(areaWidth - overlayWidth, 0));
        int clampedTop = Math.Min(Math.Max(overlayTop, 0), Math.Max(areaHeight - overlayHeight, 0));
        int rightInset = Math.Max(areaWidth - overlayWidth - clampedLeft, 0);
        int bottomInset = Math.Max(areaHeight - overlayHeight - clampedTop, 0);

        StatusPlacement[] candidates =
        {
            new("TopLeft", clampedLeft, clampedTop),
            new("TopRight", rightInset, clampedTop),
            new("BottomLeft", clampedLeft, bottomInset),
            new("BottomRight", rightInset, bottomInset),
        };

        return candidates
            .OrderBy(candidate => candidate.OffsetX + candidate.OffsetY)
            .ThenBy(candidate => candidate.Position, StringComparer.Ordinal)
            .First();
    }
}
