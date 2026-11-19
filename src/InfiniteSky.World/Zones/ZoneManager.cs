using System.Diagnostics;

using InfiniteSky.Domain.Zones;
using InfiniteSky.GameData.Zones;

namespace InfiniteSky.World.Zones;

/// <summary>
/// Initializes, publishes, and shuts down the statically selected zone set.
/// </summary>
public sealed class ZoneManager : IZoneManager
{
    private readonly object _sync = new();
    private readonly IZoneRegistry _registry;
    private readonly IZoneResourceLoader _resourceLoader;
    private readonly ZoneMetrics _metrics;
    private Task? _startTask;
    private ZoneManagerState _state = ZoneManagerState.Created;
    private int _configuredZoneCount;
    private int _failedZoneCount;
    private int[]? _requestedZoneIds;

    public ZoneManager(IZoneRegistry registry, IZoneResourceLoader resourceLoader, ZoneMetrics metrics)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public bool IsReady => Current.IsReady;
    public int LoadedZoneCount => Current.LoadedZoneCount;

    public ZoneManagerSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                IReadOnlyList<ZoneRuntime> zones = _registry.GetAll();
                int runningCount = zones.Count(static zone => zone.State == ZoneLifecycleState.Running);
                bool ready = _state == ZoneManagerState.Running
                    && _configuredZoneCount > 0
                    && runningCount == _configuredZoneCount
                    && _failedZoneCount == 0;
                return new ZoneManagerSnapshot(
                    _state,
                    _configuredZoneCount,
                    runningCount,
                    _failedZoneCount,
                    ready);
            }
        }
    }

    public ZoneRuntime? Find(InfiniteSky.Domain.Identifiers.ZoneId id) => _registry.Find(id);
    public IReadOnlyList<ZoneRuntime> GetZones() => _registry.GetAll();

    public Task StartAsync(
        IReadOnlyList<ZoneDescriptor> zones,
        ZoneManagerOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(zones);
        ArgumentNullException.ThrowIfNull(options);

        ZoneDescriptor[] descriptors = zones.ToArray();
        int[] requestedIds = descriptors.Select(static zone => zone.Id.Value).ToArray();
        lock (_sync)
        {
            if (_startTask is not null)
            {
                if (!_requestedZoneIds!.SequenceEqual(requestedIds))
                {
                    throw new InvalidOperationException("Zone manager startup was requested with a different zone set.");
                }

                return _startTask;
            }

            ValidateStartRequest(descriptors, options);
            _requestedZoneIds = requestedIds;
            _configuredZoneCount = descriptors.Length;
            _state = ZoneManagerState.Initializing;
            _startTask = StartCoreAsync(descriptors, options, cancellationToken);
            return _startTask;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ZoneRuntime[] zones;
        lock (_sync)
        {
            if (_state == ZoneManagerState.Stopped)
            {
                return;
            }

            if (_state == ZoneManagerState.Initializing)
            {
                throw new InvalidOperationException("Zone manager startup must finish before shutdown.");
            }

            _state = ZoneManagerState.Stopping;
            zones = _registry.GetAll().Reverse().ToArray();
            foreach (ZoneRuntime zone in zones)
            {
                if (_registry.Unregister(zone.Descriptor.Id))
                {
                    _metrics.RecordZoneRegistration(-1);
                }
            }
        }

        List<Exception>? failures = null;
        foreach (ZoneRuntime zone in zones)
        {
            try
            {
                await zone.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
                _metrics.RecordZoneLifecycleFailure();
            }
        }

        lock (_sync)
        {
            _state = failures is null ? ZoneManagerState.Stopped : ZoneManagerState.Failed;
        }

        if (failures is not null)
        {
            throw new AggregateException("One or more zones failed to shut down.", failures);
        }
    }

    private async Task StartCoreAsync(ZoneDescriptor[] descriptors, ZoneManagerOptions options, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Parallel.ForEachAsync(
                descriptors,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = options.LoadConcurrency,
                },
                async (descriptor, token) =>
                {
                    var zone = new ZoneRuntime(descriptor, _resourceLoader, options, _metrics);
                    long started = Stopwatch.GetTimestamp();
                    try
                    {
                        await zone.InitializeAsync(token).ConfigureAwait(false);
                        await zone.StartAsync(token).ConfigureAwait(false);
                        if (!_registry.TryRegister(zone))
                        {
                            throw new InvalidOperationException(
                                $"Zone ID {descriptor.Id.Value} is already registered.");
                        }
                        _metrics.RecordZoneRegistration(1);
                    }
                    catch
                    {
                        await zone.StopAsync(CancellationToken.None).ConfigureAwait(false);
                        throw;
                    }
                    finally
                    {
                        _metrics.RecordZoneInitialization(Stopwatch.GetElapsedTime(started));
                    }
                }).ConfigureAwait(false);

            lock (_sync)
            {
                _state = ZoneManagerState.Running;
            }
        }
        catch (Exception startupFailure)
        {
            lock (_sync)
            {
                _failedZoneCount++;
                _state = ZoneManagerState.Failed;
            }

            _metrics.RecordZoneLifecycleFailure();
            try
            {
                await StopRegisteredZonesAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    "Zone startup and cleanup both failed.",
                    startupFailure,
                    cleanupFailure);
            }

            throw;
        }
    }

    private async Task StopRegisteredZonesAsync()
    {
        ZoneRuntime[] zones = _registry.GetAll().Reverse().ToArray();
        foreach (ZoneRuntime zone in zones)
        {
            if (_registry.Unregister(zone.Descriptor.Id))
            {
                _metrics.RecordZoneRegistration(-1);
            }
        }

        List<Exception>? failures = null;
        foreach (ZoneRuntime zone in zones)
        {
            try
            {
                await zone.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
                _metrics.RecordZoneLifecycleFailure();
            }
        }

        if (failures is not null)
        {
            throw new AggregateException("One or more zones failed to stop after startup failure.", failures);
        }
    }

    private static void ValidateStartRequest(ZoneDescriptor[] descriptors, ZoneManagerOptions options)
    {
        if (descriptors.Any(static zone => !zone.Load))
        {
            throw new ArgumentException("Only zones configured with Load=true may be started.", nameof(descriptors));
        }

        if (descriptors.Any(static zone => zone.Id.Value <= 0))
        {
            throw new ArgumentException("Zone IDs must be positive.", nameof(descriptors));
        }

        if (descriptors.Select(static zone => zone.Id).Distinct().Count() != descriptors.Length)
        {
            throw new ArgumentException("Zone startup cannot contain duplicate IDs.", nameof(descriptors));
        }

        if (options.LoadConcurrency is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Zone load concurrency must be between 1 and 128.");
        }

        if (options.QuadTreeDepth is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "QuadTree depth must be between 1 and 32.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.QuadTreeNodeLimit);
        if (!double.IsFinite(options.ViewableActionDistance) || options.ViewableActionDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Viewable action distance must be finite and non-negative.");
        }
    }
}
