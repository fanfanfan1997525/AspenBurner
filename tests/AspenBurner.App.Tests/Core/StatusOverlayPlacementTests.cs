using AspenBurner.App.Core;
using System.Drawing;

namespace AspenBurner.App.Tests.Core;

/// <summary>
/// Compatibility tests for status overlay anchoring and drag placement.
/// </summary>
[TestClass]
public sealed class StatusOverlayPlacementTests
{
    /// <summary>
    /// Ensures top-right anchoring matches legacy coordinates.
    /// </summary>
    [TestMethod]
    public void GetBounds_AnchorsTopRight()
    {
        Rectangle bounds = StatusOverlayPlacement.GetBounds(
            areaLeft: 100,
            areaTop: 200,
            areaWidth: 1600,
            areaHeight: 900,
            overlayWidth: 180,
            overlayHeight: 30,
            position: "TopRight",
            offsetX: 24,
            offsetY: 16);

        Assert.AreEqual(1496, bounds.Left);
        Assert.AreEqual(216, bounds.Top);
    }

    /// <summary>
    /// Ensures bottom-left anchoring matches legacy coordinates.
    /// </summary>
    [TestMethod]
    public void GetBounds_AnchorsBottomLeft()
    {
        Rectangle bounds = StatusOverlayPlacement.GetBounds(
            areaLeft: 100,
            areaTop: 200,
            areaWidth: 1600,
            areaHeight: 900,
            overlayWidth: 180,
            overlayHeight: 30,
            position: "BottomLeft",
            offsetX: 12,
            offsetY: 18);

        Assert.AreEqual(112, bounds.Left);
        Assert.AreEqual(1052, bounds.Top);
    }

    /// <summary>
    /// Ensures drags near the top-right keep that anchor.
    /// </summary>
    [TestMethod]
    public void ResolvePlacement_KeepsTopRightAnchor()
    {
        StatusPlacement placement = StatusOverlayPlacement.Resolve(
            areaWidth: 400,
            areaHeight: 240,
            overlayWidth: 120,
            overlayHeight: 24,
            overlayLeft: 250,
            overlayTop: 18);

        Assert.AreEqual("TopRight", placement.Position);
        Assert.AreEqual(30, placement.OffsetX);
        Assert.AreEqual(18, placement.OffsetY);
    }

    /// <summary>
    /// Ensures drags near the bottom-left keep that anchor.
    /// </summary>
    [TestMethod]
    public void ResolvePlacement_KeepsBottomLeftAnchor()
    {
        StatusPlacement placement = StatusOverlayPlacement.Resolve(
            areaWidth: 400,
            areaHeight: 240,
            overlayWidth: 120,
            overlayHeight: 24,
            overlayLeft: 16,
            overlayTop: 190);

        Assert.AreEqual("BottomLeft", placement.Position);
        Assert.AreEqual(16, placement.OffsetX);
        Assert.AreEqual(26, placement.OffsetY);
    }

    /// <summary>
    /// Ensures out-of-bounds drags are clamped to a valid placement.
    /// </summary>
    [TestMethod]
    public void ResolvePlacement_ClampsOutOfBoundsDrags()
    {
        StatusPlacement placement = StatusOverlayPlacement.Resolve(
            areaWidth: 400,
            areaHeight: 240,
            overlayWidth: 120,
            overlayHeight: 24,
            overlayLeft: 500,
            overlayTop: -30);

        Assert.AreEqual("TopRight", placement.Position);
        Assert.AreEqual(0, placement.OffsetX);
        Assert.AreEqual(0, placement.OffsetY);
    }
}
