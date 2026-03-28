using AspenBurner.App.Configuration;

namespace AspenBurner.App.Tests.Configuration;

/// <summary>
/// Regression coverage for recommended crosshair presets.
/// </summary>
[TestClass]
public sealed class CrosshairPresetCatalogTests
{
    private readonly CrosshairConfigService configService = new();

    /// <summary>
    /// Ensures the delta small green preset keeps status settings while replacing crosshair tuning.
    /// </summary>
    [TestMethod]
    public void ApplyRecommended_PreservesStatusSettingsAndUsesSmallGreenStyle()
    {
        CrosshairConfig startingConfig = new()
        {
            StatusEnabled = true,
            StatusPosition = "BottomLeft",
            StatusOffsetX = 48,
            StatusOffsetY = 52,
        };

        CrosshairConfig preset = CrosshairPresetCatalog.Apply("delta_small_green", startingConfig);

        Assert.AreEqual("Green", preset.Color);
        Assert.AreEqual(3, preset.Length);
        Assert.AreEqual(4, preset.Gap);
        Assert.AreEqual(1, preset.Thickness);
        Assert.AreEqual(0, preset.OutlineThickness);
        Assert.AreEqual(250, preset.Opacity);
        Assert.IsTrue(preset.StatusEnabled);
        Assert.AreEqual("BottomLeft", preset.StatusPosition);
        Assert.AreEqual(48, preset.StatusOffsetX);
        Assert.AreEqual(52, preset.StatusOffsetY);
        this.configService.Validate(preset);
    }

    /// <summary>
    /// Ensures the yellow T preset removes the top arm without breaking config validity.
    /// </summary>
    [TestMethod]
    public void ApplyRecommended_UsesYellowTShape()
    {
        CrosshairConfig preset = CrosshairPresetCatalog.Apply("delta_t_yellow", new CrosshairConfig());

        Assert.AreEqual("Yellow", preset.Color);
        Assert.AreEqual(3, preset.Length);
        Assert.IsTrue(preset.ShowLeftArm);
        Assert.IsTrue(preset.ShowRightArm);
        Assert.IsFalse(preset.ShowTopArm);
        Assert.IsTrue(preset.ShowBottomArm);
        this.configService.Validate(preset);
    }

    /// <summary>
    /// Ensures reset returns the canonical default config.
    /// </summary>
    [TestMethod]
    public void Reset_ReturnsCanonicalDefaults()
    {
        CrosshairConfig reset = CrosshairPresetCatalog.Reset();

        Assert.AreEqual(new CrosshairConfig(), reset);
        this.configService.Validate(reset);
    }
}
