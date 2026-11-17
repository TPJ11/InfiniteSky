using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Zones;

/// <summary>
/// The named fields from a signed 8-integer WREGION record.
/// </summary>
public readonly record struct WorldRegion(
    int Unknown1,
    ZoneId ZoneId,
    int Unknown2,
    int Unknown3,
    int X,
    int Y,
    int Z,
    int Radius);
