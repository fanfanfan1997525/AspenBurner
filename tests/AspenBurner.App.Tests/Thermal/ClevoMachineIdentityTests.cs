using AspenBurner.App.Thermal;

namespace AspenBurner.App.Tests.Thermal;

/// <summary>
/// Covers the narrow machine gate for AspenBurner's built-in thermal automation.
/// </summary>
[TestClass]
public sealed class ClevoMachineIdentityTests
{
    /// <summary>
    /// Ensures the known NP5x_6x_7x_SNx board string is accepted.
    /// </summary>
    [TestMethod]
    public void IsSupportedModel_ReturnsTrueForKnownBoardProduct()
    {
        Assert.IsTrue(ClevoMachineIdentity.IsSupportedModel("NP5x_6x_7x_SNx"));
    }

    /// <summary>
    /// Ensures case differences do not disable the feature on the same machine.
    /// </summary>
    [TestMethod]
    public void IsSupportedModel_IsCaseInsensitive()
    {
        Assert.IsTrue(ClevoMachineIdentity.IsSupportedModel("np5X_6X_7X_snx"));
    }

    /// <summary>
    /// Ensures unrelated models are rejected.
    /// </summary>
    [TestMethod]
    public void IsSupportedModel_ReturnsFalseForOtherMachines()
    {
        Assert.IsFalse(ClevoMachineIdentity.IsSupportedModel("XMG-FOO-123"));
    }
}
