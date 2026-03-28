using AspenBurner.App.Telemetry;

namespace AspenBurner.App.Tests.Telemetry;

/// <summary>
/// Covers local caching for vendor runtime files that cannot be loaded from WindowsApps directly.
/// </summary>
[TestClass]
public sealed class ControlCenterRuntimeCacheTests
{
    /// <summary>
    /// Ensures the vendor runtime is copied into an app-controlled cache.
    /// </summary>
    [TestMethod]
    public void Prepare_CopiesAssemblyAndNativeDirectoriesIntoCache()
    {
        using TempDirectoryScope scope = new();
        string sourceRoot = Path.Combine(scope.RootPath, "source");
        string cc40Directory = Path.Combine(sourceRoot, "CC40");
        string dchuDirectory = Path.Combine(sourceRoot, "DCHU");
        string cacheBaseDirectory = Path.Combine(scope.RootPath, "cache");

        Directory.CreateDirectory(cc40Directory);
        Directory.CreateDirectory(dchuDirectory);
        File.WriteAllText(Path.Combine(cc40Directory, "CC40.exe"), "cc40");
        File.WriteAllText(Path.Combine(cc40Directory, "GetCurrentCpuSpeed.dll"), "speed");
        File.WriteAllText(Path.Combine(dchuDirectory, "InsydeDCHU.dll"), "dchu");

        ControlCenterRuntimePaths cachedPaths = ControlCenterRuntimeCache.Prepare(
            new ControlCenterRuntimePaths(
                Path.Combine(cc40Directory, "CC40.exe"),
                dchuDirectory,
                "CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0"),
            cacheBaseDirectory);

        Assert.AreEqual(Path.Combine(cacheBaseDirectory, "AspenBurner", "vendor-cache", "CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0", "CC40", "CC40.exe"), cachedPaths.AssemblyPath);
        Assert.AreEqual(Path.Combine(cacheBaseDirectory, "AspenBurner", "vendor-cache", "CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0", "DCHU"), cachedPaths.NativeDirectory);
        Assert.IsTrue(File.Exists(cachedPaths.AssemblyPath));
        Assert.IsTrue(File.Exists(Path.Combine(Path.GetDirectoryName(cachedPaths.AssemblyPath)!, "GetCurrentCpuSpeed.dll")));
        Assert.IsTrue(File.Exists(Path.Combine(cachedPaths.NativeDirectory!, "InsydeDCHU.dll")));
    }

    /// <summary>
    /// Ensures newer source files refresh an existing cache.
    /// </summary>
    [TestMethod]
    public void Prepare_RefreshesCacheWhenSourceFilesChange()
    {
        using TempDirectoryScope scope = new();
        string sourceRoot = Path.Combine(scope.RootPath, "source");
        string cc40Directory = Path.Combine(sourceRoot, "CC40");
        string dchuDirectory = Path.Combine(sourceRoot, "DCHU");
        string cacheBaseDirectory = Path.Combine(scope.RootPath, "cache");

        Directory.CreateDirectory(cc40Directory);
        Directory.CreateDirectory(dchuDirectory);

        string sourceAssemblyPath = Path.Combine(cc40Directory, "CC40.exe");
        string sourceNativePath = Path.Combine(dchuDirectory, "InsydeDCHU.dll");
        File.WriteAllText(sourceAssemblyPath, "v1");
        File.WriteAllText(sourceNativePath, "d1");

        ControlCenterRuntimePaths firstCachedPaths = ControlCenterRuntimeCache.Prepare(
            new ControlCenterRuntimePaths(sourceAssemblyPath, dchuDirectory, "CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0"),
            cacheBaseDirectory);

        System.Threading.Thread.Sleep(1100);
        File.WriteAllText(sourceAssemblyPath, "v2");
        File.WriteAllText(sourceNativePath, "d2");

        ControlCenterRuntimePaths secondCachedPaths = ControlCenterRuntimeCache.Prepare(
            new ControlCenterRuntimePaths(sourceAssemblyPath, dchuDirectory, "CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0"),
            cacheBaseDirectory);

        Assert.AreEqual(firstCachedPaths.AssemblyPath, secondCachedPaths.AssemblyPath);
        Assert.AreEqual("v2", File.ReadAllText(secondCachedPaths.AssemblyPath));
        Assert.AreEqual("d2", File.ReadAllText(Path.Combine(secondCachedPaths.NativeDirectory!, "InsydeDCHU.dll")));
    }

    private sealed class TempDirectoryScope : IDisposable
    {
        public TempDirectoryScope()
        {
            this.RootPath = Path.Combine(Path.GetTempPath(), "AspenBurner.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.RootPath);
        }

        public string RootPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(this.RootPath))
            {
                Directory.Delete(this.RootPath, recursive: true);
            }
        }
    }
}
