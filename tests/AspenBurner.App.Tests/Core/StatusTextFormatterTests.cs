using AspenBurner.App.Core;

namespace AspenBurner.App.Tests.Core;

/// <summary>
/// Compatibility tests for CPU status text formatting.
/// </summary>
[TestClass]
public sealed class StatusTextFormatterTests
{
    /// <summary>
    /// Ensures direct readings format with rounded frequency and temperature.
    /// </summary>
    [TestMethod]
    public void FormatCpuStatus_FormatsDirectReadings()
    {
        string text = StatusTextFormatter.FormatCpuStatus(4387, 94.6, approximateTemperature: false, showTemperature: true);

        Assert.AreEqual("CPU 4.4GHz | 95C", text);
    }

    /// <summary>
    /// Ensures missing temperatures show a placeholder instead of stale values.
    /// </summary>
    [TestMethod]
    public void FormatCpuStatus_UsesPlaceholderForMissingTemperature()
    {
        string text = StatusTextFormatter.FormatCpuStatus(3200, null, approximateTemperature: false, showTemperature: true);

        Assert.AreEqual("CPU 3.2GHz | --C", text);
    }

    /// <summary>
    /// Ensures approximate thermal-zone readings are labeled.
    /// </summary>
    [TestMethod]
    public void FormatCpuStatus_MarksApproximateTemperatures()
    {
        string text = StatusTextFormatter.FormatCpuStatus(4100, 35.2, approximateTemperature: true, showTemperature: true);

        Assert.AreEqual("CPU 4.1GHz | TZ 35C", text);
    }

    /// <summary>
    /// Ensures temperature text can be suppressed.
    /// </summary>
    [TestMethod]
    public void FormatCpuStatus_CanOmitTemperature()
    {
        string text = StatusTextFormatter.FormatCpuStatus(4100, 35.2, approximateTemperature: true, showTemperature: false);

        Assert.AreEqual("CPU 4.1GHz", text);
    }
}
