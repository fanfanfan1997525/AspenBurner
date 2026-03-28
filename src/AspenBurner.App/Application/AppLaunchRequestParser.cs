using System.Globalization;

namespace AspenBurner.App.Application;

/// <summary>
/// Parses desktop entrypoint arguments into a normalized launch request.
/// </summary>
public static class AppLaunchRequestParser
{
    /// <summary>
    /// Parses the raw argument vector.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="defaultConfigPath">Fallback config path.</param>
    /// <returns>Normalized launch request.</returns>
    public static AppLaunchRequest Parse(string[] args, string defaultConfigPath)
    {
        AppCommand? command = null;
        int previewSeconds = 8;
        string configPath = Path.GetFullPath(defaultConfigPath);

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i].Trim();
            switch (argument.ToLowerInvariant())
            {
                case "--config-path":
                case "-configpath":
                    configPath = Path.GetFullPath(args[++i]);
                    break;
                case "--command":
                case "-command":
                    command = ParseCommandValue(args[++i], previewSeconds, configPath);
                    break;
                case "--preview-seconds":
                case "-previewseconds":
                    previewSeconds = int.Parse(args[++i], CultureInfo.InvariantCulture);
                    if (command?.Kind == AppCommandKind.Preview)
                    {
                        command = new AppCommand(AppCommandKind.Preview, previewSeconds, configPath);
                    }

                    break;
                case "--show-settings":
                case "-showsettings":
                    command = new AppCommand(AppCommandKind.ShowSettings, 0, configPath);
                    break;
                case "--preview":
                    command = new AppCommand(AppCommandKind.Preview, previewSeconds, configPath);
                    break;
                case "--stop":
                    command = new AppCommand(AppCommandKind.Stop);
                    break;
                case "--start":
                case "--resume":
                    command = new AppCommand(AppCommandKind.Resume, 0, configPath);
                    break;
            }
        }

        if (command is { Kind: not AppCommandKind.Stop } resolvedCommand &&
            !string.Equals(resolvedCommand.ConfigPath, configPath, StringComparison.OrdinalIgnoreCase))
        {
            command = resolvedCommand with { ConfigPath = configPath };
        }

        return new AppLaunchRequest(command, configPath);
    }

    private static AppCommand ParseCommandValue(string value, int previewSeconds, string configPath)
    {
        return value.ToLowerInvariant() switch
        {
            "show-settings" => new AppCommand(AppCommandKind.ShowSettings, 0, configPath),
            "preview" => new AppCommand(AppCommandKind.Preview, previewSeconds, configPath),
            "health" => new AppCommand(AppCommandKind.Health, 0, configPath),
            "resume" => new AppCommand(AppCommandKind.Resume, 0, configPath),
            "stop" => new AppCommand(AppCommandKind.Stop),
            _ => throw new ArgumentException($"Unknown command argument: {value}", nameof(value)),
        };
    }
}
