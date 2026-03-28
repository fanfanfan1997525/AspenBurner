namespace AspenBurner.Bench;

/// <summary>
/// Represents the final bench conclusion with human-readable reasons.
/// </summary>
public sealed record BenchAssessment(
    BenchOutcome Outcome,
    string Summary,
    IReadOnlyList<string> Reasons);
