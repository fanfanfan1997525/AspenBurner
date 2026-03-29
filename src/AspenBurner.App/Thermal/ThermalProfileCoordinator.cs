using AspenBurner.App.Configuration;
using AspenBurner.App.Diagnostics;

namespace AspenBurner.App.Thermal;

/// <summary>
/// Bridges the pure thermal state machine to timer and driver side effects.
/// </summary>
public sealed class ThermalProfileCoordinator : IDisposable
{
    private static readonly TimeSpan PromotionCadence = TimeSpan.FromMinutes(5);

    private readonly AppLogger logger;
    private readonly IThermalProfileDriver driver;
    private readonly IThermalCadenceTimer timer;
    private readonly ThermalProfileController controller = new();
    private CrosshairConfig currentConfig = new();
    private bool disposed;

    /// <summary>
    /// Initializes a new coordinator.
    /// </summary>
    public ThermalProfileCoordinator(AppLogger logger, IThermalProfileDriver driver, IThermalCadenceTimer timer)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
        this.timer = timer ?? throw new ArgumentNullException(nameof(timer));
        this.timer.Tick += this.OnCadenceTick;
    }

    /// <summary>
    /// Updates the latest persisted config.
    /// </summary>
    public void UpdateConfig(CrosshairConfig config)
    {
        this.currentConfig = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Updates runtime health and reconciles any requested thermal action.
    /// </summary>
    public void UpdateHealth(HealthSnapshot snapshot)
    {
        if (!this.driver.IsSupported)
        {
            return;
        }

        this.ApplyDecision(this.controller.Reconcile(this.currentConfig, snapshot));
    }

    /// <summary>
    /// Requests a final cooling fallback during shutdown.
    /// </summary>
    public void Shutdown()
    {
        if (!this.driver.IsSupported)
        {
            this.timer.Stop();
            return;
        }

        this.ApplyDecision(this.controller.OnShutdown());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.timer.Tick -= this.OnCadenceTick;
        this.timer.Dispose();
    }

    private void OnCadenceTick(object? sender, EventArgs e)
    {
        if (!this.driver.IsSupported)
        {
            this.timer.Stop();
            return;
        }

        this.ApplyDecision(this.controller.OnCadenceTick());
    }

    private void ApplyDecision(ThermalProfileDecision decision)
    {
        switch (decision.TimerCommand)
        {
            case ThermalTimerCommand.Start:
                this.timer.Start(PromotionCadence);
                break;
            case ThermalTimerCommand.Stop:
                this.timer.Stop();
                break;
        }

        if (decision.RequestedProfile is not ThermalProfileKind profile)
        {
            return;
        }

        if (this.driver.TryApply(profile, out string message))
        {
            this.logger.Info($"Thermal profile applied: {profile}. {message}");
        }
        else
        {
            this.logger.Info($"Thermal profile apply skipped/failed: {profile}. {message}");
        }
    }
}
