using System.Diagnostics;
using AspenBurner.App.Configuration;
using AspenBurner.App.Diagnostics;
using AspenBurner.App.Runtime;
using AspenBurner.App.Telemetry;
using AspenBurner.App.UI;

namespace AspenBurner.App.Application;

/// <summary>
/// Coordinates single-instance lifecycle, tray UX, settings UI, and overlay runtime.
/// </summary>
public sealed class AspenBurnerApplicationContext : global::System.Windows.Forms.ApplicationContext
{
    private readonly string configPath;
    private readonly CrosshairConfigService configService;
    private readonly AppLogger logger;
    private readonly OverlayRuntime overlayRuntime;
    private readonly SettingsForm settingsForm;
    private readonly NotifyIcon notifyIcon;
    private readonly AppCommandServer commandServer;
    private readonly System.Windows.Forms.Timer saveTimer;
    private readonly ToolStripMenuItem lifecycleMenuItem;
    private readonly ToolStripMenuItem targetMenuItem;
    private readonly ToolStripMenuItem telemetryMenuItem;
    private readonly ToolStripMenuItem pauseResumeMenuItem;
    private readonly Control uiInvoker;
    private CrosshairConfig currentConfig;
    private bool disposed;

    /// <summary>
    /// Initializes a new application context.
    /// </summary>
    public AspenBurnerApplicationContext(string configPath, AppCommand? initialCommand = null)
    {
        this.configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
        this.configService = new CrosshairConfigService();

        string repositoryRoot = Directory.GetParent(Path.GetDirectoryName(configPath) ?? configPath)?.FullName
            ?? AppContext.BaseDirectory;
        string logDirectory = Path.Combine(repositoryRoot, "logs");
        this.logger = new AppLogger(logDirectory);
        this.logger.Info("Application context initialization started.");

        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory);
        bool createdDefaultConfig = false;
        if (!File.Exists(configPath))
        {
            this.configService.SaveToFile(configPath, this.configService.CreateDefault());
            createdDefaultConfig = true;
        }

        this.currentConfig = this.configService.LoadFromFile(configPath);
        this.uiInvoker = new Control();
        this.uiInvoker.CreateControl();

        CpuStatusService cpuStatusService = new(new ControlCenterCpuStatusProvider(), new FallbackCpuStatusProvider());
        this.overlayRuntime = new OverlayRuntime(this.logger, new ForegroundWindowSource(), cpuStatusService);

        this.settingsForm = new SettingsForm();
        this.settingsForm.SetConfig(this.currentConfig);
        this.settingsForm.ConfigEdited += this.OnConfigEdited;
        this.settingsForm.SaveRequested += this.OnSaveRequested;
        this.settingsForm.PreviewRequested += this.OnPreviewRequested;
        this.settingsForm.ExitRequested += this.OnExitRequested;

        this.saveTimer = new System.Windows.Forms.Timer
        {
            Interval = 300,
        };
        this.saveTimer.Tick += this.OnSaveTimerTick;

        ContextMenuStrip trayMenu = new();
        this.lifecycleMenuItem = new ToolStripMenuItem("AspenBurner") { Enabled = false };
        this.targetMenuItem = new ToolStripMenuItem("等待目标窗口") { Enabled = false };
        this.telemetryMenuItem = new ToolStripMenuItem("遥测：Unavailable") { Enabled = false };
        ToolStripMenuItem showSettingsItem = new("显示设置", null, (_, _) => this.ShowSettings());
        ToolStripMenuItem previewItem = new("桌面预览 8 秒", null, (_, _) => this.StartPreview());
        this.pauseResumeMenuItem = new ToolStripMenuItem("暂停显示", null, (_, _) => this.TogglePause());
        ToolStripMenuItem openLogFolderItem = new("打开日志目录", null, (_, _) => this.OpenLogFolder());
        ToolStripMenuItem exitItem = new("退出程序", null, (_, _) => this.ExitThread());

        trayMenu.Items.AddRange(
        [
            this.lifecycleMenuItem,
            this.targetMenuItem,
            this.telemetryMenuItem,
            new ToolStripSeparator(),
            showSettingsItem,
            previewItem,
            this.pauseResumeMenuItem,
            openLogFolderItem,
            exitItem,
        ]);

        this.notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "AspenBurner",
            Visible = true,
            ContextMenuStrip = trayMenu,
        };
        this.notifyIcon.DoubleClick += (_, _) => this.ShowSettings();

        this.overlayRuntime.HealthChanged += this.OnHealthChanged;
        this.overlayRuntime.UpdateConfig(this.currentConfig);
        this.overlayRuntime.Start();
        this.logger.Info("Overlay runtime started.");

        this.commandServer = new AppCommandServer(
            AppIdentity.PipeName,
            this.DispatchRemoteCommand,
            exception => this.logger.Error("Named-pipe command server error.", exception));
        this.commandServer.Start();

        this.logger.Info("AspenBurner runtime started.");
        this.UpdateTrayFromHealth(this.overlayRuntime.GetHealthSnapshot());

        if (createdDefaultConfig || initialCommand?.Kind == AppCommandKind.ShowSettings)
        {
            this.ShowSettings();
        }

        if (initialCommand?.Kind == AppCommandKind.Preview)
        {
            this.overlayRuntime.StartDesktopPreview(initialCommand.Value.Argument <= 0 ? 8 : initialCommand.Value.Argument);
        }

        if (initialCommand?.Kind == AppCommandKind.Resume)
        {
            this.overlayRuntime.Resume();
        }
    }

    /// <inheritdoc />
    protected override void ExitThreadCore()
    {
        if (this.disposed)
        {
            base.ExitThreadCore();
            return;
        }

        this.disposed = true;
        this.logger.Info("AspenBurner runtime stopping.");

        this.commandServer.Dispose();
        this.saveTimer.Stop();
        this.SaveCurrentConfig("退出前已保存配置。");

        this.notifyIcon.Visible = false;
        this.notifyIcon.Dispose();

        this.settingsForm.AllowExit = true;
        this.settingsForm.Close();
        this.settingsForm.Dispose();

        this.overlayRuntime.Dispose();
        this.uiInvoker.Dispose();

        base.ExitThreadCore();
    }

    private void DispatchRemoteCommand(AppCommand command)
    {
        if (this.uiInvoker.IsDisposed)
        {
            return;
        }

        this.uiInvoker.BeginInvoke(new Action(() =>
        {
            switch (command.Kind)
            {
                case AppCommandKind.ShowSettings:
                    this.ShowSettings();
                    break;
                case AppCommandKind.Preview:
                    this.overlayRuntime.StartDesktopPreview(command.Argument <= 0 ? 8 : command.Argument);
                    this.settingsForm.SetFeedback("桌面预览已启动。");
                    break;
                case AppCommandKind.Resume:
                    this.overlayRuntime.Resume();
                    this.settingsForm.SetFeedback("准心已恢复显示。");
                    break;
                case AppCommandKind.Stop:
                    this.ExitThread();
                    break;
                case AppCommandKind.Health:
                    this.ShowSettings();
                    break;
            }
        }));
    }

    private void OnConfigEdited(object? sender, CrosshairConfig config)
    {
        this.currentConfig = config;
        this.overlayRuntime.UpdateConfig(config);
        this.saveTimer.Stop();
        this.saveTimer.Start();
    }

    private void OnSaveRequested(object? sender, EventArgs e)
    {
        this.saveTimer.Stop();
        this.SaveCurrentConfig("已保存并应用。");
    }

    private void OnPreviewRequested(object? sender, EventArgs e)
    {
        this.StartPreview();
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        this.ExitThread();
    }

    private void OnSaveTimerTick(object? sender, EventArgs e)
    {
        this.saveTimer.Stop();
        this.SaveCurrentConfig("已保存并应用。");
    }

    private void OnHealthChanged(object? sender, HealthSnapshot snapshot)
    {
        this.UpdateTrayFromHealth(snapshot);
        this.settingsForm.UpdateHealth(snapshot);
    }

    private void SaveCurrentConfig(string successMessage)
    {
        try
        {
            this.configService.SaveToFile(this.configPath, this.currentConfig);
            this.settingsForm.SetFeedback(successMessage);
        }
        catch (Exception exception)
        {
            this.logger.Error("Failed to save configuration.", exception);
            this.settingsForm.SetFeedback($"保存失败：{exception.Message}", isError: true);
        }
    }

    private void StartPreview()
    {
        this.overlayRuntime.StartDesktopPreview(8);
        this.settingsForm.SetFeedback("桌面预览 8 秒已启动。");
        this.ShowSettings();
    }

    private void TogglePause()
    {
        if (this.overlayRuntime.LifecycleState == AppLifecycleState.Paused)
        {
            this.overlayRuntime.Resume();
            this.settingsForm.SetFeedback("准心已恢复显示。");
        }
        else
        {
            this.overlayRuntime.Pause();
            this.settingsForm.SetFeedback("准心已暂停显示。");
        }
    }

    private void ShowSettings()
    {
        if (!this.settingsForm.Visible)
        {
            this.settingsForm.Show();
        }

        if (this.settingsForm.WindowState == FormWindowState.Minimized)
        {
            this.settingsForm.WindowState = FormWindowState.Normal;
        }

        this.settingsForm.BringToFront();
        this.settingsForm.Activate();
    }

    private void OpenLogFolder()
    {
        try
        {
            string logDirectory = Path.GetDirectoryName(this.logger.LogFilePath) ?? AppContext.BaseDirectory;
            Process.Start("explorer.exe", logDirectory);
        }
        catch (Exception exception)
        {
            this.logger.Error("Failed to open log folder.", exception);
        }
    }

    private void UpdateTrayFromHealth(HealthSnapshot snapshot)
    {
        this.lifecycleMenuItem.Text = snapshot.Presence.Lifecycle switch
        {
            AppLifecycleState.Paused => "AspenBurner：已暂停",
            AppLifecycleState.Stopped => "AspenBurner：已停止",
            _ => "AspenBurner：运行中",
        };

        this.targetMenuItem.Text = snapshot.Presence.Target switch
        {
            TargetWindowState.TargetMatched => "目标窗口已命中",
            TargetWindowState.DesktopPreview => $"桌面预览中（{snapshot.Presence.PreviewSecondsRemaining}s）",
            _ => "等待目标窗口",
        };

        string freshness = snapshot.Presence.Telemetry switch
        {
            TelemetryFreshnessState.Fresh => "Fresh",
            TelemetryFreshnessState.Stale => "Stale",
            _ => "Unavailable",
        };
        this.telemetryMenuItem.Text = $"遥测：{snapshot.TelemetrySource} | {freshness}";
        this.pauseResumeMenuItem.Text = snapshot.Presence.Lifecycle == AppLifecycleState.Paused
            ? "恢复显示"
            : "暂停显示";

        string tooltip = $"{this.lifecycleMenuItem.Text} | {this.targetMenuItem.Text}";
        this.notifyIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
    }
}
