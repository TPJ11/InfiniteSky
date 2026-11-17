namespace InfiniteSky.Application.Health;

/// <summary>
/// A point-in-time, immutable view of the application's actual startup state.
/// </summary>
public sealed record ApplicationReadinessSnapshot(
    bool IsReady,
    ApplicationLifecyclePhase Phase,
    string Reason,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, string> Checks);
