using System.Drawing;
using AspenBurner.App.Configuration;
using AspenBurner.App.Diagnostics;
using AspenBurner.App.Runtime;
using AspenBurner.App.Telemetry;

namespace AspenBurner.App.Tests.Runtime;

/// <summary>
/// Regression coverage for runtime crash shielding and immediate config application.
/// </summary>
[TestClass]
public sealed class OverlayRuntimeTests
{
    /// <summary>
    /// Ensures foreground-window failures do not escape the UI tick path.
    /// </summary>
    [TestMethod]
    public void Resume_DoesNotThrowWhenForegroundWindowSourceFails()
    {
        RunInSta(() =>
        {
            using OverlayRuntime runtime = CreateRuntime(
                new ThrowingForegroundWindowSource(),
                new CpuStatusService(
                    new FakeProvider(new CpuStatusSnapshot(4227, 82, false, "Control Center", DateTimeOffset.Now)),
                    new FakeProvider(new CpuStatusSnapshot(3200, null, false, "Fallback", DateTimeOffset.Now))));

            runtime.Start();
            runtime.Resume();

            HealthSnapshot snapshot = runtime.GetHealthSnapshot();
            Assert.AreEqual(TargetWindowState.WaitingForTarget, snapshot.Presence.Target);
        });
    }

    /// <summary>
    /// Ensures config changes refresh the live status text without waiting for a timer tick.
    /// </summary>
    [TestMethod]
    public void UpdateConfig_RefreshesStatusTextImmediatelyWhenTargetIsMatched()
    {
        RunInSta(() =>
        {
            using OverlayRuntime runtime = CreateRuntime(
                new FixedForegroundWindowSource(new TargetWindowInfo(
                    IntPtr.Zero,
                    "DeltaForceClient-Win64-Shipping",
                    new Rectangle(0, 0, 1920, 1080))),
                new CpuStatusService(
                    new FakeProvider(new CpuStatusSnapshot(4227, 82, false, "Control Center", DateTimeOffset.Now)),
                    new FakeProvider(new CpuStatusSnapshot(3200, null, false, "Fallback", DateTimeOffset.Now))));

            runtime.Start();
            runtime.UpdateConfig(new CrosshairConfig { StatusEnabled = true, StatusShowTemperature = true });

            HealthSnapshot snapshot = runtime.GetHealthSnapshot();
            Assert.AreEqual(TargetWindowState.TargetMatched, snapshot.Presence.Target);
            StringAssert.Contains(snapshot.LastStatusText, "CPU 4.2GHz");
            StringAssert.Contains(snapshot.LastStatusText, "82C");
        });
    }

    private static OverlayRuntime CreateRuntime(IForegroundWindowSource foregroundWindowSource, CpuStatusService cpuStatusService)
    {
        string logDirectory = Path.Combine(Path.GetTempPath(), "AspenBurner.Tests", Guid.NewGuid().ToString("N"));
        return new OverlayRuntime(new AppLogger(logDirectory), foregroundWindowSource, cpuStatusService);
    }

    private static void RunInSta(Action action)
    {
        Exception? captured = null;
        using ManualResetEventSlim completed = new(false);
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                captured = exception;
            }
            finally
            {
                completed.Set();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(completed.Wait(TimeSpan.FromSeconds(15)), "STA test timed out.");
        thread.Join();

        if (captured is not null)
        {
            throw new AssertFailedException("STA test failed.", captured);
        }
    }

    private sealed class FixedForegroundWindowSource : IForegroundWindowSource
    {
        private readonly TargetWindowInfo targetWindowInfo;

        public FixedForegroundWindowSource(TargetWindowInfo targetWindowInfo)
        {
            this.targetWindowInfo = targetWindowInfo;
        }

        public TargetWindowInfo? TryGetForegroundWindow()
        {
            return this.targetWindowInfo;
        }
    }

    private sealed class ThrowingForegroundWindowSource : IForegroundWindowSource
    {
        public TargetWindowInfo? TryGetForegroundWindow()
        {
            throw new InvalidOperationException("boom");
        }
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
