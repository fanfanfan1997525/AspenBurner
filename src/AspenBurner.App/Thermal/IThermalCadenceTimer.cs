namespace AspenBurner.App.Thermal;

/// <summary>
/// Abstracts the five-minute cadence timer used to reassert profile A.
/// </summary>
public interface IThermalCadenceTimer : IDisposable
{
    /// <summary>
    /// Raised when the cadence interval elapses.
    /// </summary>
    event EventHandler? Tick;

    /// <summary>
    /// Gets a value indicating whether the timer is currently running.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Gets the active cadence interval.
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// Starts the cadence timer using the provided interval.
    /// </summary>
    void Start(TimeSpan interval);

    /// <summary>
    /// Stops the cadence timer.
    /// </summary>
    void Stop();
}
