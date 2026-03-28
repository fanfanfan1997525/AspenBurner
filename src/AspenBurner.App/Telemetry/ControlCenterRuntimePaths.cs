namespace AspenBurner.App.Telemetry;

/// <summary>
/// Describes one vendor runtime package and the paths needed to load it.
/// </summary>
public sealed record ControlCenterRuntimePaths(string AssemblyPath, string? NativeDirectory, string PackageIdentity);
