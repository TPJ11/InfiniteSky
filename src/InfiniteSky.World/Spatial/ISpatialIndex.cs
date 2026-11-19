using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.World.Spatial;

/// <summary>
/// Thread-safe, zone-local spatial query and position index.
/// </summary>
public interface ISpatialIndex
{
    int Count { get; }

    EntityId Add(Position3 position, string type, double size = 1);
    bool Update(EntityId id, Position3 position, double? size = null);
    bool Remove(EntityId id);
    SpatialObjectSnapshot? Find(EntityId id);
    IReadOnlyList<SpatialObjectSnapshot> Query(
        Position3 center,
        double radius,
        IReadOnlySet<string>? types = null,
        bool sortByDistance = false);
    void Clear();
}
