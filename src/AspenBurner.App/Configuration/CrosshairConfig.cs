namespace AspenBurner.App.Configuration;

/// <summary>
/// Canonical persisted configuration for the AspenBurner desktop application.
/// </summary>
public sealed record CrosshairConfig
{
    /// <summary>
    /// Gets the persisted schema version.
    /// </summary>
    public int ConfigVersion { get; init; } = 1;

    /// <summary>
    /// Gets the crosshair color mode.
    /// </summary>
    public string Color { get; init; } = "Green";

    /// <summary>
    /// Gets the custom red channel value.
    /// </summary>
    public int ColorR { get; init; } = 0;

    /// <summary>
    /// Gets the custom green channel value.
    /// </summary>
    public int ColorG { get; init; } = 255;

    /// <summary>
    /// Gets the custom blue channel value.
    /// </summary>
    public int ColorB { get; init; } = 0;

    /// <summary>
    /// Gets the crosshair arm length.
    /// </summary>
    public int Length { get; init; } = 6;

    /// <summary>
    /// Gets the center gap.
    /// </summary>
    public int Gap { get; init; } = 4;

    /// <summary>
    /// Gets the stroke thickness.
    /// </summary>
    public int Thickness { get; init; } = 2;

    /// <summary>
    /// Gets the outline thickness.
    /// </summary>
    public int OutlineThickness { get; init; } = 1;

    /// <summary>
    /// Gets the crosshair opacity.
    /// </summary>
    public int Opacity { get; init; } = 255;

    /// <summary>
    /// Gets the horizontal center offset.
    /// </summary>
    public int OffsetX { get; init; } = 0;

    /// <summary>
    /// Gets the vertical center offset.
    /// </summary>
    public int OffsetY { get; init; } = 0;

    /// <summary>
    /// Gets whether the left arm is enabled.
    /// </summary>
    public bool ShowLeftArm { get; init; } = true;

    /// <summary>
    /// Gets whether the right arm is enabled.
    /// </summary>
    public bool ShowRightArm { get; init; } = true;

    /// <summary>
    /// Gets whether the top arm is enabled.
    /// </summary>
    public bool ShowTopArm { get; init; } = true;

    /// <summary>
    /// Gets whether the bottom arm is enabled.
    /// </summary>
    public bool ShowBottomArm { get; init; } = true;

    /// <summary>
    /// Gets whether the CPU status overlay is enabled.
    /// </summary>
    public bool StatusEnabled { get; init; } = false;

    /// <summary>
    /// Gets the status anchor position.
    /// </summary>
    public string StatusPosition { get; init; } = "TopRight";

    /// <summary>
    /// Gets the horizontal status inset.
    /// </summary>
    public int StatusOffsetX { get; init; } = 24;

    /// <summary>
    /// Gets the vertical status inset.
    /// </summary>
    public int StatusOffsetY { get; init; } = 24;

    /// <summary>
    /// Gets the CPU status refresh interval in milliseconds.
    /// </summary>
    public int StatusRefreshMs { get; init; } = 1500;

    /// <summary>
    /// Gets the status text color preset.
    /// </summary>
    public string StatusTextColor { get; init; } = "Yellow";

    /// <summary>
    /// Gets the status text opacity.
    /// </summary>
    public int StatusOpacity { get; init; } = 220;

    /// <summary>
    /// Gets the status font size.
    /// </summary>
    public int StatusFontSize { get; init; } = 11;

    /// <summary>
    /// Gets whether temperature should be shown in the status overlay.
    /// </summary>
    public bool StatusShowTemperature { get; init; } = true;
}
