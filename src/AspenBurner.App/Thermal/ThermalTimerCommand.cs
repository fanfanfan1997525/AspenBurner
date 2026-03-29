namespace AspenBurner.App.Thermal;

/// <summary>
/// Describes how the five-minute thermal cadence timer should change.
/// </summary>
public enum ThermalTimerCommand
{
    /// <summary>
    /// Leaves the timer state unchanged.
    /// </summary>
    None,

    /// <summary>
    /// Starts the timer cadence.
    /// </summary>
    Start,

    /// <summary>
    /// Stops the timer cadence.
    /// </summary>
    Stop,
}
