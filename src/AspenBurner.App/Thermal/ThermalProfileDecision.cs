namespace AspenBurner.App.Thermal;

/// <summary>
/// Represents the next thermal control action requested by the state machine.
/// </summary>
public readonly record struct ThermalProfileDecision(
    ThermalTimerCommand TimerCommand,
    ThermalProfileKind? RequestedProfile);
