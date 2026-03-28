namespace AspenBurner.App.Telemetry;

/// <summary>
/// Mirrors vendor runtime files into an app-controlled cache so native DLLs can be loaded reliably.
/// </summary>
public static class ControlCenterRuntimeCache
{
    /// <summary>
    /// Ensures the vendor runtime exists in a local cache and returns cache-backed load paths.
    /// </summary>
    public static ControlCenterRuntimePaths Prepare(ControlCenterRuntimePaths sourcePaths, string localApplicationDataRoot)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataRoot);

        string cacheRoot = Path.Combine(
            Path.GetFullPath(localApplicationDataRoot),
            "AspenBurner",
            "vendor-cache",
            SanitizePathSegment(sourcePaths.PackageIdentity));

        string sourceAssemblyDirectory = Path.GetDirectoryName(sourcePaths.AssemblyPath)
            ?? throw new InvalidOperationException("Assembly path must have a parent directory.");
        string cachedAssemblyDirectory = Path.Combine(cacheRoot, "CC40");
        CopyDirectoryContents(sourceAssemblyDirectory, cachedAssemblyDirectory);

        string cachedAssemblyPath = Path.Combine(cachedAssemblyDirectory, Path.GetFileName(sourcePaths.AssemblyPath));
        string? cachedNativeDirectory = null;
        if (!string.IsNullOrWhiteSpace(sourcePaths.NativeDirectory))
        {
            cachedNativeDirectory = Path.Combine(cacheRoot, "DCHU");
            CopyDirectoryContents(sourcePaths.NativeDirectory, cachedNativeDirectory);
        }

        return new ControlCenterRuntimePaths(cachedAssemblyPath, cachedNativeDirectory, sourcePaths.PackageIdentity);
    }

    private static void CopyDirectoryContents(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string sourcePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            string destinationPath = Path.Combine(destinationDirectory, relativePath);
            string? destinationParent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationParent))
            {
                Directory.CreateDirectory(destinationParent);
            }

            if (ShouldCopyFile(sourcePath, destinationPath))
            {
                File.Copy(sourcePath, destinationPath, overwrite: true);
                File.SetLastWriteTimeUtc(destinationPath, File.GetLastWriteTimeUtc(sourcePath));
            }
        }
    }

    private static bool ShouldCopyFile(string sourcePath, string destinationPath)
    {
        if (!File.Exists(destinationPath))
        {
            return true;
        }

        FileInfo sourceInfo = new(sourcePath);
        FileInfo destinationInfo = new(destinationPath);
        return sourceInfo.Length != destinationInfo.Length ||
               sourceInfo.LastWriteTimeUtc > destinationInfo.LastWriteTimeUtc;
    }

    private static string SanitizePathSegment(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalidChars.Contains(character) ? '_' : character).ToArray());
    }
}
