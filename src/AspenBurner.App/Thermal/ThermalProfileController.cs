using AspenBurner.App.Configuration;
using AspenBurner.App.Diagnostics;
using AspenBurner.App.Runtime;

namespace AspenBurner.App.Thermal;

/// <summary>
/// Pure state machine that decides when the app should promote to profile A or fall back to profile C.
/// </summary>
public sealed class ThermalProfileController
{
    private CrosshairConfig currentConfig = new();
    private HealthSnapshot? lastSnapshot;
    private bool cadenceRunning;
    private ThermalProfileKind? lastRequestedProfile;

    /// <summary>
    /// Reconciles the latest config and runtime snapshot.
    /// </summary>
    public ThermalProfileDecision Reconcile(CrosshairConfig config, HealthSnapshot snapshot)
    {
        this.currentConfig = config ?? throw new ArgumentNullException(nameof(config));
        this.lastSnapshot = snapshot;

        if (RequiresCoolingFallback(snapshot, config))
        {
            this.cadenceRunning = false;
            return this.Request(ThermalTimerCommand.Stop, ThermalProfileKind.CoolingC);
        }

        if (IsPromotionEligible(snapshot, config))
        {
            if (!this.cadenceRunning)
            {
                this.cadenceRunning = true;
                return this.Request(ThermalTimerCommand.Start, null);
            }

            return this.Request(ThermalTimerCommand.None, null);
        }

        if (this.cadenceRunning)
        {
            this.cadenceRunning = false;
            return this.Request(ThermalTimerCommand.Stop, null);
        }

        return this.Request(ThermalTimerCommand.None, null);
    }

    /// <summary>
    /// Handles the five-minute cadence tick while runtime stays armed.
    /// </summary>
    public ThermalProfileDecision OnCadenceTick()
    {
        if (this.lastSnapshot is not null && IsPromotionEligible(this.lastSnapshot, this.currentConfig))
        {
            return this.Request(ThermalTimerCommand.None, ThermalProfileKind.PerformanceA);
        }

        if (this.cadenceRunning)
        {
            this.cadenceRunning = false;
            return this.Request(ThermalTimerCommand.Stop, null);
        }

        return this.Request(ThermalTimerCommand.None, null);
    }

    /// <summary>
    /// Requests a final fallback to profile C during application shutdown.
    /// </summary>
    public ThermalProfileDecision OnShutdown()
    {
        this.cadenceRunning = false;
        return this.Request(ThermalTimerCommand.Stop, ThermalProfileKind.CoolingC);
    }

    private static bool IsPromotionEligible(HealthSnapshot snapshot, CrosshairConfig config)
    {
        return snapshot.Presence.Lifecycle == AppLifecycleState.Running &&
               config.StatusEnabled &&
               snapshot.Presence.Target != TargetWindowState.DesktopPreview;
    }

    private static bool RequiresCoolingFallback(HealthSnapshot snapshot, CrosshairConfig config)
    {
        return snapshot.Presence.Lifecycle is AppLifecycleState.Paused or AppLifecycleState.Stopped ||
               !config.StatusEnabled;
    }

    private ThermalProfileDecision Request(ThermalTimerCommand timerCommand, ThermalProfileKind? profile)
    {
        if (profile is not null)
        {
            this.lastRequestedProfile = profile;
        }

        return new ThermalProfileDecision(timerCommand, profile);
    }
}
