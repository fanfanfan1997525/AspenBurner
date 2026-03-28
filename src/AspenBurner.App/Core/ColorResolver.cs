using System.Drawing;
using AspenBurner.App.Configuration;

namespace AspenBurner.App.Core;

/// <summary>
/// Resolves configured overlay colors into concrete drawing colors.
/// </summary>
public static class ColorResolver
{
    /// <summary>
    /// Resolves the configured crosshair color.
    /// </summary>
    public static Color ResolveCrosshairColor(CrosshairConfig config)
    {
        return config.Color switch
        {
            "Green" => Color.FromArgb(config.Opacity, Color.Lime),
            "Yellow" => Color.FromArgb(config.Opacity, Color.Yellow),
            "Custom" => Color.FromArgb(config.Opacity, config.ColorR, config.ColorG, config.ColorB),
            _ => Color.FromArgb(config.Opacity, Color.Lime),
        };
    }

    /// <summary>
    /// Resolves the configured status text color.
    /// </summary>
    public static Color ResolveStatusColor(CrosshairConfig config)
    {
        return config.StatusTextColor switch
        {
            "Green" => Color.FromArgb(config.StatusOpacity, Color.Lime),
            "Yellow" => Color.FromArgb(config.StatusOpacity, Color.Yellow),
            "White" => Color.FromArgb(config.StatusOpacity, Color.White),
            _ => Color.FromArgb(config.StatusOpacity, Color.White),
        };
    }
}
