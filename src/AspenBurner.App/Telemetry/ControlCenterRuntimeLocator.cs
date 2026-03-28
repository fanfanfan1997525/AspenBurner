using System.Diagnostics;

namespace AspenBurner.App.Telemetry;

/// <summary>
/// Locates the vendor Control Center package on disk.
/// </summary>
public static class ControlCenterRuntimeLocator
{
    private const string PackagePrefix = "CLEVOCO.FnhotkeysandOSD_";

    /// <summary>
    /// Finds the vendor runtime package and returns its assembly/native paths.
    /// </summary>
    public static ControlCenterRuntimePaths? FindInstallPaths()
    {
        string? packageRoot = TryResolvePackageRootFromRunningProcesses() ?? TryResolvePackageRootFromWindowsApps();
        if (string.IsNullOrWhiteSpace(packageRoot))
        {
            return null;
        }

        string assemblyPath = Path.Combine(packageRoot, "FnKey", "CC40", "CC40.exe");
        if (!File.Exists(assemblyPath))
        {
            return null;
        }

        string? nativeDirectory = Path.Combine(packageRoot, "FnKey", "DCHU");
        if (!Directory.Exists(nativeDirectory))
        {
            nativeDirectory = null;
        }

        return new ControlCenterRuntimePaths(assemblyPath, nativeDirectory, Path.GetFileName(packageRoot));
    }

    /// <summary>
    /// Extracts the package root from a known executable path.
    /// </summary>
    public static string? TryExtractPackageRoot(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        DirectoryInfo? current = new(Path.GetDirectoryName(executablePath) ?? executablePath);
        while (current is not null)
        {
            if (current.Name.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? TryResolvePackageRootFromRunningProcesses()
    {
        foreach (string processName in new[] { "FnKey", "CC40" })
        {
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                try
                {
                    string? processPath = process.MainModule?.FileName;
                    string? packageRoot = TryExtractPackageRoot(processPath ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(packageRoot))
                    {
                        return packageRoot;
                    }
                }
                catch
                {
                    // Probe the next process candidate.
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        return null;
    }

    private static string? TryResolvePackageRootFromWindowsApps()
    {
        string windowsAppsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "WindowsApps");

        try
        {
            return Directory
                .EnumerateDirectories(windowsAppsDirectory, $"{PackagePrefix}*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
