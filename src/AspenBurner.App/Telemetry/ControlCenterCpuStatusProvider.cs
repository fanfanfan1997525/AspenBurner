using System.Globalization;
using System.Reflection;
using AspenBurner.App.Native;

namespace AspenBurner.App.Telemetry;

/// <summary>
/// Reads CPU telemetry from the vendor Control Center runtime when present.
/// </summary>
public sealed class ControlCenterCpuStatusProvider : ICpuStatusProvider
{
    private readonly string? assemblyDirectory;
    private readonly string? nativeDirectory;
    private readonly ResolveEventHandler? resolveHandler;
    private readonly object? cpuInterface;
    private readonly PropertyInfo? clockProperty;
    private readonly PropertyInfo? temperatureProperty;
    private readonly MethodInfo? stopMethod;

    /// <summary>
    /// Initializes a new provider instance.
    /// </summary>
    public ControlCenterCpuStatusProvider()
    {
        try
        {
            ControlCenterRuntimePaths? installPaths = ControlCenterRuntimeLocator.FindInstallPaths();
            if (installPaths is null)
            {
                return;
            }

            ControlCenterRuntimePaths cachedPaths = ControlCenterRuntimeCache.Prepare(
                installPaths,
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            this.assemblyDirectory = Path.GetDirectoryName(cachedPaths.AssemblyPath);
            this.nativeDirectory = cachedPaths.NativeDirectory;

            if (!string.IsNullOrWhiteSpace(this.nativeDirectory))
            {
                _ = NativeMethods.SetDllDirectory(this.nativeDirectory);
            }

            this.resolveHandler = this.ResolveAssembly;
            AppDomain.CurrentDomain.AssemblyResolve += this.resolveHandler;

            Assembly assembly = Assembly.LoadFrom(cachedPaths.AssemblyPath);
            Type? cpuType = assembly.GetType("CC40.PageSystemElement.Interface_CPU", throwOnError: false);
            if (cpuType is null)
            {
                return;
            }

            this.cpuInterface = Activator.CreateInstance(cpuType);
            if (this.cpuInterface is null)
            {
                return;
            }

            MethodInfo? initMethod = cpuType.GetMethod("Init");
            initMethod?.Invoke(this.cpuInterface, null);
            this.clockProperty = cpuType.GetProperty("Clock");
            this.temperatureProperty = cpuType.GetProperty("Temperature");
            this.stopMethod = cpuType.GetMethod("StopCpuInfoTimer");
        }
        catch
        {
            // Vendor chain is optional. The app falls back gracefully.
        }
    }

    /// <inheritdoc />
    public CpuStatusSnapshot Capture()
    {
        if (this.cpuInterface is null)
        {
            return new CpuStatusSnapshot(0, null, false, "Unavailable", DateTimeOffset.Now);
        }

        try
        {
            int frequencyMHz = SanitizeFrequency(this.clockProperty?.GetValue(this.cpuInterface));
            double? temperatureC = SanitizeTemperature(this.temperatureProperty?.GetValue(this.cpuInterface));
            return new CpuStatusSnapshot(frequencyMHz, temperatureC, false, "Control Center", DateTimeOffset.Now);
        }
        catch
        {
            return new CpuStatusSnapshot(0, null, false, "Control Center", DateTimeOffset.Now);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            this.stopMethod?.Invoke(this.cpuInterface, null);
        }
        catch
        {
            // Best-effort cleanup only.
        }

        if (this.resolveHandler is not null)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this.resolveHandler;
        }
    }

    private Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        string? requestedName = new AssemblyName(args.Name).Name;
        if (string.IsNullOrWhiteSpace(requestedName))
        {
            return null;
        }

        foreach (string? candidateDirectory in new[] { this.assemblyDirectory, this.nativeDirectory })
        {
            if (string.IsNullOrWhiteSpace(candidateDirectory))
            {
                continue;
            }

            string candidatePath = Path.Combine(candidateDirectory, $"{requestedName}.dll");
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            try
            {
                return Assembly.LoadFrom(candidatePath);
            }
            catch
            {
                // Keep probing.
            }
        }

        return null;
    }

    private static int SanitizeFrequency(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        int frequencyMHz = Convert.ToInt32(value, CultureInfo.InvariantCulture);
        return frequencyMHz is > 0 and < 10000 ? frequencyMHz : 0;
    }

    private static double? SanitizeTemperature(object? value)
    {
        if (value is null)
        {
            return null;
        }

        double temperatureC = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return temperatureC is > 0 and < 130 ? temperatureC : null;
    }
}
