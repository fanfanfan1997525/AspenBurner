namespace AspenBurner.Bench.Tests.System;

[TestClass]
public sealed class Event37ProbeTests
{
    [TestMethod]
    public void CaptureDelta_UsesCurrentMinusBaseline()
    {
        FakeEvent37Reader reader = new([2, 5]);
        Event37Probe probe = new(reader);

        probe.CaptureBaseline(DateTimeOffset.Parse("2026-03-29T01:00:00+08:00"));
        int delta = probe.CaptureDelta(DateTimeOffset.Parse("2026-03-29T01:01:00+08:00"));

        Assert.AreEqual(3, delta);
    }

    [TestMethod]
    public void CaptureDelta_NeverReturnsNegative()
    {
        FakeEvent37Reader reader = new([5, 2]);
        Event37Probe probe = new(reader);

        probe.CaptureBaseline(DateTimeOffset.Parse("2026-03-29T01:00:00+08:00"));
        int delta = probe.CaptureDelta(DateTimeOffset.Parse("2026-03-29T01:01:00+08:00"));

        Assert.AreEqual(0, delta);
    }

    private sealed class FakeEvent37Reader : IEvent37Reader
    {
        private readonly Queue<int> counts;

        public FakeEvent37Reader(IEnumerable<int> counts)
        {
            this.counts = new Queue<int>(counts);
        }

        public int CountSince(DateTimeOffset startTime)
        {
            return this.counts.Dequeue();
        }
    }
}
