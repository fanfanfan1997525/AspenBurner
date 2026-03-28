using AspenBurner.App.Runtime;
using AspenBurner.App.Telemetry;

namespace AspenBurner.App.Tests.Telemetry;

/// <summary>
/// Contract tests for CPU telemetry merge and freshness rules.
/// </summary>
[TestClass]
public sealed class CpuStatusServiceTests
{
    /// <summary>
    /// Ensures preferred provider values win when available.
    /// </summary>
    [TestMethod]
    public void Capture_UsesPreferredProviderWhenSignalExists()
    {
        using CpuStatusService service = new(
            new FakeProvider(new CpuStatusSnapshot(4227, 82, false, "Control Center", DateTimeOffset.Now)),
            new FakeProvider(new CpuStatusSnapshot(3200, null, false, "Fallback", DateTimeOffset.Now)));

        CpuStatusSnapshot snapshot = service.Capture();

        Assert.AreEqual(4227, snapshot.FrequencyMHz);
        Assert.AreEqual(82, snapshot.TemperatureC);
        Assert.AreEqual("Control Center", snapshot.Source);
    }

    /// <summary>
    /// Ensures fallback frequency is used when preferred provider has no signal.
    /// </summary>
    [TestMethod]
    public void Capture_FallsBackWhenPreferredProviderHasNoSignal()
    {
        using CpuStatusService service = new(
            new FakeProvider(new CpuStatusSnapshot(0, null, false, "Unavailable", DateTimeOffset.Now)),
            new FakeProvider(new CpuStatusSnapshot(3185, null, false, "Fallback", DateTimeOffset.Now)));

        CpuStatusSnapshot snapshot = service.Capture();

        Assert.AreEqual(3185, snapshot.FrequencyMHz);
        Assert.IsNull(snapshot.TemperatureC);
        Assert.AreEqual("Fallback", snapshot.Source);
    }

    /// <summary>
    /// Ensures freshness is unavailable before any sample exists.
    /// </summary>
    [TestMethod]
    public void GetFreshness_IsUnavailableBeforeCapture()
    {
        using CpuStatusService service = new(
            new FakeProvider(new CpuStatusSnapshot(0, null, false, "Unavailable", DateTimeOffset.Now)),
            new FakeProvider(new CpuStatusSnapshot(0, null, false, "Unavailable", DateTimeOffset.Now)));

        TelemetryFreshnessState freshness = service.GetFreshness(DateTimeOffset.Now);

        Assert.AreEqual(TelemetryFreshnessState.Unavailable, freshness);
    }

    /// <summary>
    /// Ensures freshness degrades after the stale window expires.
    /// </summary>
    [TestMethod]
    public void GetFreshness_BecomesStaleAfterThreshold()
    {
        using CpuStatusService service = new(
            new FakeProvider(new CpuStatusSnapshot(4000, 80, false, "Control Center", DateTimeOffset.Now)),
            new FakeProvider(new CpuStatusSnapshot(3200, null, false, "Fallback", DateTimeOffset.Now)));

        CpuStatusSnapshot snapshot = service.Capture();
        TelemetryFreshnessState freshness = service.GetFreshness(snapshot.CapturedAt.AddSeconds(4));

        Assert.AreEqual(TelemetryFreshnessState.Stale, freshness);
    }

    private sealed class FakeProvider : ICpuStatusProvider
    {
        private readonly CpuStatusSnapshot snapshot;

        public FakeProvider(CpuStatusSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public CpuStatusSnapshot Capture()
        {
            return this.snapshot with { CapturedAt = DateTimeOffset.Now };
        }

        public void Dispose()
        {
        }
    }
}
