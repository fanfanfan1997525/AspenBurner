namespace AspenBurner.App.Runtime;

/// <summary>
/// Represents the user-visible presence state shown in tooltips and settings.
/// </summary>
public readonly record struct AppPresenceState(
    AppLifecycleState Lifecycle,
    TargetWindowState Target,
    TelemetryFreshnessState Telemetry,
    int PreviewSecondsRemaining);
