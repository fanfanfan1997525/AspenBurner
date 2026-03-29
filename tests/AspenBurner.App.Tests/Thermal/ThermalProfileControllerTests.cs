using AspenBurner.App.Configuration;
using AspenBurner.App.Diagnostics;
using AspenBurner.App.Runtime;
using AspenBurner.App.Thermal;

namespace AspenBurner.App.Tests.Thermal;

/// <summary>
/// Covers the pure state-machine rules for A/C thermal profile linkage.
/// </summary>
[TestClass]
public sealed class ThermalProfileControllerTests
{
    /// <summary>
    /// Ensures the five-minute cadence is armed once runtime and status overlay are enabled.
    /// </summary>
    [TestMethod]
    public void Reconcile_StartsCadenceWhenRuntimeRunningAndStatusEnabled()
    {
        ThermalProfileController controller = new();

        ThermalProfileDecision decision = controller.Reconcile(
            CreateConfig(statusEnabled: true),
            CreateSnapshot(AppLifecycleState.Running, TargetWindowState.WaitingForTarget));

        Assert.AreEqual(ThermalTimerCommand.Start, decision.TimerCommand);
        Assert.IsNull(decision.RequestedProfile);
    }

    /// <summary>
    /// Ensures desktop preview stops the cadence without forcing the cooling profile.
    /// </summary>
    [TestMethod]
    public void Reconcile_StopsCadenceDuringPreviewWithoutCooling()
    {
        ThermalProfileController controller = new();
        _ = controller.Reconcile(CreateConfig(statusEnabled: true), CreateSnapshot(AppLifecycleState.Running, TargetWindowState.WaitingForTarget));

        ThermalProfileDecision decision = controller.Reconcile(
            CreateConfig(statusEnabled: true),
            CreateSnapshot(AppLifecycleState.Running, TargetWindowState.DesktopPreview));

        Assert.AreEqual(ThermalTimerCommand.Stop, decision.TimerCommand);
        Assert.IsNull(decision.RequestedProfile);
    }

    /// <summary>
    /// Ensures manual pause immediately requests the cooling profile.
    /// </summary>
    [TestMethod]
    public void Reconcile_PauseRequestsCoolingProfile()
    {
        ThermalProfileController controller = new();
        _ = controller.Reconcile(CreateConfig(statusEnabled: true), CreateSnapshot(AppLifecycleState.Running, TargetWindowState.WaitingForTarget));

        ThermalProfileDecision decision = controller.Reconcile(
            CreateConfig(statusEnabled: true),
            CreateSnapshot(AppLifecycleState.Paused, TargetWindowState.WaitingForTarget));

        Assert.AreEqual(ThermalTimerCommand.Stop, decision.TimerCommand);
        Assert.AreEqual(ThermalProfileKind.CoolingC, decision.RequestedProfile);
    }

    /// <summary>
    /// Ensures disabling the CPU status overlay is treated as a manual fallback to C.
    /// </summary>
    [TestMethod]
    public void Reconcile_StatusDisabledRequestsCoolingProfile()
    {
        ThermalProfileController controller = new();
        _ = controller.Reconcile(CreateConfig(statusEnabled: true), CreateSnapshot(AppLifecycleState.Running, TargetWindowState.TargetMatched));

        ThermalProfileDecision decision = controller.Reconcile(
            CreateConfig(statusEnabled: false),
            CreateSnapshot(AppLifecycleState.Running, TargetWindowState.TargetMatched));

        Assert.AreEqual(ThermalTimerCommand.Stop, decision.TimerCommand);
        Assert.AreEqual(ThermalProfileKind.CoolingC, decision.RequestedProfile);
    }

    /// <summary>
    /// Ensures the cadence tick promotes the system into the performance profile.
    /// </summary>
    [TestMethod]
    public void OnCadenceTick_RequestsPerformanceProfileWhenEligible()
    {
        ThermalProfileController controller = new();
        _ = controller.Reconcile(CreateConfig(statusEnabled: true), CreateSnapshot(AppLifecycleState.Running, TargetWindowState.WaitingForTarget));

        ThermalProfileDecision decision = controller.OnCadenceTick();

        Assert.AreEqual(ThermalTimerCommand.None, decision.TimerCommand);
        Assert.AreEqual(ThermalProfileKind.PerformanceA, decision.RequestedProfile);
    }

    /// <summary>
    /// Ensures waiting for a foreground target does not count as a manual close.
    /// </summary>
    [TestMethod]
    public void Reconcile_WaitingForTargetDoesNotForceCooling()
    {
        ThermalProfileController controller = new();
        _ = controller.Reconcile(CreateConfig(statusEnabled: true), CreateSnapshot(AppLifecycleState.Running, TargetWindowState.TargetMatched));

        ThermalProfileDecision decision = controller.Reconcile(
            CreateConfig(statusEnabled: true),
            CreateSnapshot(AppLifecycleState.Running, TargetWindowState.WaitingForTarget));

        Assert.AreEqual(ThermalTimerCommand.None, decision.TimerCommand);
        Assert.IsNull(decision.RequestedProfile);
    }

    private static CrosshairConfig CreateConfig(bool statusEnabled)
    {
        return new CrosshairConfig { StatusEnabled = statusEnabled };
    }

    private static HealthSnapshot CreateSnapshot(AppLifecycleState lifecycle, TargetWindowState target)
    {
        return new HealthSnapshot(
            new AppPresenceState(lifecycle, target, TelemetryFreshnessState.Fresh, 0),
            "Control Center",
            "CPU 4.2GHz | 82C",
            DateTimeOffset.Now);
    }
}
