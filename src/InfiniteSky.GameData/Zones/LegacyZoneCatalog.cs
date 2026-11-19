using System.Collections.ObjectModel;

using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Zones;

namespace InfiniteSky.GameData.Zones;

/// <summary>
/// Zone IDs and display names present in the original config.json-dist.
/// </summary>
public static class LegacyZoneCatalog
{
    private static readonly int[] ZoneIds =
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
        34, 37, 38, 39, 40, 43, 46, 49, 50, 55, 56, 59, 71, 72, 73, 74, 75, 78, 81,
        82, 89, 90, 91, 94, 101, 104, 105, 110, 112, 115, 116, 119, 120, 124, 125,
        126, 138, 140, 141, 142, 143,
    ];

    private static readonly IReadOnlyDictionary<int, string> DisplayNames =
        new ReadOnlyDictionary<int, string>(new Dictionary<int, string>
        {
            [1] = "Gungsong Fortress",
            [6] = "Padsong Fortress",
            [11] = "Hanma Palace",
            [119] = "Nangin Track",
        });

    private static readonly IReadOnlyList<ZoneDescriptor> Zones = Array.AsReadOnly(
        ZoneIds.Select(static id => new ZoneDescriptor
        {
            Id = new ZoneId(id),
            Name = $"Z{id:D3}",
            DisplayName = DisplayNames.GetValueOrDefault(id),
            Load = true,
        }).ToArray());

    /// <summary>
    /// Gets the configured zone definitions in numeric source order.
    /// </summary>
    public static IReadOnlyList<ZoneDescriptor> All => Zones;

    /// <summary>
    /// Resolves an original configured zone ID; unknown IDs are not synthesized.
    /// </summary>
    public static ZoneDescriptor Get(ZoneId id)
        => Zones.FirstOrDefault(zone => zone.Id == id)
            ?? throw new KeyNotFoundException($"Zone ID {id.Value} is not present in the original zone catalog.");
}
