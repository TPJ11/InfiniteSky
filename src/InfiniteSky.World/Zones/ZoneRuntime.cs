using InfiniteSky.Domain.Values;
using InfiniteSky.Domain.Zones;
using InfiniteSky.GameData.Zones;
using InfiniteSky.Protocol.Structures;
using InfiniteSky.World.Spatial;

namespace InfiniteSky.World.Zones;

/// <summary>
/// Runtime data, lifecycle, and spatial boundary for one statically configured zone.
/// </summary>
public sealed class ZoneRuntime
{
    private readonly object _sync = new();
    private readonly IZoneResourceLoader _resourceLoader;
    private readonly ZoneManagerOptions _options;
    private readonly ZoneMetrics _metrics;
    private ZoneResourceData? _resources;
    private ZoneSpatialIndex? _spatialIndex;
    private Task? _initializationTask;
    private ZoneLifecycleState _state = ZoneLifecycleState.Created;
    private bool _registered;

    public ZoneRuntime(
        ZoneDescriptor descriptor,
        IZoneResourceLoader resourceLoader,
        ZoneManagerOptions options,
        ZoneMetrics metrics)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public ZoneDescriptor Descriptor { get; }

    public ZoneLifecycleState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    public int SpatialObjectCount => RequireSpatialIndex().Count;
    public IReadOnlyList<SpawnDefinition> NpcSpawns => RequireResources().NpcSpawns;
    public IReadOnlyList<LegacyWorldRegion> MoveRegions => RequireResources().MoveRegions;
    public IReadOnlyList<LegacyWorldRegion> SafeRegions => RequireResources().SafeRegions;
    public bool HasNpcSpawnFile => RequireResources().HasNpcSpawnFile;
    public bool HasMoveRegionFile => RequireResources().HasMoveRegionFile;
    public bool HasSafeRegionFile => RequireResources().HasSafeRegionFile;
    public double ViewableActionDistance => _options.ViewableActionDistance;

    public ZoneRuntimeSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                return new ZoneRuntimeSnapshot(
                    Descriptor,
                    _state,
                    _spatialIndex?.Count ?? 0,
                    _resources?.NpcSpawns.Count ?? 0,
                    _resources?.MoveRegions.Count ?? 0,
                    _resources?.SafeRegions.Count ?? 0,
                    _resources?.HasNpcSpawnFile ?? false,
                    _resources?.HasMoveRegionFile ?? false,
                    _resources?.HasSafeRegionFile ?? false);
            }
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_initializationTask is not null)
            {
                return _initializationTask;
            }

            if (_state != ZoneLifecycleState.Created)
            {
                throw new InvalidOperationException($"Zone {Descriptor.Id.Value} cannot initialize from {_state}.");
            }

            _state = ZoneLifecycleState.Initializing;
            _initializationTask = InitializeCoreAsync(cancellationToken);
            return _initializationTask;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_state == ZoneLifecycleState.Running)
            {
                return Task.CompletedTask;
            }

            if (_state != ZoneLifecycleState.Initialized)
            {
                throw new InvalidOperationException($"Zone {Descriptor.Id.Value} cannot start from {_state}.");
            }

            _state = ZoneLifecycleState.Starting;
            _state = ZoneLifecycleState.Running;
            return Task.CompletedTask;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        lock (_sync)
        {
            if (_state is ZoneLifecycleState.Stopped or ZoneLifecycleState.Failed)
            {
                return Task.CompletedTask;
            }

            if (_state == ZoneLifecycleState.Initializing)
            {
                throw new InvalidOperationException(
                    $"Zone {Descriptor.Id.Value} initialization must finish before shutdown.");
            }

            if (_registered)
            {
                throw new InvalidOperationException(
                    $"Zone {Descriptor.Id.Value} must be unregistered before shutdown.");
            }

            if (_state == ZoneLifecycleState.Created)
            {
                _state = ZoneLifecycleState.Stopped;
                return Task.CompletedTask;
            }

            _state = ZoneLifecycleState.Stopping;
            _spatialIndex?.Dispose();
            _spatialIndex = null;
            _resources = null;
            _state = ZoneLifecycleState.Stopped;
            return Task.CompletedTask;
        }
    }

    public ISpatialIndex GetSpatialIndex() => RequireSpatialIndex();

    public IReadOnlyList<SpatialObjectSnapshot> QueryActionCandidates(
        Position3 center,
        IReadOnlySet<string>? types = null)
        => RequireSpatialIndex().Query(center, _options.ViewableActionDistance, types);

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            ZoneResourceData resources = await _resourceLoader
                .LoadAsync(Descriptor.Id, cancellationToken)
                .ConfigureAwait(false);
            var spatialIndex = new ZoneSpatialIndex(
                _options.QuadTreeDepth,
                _options.QuadTreeNodeLimit,
                _metrics);

            lock (_sync)
            {
                _resources = resources;
                _spatialIndex = spatialIndex;
                _state = ZoneLifecycleState.Initialized;
            }
        }
        catch
        {
            lock (_sync)
            {
                _state = ZoneLifecycleState.Failed;
            }

            _metrics.RecordZoneLifecycleFailure();
            throw;
        }
    }

    private ZoneResourceData RequireResources()
    {
        lock (_sync)
        {
            return _resources
                ?? throw new InvalidOperationException($"Zone {Descriptor.Id.Value} has no initialized resources.");
        }
    }

    private ZoneSpatialIndex RequireSpatialIndex()
    {
        lock (_sync)
        {
            if (_state is not (ZoneLifecycleState.Initialized
                or ZoneLifecycleState.Starting
                or ZoneLifecycleState.Running))
            {
                throw new InvalidOperationException(
                    $"Zone {Descriptor.Id.Value} has no operational spatial index while in {_state}.");
            }

            return _spatialIndex
                ?? throw new InvalidOperationException($"Zone {Descriptor.Id.Value} has no initialized spatial index.");
        }
    }

    internal bool TryMarkRegistered()
    {
        lock (_sync)
        {
            if (_state != ZoneLifecycleState.Running || _registered)
            {
                return false;
            }

            _registered = true;
            return true;
        }
    }

    internal void MarkUnregistered()
    {
        lock (_sync)
        {
            _registered = false;
        }
    }
}

/// <summary>
/// Immutable point-in-time zone diagnostics.
/// </summary>
public sealed record ZoneRuntimeSnapshot(
    ZoneDescriptor Descriptor,
    ZoneLifecycleState State,
    int SpatialObjectCount,
    int NpcSpawnCount,
    int MoveRegionCount,
    int SafeRegionCount,
    bool HasNpcSpawnFile,
    bool HasMoveRegionFile,
    bool HasSafeRegionFile);
