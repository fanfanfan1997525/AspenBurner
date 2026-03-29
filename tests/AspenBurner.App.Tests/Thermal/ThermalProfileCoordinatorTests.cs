using AspenBurner.App.Configuration;
using AspenBurner.App.Diagnostics;
using AspenBurner.App.Runtime;
using AspenBurner.App.Thermal;

namespace AspenBurner.App.Tests.Thermal;

/// <summary>
/// Covers timer and driver orchestration around the thermal profile state machine.
/// </summary>
[TestClass]
public sealed class ThermalProfileCoordinatorTests
{
    /// <summary>
    /// Ensures a supported machine arms the cadence timer instead of switching immediately.
    /// </summary>
    [TestMethod]
    public void UpdateHealth_StartsCadenceWhenDriverSupported()
    {
        FakeThermalCadenceTimer timer = new();
        FakeThermalProfileDriver driver = new(isSupported: true);
        using ThermalProfileCoordinator coordinator = CreateCoordinator(driver, timer);

        coordinator.UpdateConfig(new CrosshairConfig { StatusEnabled = true });
        coordinator.UpdateHealth(CreateSnapshot(AppLifecycleState.Running, TargetWindowState.WaitingForTarget));

        Assert.AreEqual(1, timer.StartCalls);
        Assert.AreEqual(0, driver.AppliedProfiles.Count);
    }

    /// <summary>
    /// Ensures the five-minute cadence promotes the machine into profile A.
    /// </summary>
    [TestMethod]
    public void CadenceTick_AppliesPerformanceProfile()
    {
        FakeThermalCadenceTimer timer = new();
        FakeThermalProfileDriver driver = new(isSupported: true);
        using ThermalProfileCoordinator coordinator = CreateCoordinator(driver, timer);

        coordinator.UpdateConfig(new CrosshairConfig { StatusEnabled = true });
        coordinator.UpdateHealth(CreateSnapshot(AppLifecycleState.Running, TargetWindowState.WaitingForTarget));
        timer.RaiseTick();

        CollectionAssert.AreEqual(new[] { ThermalProfileKind.PerformanceA }, driver.AppliedProfiles);
    }

    /// <summary>
    /// Ensures manual pause requests profile C and stops the cadence.
    /// </summary>
    [TestMethod]
    public void UpdateHealth_PauseAppliesCoolingAndStopsCadence()
    {
        FakeThermalCadenceTimer timer = new();
        FakeThermalProfileDriver driver = new(isSupported: true);
        using ThermalProfileCoordinator coordinator = CreateCoordinator(driver, timer);

        coordinator.UpdateConfig(new CrosshairConfig { StatusEnabled = true });
        coordinator.UpdateHealth(CreateSnapshot(AppLifecycleState.Running, TargetWindowState.TargetMatched));
        coordinator.UpdateHealth(CreateSnapshot(AppLifecycleState.Paused, TargetWindowState.WaitingForTarget));

        Assert.AreEqual(1, timer.StopCalls);
        CollectionAssert.Contains(driver.AppliedProfiles, ThermalProfileKind.CoolingC);
    }

    /// <summary>
    /// Ensures unsupported machines do not arm cadence or switch profiles.
    /// </summary>
    [TestMethod]
    public void UpdateHealth_DoesNothingWhenDriverUnsupported()
    {
        FakeThermalCadenceTimer timer = new();
        FakeThermalProfileDriver driver = new(isSupported: false);
        using ThermalProfileCoordinator coordinator = CreateCoordinator(driver, timer);

        coordinator.UpdateConfig(new CrosshairConfig { StatusEnabled = true });
        coordinator.UpdateHealth(CreateSnapshot(AppLifecycleState.Running, TargetWindowState.WaitingForTarget));
        timer.RaiseTick();

        Assert.AreEqual(0, timer.StartCalls);
        Assert.AreEqual(0, driver.AppliedProfiles.Count);
    }

    /// <summary>
    /// Ensures shutdown tries to return to profile C.
    /// </summary>
    [TestMethod]
    public void Shutdown_RequestsCoolingProfile()
    {
        FakeThermalCadenceTimer timer = new();
        FakeThermalProfileDriver driver = new(isSupported: true);
        using ThermalProfileCoordinator coordinator = CreateCoordinator(driver, timer);

        coordinator.UpdateConfig(new CrosshairConfig { StatusEnabled = true });
        coordinator.UpdateHealth(CreateSnapshot(AppLifecycleState.Running, TargetWindowState.TargetMatched));
        coordinator.Shutdown();

        CollectionAssert.Contains(driver.AppliedProfiles, ThermalProfileKind.CoolingC);
    }

    private static ThermalProfileCoordinator CreateCoordinator(FakeThermalProfileDriver driver, FakeThermalCadenceTimer timer)
    {
        string logDirectory = Path.Combine(Path.GetTempPath(), "AspenBurner.Tests", Guid.NewGuid().ToString("N"));
        return new ThermalProfileCoordinator(new AppLogger(logDirectory), driver, timer);
    }

    private static HealthSnapshot CreateSnapshot(AppLifecycleState lifecycle, TargetWindowState target)
    {
        return new HealthSnapshot(
            new AppPresenceState(lifecycle, target, TelemetryFreshnessState.Fresh, 0),
            "Control Center",
            "CPU 4.2GHz | 82C",
            DateTimeOffset.Now);
    }

    private sealed class FakeThermalProfileDriver : IThermalProfileDriver
    {
        public FakeThermalProfileDriver(bool isSupported)
        {
            this.IsSupported = isSupported;
        }

        public bool IsSupported { get; }

        public List<ThermalProfileKind> AppliedProfiles { get; } = [];

        public bool TryApply(ThermalProfileKind profile, out string message)
        {
            this.AppliedProfiles.Add(profile);
            message = "ok";
            return true;
        }
    }

    private sealed class FakeThermalCadenceTimer : IThermalCadenceTimer
    {
        public event EventHandler? Tick;

        public bool Enabled { get; private set; }

        public TimeSpan Interval { get; private set; }

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public void Dispose()
        {
        }

        public void RaiseTick()
        {
            this.Tick?.Invoke(this, EventArgs.Empty);
        }

        public void Start(TimeSpan interval)
        {
            this.Interval = interval;
            this.Enabled = true;
            this.StartCalls++;
        }

        public void Stop()
        {
            this.Enabled = false;
            this.StopCalls++;
        }
    }
}
