using AspenBurner.App.Telemetry;

namespace AspenBurner.Bench.Tests.System;

[TestClass]
public sealed class TelemetrySamplerTests
{
    [TestMethod]
    public void Summarize_ComputesAveragePeakAndSource()
    {
        TelemetrySummary summary = TelemetrySampler.Summarize(
        [
            new CpuStatusSnapshot(2100, 85, false, "Control Center", DateTimeOffset.Parse("2026-03-29T01:00:00+08:00")),
            new CpuStatusSnapshot(2400, 88, false, "Control Center", DateTimeOffset.Parse("2026-03-29T01:00:01+08:00")),
            new CpuStatusSnapshot(2300, null, false, "Control Center", DateTimeOffset.Parse("2026-03-29T01:00:02+08:00")),
        ]);

        Assert.AreEqual(2267, summary.AverageFrequencyMHz);
        Assert.AreEqual(2400, summary.MaxFrequencyMHz);
        Assert.AreEqual(88, summary.PeakTemperatureC);
        Assert.AreEqual(3, summary.SampleCount);
        Assert.AreEqual("Control Center", summary.Source);
    }

    [TestMethod]
    public void Summarize_ReturnsUnavailableDefaultsWhenEmpty()
    {
        TelemetrySummary summary = TelemetrySampler.Summarize([]);

        Assert.AreEqual(0, summary.AverageFrequencyMHz);
        Assert.AreEqual(0, summary.MaxFrequencyMHz);
        Assert.IsNull(summary.PeakTemperatureC);
        Assert.AreEqual(0, summary.SampleCount);
        Assert.AreEqual("Unavailable", summary.Source);
    }
}
