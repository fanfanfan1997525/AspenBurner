namespace AspenBurner.App.Application;

/// <summary>
/// Represents one instance-control command.
/// </summary>
public readonly record struct AppCommand(AppCommandKind Kind, int Argument = 0);
