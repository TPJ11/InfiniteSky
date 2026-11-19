using System.Collections.Concurrent;

using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;
using InfiniteSky.Domain.Zones;
using InfiniteSky.GameData.Zones;
using InfiniteSky.Protocol.Structures;
using InfiniteSky.World.Spatial;
using InfiniteSky.World.Zones;

namespace InfiniteSky.World.Tests;

public sealed class ZoneManagementTests
{
    [Fact]
    public void SpatialQueriesUseHorizontalRadiusAndObjectSize()
    {
        var index = new ZoneSpatialIndex();
        EntityId id = index.Add(new Position3(3, 10_000, 4), "client", size: 1);

        SpatialObjectSnapshot result = Assert.Single(index.Query(new Position3(0, -10_000, 0), radius: 4));

        Assert.Equal(id, result.Id);
        Assert.Equal(4, result.Distance);
        Assert.Empty(index.Query(new Position3(0, 0, 0), radius: 3.99));
    }

    [Fact]
    public void SpatialQueryRetainsLegacyInclusiveObjectBoundaryAndStrictChildPruning()
    {
        using var unpartitioned = new ZoneSpatialIndex();
        EntityId edgeId = unpartitioned.Add(new Position3(11, 0, 0), "npc", size: 1);
        Assert.Contains(unpartitioned.Query(Position3.Zero, 10), result => result.Id == edgeId);

        using var partitioned = new ZoneSpatialIndex();
        for (int index = 0; index < 11; index++)
        {
            partitioned.Add(new Position3(-40_000 + (index * 10), 0, -40_000), "npc");
        }

        EntityId prunedId = partitioned.Add(new Position3(2, 0, 100), "npc", size: 1);
        Assert.DoesNotContain(
            partitioned.Query(new Position3(-10, 0, 100), radius: 10),
            result => result.Id == prunedId);
    }

    [Fact]
    public void UpdatesRemovalAndTypeFilteringKeepIndexConsistent()
    {
        using var index = new ZoneSpatialIndex();
        EntityId npc = index.Add(new Position3(1, 0, 1), "npc");
        EntityId client = index.Add(new Position3(1, 0, 1), "client");
        var npcTypes = new HashSet<string>(StringComparer.Ordinal) { "npc" };

        Assert.Single(index.Query(Position3.Zero, 1, npcTypes));
        Assert.True(index.Update(npc, new Position3(20, 0, 20)));
        Assert.Empty(index.Query(Position3.Zero, 1, npcTypes));
        Assert.Single(index.Query(new Position3(20, 0, 20), 0));
        Assert.True(index.Remove(client));
        Assert.False(index.Remove(client));
        Assert.False(index.Update(new EntityId(999), Position3.Zero));
        Assert.Equal(1, index.Count);

        index.Clear();
        Assert.Equal(0, index.Count);
    }

    [Fact]
    public void ZeroSizeUsesLegacyDefaultAndSizeUpdatesRepartitionTheObject()
    {
        using var index = new ZoneSpatialIndex();
        EntityId id = index.Add(new Position3(10, 0, 10), "npc", size: 0);

        Assert.Equal(1, index.Find(id)!.Value.Size);
        Assert.True(index.Update(id, new Position3(10, 0, 10), size: 2));
        Assert.Equal(2, index.Find(id)!.Value.Size);
        Assert.Equal(-2, Assert.Single(index.Query(new Position3(10, 0, 10), 0)).Distance);
    }

    [Fact]
    public void ConcurrentRegistrationUpdateQueryAndRemovalRemainConsistent()
    {
        using var index = new ZoneSpatialIndex();
        var ids = new ConcurrentBag<EntityId>();

        Parallel.For(0, 500, value =>
        {
            ids.Add(index.Add(new Position3(value, 0, value), value % 2 == 0 ? "npc" : "client"));
        });

        Parallel.ForEach(ids, id =>
        {
            double coordinate = id.Value;
            Assert.True(index.Update(id, new Position3(coordinate, 0, coordinate)));
            _ = index.Query(new Position3(coordinate, 0, coordinate), 5);
            Assert.True(index.Remove(id));
        });

        Assert.Equal(0, index.Count);
    }

    [Fact]
    public async Task ManagerRegistersOnlyRunningZonesAndCoordinatesShutdown()
    {
        using var metrics = new ZoneMetrics();
        var registry = new ZoneRegistry();
        var manager = new ZoneManager(registry, new EmptyZoneResourceLoader(), metrics);
        ZoneDescriptor descriptor = Descriptor(1);

        await manager.StartAsync([descriptor], new ZoneManagerOptions(), CancellationToken.None);

        Assert.True(manager.IsReady);
        Assert.Equal(1, manager.LoadedZoneCount);
        ZoneRuntime zone = Assert.Single(manager.GetZones());
        ISpatialIndex index = zone.GetSpatialIndex();
        Assert.Equal(ZoneLifecycleState.Running, zone.State);
        index.Add(new Position3(400, 0, 0), "npc");
        index.Add(new Position3(502, 0, 0), "npc");
        Assert.Single(zone.QueryActionCandidates(Position3.Zero));
        Assert.False(registry.TryRegister(zone));

        await manager.StopAsync(CancellationToken.None);

        Assert.False(manager.IsReady);
        Assert.Empty(manager.GetZones());
        Assert.Equal(ZoneLifecycleState.Stopped, zone.State);
        Assert.Throws<ObjectDisposedException>(() => index.Add(Position3.Zero, "client"));
    }

    [Fact]
    public async Task ConcurrentRegistryOperationsPublishOnlyRunningZones()
    {
        using var metrics = new ZoneMetrics();
        var registry = new ZoneRegistry();
        var loader = new EmptyZoneResourceLoader();
        ZoneRuntime[] zones = Enumerable.Range(1, 32)
            .Select(id => new ZoneRuntime(Descriptor(id), loader, new ZoneManagerOptions(), metrics))
            .ToArray();

        await Task.WhenAll(zones.Select(async zone =>
        {
            await zone.InitializeAsync(CancellationToken.None);
            await zone.StartAsync(CancellationToken.None);
        }));
        Parallel.ForEach(zones, zone => Assert.True(registry.TryRegister(zone)));

        Assert.Equal(32, registry.GetAll().Count);
        Assert.Equal(
            Enumerable.Range(1, 32),
            registry.GetAll().Select(static zone => zone.Descriptor.Id.Value));

        Parallel.ForEach(zones, zone => Assert.True(registry.Unregister(zone.Descriptor.Id)));
        await Task.WhenAll(zones.Select(zone => zone.StopAsync(CancellationToken.None)));
        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public async Task FailedInitializationDoesNotPublishPartialZones()
    {
        using var metrics = new ZoneMetrics();
        var registry = new ZoneRegistry();
        var manager = new ZoneManager(registry, new FailingZoneResourceLoader(), metrics);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            manager.StartAsync([Descriptor(1)], new ZoneManagerOptions(), CancellationToken.None));

        Assert.Equal(ZoneManagerState.Failed, manager.Current.State);
        Assert.Equal(0, manager.LoadedZoneCount);
        Assert.Empty(manager.GetZones());
    }

    [Fact]
    public async Task EmptyConfigurationIsNotReportedReady()
    {
        using var metrics = new ZoneMetrics();
        var manager = new ZoneManager(new ZoneRegistry(), new EmptyZoneResourceLoader(), metrics);

        await manager.StartAsync([], new ZoneManagerOptions(), CancellationToken.None);

        Assert.Equal(ZoneManagerState.Running, manager.Current.State);
        Assert.False(manager.IsReady);
        Assert.Equal(0, manager.LoadedZoneCount);
        await manager.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DuplicateZoneIDsAreRejectedBeforeAnyRegistration()
    {
        using var metrics = new ZoneMetrics();
        var registry = new ZoneRegistry();
        var manager = new ZoneManager(registry, new EmptyZoneResourceLoader(), metrics);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.StartAsync([Descriptor(1), Descriptor(1)], new ZoneManagerOptions(), CancellationToken.None));

        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public async Task CanceledStartupDoesNotPublishZones()
    {
        using var metrics = new ZoneMetrics();
        var registry = new ZoneRegistry();
        var manager = new ZoneManager(registry, new EmptyZoneResourceLoader(), metrics);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            manager.StartAsync([Descriptor(1)], new ZoneManagerOptions(), cancellation.Token));

        Assert.Equal(ZoneManagerState.Failed, manager.Current.State);
        Assert.Empty(registry.GetAll());
    }

    private static ZoneDescriptor Descriptor(int id)
        => new()
        {
            Id = new ZoneId(id),
            Name = $"Z{id:D3}",
            Load = true,
        };

    private sealed class EmptyZoneResourceLoader : IZoneResourceLoader
    {
        public Task<ZoneResourceData> LoadAsync(ZoneId zoneId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ZoneResourceData(
                Array.Empty<SpawnDefinition>(),
                Array.Empty<LegacyWorldRegion>(),
                Array.Empty<LegacyWorldRegion>(),
                false,
                false,
                false));
        }
    }

    private sealed class FailingZoneResourceLoader : IZoneResourceLoader
    {
        public Task<ZoneResourceData> LoadAsync(ZoneId zoneId, CancellationToken cancellationToken)
            => Task.FromException<ZoneResourceData>(new InvalidDataException("Test failure."));
    }
}
