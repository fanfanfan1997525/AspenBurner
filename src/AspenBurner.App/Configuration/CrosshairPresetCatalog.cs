namespace AspenBurner.App.Configuration;

/// <summary>
/// Provides recommended crosshair presets and default reset behavior.
/// </summary>
public static class CrosshairPresetCatalog
{
    private static readonly IReadOnlyList<CrosshairPresetDefinition> Presets =
    [
        new(
            "delta_small_green",
            "推荐：小绿十字",
            "适合三角洲的小尺寸绿色十字，无中心点，偏稳妥。",
            static basis => basis with
            {
                Color = "Green",
                Length = 3,
                Gap = 4,
                Thickness = 1,
                OutlineThickness = 0,
                Opacity = 250,
                OffsetX = 0,
                OffsetY = 0,
                ShowLeftArm = true,
                ShowRightArm = true,
                ShowTopArm = true,
                ShowBottomArm = true,
            }),
        new(
            "delta_small_yellow",
            "推荐：小黄十字",
            "更亮一点的小尺寸黄色十字，便于在复杂背景下识别。",
            static basis => basis with
            {
                Color = "Yellow",
                Length = 3,
                Gap = 4,
                Thickness = 1,
                OutlineThickness = 0,
                Opacity = 250,
                OffsetX = 0,
                OffsetY = 0,
                ShowLeftArm = true,
                ShowRightArm = true,
                ShowTopArm = true,
                ShowBottomArm = true,
            }),
        new(
            "delta_t_yellow",
            "推荐：黄 T 字",
            "黄色 T 字准心，上方留空，更接近部分 FPS 玩家习惯。",
            static basis => basis with
            {
                Color = "Yellow",
                Length = 3,
                Gap = 4,
                Thickness = 1,
                OutlineThickness = 0,
                Opacity = 250,
                OffsetX = 0,
                OffsetY = 0,
                ShowLeftArm = true,
                ShowRightArm = true,
                ShowTopArm = false,
                ShowBottomArm = true,
            }),
    ];

    /// <summary>
    /// Gets the built-in recommended presets.
    /// </summary>
    public static IReadOnlyList<CrosshairPresetDefinition> GetRecommendations()
    {
        return Presets;
    }

    /// <summary>
    /// Applies the selected preset over the supplied config while preserving unrelated settings.
    /// </summary>
    public static CrosshairConfig Apply(string presetId, CrosshairConfig basis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(presetId);
        ArgumentNullException.ThrowIfNull(basis);

        CrosshairPresetDefinition preset = Presets.FirstOrDefault(preset => string.Equals(preset.Id, presetId, StringComparison.Ordinal))
            ?? throw new ArgumentOutOfRangeException(nameof(presetId), $"Unknown preset '{presetId}'.");
        return preset.Apply(basis);
    }

    /// <summary>
    /// Restores the canonical product defaults.
    /// </summary>
    public static CrosshairConfig Reset()
    {
        return new CrosshairConfig();
    }
}

/// <summary>
/// Describes a built-in preset option shown in the settings UI.
/// </summary>
public sealed record CrosshairPresetDefinition(string Id, string DisplayName, string Description, Func<CrosshairConfig, CrosshairConfig> Apply);
