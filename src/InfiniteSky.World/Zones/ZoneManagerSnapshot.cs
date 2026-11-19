namespace InfiniteSky.World.Zones;

/// <summary>
/// Immutable point-in-time zone manager diagnostics.
/// </summary>
public sealed record ZoneManagerSnapshot(
    ZoneManagerState State,
    int ConfiguredZoneCount,
    int LoadedZoneCount,
    int FailedZoneCount,
    bool IsReady);
