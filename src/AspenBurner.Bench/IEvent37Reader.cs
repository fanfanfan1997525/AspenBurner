namespace AspenBurner.Bench;

/// <summary>
/// Abstracts querying Event 37 counts for deterministic tests.
/// </summary>
public interface IEvent37Reader
{
    /// <summary>
    /// Counts Event 37 entries since the given start time.
    /// </summary>
    int CountSince(DateTimeOffset startTime);
}
