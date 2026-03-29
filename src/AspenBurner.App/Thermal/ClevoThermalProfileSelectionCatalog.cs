namespace AspenBurner.App.Thermal;

/// <summary>
/// Holds the validated A/C profile mappings for the current Clevo machine.
/// </summary>
public static class ClevoThermalProfileSelectionCatalog
{
    /// <summary>
    /// Returns the selection mapping for the requested profile.
    /// </summary>
    public static ClevoThermalProfileSelection GetSelection(ThermalProfileKind profile)
    {
        return profile switch
        {
            ThermalProfileKind.PerformanceA => new ClevoThermalProfileSelection(
                "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                "btn_performance",
                "RB_FAN_max",
                "2"),
            ThermalProfileKind.CoolingC => new ClevoThermalProfileSelection(
                "381b4222-f694-41f0-9685-ff5bb260df2e",
                "Btn_entertainment",
                "RB_FAN_max",
                "3"),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unsupported thermal profile."),
        };
    }
}
