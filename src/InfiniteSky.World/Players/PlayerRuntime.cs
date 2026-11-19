using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;
using InfiniteSky.World.Zones;

namespace InfiniteSky.World.Players;

/// <summary>
/// Runtime ownership of one loaded character in a world connection and running zone.
/// </summary>
public sealed class PlayerRuntime
{
    private readonly object _sync = new();
    private Position3 _position;
    private PlayerLifecycleState _state = PlayerLifecycleState.Active;

    internal PlayerRuntime(
        Character character,
        Guid authenticationSessionId,
        Guid worldConnectionId,
        ZoneRuntime zone,
        EntityId entityId,
        Position3 position)
    {
        Character = character;
        AuthenticationSessionId = authenticationSessionId;
        WorldConnectionId = worldConnectionId;
        Zone = zone;
        EntityId = entityId;
        _position = position;
    }

    public Character Character { get; }
    public Guid AuthenticationSessionId { get; }
    public Guid WorldConnectionId { get; }
    public ZoneRuntime Zone { get; }
    public EntityId EntityId { get; }
    public PlayerLifecycleState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    public Position3 Position
    {
        get
        {
            lock (_sync)
            {
                return _position;
            }
        }
        internal set
        {
            lock (_sync)
            {
                _position = value;
            }
        }
    }

    public PlayerRuntimeSnapshot Snapshot()
        => new(
            Character.Id,
            Character.AccountId,
            AuthenticationSessionId,
            WorldConnectionId,
            Zone.Descriptor.Id,
            EntityId,
            Position);

    internal void MarkRemoved()
    {
        lock (_sync)
        {
            _state = PlayerLifecycleState.Removed;
        }
    }
}
