namespace AspenBurner.App.Thermal;

/// <summary>
/// Applies a validated thermal profile against the local machine.
/// </summary>
public interface IThermalProfileDriver
{
    /// <summary>
    /// Gets a value indicating whether this driver can control the current machine.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Tries to apply the requested profile.
    /// </summary>
    bool TryApply(ThermalProfileKind profile, out string message);
}
