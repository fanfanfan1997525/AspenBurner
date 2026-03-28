namespace AspenBurner.App.Application;

/// <summary>
/// Defines single-instance identifiers shared across entrypoints.
/// </summary>
public static class AppIdentity
{
    /// <summary>
    /// Gets the global mutex name.
    /// </summary>
    public const string MutexName = @"Global\AspenBurner.MainInstance";

    /// <summary>
    /// Gets the named-pipe command channel.
    /// </summary>
    public const string PipeName = "AspenBurner.CommandPipe";
}
