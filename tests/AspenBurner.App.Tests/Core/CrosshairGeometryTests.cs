using AspenBurner.App.Core;
using System.Drawing;

namespace AspenBurner.App.Tests.Core;

/// <summary>
/// Compatibility tests for crosshair geometry calculations.
/// </summary>
[TestClass]
public sealed class CrosshairGeometryTests
{
    /// <summary>
    /// Ensures all four arms are produced for a standard crosshair.
    /// </summary>
    [TestMethod]
    public void GetSegments_ReturnsFourSegmentsByDefault()
    {
        IReadOnlyList<CrosshairSegment> segments = CrosshairGeometry.GetSegments(100, 100, 6, 4);

        Assert.AreEqual(4, segments.Count);
    }

    /// <summary>
    /// Ensures disabled arms are omitted instead of being forced into the output.
    /// </summary>
    [TestMethod]
    public void GetSegments_OmitsDisabledArms()
    {
        IReadOnlyList<CrosshairSegment> segments = CrosshairGeometry.GetSegments(
            centerX: 100,
            centerY: 100,
            length: 6,
            gap: 4,
            showTopArm: false,
            showBottomArm: false);

        Assert.AreEqual(2, segments.Count);
        Assert.IsFalse(segments.Any(segment => segment.Name == "Top"));
        Assert.IsFalse(segments.Any(segment => segment.Name == "Bottom"));
    }

    /// <summary>
    /// Ensures the legacy known coordinates stay stable.
    /// </summary>
    [TestMethod]
    public void GetSegments_ProducesLegacyCoordinates()
    {
        IReadOnlyList<CrosshairSegment> segments = CrosshairGeometry.GetSegments(100, 100, 6, 4);

        Assert.AreEqual(90, segments.Single(segment => segment.Name == "Left").X1);
        Assert.AreEqual(110, segments.Single(segment => segment.Name == "Right").X2);
        Assert.AreEqual(90, segments.Single(segment => segment.Name == "Top").Y1);
        Assert.AreEqual(110, segments.Single(segment => segment.Name == "Bottom").Y2);
    }

    /// <summary>
    /// Ensures the center point remains empty when the gap is non-zero.
    /// </summary>
    [TestMethod]
    public void GetSegments_LeavesCenterEmpty()
    {
        IReadOnlyList<CrosshairSegment> segments = CrosshairGeometry.GetSegments(100, 100, 6, 4);

        bool centerTouched = segments.Any(segment =>
            (segment.X1 == segment.X2 && segment.X1 == 100 && segment.Y1 <= 100 && segment.Y2 >= 100) ||
            (segment.Y1 == segment.Y2 && segment.Y1 == 100 && segment.X1 <= 100 && segment.X2 >= 100));

        Assert.IsFalse(centerTouched);
    }

    /// <summary>
    /// Ensures overlay bounds match the legacy compact calculation.
    /// </summary>
    [TestMethod]
    public void GetOverlayBounds_MatchesLegacyCompactBounds()
    {
        Rectangle bounds = CrosshairGeometry.GetOverlayBounds(
            areaLeft: 0,
            areaTop: 0,
            areaWidth: 1920,
            areaHeight: 1080,
            length: 3,
            gap: 4,
            thickness: 1,
            outlineThickness: 0,
            offsetX: 0,
            offsetY: 0);

        Assert.AreEqual(950, bounds.Left);
        Assert.AreEqual(530, bounds.Top);
        Assert.AreEqual(21, bounds.Width);
        Assert.AreEqual(21, bounds.Height);
    }
}
