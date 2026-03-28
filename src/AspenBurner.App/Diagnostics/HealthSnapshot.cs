using AspenBurner.App.Runtime;

namespace AspenBurner.App.Diagnostics;

/// <summary>
/// Represents a user-facing runtime health snapshot.
/// </summary>
public sealed record HealthSnapshot(
    AppPresenceState Presence,
    string TelemetrySource,
    string LastStatusText,
    DateTimeOffset UpdatedAt);
