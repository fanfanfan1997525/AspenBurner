using System.Diagnostics;
using AspenBurner.App.Telemetry;

namespace AspenBurner.App.Thermal;

/// <summary>
/// Applies the validated A/C thermal profiles by combining Windows power plans and CC40 automation.
/// </summary>
public sealed class ClevoThermalProfileDriver : IThermalProfileDriver
{
    private string? cc40ExecutablePath;
    private string? automationScriptPath;
    private string supportMessage = string.Empty;

    /// <summary>
    /// Initializes a new thermal driver for the validated Clevo platform.
    /// </summary>
    public ClevoThermalProfileDriver()
    {
        bool isCurrentMachineSupported = ClevoMachineIdentity.IsCurrentMachineSupported();
        string resolutionMessage = "Current machine does not match the validated Clevo model gate.";
        string? resolvedCc40Path = null;
        if (isCurrentMachineSupported &&
            !TryResolveCc40ExecutablePath(out resolvedCc40Path, out resolutionMessage))
        {
            isCurrentMachineSupported = false;
        }

        string? resolvedScriptPath = ResolveAutomationScriptPath(AppContext.BaseDirectory);
        this.Initialize(isCurrentMachineSupported, resolvedCc40Path, resolvedScriptPath, resolutionMessage ?? string.Empty);
    }

    /// <summary>
    /// Initializes a testable driver with injected support inputs.
    /// </summary>
    internal ClevoThermalProfileDriver(bool isCurrentMachineSupported, string? cc40ExecutablePath, string? automationScriptPath)
    {
        this.Initialize(isCurrentMachineSupported, cc40ExecutablePath, automationScriptPath, "Injected thermal driver inputs.");
    }

    /// <inheritdoc />
    public bool IsSupported { get; private set; }

    /// <inheritdoc />
    public bool TryApply(ThermalProfileKind profile, out string message)
    {
        if (!this.IsSupported || string.IsNullOrWhiteSpace(this.cc40ExecutablePath))
        {
            message = this.supportMessage;
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.automationScriptPath) || !File.Exists(this.automationScriptPath))
        {
            message = "Thermal automation script is missing from the application output.";
            return false;
        }

        return this.ApplyCore(profile, out message);
    }

    internal static string? ResolveAutomationScriptPath(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        string localPath = Path.Combine(baseDirectory, "Scripts", "Apply-ClevoThermalProfile.ps1");
        if (File.Exists(localPath))
        {
            return localPath;
        }

        string repositoryPath = Path.Combine(baseDirectory, "..", "..", "..", "Scripts", "Apply-ClevoThermalProfile.ps1");
        string fullRepositoryPath = Path.GetFullPath(repositoryPath);
        return File.Exists(fullRepositoryPath) ? fullRepositoryPath : null;
    }

    private void Initialize(bool isCurrentMachineSupported, string? cc40ExecutablePath, string? automationScriptPath, string fallbackMessage)
    {
        if (!isCurrentMachineSupported)
        {
            this.IsSupported = false;
            this.cc40ExecutablePath = null;
            this.automationScriptPath = null;
            this.supportMessage = string.IsNullOrWhiteSpace(fallbackMessage)
                ? "Current machine does not match the validated Clevo model gate."
                : fallbackMessage;
            return;
        }

        if (string.IsNullOrWhiteSpace(cc40ExecutablePath) || !File.Exists(cc40ExecutablePath))
        {
            this.IsSupported = false;
            this.cc40ExecutablePath = null;
            this.automationScriptPath = null;
            this.supportMessage = "Cached CC40 executable is missing.";
            return;
        }

        if (string.IsNullOrWhiteSpace(automationScriptPath) || !File.Exists(automationScriptPath))
        {
            this.IsSupported = false;
            this.cc40ExecutablePath = null;
            this.automationScriptPath = null;
            this.supportMessage = "Thermal automation script is missing from the application output.";
            return;
        }

        this.IsSupported = true;
        this.cc40ExecutablePath = cc40ExecutablePath;
        this.automationScriptPath = automationScriptPath;
        this.supportMessage = "Clevo thermal automation is available.";
    }

    private static bool TryResolveCc40ExecutablePath(out string? executablePath, out string message)
    {
        executablePath = null;

        try
        {
            ControlCenterRuntimePaths? installPaths = ControlCenterRuntimeLocator.FindInstallPaths();
            if (installPaths is null)
            {
                message = "Control Center runtime package could not be located.";
                return false;
            }

            ControlCenterRuntimePaths cachedPaths = ControlCenterRuntimeCache.Prepare(
                installPaths,
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            if (!File.Exists(cachedPaths.AssemblyPath))
            {
                message = "Cached CC40 executable is missing.";
                return false;
            }

            executablePath = cachedPaths.AssemblyPath;
            message = "Resolved cached CC40 runtime.";
            return true;
        }
        catch (Exception exception)
        {
            message = $"Failed to prepare Control Center cache: {exception.Message}";
            return false;
        }
    }

    private bool ApplyCore(ThermalProfileKind profile, out string message)
    {
        ClevoThermalProfileSelection selection = ClevoThermalProfileSelectionCatalog.GetSelection(profile);
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments =
                $"-NoProfile -ExecutionPolicy Bypass -File \"{this.automationScriptPath}\" " +
                $"-Cc40Path \"{this.cc40ExecutablePath}\" " +
                $"-PowerPlanGuid \"{selection.PowerPlanGuid}\" " +
                $"-PowerModeAutomationId \"{selection.PowerModeAutomationId}\" " +
                $"-FanModeAutomationId \"{selection.FanModeAutomationId}\" " +
                $"-GpuSwitchAutomationId \"{selection.GpuSwitchAutomationId}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("Failed to launch thermal automation PowerShell.");

        if (!process.WaitForExit(30000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            message = "Thermal automation PowerShell timed out.";
            return false;
        }

        string standardOutput = process.StandardOutput.ReadToEnd().Trim();
        string standardError = process.StandardError.ReadToEnd().Trim();
        message = string.IsNullOrWhiteSpace(standardOutput) ? standardError : standardOutput;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"PowerShell exit code {process.ExitCode}.";
        }

        return process.ExitCode == 0;
    }
}
