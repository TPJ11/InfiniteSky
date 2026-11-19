using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.World.Zones;

/// <summary>
/// Thread-safe registry of fully running zones.
/// </summary>
public interface IZoneRegistry
{
    bool TryRegister(ZoneRuntime zone);
    bool Unregister(ZoneId id);
    ZoneRuntime? Find(ZoneId id);
    IReadOnlyList<ZoneRuntime> GetAll();
}
