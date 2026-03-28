namespace AspenBurner.App.Application;

/// <summary>
/// Captures the parsed startup command and resolved configuration path.
/// </summary>
public readonly record struct AppLaunchRequest(AppCommand? Command, string ConfigPath);
