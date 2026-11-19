namespace InfiniteSky.World;

/// <summary>
/// Reports zone bootstrap readiness without exposing zone runtime internals to the world host.
/// </summary>
public interface IWorldZoneReadiness
{
    bool IsReady { get; }
    int LoadedZoneCount { get; }
}
