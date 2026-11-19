using InfiniteSky.Domain.Guilds;

namespace InfiniteSky.World;

/// <summary>
/// Process-local state for the single world instance in the legacy server.
/// </summary>
public sealed class WorldRuntime(TimeProvider timeProvider)
{
    private readonly object _sync = new();
    private Dictionary<string, WorldGuildSnapshot> _guilds = new(StringComparer.Ordinal);
    private WorldLifecycleState _state = WorldLifecycleState.Created;
    private WorldReadinessStatus _readiness = WorldReadinessStatus.NotInitialized;
    private WorldConfiguration? _configuration;
    private WorldReadinessBlocker[] _blockers = [];
    private int _loadedZoneCount;
    private long _tickCount;
    private double? _lastTickMilliseconds;
    private DateTimeOffset _updatedAt = timeProvider.GetUtcNow();

    public WorldRuntimeSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                return new WorldRuntimeSnapshot(
                    _state,
                    _readiness,
                    _configuration,
                    Array.AsReadOnly((WorldReadinessBlocker[])_blockers.Clone()),
                    _guilds.Count,
                    _loadedZoneCount,
                    _tickCount,
                    _lastTickMilliseconds,
                    _updatedAt);
            }
        }
    }

    public WorldGuildSnapshot? FindGuildByName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        lock (_sync)
        {
            return _guilds.GetValueOrDefault(name);
        }
    }

    public void BeginInitialization()
    {
        lock (_sync)
        {
            Transition(WorldLifecycleState.Initializing, WorldReadinessStatus.Initializing);
        }
    }

    public void CompleteInitialization(
        WorldConfiguration configuration,
        IEnumerable<Guild> guilds,
        bool guildDataAvailable,
        bool zonesAvailable,
        int loadedZoneCount)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(guilds);
        ArgumentOutOfRangeException.ThrowIfNegative(loadedZoneCount);

        var guildSnapshot = new Dictionary<string, WorldGuildSnapshot>(StringComparer.Ordinal);
        foreach (Guild guild in guilds)
        {
            var snapshot = new WorldGuildSnapshot(guild);
            if (!guildSnapshot.TryAdd(snapshot.Name, snapshot))
            {
                throw new InvalidOperationException("World bootstrap contains duplicate guild names.");
            }
        }

        var blockers = new List<WorldReadinessBlocker>();
        if (!guildDataAvailable)
        {
            blockers.Add(WorldReadinessBlocker.GuildDataUnavailable);
        }
        if (!zonesAvailable)
        {
            blockers.Add(WorldReadinessBlocker.ZoneRuntimeUnavailable);
        }

        lock (_sync)
        {
            EnsureState(WorldLifecycleState.Initializing, WorldLifecycleState.Initialized);
            _state = WorldLifecycleState.Initialized;
            _configuration = configuration;
            _guilds = guildSnapshot;
            _blockers = blockers.ToArray();
            _loadedZoneCount = loadedZoneCount;
            _readiness = blockers.Count == 0
                ? WorldReadinessStatus.NotReady
                : WorldReadinessStatus.Degraded;
            UpdateTimestamp();
        }
    }

    public void BeginStart()
    {
        lock (_sync)
        {
            Transition(WorldLifecycleState.Starting, _readiness);
        }
    }

    public void MarkRunning()
    {
        lock (_sync)
        {
            EnsureState(WorldLifecycleState.Starting, WorldLifecycleState.Running);
            _state = WorldLifecycleState.Running;
            _readiness = _blockers.Length == 0
                ? WorldReadinessStatus.Ready
                : WorldReadinessStatus.Degraded;
            UpdateTimestamp();
        }
    }

    public void RecordTick(TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);

        lock (_sync)
        {
            if (_state != WorldLifecycleState.Running)
            {
                throw new WorldTransitionException(_state, WorldLifecycleState.Running);
            }

            _tickCount++;
            _lastTickMilliseconds = elapsed.TotalMilliseconds;
            UpdateTimestamp();
        }
    }

    public void BeginStop()
    {
        lock (_sync)
        {
            if (_state is WorldLifecycleState.Created
                or WorldLifecycleState.Stopping
                or WorldLifecycleState.Stopped
                or WorldLifecycleState.Failed)
            {
                return;
            }

            Transition(WorldLifecycleState.Stopping, WorldReadinessStatus.Stopping);
        }
    }

    public void MarkStopped()
    {
        lock (_sync)
        {
            if (_state is WorldLifecycleState.Stopped or WorldLifecycleState.Failed)
            {
                return;
            }

            if (_state != WorldLifecycleState.Stopping)
            {
                throw new WorldTransitionException(_state, WorldLifecycleState.Stopped);
            }

            Transition(WorldLifecycleState.Stopped, WorldReadinessStatus.Stopped);
        }
    }

    public void MarkFailed()
    {
        lock (_sync)
        {
            if (_state is WorldLifecycleState.Stopped or WorldLifecycleState.Failed)
            {
                return;
            }

            _state = WorldLifecycleState.Failed;
            _readiness = WorldReadinessStatus.Failed;
            UpdateTimestamp();
        }
    }

    private void Transition(WorldLifecycleState next, WorldReadinessStatus readiness)
    {
        EnsureState(_state, next);
        _state = next;
        _readiness = readiness;
        UpdateTimestamp();
    }

    private void EnsureState(WorldLifecycleState expected, WorldLifecycleState next)
    {
        if (_state != expected || !IsAllowed(expected, next))
        {
            throw new WorldTransitionException(_state, next);
        }
    }

    private static bool IsAllowed(WorldLifecycleState current, WorldLifecycleState next)
    {
        return (current, next) switch
        {
            (WorldLifecycleState.Created, WorldLifecycleState.Initializing) => true,
            (WorldLifecycleState.Initializing, WorldLifecycleState.Initialized) => true,
            (WorldLifecycleState.Initialized, WorldLifecycleState.Starting) => true,
            (WorldLifecycleState.Starting, WorldLifecycleState.Running) => true,
            (WorldLifecycleState.Running, WorldLifecycleState.Stopping) => true,
            (WorldLifecycleState.Initialized, WorldLifecycleState.Stopping) => true,
            (WorldLifecycleState.Starting, WorldLifecycleState.Stopping) => true,
            (WorldLifecycleState.Stopping, WorldLifecycleState.Stopped) => true,
            _ => false,
        };
    }

    private void UpdateTimestamp() => _updatedAt = timeProvider.GetUtcNow();
}
