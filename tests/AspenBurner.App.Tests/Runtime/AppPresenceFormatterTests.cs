using AspenBurner.App.Runtime;

namespace AspenBurner.App.Tests.Runtime;

/// <summary>
/// UX contract tests for tray and settings status wording.
/// </summary>
[TestClass]
public sealed class AppPresenceFormatterTests
{
    /// <summary>
    /// Ensures waiting-for-target state is explicit.
    /// </summary>
    [TestMethod]
    public void BuildTooltip_DescribesWaitingForTarget()
    {
        string text = AppPresenceFormatter.BuildTooltip(new AppPresenceState(
            Lifecycle: AppLifecycleState.Running,
            Target: TargetWindowState.WaitingForTarget,
            Telemetry: TelemetryFreshnessState.Fresh,
            PreviewSecondsRemaining: 0));

        StringAssert.Contains(text, "等待目标窗口");
    }

    /// <summary>
    /// Ensures preview state is explicit and includes countdown.
    /// </summary>
    [TestMethod]
    public void BuildTooltip_DescribesDesktopPreviewCountdown()
    {
        string text = AppPresenceFormatter.BuildTooltip(new AppPresenceState(
            Lifecycle: AppLifecycleState.Running,
            Target: TargetWindowState.DesktopPreview,
            Telemetry: TelemetryFreshnessState.Fresh,
            PreviewSecondsRemaining: 6));

        StringAssert.Contains(text, "桌面预览");
        StringAssert.Contains(text, "6s");
    }

    /// <summary>
    /// Ensures paused state is explicit.
    /// </summary>
    [TestMethod]
    public void BuildTooltip_DescribesPausedState()
    {
        string text = AppPresenceFormatter.BuildTooltip(new AppPresenceState(
            Lifecycle: AppLifecycleState.Paused,
            Target: TargetWindowState.WaitingForTarget,
            Telemetry: TelemetryFreshnessState.Fresh,
            PreviewSecondsRemaining: 0));

        StringAssert.Contains(text, "已暂停");
    }

    /// <summary>
    /// Ensures telemetry faults are visible.
    /// </summary>
    [TestMethod]
    public void BuildTooltip_DescribesTelemetryFaults()
    {
        string text = AppPresenceFormatter.BuildTooltip(new AppPresenceState(
            Lifecycle: AppLifecycleState.Running,
            Target: TargetWindowState.TargetMatched,
            Telemetry: TelemetryFreshnessState.Unavailable,
            PreviewSecondsRemaining: 0));

        StringAssert.Contains(text, "遥测不可用");
    }
}
