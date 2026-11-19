using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;
using InfiniteSky.World.Spatial;
using InfiniteSky.World.Zones;

namespace InfiniteSky.World.Players;

/// <summary>
/// Registers loaded players once and keeps zone spatial membership consistent with runtime ownership.
/// </summary>
public sealed class PlayerManager(PlayerMetrics metrics) : IPlayerManager
{
    public const string SpatialType = "client";

    private readonly object _sync = new();
    private readonly Dictionary<CharacterId, PlayerRuntime> _byCharacter = [];
    private readonly Dictionary<Guid, PlayerRuntime> _byWorldConnection = [];
    private readonly Dictionary<Guid, PlayerRuntime> _byAuthenticationSession = [];
    private readonly Dictionary<(ZoneId ZoneId, EntityId EntityId), PlayerRuntime> _byEntity = [];

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _byCharacter.Count;
            }
        }
    }

    public PlayerRuntime Activate(
        Character character,
        Guid authenticationSessionId,
        Guid worldConnectionId,
        ZoneRuntime zone)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(zone);
        if (authenticationSessionId == Guid.Empty || worldConnectionId == Guid.Empty)
        {
            throw new ArgumentException("Player activation requires non-empty session and connection IDs.");
        }

        if (character.SavedLocation.ZoneId != zone.Descriptor.Id
            || character.SavedLocation.Position is not Position3 position)
        {
            throw new InvalidOperationException("The selected zone must match the character's complete saved location.");
        }

        PlayerRuntime? activated = null;
        lock (_sync)
        {
            if (_byCharacter.ContainsKey(character.Id)
                || _byWorldConnection.ContainsKey(worldConnectionId)
                || _byAuthenticationSession.ContainsKey(authenticationSessionId))
            {
                metrics.RecordActivationFailure();
                throw new InvalidOperationException("A player, connection, or authentication session is already active.");
            }

            bool registered;
            try
            {
                registered = zone.TryRegisterSpatialObject(
                    position,
                    SpatialType,
                    character.Progression.Level,
                    entityId =>
                    {
                        var player = new PlayerRuntime(
                            character,
                            authenticationSessionId,
                            worldConnectionId,
                            zone,
                            entityId,
                            position);
                        try
                        {
                            _byCharacter.Add(character.Id, player);
                            _byWorldConnection.Add(worldConnectionId, player);
                            _byAuthenticationSession.Add(authenticationSessionId, player);
                            _byEntity.Add((zone.Descriptor.Id, entityId), player);
                            activated = player;
                            metrics.RecordActivePlayerDelta(1);
                            return true;
                        }
                        catch
                        {
                            _byCharacter.Remove(character.Id);
                            _byWorldConnection.Remove(worldConnectionId);
                            _byAuthenticationSession.Remove(authenticationSessionId);
                            _byEntity.Remove((zone.Descriptor.Id, entityId));
                            throw;
                        }
                    });
            }
            catch
            {
                metrics.RecordActivationFailure();
                throw;
            }

            if (!registered || activated is null)
            {
                metrics.RecordActivationFailure();
                throw new InvalidOperationException("The player could not be registered in the running zone.");
            }
        }

        return activated;
    }

    public bool TryFindByCharacter(CharacterId characterId, out PlayerRuntime? player)
    {
        lock (_sync)
        {
            return _byCharacter.TryGetValue(characterId, out player);
        }
    }

    public bool TryFindByWorldConnection(Guid worldConnectionId, out PlayerRuntime? player)
    {
        lock (_sync)
        {
            return _byWorldConnection.TryGetValue(worldConnectionId, out player);
        }
    }

    public bool TryFindByAuthenticationSession(Guid sessionId, out PlayerRuntime? player)
    {
        lock (_sync)
        {
            return _byAuthenticationSession.TryGetValue(sessionId, out player);
        }
    }

    public bool TryFindByEntity(ZoneId zoneId, EntityId entityId, out PlayerRuntime? player)
    {
        lock (_sync)
        {
            return _byEntity.TryGetValue((zoneId, entityId), out player);
        }
    }

    public IReadOnlyList<PlayerRuntimeSnapshot> GetNearbyPlayers(PlayerRuntime player)
    {
        ArgumentNullException.ThrowIfNull(player);
        lock (_sync)
        {
            if (!_byCharacter.TryGetValue(player.Character.Id, out PlayerRuntime? active)
                || !ReferenceEquals(active, player))
            {
                return [];
            }

            IReadOnlyList<SpatialObjectSnapshot> candidates =
                player.Zone.QueryActionCandidates(player.Position, new HashSet<string>([SpatialType]));
            var nearbyPlayers = new List<PlayerRuntimeSnapshot>(candidates.Count);
            foreach (SpatialObjectSnapshot candidate in candidates)
            {
                if (candidate.Id != player.EntityId
                    && _byEntity.TryGetValue(
                        (player.Zone.Descriptor.Id, candidate.Id),
                        out PlayerRuntime? nearby)
                    && nearby is not null)
                {
                    nearbyPlayers.Add(nearby.Snapshot());
                }
            }

            return nearbyPlayers;
        }
    }

    public bool TryUpdatePosition(PlayerRuntime player, Position3 position)
    {
        ArgumentNullException.ThrowIfNull(player);
        lock (_sync)
        {
            if (!_byCharacter.TryGetValue(player.Character.Id, out PlayerRuntime? active)
                || !ReferenceEquals(active, player))
            {
                return false;
            }

            if (!player.Zone.TryUpdateSpatialObject(player.EntityId, position))
            {
                return false;
            }

            player.Position = position;
            return true;
        }
    }

    public PlayerRuntimeSnapshot? RemoveByWorldConnection(Guid worldConnectionId)
    {
        lock (_sync)
        {
            if (!_byWorldConnection.TryGetValue(worldConnectionId, out PlayerRuntime? player))
            {
                return null;
            }

            return RemoveCore(player);
        }
    }

    public IReadOnlyList<PlayerRuntimeSnapshot> RemoveAll()
    {
        lock (_sync)
        {
            PlayerRuntime[] players = _byCharacter.Values.ToArray();
            var snapshots = new PlayerRuntimeSnapshot[players.Length];
            for (int index = 0; index < players.Length; index++)
            {
                snapshots[index] = RemoveCore(players[index]);
            }

            return snapshots;
        }
    }

    private PlayerRuntimeSnapshot RemoveCore(PlayerRuntime player)
    {
        _ = player.Zone.TryRemoveSpatialObject(player.EntityId);
        player.MarkRemoved();
        _byCharacter.Remove(player.Character.Id);
        _byWorldConnection.Remove(player.WorldConnectionId);
        _byAuthenticationSession.Remove(player.AuthenticationSessionId);
        _byEntity.Remove((player.Zone.Descriptor.Id, player.EntityId));
        metrics.RecordActivePlayerDelta(-1);
        return player.Snapshot();
    }
}
