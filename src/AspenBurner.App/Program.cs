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

        AppLaunchRequest launchRequest = AppLaunchRequestParser.Parse(args, GetDefaultConfigPath());
        AppCommand? initialCommand = launchRequest.Command;
        using Mutex mutex = new(initiallyOwned: true, AppIdentity.MutexName, out bool createdNew);

        if (!createdNew)
        {
            AppCommand command = initialCommand ?? new AppCommand(AppCommandKind.Resume, 0, launchRequest.ConfigPath);
            AppCommandClient client = new();
            _ = client.TrySend(AppIdentity.PipeName, command);
            return;
        }

        if (initialCommand?.Kind == AppCommandKind.Stop)
        {
            return;
        }

        AppCommand startupCommand = initialCommand ?? new AppCommand(AppCommandKind.Resume, 0, launchRequest.ConfigPath);
        using AspenBurnerApplicationContext context = new(launchRequest.ConfigPath, startupCommand);
        global::System.Windows.Forms.Application.Run(context);
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
