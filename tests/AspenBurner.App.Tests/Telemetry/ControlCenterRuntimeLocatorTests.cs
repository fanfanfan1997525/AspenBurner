using AspenBurner.App.Telemetry;

namespace AspenBurner.App.Tests.Telemetry;

/// <summary>
/// Covers vendor runtime path discovery helpers.
/// </summary>
[TestClass]
public sealed class ControlCenterRuntimeLocatorTests
{
    /// <summary>
    /// Ensures package roots can be extracted from the running FnKey path.
    /// </summary>
    [TestMethod]
    public void TryExtractPackageRoot_ReturnsRootForFnKeyExecutable()
    {
        string? packageRoot = ControlCenterRuntimeLocator.TryExtractPackageRoot(
            @"C:\Program Files\WindowsApps\CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0\FnKey\FnKey.exe");

        Assert.AreEqual(
            @"C:\Program Files\WindowsApps\CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0",
            packageRoot);
    }

    /// <summary>
    /// Ensures package roots can be extracted from the running CC40 path.
    /// </summary>
    [TestMethod]
    public void TryExtractPackageRoot_ReturnsRootForCc40Executable()
    {
        string? packageRoot = ControlCenterRuntimeLocator.TryExtractPackageRoot(
            @"C:\Program Files\WindowsApps\CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0\FnKey\CC40\CC40.exe");

        Assert.AreEqual(
            @"C:\Program Files\WindowsApps\CLEVOCO.FnhotkeysandOSD_7.53.9.0_x64__6h6z29zh29qx0",
            packageRoot);
    }

    /// <summary>
    /// Ensures unrelated executable paths do not produce false package roots.
    /// </summary>
    [TestMethod]
    public void TryExtractPackageRoot_ReturnsNullForUnrelatedExecutable()
    {
        string? packageRoot = ControlCenterRuntimeLocator.TryExtractPackageRoot(
            @"C:\Program Files\AspenBurner\AspenBurner.exe");

        Assert.IsNull(packageRoot);
    }
}
