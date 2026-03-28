using System.Globalization;
using System.Threading;
using AspenBurner.App.Application;

namespace AspenBurner.App;

internal static class Program
{
    /// <summary>
    /// Application entrypoint.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        (AppCommand? initialCommand, string configPath) = ParseArguments(args);
        using Mutex mutex = new(initiallyOwned: true, AppIdentity.MutexName, out bool createdNew);

        if (!createdNew)
        {
            AppCommand command = initialCommand ?? new AppCommand(AppCommandKind.Resume);
            AppCommandClient client = new();
            _ = client.TrySend(AppIdentity.PipeName, command);
            return;
        }

        if (initialCommand?.Kind == AppCommandKind.Stop)
        {
            return;
        }

        AppCommand startupCommand = initialCommand ?? new AppCommand(AppCommandKind.Resume);
        using AspenBurnerApplicationContext context = new(configPath, startupCommand);
        global::System.Windows.Forms.Application.Run(context);
    }

    private static (AppCommand? Command, string ConfigPath) ParseArguments(string[] args)
    {
        AppCommand? command = null;
        int previewSeconds = 8;
        string configPath = GetDefaultConfigPath();

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i].Trim();
            switch (argument.ToLowerInvariant())
            {
                case "--config-path":
                    configPath = Path.GetFullPath(args[++i]);
                    break;
                case "--command":
                    command = ParseCommandValue(args[++i], previewSeconds);
                    break;
                case "--preview-seconds":
                    previewSeconds = int.Parse(args[++i], CultureInfo.InvariantCulture);
                    if (command?.Kind == AppCommandKind.Preview)
                    {
                        command = new AppCommand(AppCommandKind.Preview, previewSeconds);
                    }

                    break;
                case "--show-settings":
                    command = new AppCommand(AppCommandKind.ShowSettings);
                    break;
                case "--preview":
                    command = new AppCommand(AppCommandKind.Preview, previewSeconds);
                    break;
                case "--stop":
                    command = new AppCommand(AppCommandKind.Stop);
                    break;
                case "--start":
                case "--resume":
                    command = new AppCommand(AppCommandKind.Resume);
                    break;
            }
        }

        return (command, configPath);
    }

    private static AppCommand ParseCommandValue(string value, int previewSeconds)
    {
        return value.ToLowerInvariant() switch
        {
            "show-settings" => new AppCommand(AppCommandKind.ShowSettings),
            "preview" => new AppCommand(AppCommandKind.Preview, previewSeconds),
            "health" => new AppCommand(AppCommandKind.Health),
            "resume" => new AppCommand(AppCommandKind.Resume),
            "stop" => new AppCommand(AppCommandKind.Stop),
            _ => throw new ArgumentException($"Unknown command argument: {value}", nameof(value)),
        };
    }

    private static string GetDefaultConfigPath()
    {
        string localConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "crosshair.json");
        string parentDirectory = Directory.GetParent(AppContext.BaseDirectory)?.FullName ?? AppContext.BaseDirectory;
        string parentConfigPath = Path.Combine(parentDirectory, "config", "crosshair.json");
        string grandParentConfigPath = Path.Combine(Directory.GetParent(parentDirectory)?.FullName ?? parentDirectory, "config", "crosshair.json");
        return File.Exists(localConfigPath) || Directory.Exists(Path.GetDirectoryName(localConfigPath)!)
            ? localConfigPath
            : File.Exists(parentConfigPath) || Directory.Exists(Path.GetDirectoryName(parentConfigPath)!)
                ? parentConfigPath
                : grandParentConfigPath;
    }
}
