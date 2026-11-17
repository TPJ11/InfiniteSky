namespace InfiniteSky.Application.Health;

/// <summary>
/// Describes one accepted application lifecycle transition.
/// </summary>
public sealed record ApplicationLifecycleTransition(
    ApplicationLifecyclePhase Previous,
    ApplicationLifecyclePhase Current,
    string Reason,
    DateTimeOffset OccurredAt);
