using InfiniteSky.Domain.Values;

namespace InfiniteSky.Domain.Zones;

/// <summary>
/// A fixed spawn record used by NPC and MOB files.
/// </summary>
public readonly record struct SpawnDefinition(
    uint UniqueId,
    uint DefinitionId,
    Position3 Location,
    double Direction);
