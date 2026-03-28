using AspenBurner.App.Configuration;
using AspenBurner.App.Core;
using AspenBurner.App.Diagnostics;
using AspenBurner.App.Telemetry;
using AspenBurner.App.UI;

namespace AspenBurner.App.Runtime;

/// <summary>
/// Owns the live overlay windows, target detection loop, and telemetry refresh loop.
/// </summary>
public sealed class OverlayRuntime : IDisposable
{
    private const int HideAfterMisses = 4;
    private const int StateTickMs = 200;
    private static readonly string[] TargetProcessNames = ["DeltaForceClient-Win64-Shipping", "delta_force_launcher"];

    private readonly AppLogger logger;
    private readonly IForegroundWindowSource foregroundWindowSource;
    private readonly CpuStatusService cpuStatusService;
    private readonly CrosshairOverlayForm crosshairForm;
    private readonly StatusOverlayForm statusForm;
    private readonly System.Windows.Forms.Timer stateTimer;
    private CrosshairConfig currentConfig = new();
    private AppLifecycleState lifecycleState = AppLifecycleState.Running;
    private TargetWindowState targetState = TargetWindowState.WaitingForTarget;
    private TelemetryFreshnessState telemetryFreshness = TelemetryFreshnessState.Unavailable;
    private DateTimeOffset previewEndsAt = DateTimeOffset.MinValue;
    private DateTimeOffset lastStatusRefreshAt = DateTimeOffset.MinValue;
    private DateTimeOffset lastTopMostRefreshAt = DateTimeOffset.MinValue;
    private string lastStatusText = string.Empty;
    private int missCount;
    private IntPtr lastTargetHandle = IntPtr.Zero;
    private bool disposed;

    /// <summary>
    /// Initializes a new overlay runtime.
    /// </summary>
    public OverlayRuntime(AppLogger logger, IForegroundWindowSource foregroundWindowSource, CpuStatusService cpuStatusService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.foregroundWindowSource = foregroundWindowSource ?? throw new ArgumentNullException(nameof(foregroundWindowSource));
        this.cpuStatusService = cpuStatusService ?? throw new ArgumentNullException(nameof(cpuStatusService));
        this.crosshairForm = new CrosshairOverlayForm();
        this.statusForm = new StatusOverlayForm();
        this.stateTimer = new System.Windows.Forms.Timer
        {
            Interval = StateTickMs,
        };
        this.stateTimer.Tick += this.OnStateTick;
    }

    /// <summary>
    /// Raised whenever the user-visible runtime health snapshot changes.
    /// </summary>
    public event EventHandler<HealthSnapshot>? HealthChanged;

    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    public AppLifecycleState LifecycleState => this.lifecycleState;

    /// <summary>
    /// Starts the runtime timers.
    /// </summary>
    public void Start()
    {
        this.crosshairForm.Hide();
        this.statusForm.Hide();
        this.stateTimer.Start();
        this.PublishHealth();
    }

    /// <summary>
    /// Applies a new configuration immediately.
    /// </summary>
    public void UpdateConfig(CrosshairConfig config)
    {
        this.currentConfig = config ?? throw new ArgumentNullException(nameof(config));
        this.crosshairForm.ApplyConfig(config);
        this.lastStatusRefreshAt = DateTimeOffset.MinValue;
        this.OnStateTick(this, EventArgs.Empty);
    }

    /// <summary>
    /// Starts a temporary desktop preview.
    /// </summary>
    public void StartDesktopPreview(int seconds)
    {
        int boundedSeconds = Math.Clamp(seconds, 1, 30);
        this.previewEndsAt = DateTimeOffset.Now.AddSeconds(boundedSeconds);
        this.lifecycleState = AppLifecycleState.Running;
        this.logger.Info($"Desktop preview started for {boundedSeconds}s.");
        this.OnStateTick(this, EventArgs.Empty);
    }

    /// <summary>
    /// Pauses all overlay drawing while keeping the process resident.
    /// </summary>
    public void Pause()
    {
        this.previewEndsAt = DateTimeOffset.MinValue;
        this.lifecycleState = AppLifecycleState.Paused;
        this.HideAll();
        this.logger.Info("Overlay paused.");
        this.PublishHealth();
    }

    /// <summary>
    /// Resumes normal runtime operation.
    /// </summary>
    public void Resume()
    {
        this.lifecycleState = AppLifecycleState.Running;
        this.logger.Info("Overlay resumed.");
        this.OnStateTick(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns the latest user-facing health snapshot.
    /// </summary>
    public HealthSnapshot GetHealthSnapshot()
    {
        return this.BuildHealthSnapshot();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.stateTimer.Stop();
        this.stateTimer.Tick -= this.OnStateTick;
        this.stateTimer.Dispose();
        this.HideAll();
        this.statusForm.Close();
        this.crosshairForm.Close();
        this.statusForm.Dispose();
        this.crosshairForm.Dispose();
        this.cpuStatusService.Dispose();
    }

    private void OnStateTick(object? sender, EventArgs e)
    {
        try
        {
            this.OnStateTickCore();
        }
        catch (Exception exception)
        {
            this.HandleStateTickFailure(exception);
        }
    }

    private void OnStateTickCore()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        bool previewActive = this.previewEndsAt > now;
        TargetWindowInfo? foregroundWindow = this.foregroundWindowSource.TryGetForegroundWindow();
        bool targetMatched = foregroundWindow is not null && TargetProcessNames.Contains(foregroundWindow.ProcessName, StringComparer.OrdinalIgnoreCase);

        if (this.lifecycleState == AppLifecycleState.Paused)
        {
            this.targetState = TargetWindowState.WaitingForTarget;
            this.HideAll();
            this.PublishHealth();
            return;
        }

        this.targetState = previewActive
            ? TargetWindowState.DesktopPreview
            : targetMatched ? TargetWindowState.TargetMatched : TargetWindowState.WaitingForTarget;

        OverlayVisibilityState visibilityState = OverlayVisibilityStateMachine.Next(
            this.crosshairForm.Visible,
            previewActive || targetMatched,
            this.missCount,
            HideAfterMisses);
        this.missCount = visibilityState.MissCount;

        if (!visibilityState.ShouldShow)
        {
            this.HideAll();
            this.lastTargetHandle = IntPtr.Zero;

            if (this.previewEndsAt != DateTimeOffset.MinValue && !previewActive)
            {
                this.previewEndsAt = DateTimeOffset.MinValue;
                this.logger.Info("Desktop preview ended; returning to target-window detection.");
            }

            this.PublishHealth();
            return;
        }

        Rectangle areaBounds = previewActive
            ? Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080)
            : foregroundWindow?.Bounds ?? Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

        this.ApplyCrosshairBounds(areaBounds);
        this.UpdateStatusOverlay(areaBounds, now);
        this.EnsureVisible(now, foregroundWindow?.Handle ?? IntPtr.Zero);
        this.PublishHealth();
    }

    private void ApplyCrosshairBounds(Rectangle areaBounds)
    {
        Rectangle overlayBounds = CrosshairGeometry.GetOverlayBounds(
            areaBounds.Left,
            areaBounds.Top,
            areaBounds.Width,
            areaBounds.Height,
            this.currentConfig.Length,
            this.currentConfig.Gap,
            this.currentConfig.Thickness,
            this.currentConfig.OutlineThickness,
            this.currentConfig.OffsetX,
            this.currentConfig.OffsetY);

        if (this.crosshairForm.Bounds != overlayBounds)
        {
            this.crosshairForm.Bounds = overlayBounds;
            this.crosshairForm.Invalidate();
        }
    }

    private void UpdateStatusOverlay(Rectangle areaBounds, DateTimeOffset now)
    {
        if (!this.currentConfig.StatusEnabled)
        {
            this.lastStatusText = string.Empty;
            this.statusForm.Hide();
            this.telemetryFreshness = this.cpuStatusService.GetFreshness(now);
            return;
        }

        if (!this.statusForm.Visible || now - this.lastStatusRefreshAt >= TimeSpan.FromMilliseconds(this.currentConfig.StatusRefreshMs))
        {
            CpuStatusSnapshot snapshot = this.cpuStatusService.Capture();
            this.lastStatusText = StatusTextFormatter.FormatCpuStatus(
                snapshot.FrequencyMHz,
                snapshot.TemperatureC,
                snapshot.ApproximateTemperature,
                this.currentConfig.StatusShowTemperature);
            this.statusForm.ApplyStatus(this.lastStatusText, this.currentConfig.StatusFontSize, ColorResolver.ResolveStatusColor(this.currentConfig));
            this.lastStatusRefreshAt = now;
        }

        this.telemetryFreshness = this.cpuStatusService.GetFreshness(now);
        Rectangle statusBounds = StatusOverlayPlacement.GetBounds(
            areaBounds.Left,
            areaBounds.Top,
            areaBounds.Width,
            areaBounds.Height,
            this.statusForm.Width,
            this.statusForm.Height,
            this.currentConfig.StatusPosition,
            this.currentConfig.StatusOffsetX,
            this.currentConfig.StatusOffsetY);

        if (this.statusForm.Bounds != statusBounds)
        {
            this.statusForm.Bounds = statusBounds;
        }
    }

    private void EnsureVisible(DateTimeOffset now, IntPtr targetHandle)
    {
        if (!this.crosshairForm.Visible)
        {
            this.crosshairForm.Show();
        }

        if (this.currentConfig.StatusEnabled && !this.statusForm.Visible)
        {
            this.statusForm.Show();
        }

        if (targetHandle != this.lastTargetHandle || now - this.lastTopMostRefreshAt >= TimeSpan.FromSeconds(2))
        {
            this.crosshairForm.PinTopMost();

            if (this.currentConfig.StatusEnabled)
            {
                this.statusForm.PinTopMost();
            }

            this.lastTargetHandle = targetHandle;
            this.lastTopMostRefreshAt = now;
        }
    }

    private void HideAll()
    {
        if (this.crosshairForm.Visible)
        {
            this.crosshairForm.Hide();
        }

        if (this.statusForm.Visible)
        {
            this.statusForm.Hide();
        }
    }

    private void PublishHealth()
    {
        this.HealthChanged?.Invoke(this, this.BuildHealthSnapshot());
    }

    private void HandleStateTickFailure(Exception exception)
    {
        this.logger.Error("Overlay state tick failed.", exception);
        this.previewEndsAt = DateTimeOffset.MinValue;
        this.targetState = TargetWindowState.WaitingForTarget;
        this.telemetryFreshness = this.cpuStatusService.GetFreshness(DateTimeOffset.Now);
        this.lastTargetHandle = IntPtr.Zero;
        this.HideAll();
        this.PublishHealth();
    }

    private HealthSnapshot BuildHealthSnapshot()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        int previewSecondsRemaining = this.previewEndsAt > now
            ? (int)Math.Ceiling((this.previewEndsAt - now).TotalSeconds)
            : 0;

        AppPresenceState presence = new(
            this.lifecycleState,
            this.targetState,
            this.telemetryFreshness,
            previewSecondsRemaining);

        string telemetrySource = this.cpuStatusService.LastSnapshot?.Source ?? "Unavailable";
        return new HealthSnapshot(presence, telemetrySource, this.lastStatusText, now);
    }
}
