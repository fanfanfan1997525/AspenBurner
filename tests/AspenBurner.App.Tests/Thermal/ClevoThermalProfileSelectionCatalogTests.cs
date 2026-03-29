using AspenBurner.App.Thermal;

namespace AspenBurner.App.Tests.Thermal;

/// <summary>
/// Verifies the hard-coded A/C profile mappings used for this Clevo machine.
/// </summary>
[TestClass]
public sealed class ClevoThermalProfileSelectionCatalogTests
{
    /// <summary>
    /// Ensures profile A maps to the aggressive gaming settings.
    /// </summary>
    [TestMethod]
    public void GetSelection_ReturnsProfileA()
    {
        ClevoThermalProfileSelection selection = ClevoThermalProfileSelectionCatalog.GetSelection(ThermalProfileKind.PerformanceA);

        Assert.AreEqual("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", selection.PowerPlanGuid);
        Assert.AreEqual("btn_performance", selection.PowerModeAutomationId);
        Assert.AreEqual("RB_FAN_max", selection.FanModeAutomationId);
        Assert.AreEqual("2", selection.GpuSwitchAutomationId);
    }

    /// <summary>
    /// Ensures profile C maps to the cooler long-session settings.
    /// </summary>
    [TestMethod]
    public void GetSelection_ReturnsProfileC()
    {
        ClevoThermalProfileSelection selection = ClevoThermalProfileSelectionCatalog.GetSelection(ThermalProfileKind.CoolingC);

        Assert.AreEqual("381b4222-f694-41f0-9685-ff5bb260df2e", selection.PowerPlanGuid);
        Assert.AreEqual("Btn_entertainment", selection.PowerModeAutomationId);
        Assert.AreEqual("RB_FAN_max", selection.FanModeAutomationId);
        Assert.AreEqual("3", selection.GpuSwitchAutomationId);
    }
}
