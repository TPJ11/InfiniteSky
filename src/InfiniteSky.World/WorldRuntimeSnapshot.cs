namespace InfiniteSky.World;

public sealed record WorldRuntimeSnapshot(
    WorldLifecycleState State,
    WorldReadinessStatus Readiness,
    WorldConfiguration? Configuration,
    IReadOnlyList<WorldReadinessBlocker> Blockers,
    int GuildCount,
    int LoadedZoneCount,
    long TickCount,
    double? LastTickMilliseconds,
    DateTimeOffset UpdatedAt)
{
    public bool IsReady => State == WorldLifecycleState.Running
        && Readiness == WorldReadinessStatus.Ready;
}
