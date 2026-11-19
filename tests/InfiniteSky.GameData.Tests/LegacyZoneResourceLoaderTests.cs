using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Zones;
using InfiniteSky.GameData.Zones;
using InfiniteSky.Protocol.Binary;
using InfiniteSky.Protocol.Structures;

namespace InfiniteSky.GameData.Tests;

public sealed class LegacyZoneResourceLoaderTests
{
    [Fact]
    public async Task LoadsCountPrefixedNpcAndSignedRegionRecords()
    {
        string dataDirectory = CreateDataDirectory();
        try
        {
            string spawnDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory, "spawninfo")).FullName;
            string worldDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory, "world")).FullName;

            var spawnWriter = new LegacyBinaryWriter();
            spawnWriter.WriteUInt32(2);
            new LegacySpawnInfo(1, 0, new LegacyStandardVector3(1, 2, 3), 4).Encode(spawnWriter);
            new LegacySpawnInfo(2, 400, new LegacyStandardVector3(-1.5f, 50.25f, 3.5f), 90).Encode(spawnWriter);
            await File.WriteAllBytesAsync(Path.Combine(spawnDirectory, "001.NPC"), spawnWriter.ToArray());

            var moveWriter = new LegacyBinaryWriter();
            moveWriter.WriteUInt32(1);
            var moveRegion = new LegacyWorldRegion(0, 6, 1, -1, -100, 50, 300, 25);
            moveRegion.Encode(moveWriter);
            await File.WriteAllBytesAsync(
                Path.Combine(worldDirectory, "Z001_ZONEMOVEREGION.WREGION"),
                moveWriter.ToArray());

            var safeWriter = new LegacyBinaryWriter();
            safeWriter.WriteUInt32(1);
            var safeRegion = new LegacyWorldRegion(-4, 1, -2, -3, 10, -20, 30, -40);
            safeRegion.Encode(safeWriter);
            await File.WriteAllBytesAsync(
                Path.Combine(worldDirectory, "Z001_ZONESAFEREGION.WREGION"),
                safeWriter.ToArray());

            ZoneResourceData resources = await new LegacyZoneResourceLoader(dataDirectory)
                .LoadAsync(new ZoneId(1), CancellationToken.None);

            Assert.True(resources.HasNpcSpawnFile);
            Assert.True(resources.HasMoveRegionFile);
            Assert.True(resources.HasSafeRegionFile);
            SpawnDefinition spawn = Assert.Single(resources.NpcSpawns);
            Assert.Equal(2u, spawn.UniqueId);
            Assert.Equal(400u, spawn.DefinitionId);
            Assert.Equal(-1.5, spawn.Location.X);
            Assert.Equal(50.25, spawn.Location.Y);
            Assert.Equal(3.5, spawn.Location.Z);
            Assert.Equal(90, spawn.Direction);
            Assert.Equal(moveRegion, Assert.Single(resources.MoveRegions));
            Assert.Equal(safeRegion, Assert.Single(resources.SafeRegions));
        }
        finally
        {
            DeleteDataDirectory(dataDirectory);
        }
    }

    [Fact]
    public async Task MissingLegacyFilesRemainOptionalAndDistinguishable()
    {
        string dataDirectory = CreateDataDirectory();
        try
        {
            ZoneResourceData resources = await new LegacyZoneResourceLoader(dataDirectory)
                .LoadAsync(new ZoneId(1), CancellationToken.None);

            Assert.Empty(resources.NpcSpawns);
            Assert.Empty(resources.MoveRegions);
            Assert.Empty(resources.SafeRegions);
            Assert.False(resources.HasNpcSpawnFile);
            Assert.False(resources.HasMoveRegionFile);
            Assert.False(resources.HasSafeRegionFile);
        }
        finally
        {
            DeleteDataDirectory(dataDirectory);
        }
    }

    [Fact]
    public async Task ParsesTrackedOriginalNpcFileWithoutChangingFloatValues()
    {
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");

        ZoneResourceData resources = await new LegacyZoneResourceLoader(dataDirectory)
            .LoadAsync(new ZoneId(1), CancellationToken.None);

        Assert.True(resources.HasNpcSpawnFile);
        Assert.Equal(49, resources.NpcSpawns.Count);
        SpawnDefinition first = resources.NpcSpawns[0];
        Assert.Equal(0u, first.UniqueId);
        Assert.Equal(1u, first.DefinitionId);
        Assert.Equal(-991f, first.Location.X);
        Assert.Equal(259f, first.Location.Y);
        Assert.Equal(236f, first.Location.Z);
        Assert.Equal(278.9097900390625, first.Direction);
    }

    [Fact]
    public async Task RejectsTruncatedLegacySpawnFile()
    {
        string dataDirectory = CreateDataDirectory();
        try
        {
            string spawnDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory, "spawninfo")).FullName;
            await File.WriteAllBytesAsync(Path.Combine(spawnDirectory, "001.NPC"), [1, 0, 0, 0, 0]);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new LegacyZoneResourceLoader(dataDirectory).LoadAsync(new ZoneId(1), CancellationToken.None));
        }
        finally
        {
            DeleteDataDirectory(dataDirectory);
        }
    }

    [Fact]
    public void CatalogContainsOnlyOriginalConfiguredIdsAndNames()
    {
        int[] expectedIds =
        [
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
            34, 37, 38, 39, 40, 43, 46, 49, 50, 55, 56, 59, 71, 72, 73, 74, 75, 78, 81,
            82, 89, 90, 91, 94, 101, 104, 105, 110, 112, 115, 116, 119, 120, 124, 125,
            126, 138, 140, 141, 142, 143,
        ];

        Assert.Equal(expectedIds, LegacyZoneCatalog.All.Select(static zone => zone.Id.Value));
        Assert.Equal("Gungsong Fortress", LegacyZoneCatalog.Get(new ZoneId(1)).DisplayName);
        Assert.Equal("Padsong Fortress", LegacyZoneCatalog.Get(new ZoneId(6)).DisplayName);
        Assert.Equal("Hanma Palace", LegacyZoneCatalog.Get(new ZoneId(11)).DisplayName);
        Assert.Equal("Nangin Track", LegacyZoneCatalog.Get(new ZoneId(119)).DisplayName);
        Assert.Throws<KeyNotFoundException>(() => LegacyZoneCatalog.Get(new ZoneId(22)));
    }

    private static string CreateDataDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "InfiniteSky-ZoneData-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDataDirectory(string path)
    {
        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            File.Delete(file);
        }

        foreach (string directory in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                     .OrderByDescending(static directory => directory.Length))
        {
            Directory.Delete(directory);
        }

        Directory.Delete(path);
    }
}
