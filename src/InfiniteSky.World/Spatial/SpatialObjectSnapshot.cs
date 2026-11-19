using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.World.Spatial;

/// <summary>
/// Immutable spatial-index result; lookup results have no distance, while query results use X/Z distance to object radius.
/// </summary>
public readonly record struct SpatialObjectSnapshot(
    EntityId Id,
    string Type,
    Position3 Position,
    double Size,
    double? Distance);
