using AspenBurner.App.Thermal;

namespace AspenBurner.App.Tests.Thermal;

/// <summary>
/// Verifies the built-in Clevo driver only enables itself when script and CC40 inputs are both real.
/// </summary>
[TestClass]
public sealed class ClevoThermalProfileDriverTests
{
    /// <summary>
    /// Ensures the driver stays disabled when the automation script is missing.
    /// </summary>
    [TestMethod]
    public void Constructor_DisablesDriverWhenAutomationScriptMissing()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "AspenBurner.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        string cc40Path = Path.Combine(tempRoot, "CC40.exe");
        File.WriteAllText(cc40Path, "cc40");

        ClevoThermalProfileDriver driver = new(
            isCurrentMachineSupported: true,
            cc40ExecutablePath: cc40Path,
            automationScriptPath: Path.Combine(tempRoot, "missing.ps1"));

        Assert.IsFalse(driver.IsSupported);
    }

    /// <summary>
    /// Ensures the repository fallback points at the real project Scripts directory.
    /// </summary>
    [TestMethod]
    public void ResolveAutomationScriptPath_FindsProjectScriptsDirectory()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "AspenBurner.Tests", Guid.NewGuid().ToString("N"));
        string baseDirectory = Path.Combine(tempRoot, "src", "AspenBurner.App", "bin", "Debug", "net8.0-windows");
        string scriptsDirectory = Path.Combine(tempRoot, "src", "AspenBurner.App", "Scripts");
        Directory.CreateDirectory(baseDirectory);
        Directory.CreateDirectory(scriptsDirectory);
        string expectedPath = Path.Combine(scriptsDirectory, "Apply-ClevoThermalProfile.ps1");
        File.WriteAllText(expectedPath, "# test");

        string? resolvedPath = ClevoThermalProfileDriver.ResolveAutomationScriptPath(baseDirectory);

        Assert.AreEqual(expectedPath, resolvedPath);
    }
}
