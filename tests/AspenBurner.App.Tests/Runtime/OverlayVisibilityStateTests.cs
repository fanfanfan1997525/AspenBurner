using AspenBurner.App.Runtime;

namespace AspenBurner.App.Tests.Runtime;

/// <summary>
/// Compatibility tests for target visibility debouncing.
/// </summary>
[TestClass]
public sealed class OverlayVisibilityStateTests
{
    /// <summary>
    /// Ensures the overlay shows immediately when the target appears.
    /// </summary>
    [TestMethod]
    public void Next_ShowsImmediatelyWhenTargetAppears()
    {
        OverlayVisibilityState state = OverlayVisibilityStateMachine.Next(
            wasVisible: false,
            shouldShowTarget: true,
            missCount: 3,
            hideAfterMisses: 4);

        Assert.IsTrue(state.ShouldShow);
        Assert.AreEqual(0, state.MissCount);
    }

    /// <summary>
    /// Ensures short misses keep the overlay alive.
    /// </summary>
    [TestMethod]
    public void Next_KeepsVisibleDuringShortMisses()
    {
        OverlayVisibilityState state = OverlayVisibilityStateMachine.Next(
            wasVisible: true,
            shouldShowTarget: false,
            missCount: 0,
            hideAfterMisses: 4);

        Assert.IsTrue(state.ShouldShow);
        Assert.AreEqual(1, state.MissCount);
    }

    /// <summary>
    /// Ensures the overlay hides after the configured miss budget is exhausted.
    /// </summary>
    [TestMethod]
    public void Next_HidesAfterConfiguredMisses()
    {
        OverlayVisibilityState state = OverlayVisibilityStateMachine.Next(
            wasVisible: true,
            shouldShowTarget: false,
            missCount: 3,
            hideAfterMisses: 4);

        Assert.IsFalse(state.ShouldShow);
        Assert.AreEqual(4, state.MissCount);
    }
}
