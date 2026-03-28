using AspenBurner.App.Configuration;

namespace AspenBurner.App.Tests.Configuration;

/// <summary>
/// Contract tests for configuration defaults, validation, migration, and JSON round-tripping.
/// </summary>
[TestClass]
public sealed class CrosshairConfigServiceTests
{
    private readonly CrosshairConfigService service = new();

    /// <summary>
    /// Ensures the default configuration matches the legacy PowerShell defaults.
    /// </summary>
    [TestMethod]
    public void CreateDefault_UsesLegacyDefaults()
    {
        CrosshairConfig config = this.service.CreateDefault();

        Assert.AreEqual(1, config.ConfigVersion);
        Assert.AreEqual("Green", config.Color);
        Assert.AreEqual(6, config.Length);
        Assert.AreEqual(4, config.Gap);
        Assert.AreEqual(2, config.Thickness);
        Assert.AreEqual(1, config.OutlineThickness);
        Assert.AreEqual(255, config.Opacity);
        Assert.AreEqual(0, config.OffsetX);
        Assert.AreEqual(0, config.OffsetY);
        Assert.IsFalse(config.StatusEnabled);
        Assert.AreEqual("TopRight", config.StatusPosition);
        Assert.AreEqual(24, config.StatusOffsetX);
        Assert.AreEqual(24, config.StatusOffsetY);
    }

    /// <summary>
    /// Ensures legacy files without a version field migrate to the current version.
    /// </summary>
    [TestMethod]
    public void LoadFromJson_MigratesLegacyConfigWithoutVersion()
    {
        const string json = """
        {
          "Color": "Yellow",
          "Gap": 5,
          "OffsetX": 1280,
          "StatusEnabled": true
        }
        """;

        CrosshairConfig config = this.service.LoadFromJson(json);

        Assert.AreEqual(1, config.ConfigVersion);
        Assert.AreEqual("Yellow", config.Color);
        Assert.AreEqual(5, config.Gap);
        Assert.AreEqual(1280, config.OffsetX);
        Assert.IsTrue(config.StatusEnabled);
    }

    /// <summary>
    /// Ensures large but still valid offsets survive round-tripping and are not silently clamped.
    /// </summary>
    [TestMethod]
    public void ToJson_RoundTripsLegacySizedOffsetsWithoutClamping()
    {
        CrosshairConfig config = this.service.CreateDefault() with
        {
            OffsetX = 3200,
            OffsetY = -2750,
        };

        string json = this.service.ToJson(config);
        CrosshairConfig reloaded = this.service.LoadFromJson(json);

        Assert.AreEqual(3200, reloaded.OffsetX);
        Assert.AreEqual(-2750, reloaded.OffsetY);
    }

    /// <summary>
    /// Ensures invalid colors are rejected.
    /// </summary>
    [TestMethod]
    public void Validate_RejectsUnsupportedColor()
    {
        CrosshairConfig config = this.service.CreateDefault() with { Color = "Blue" };

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => this.service.Validate(config));
    }

    /// <summary>
    /// Ensures at least one crosshair arm remains enabled.
    /// </summary>
    [TestMethod]
    public void Validate_RejectsConfigsWithoutArms()
    {
        CrosshairConfig config = this.service.CreateDefault() with
        {
            ShowLeftArm = false,
            ShowRightArm = false,
            ShowTopArm = false,
            ShowBottomArm = false,
        };

        Assert.ThrowsException<ArgumentException>(() => this.service.Validate(config));
    }

    /// <summary>
    /// Ensures refresh intervals below the product floor are rejected.
    /// </summary>
    [TestMethod]
    public void Validate_RejectsAggressiveStatusRefresh()
    {
        CrosshairConfig config = this.service.CreateDefault() with { StatusRefreshMs = 200 };

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => this.service.Validate(config));
    }
}
