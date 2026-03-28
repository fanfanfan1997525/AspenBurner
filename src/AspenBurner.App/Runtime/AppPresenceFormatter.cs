namespace AspenBurner.App.Runtime;

/// <summary>
/// Formats concise user-facing state text for tray and settings surfaces.
/// </summary>
public static class AppPresenceFormatter
{
    /// <summary>
    /// Builds a multiline tray tooltip for the current app presence state.
    /// </summary>
    public static string BuildTooltip(AppPresenceState state)
    {
        string lifecycle = state.Lifecycle switch
        {
            AppLifecycleState.Paused => "AspenBurner：已暂停",
            AppLifecycleState.Stopped => "AspenBurner：已停止",
            _ => "AspenBurner：运行中",
        };

        string target = state.Target switch
        {
            TargetWindowState.TargetMatched => "目标窗口已命中",
            TargetWindowState.DesktopPreview => $"桌面预览 {state.PreviewSecondsRemaining}s",
            _ => "等待目标窗口",
        };

        string telemetry = state.Telemetry switch
        {
            TelemetryFreshnessState.Fresh => "遥测正常",
            TelemetryFreshnessState.Stale => "遥测陈旧",
            _ => "遥测不可用",
        };

        return string.Join(Environment.NewLine, lifecycle, target, telemetry);
    }
}
