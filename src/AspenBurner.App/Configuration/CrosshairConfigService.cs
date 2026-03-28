using System.Text.Json;

namespace AspenBurner.App.Configuration;

/// <summary>
/// Creates, validates, and serializes persisted crosshair configuration.
/// </summary>
public sealed class CrosshairConfigService
{
    private readonly CrosshairConfigMigrator migrator = new();
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Creates the legacy-compatible default configuration.
    /// </summary>
    /// <returns>Default configuration.</returns>
    public CrosshairConfig CreateDefault()
    {
        return new CrosshairConfig();
    }

    /// <summary>
    /// Loads and validates configuration from JSON text.
    /// </summary>
    /// <param name="json">Source JSON string.</param>
    /// <returns>Merged and validated configuration.</returns>
    public CrosshairConfig LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            CrosshairConfig defaults = this.CreateDefault();
            this.Validate(defaults);
            return defaults;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        CrosshairConfig migrated = this.migrator.Migrate(document.RootElement, this.CreateDefault());
        this.Validate(migrated);
        return migrated;
    }

    /// <summary>
    /// Serializes a validated configuration to JSON.
    /// </summary>
    /// <param name="config">Configuration instance.</param>
    /// <returns>JSON payload.</returns>
    public string ToJson(CrosshairConfig config)
    {
        this.Validate(config);
        return JsonSerializer.Serialize(config with { ConfigVersion = CrosshairConfigMigrator.CurrentVersion }, this.jsonOptions);
    }

    /// <summary>
    /// Validates configuration bounds and enumerations.
    /// </summary>
    /// <param name="config">Configuration instance.</param>
    public void Validate(CrosshairConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!new[] { "Green", "Yellow", "Custom" }.Contains(config.Color, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(config.Color), "Color must be one of: Green, Yellow, Custom.");
        }

        ValidateRange(config.ColorR, 0, 255, nameof(config.ColorR));
        ValidateRange(config.ColorG, 0, 255, nameof(config.ColorG));
        ValidateRange(config.ColorB, 0, 255, nameof(config.ColorB));
        ValidateRange(config.Length, 2, 20, nameof(config.Length));
        ValidateRange(config.Gap, 1, 20, nameof(config.Gap));
        ValidateRange(config.Thickness, 1, 6, nameof(config.Thickness));
        ValidateRange(config.OutlineThickness, 0, 4, nameof(config.OutlineThickness));
        ValidateRange(config.Opacity, 64, 255, nameof(config.Opacity));
        ValidateRange(config.OffsetX, -4000, 4000, nameof(config.OffsetX));
        ValidateRange(config.OffsetY, -4000, 4000, nameof(config.OffsetY));
        ValidateRange(config.StatusOffsetX, 0, 500, nameof(config.StatusOffsetX));
        ValidateRange(config.StatusOffsetY, 0, 500, nameof(config.StatusOffsetY));
        ValidateRange(config.StatusRefreshMs, 500, 5000, nameof(config.StatusRefreshMs));
        ValidateRange(config.StatusOpacity, 64, 255, nameof(config.StatusOpacity));
        ValidateRange(config.StatusFontSize, 9, 24, nameof(config.StatusFontSize));

        if (!config.ShowLeftArm && !config.ShowRightArm && !config.ShowTopArm && !config.ShowBottomArm)
        {
            throw new ArgumentException("At least one crosshair arm must be enabled.", nameof(config));
        }

        if (!new[] { "TopLeft", "TopRight", "BottomLeft", "BottomRight" }.Contains(config.StatusPosition, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(config.StatusPosition), "StatusPosition must be one of: TopLeft, TopRight, BottomLeft, BottomRight.");
        }

        if (!new[] { "Green", "Yellow", "White" }.Contains(config.StatusTextColor, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(config.StatusTextColor), "StatusTextColor must be one of: Green, Yellow, White.");
        }
    }

    private static void ValidateRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} must be between {min} and {max}.");
        }
    }
}
