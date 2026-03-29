namespace AspenBurner.App.Thermal;

/// <summary>
/// Represents the concrete power-plan and CC40 control mapping for a thermal profile.
/// </summary>
public readonly record struct ClevoThermalProfileSelection(
    string PowerPlanGuid,
    string PowerModeAutomationId,
    string FanModeAutomationId,
    string GpuSwitchAutomationId);
