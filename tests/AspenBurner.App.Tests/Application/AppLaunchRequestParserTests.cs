using AspenBurner.App.Application;

namespace AspenBurner.App.Tests.Application;

/// <summary>
/// Covers launch-argument compatibility across desktop entrypoints.
/// </summary>
[TestClass]
public sealed class AppLaunchRequestParserTests
{
    /// <summary>
    /// Ensures the modern long-form config flag is parsed.
    /// </summary>
    [TestMethod]
    public void Parse_RecognizesDoubleDashConfigPath()
    {
        AppLaunchRequest request = AppLaunchRequestParser.Parse(
            ["--config-path", @"F:\config\crosshair.json", "--resume"],
            @"F:\fallback.json");

        Assert.AreEqual(@"F:\config\crosshair.json", request.ConfigPath);
        Assert.IsNotNull(request.Command);
        Assert.AreEqual(AppCommandKind.Resume, request.Command.Value.Kind);
        Assert.AreEqual(@"F:\config\crosshair.json", request.Command.Value.ConfigPath);
    }

    /// <summary>
    /// Ensures the legacy PowerShell-style config flag remains supported.
    /// </summary>
    [TestMethod]
    public void Parse_RecognizesSingleDashCompatibilityConfigPath()
    {
        AppLaunchRequest request = AppLaunchRequestParser.Parse(
            ["-ConfigPath", @"F:\compat\crosshair.json", "--resume"],
            @"F:\fallback.json");

        Assert.AreEqual(@"F:\compat\crosshair.json", request.ConfigPath);
        Assert.IsNotNull(request.Command);
        Assert.AreEqual(AppCommandKind.Resume, request.Command.Value.Kind);
        Assert.AreEqual(@"F:\compat\crosshair.json", request.Command.Value.ConfigPath);
    }

    /// <summary>
    /// Ensures preview commands preserve their explicit duration.
    /// </summary>
    [TestMethod]
    public void Parse_PreservesPreviewDurationAndConfigPath()
    {
        AppLaunchRequest request = AppLaunchRequestParser.Parse(
            ["--config-path", @"F:\config\crosshair.json", "--preview", "--preview-seconds", "3"],
            @"F:\fallback.json");

        Assert.IsNotNull(request.Command);
        Assert.AreEqual(AppCommandKind.Preview, request.Command.Value.Kind);
        Assert.AreEqual(3, request.Command.Value.Argument);
        Assert.AreEqual(@"F:\config\crosshair.json", request.Command.Value.ConfigPath);
    }

    /// <summary>
    /// Ensures config-less launches still bind the default config path to resume.
    /// </summary>
    [TestMethod]
    public void Parse_DefaultsResumeCommandToResolvedConfigPath()
    {
        AppLaunchRequest request = AppLaunchRequestParser.Parse(
            ["--resume"],
            @"F:\default\crosshair.json");

        Assert.AreEqual(@"F:\default\crosshair.json", request.ConfigPath);
        Assert.IsNotNull(request.Command);
        Assert.AreEqual(AppCommandKind.Resume, request.Command.Value.Kind);
        Assert.AreEqual(@"F:\default\crosshair.json", request.Command.Value.ConfigPath);
    }
}
