using System.Text.Json;

namespace AspenBurner.App.Configuration;

/// <summary>
/// Migrates persisted configuration payloads into the current in-memory model.
/// </summary>
public sealed class CrosshairConfigMigrator
{
    /// <summary>
    /// Gets the current schema version.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Applies migration and default merging to a raw JSON payload.
    /// </summary>
    /// <param name="root">Root JSON object.</param>
    /// <param name="defaults">Default configuration.</param>
    /// <returns>Migrated configuration.</returns>
    public CrosshairConfig Migrate(JsonElement root, CrosshairConfig defaults)
    {
        CrosshairConfig config = defaults with { ConfigVersion = CurrentVersion };

        config = config with
        {
            Color = ReadString(root, nameof(CrosshairConfig.Color), config.Color),
            ColorR = ReadInt(root, nameof(CrosshairConfig.ColorR), config.ColorR),
            ColorG = ReadInt(root, nameof(CrosshairConfig.ColorG), config.ColorG),
            ColorB = ReadInt(root, nameof(CrosshairConfig.ColorB), config.ColorB),
            Length = ReadInt(root, nameof(CrosshairConfig.Length), config.Length),
            Gap = ReadInt(root, nameof(CrosshairConfig.Gap), config.Gap),
            Thickness = ReadInt(root, nameof(CrosshairConfig.Thickness), config.Thickness),
            OutlineThickness = ReadInt(root, nameof(CrosshairConfig.OutlineThickness), config.OutlineThickness),
            Opacity = ReadInt(root, nameof(CrosshairConfig.Opacity), config.Opacity),
            OffsetX = ReadInt(root, nameof(CrosshairConfig.OffsetX), config.OffsetX),
            OffsetY = ReadInt(root, nameof(CrosshairConfig.OffsetY), config.OffsetY),
            ShowLeftArm = ReadBool(root, nameof(CrosshairConfig.ShowLeftArm), config.ShowLeftArm),
            ShowRightArm = ReadBool(root, nameof(CrosshairConfig.ShowRightArm), config.ShowRightArm),
            ShowTopArm = ReadBool(root, nameof(CrosshairConfig.ShowTopArm), config.ShowTopArm),
            ShowBottomArm = ReadBool(root, nameof(CrosshairConfig.ShowBottomArm), config.ShowBottomArm),
            StatusEnabled = ReadBool(root, nameof(CrosshairConfig.StatusEnabled), config.StatusEnabled),
            StatusPosition = ReadString(root, nameof(CrosshairConfig.StatusPosition), config.StatusPosition),
            StatusOffsetX = ReadInt(root, nameof(CrosshairConfig.StatusOffsetX), config.StatusOffsetX),
            StatusOffsetY = ReadInt(root, nameof(CrosshairConfig.StatusOffsetY), config.StatusOffsetY),
            StatusRefreshMs = ReadInt(root, nameof(CrosshairConfig.StatusRefreshMs), config.StatusRefreshMs),
            StatusTextColor = ReadString(root, nameof(CrosshairConfig.StatusTextColor), config.StatusTextColor),
            StatusOpacity = ReadInt(root, nameof(CrosshairConfig.StatusOpacity), config.StatusOpacity),
            StatusFontSize = ReadInt(root, nameof(CrosshairConfig.StatusFontSize), config.StatusFontSize),
            StatusShowTemperature = ReadBool(root, nameof(CrosshairConfig.StatusShowTemperature), config.StatusShowTemperature),
        };

        if (root.TryGetProperty(nameof(CrosshairConfig.ConfigVersion), out JsonElement versionElement) &&
            versionElement.ValueKind == JsonValueKind.Number &&
            versionElement.TryGetInt32(out int explicitVersion) &&
            explicitVersion > 0)
        {
            config = config with { ConfigVersion = explicitVersion };
        }

        return config with { ConfigVersion = CurrentVersion };
    }

    private static bool ReadBool(JsonElement root, string propertyName, bool fallback)
    {
        return root.TryGetProperty(propertyName, out JsonElement element) &&
               (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            ? element.GetBoolean()
            : fallback;
    }

    private static int ReadInt(JsonElement root, string propertyName, int fallback)
    {
        return root.TryGetProperty(propertyName, out JsonElement element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetInt32(out int value)
            ? value
            : fallback;
    }

    private static string ReadString(JsonElement root, string propertyName, string fallback)
    {
        return root.TryGetProperty(propertyName, out JsonElement element) &&
               element.ValueKind == JsonValueKind.String &&
               !string.IsNullOrWhiteSpace(element.GetString())
            ? element.GetString()!
            : fallback;
    }
}
