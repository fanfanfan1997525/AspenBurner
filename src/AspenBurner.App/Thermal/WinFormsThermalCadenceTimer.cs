namespace AspenBurner.App.Thermal;

/// <summary>
/// Wraps a WinForms timer behind a small cadence-timer abstraction.
/// </summary>
public sealed class WinFormsThermalCadenceTimer : IThermalCadenceTimer
{
    private readonly System.Windows.Forms.Timer timer = new();

    /// <summary>
    /// Initializes a new WinForms-backed cadence timer.
    /// </summary>
    public WinFormsThermalCadenceTimer()
    {
        this.timer.Tick += this.OnTick;
    }

    /// <inheritdoc />
    public event EventHandler? Tick;

    /// <inheritdoc />
    public bool Enabled => this.timer.Enabled;

    /// <inheritdoc />
    public TimeSpan Interval => TimeSpan.FromMilliseconds(this.timer.Interval);

    /// <inheritdoc />
    public void Dispose()
    {
        this.timer.Stop();
        this.timer.Tick -= this.OnTick;
        this.timer.Dispose();
    }

    /// <inheritdoc />
    public void Start(TimeSpan interval)
    {
        this.timer.Interval = (int)Math.Clamp(interval.TotalMilliseconds, 1, int.MaxValue);
        this.timer.Start();
    }

    /// <inheritdoc />
    public void Stop()
    {
        this.timer.Stop();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        this.Tick?.Invoke(this, EventArgs.Empty);
    }
}
