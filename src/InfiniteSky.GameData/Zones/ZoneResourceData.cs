using InfiniteSky.Domain.Zones;
using InfiniteSky.Protocol.Structures;

namespace InfiniteSky.GameData.Zones;

/// <summary>
/// Source-backed resources loaded for one configured zone. Missing legacy files remain distinguishable from empty files.
/// </summary>
public sealed record ZoneResourceData(
    IReadOnlyList<SpawnDefinition> NpcSpawns,
    IReadOnlyList<LegacyWorldRegion> MoveRegions,
    IReadOnlyList<LegacyWorldRegion> SafeRegions,
    bool HasNpcSpawnFile,
    bool HasMoveRegionFile,
    bool HasSafeRegionFile);
