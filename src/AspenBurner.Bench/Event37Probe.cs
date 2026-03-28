namespace AspenBurner.Bench;

/// <summary>
/// Tracks Event 37 count changes during one bench run.
/// </summary>
public sealed class Event37Probe
{
    private readonly IEvent37Reader reader;
    private int baselineCount;

    /// <summary>
    /// Initializes a new probe.
    /// </summary>
    public Event37Probe(IEvent37Reader reader)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>
    /// Captures the baseline count.
    /// </summary>
    public void CaptureBaseline(DateTimeOffset startTime)
    {
        this.baselineCount = this.reader.CountSince(startTime);
    }

    /// <summary>
    /// Captures the delta from the baseline.
    /// </summary>
    public int CaptureDelta(DateTimeOffset startTime)
    {
        int currentCount = this.reader.CountSince(startTime);
        return Math.Max(0, currentCount - this.baselineCount);
    }
}
