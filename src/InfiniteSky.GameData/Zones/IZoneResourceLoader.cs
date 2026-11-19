using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.GameData.Zones;

/// <summary>
/// Loads the legacy file-backed resources that are read during zone initialization.
/// </summary>
public interface IZoneResourceLoader
{
    /// <summary>
    /// Loads NPC spawn definitions and move/safe region records for a configured zone.
    /// </summary>
    Task<ZoneResourceData> LoadAsync(ZoneId zoneId, CancellationToken cancellationToken);
}
