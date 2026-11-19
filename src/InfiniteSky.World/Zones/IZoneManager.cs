using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.World.Zones;

/// <summary>
/// Exposes zone lifecycle and lookup state without exposing host-specific services.
/// </summary>
public interface IZoneManager : IWorldZoneReadiness
{
    ZoneManagerSnapshot Current { get; }
    ZoneRuntime? Find(ZoneId id);
    IReadOnlyList<ZoneRuntime> GetZones();
}
