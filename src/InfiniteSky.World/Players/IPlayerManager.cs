using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;
using InfiniteSky.World.Zones;

namespace InfiniteSky.World.Players;

/// <summary>
/// Owns active player registration and zone-local spatial membership.
/// </summary>
public interface IPlayerManager
{
    int Count { get; }

    PlayerRuntime Activate(
        Character character,
        Guid authenticationSessionId,
        Guid worldConnectionId,
        ZoneRuntime zone);

    bool TryFindByCharacter(CharacterId characterId, out PlayerRuntime? player);

    bool TryFindByWorldConnection(Guid worldConnectionId, out PlayerRuntime? player);

    bool TryFindByAuthenticationSession(Guid sessionId, out PlayerRuntime? player);

    bool TryFindByEntity(ZoneId zoneId, EntityId entityId, out PlayerRuntime? player);

    IReadOnlyList<PlayerRuntimeSnapshot> GetNearbyPlayers(PlayerRuntime player);

    bool TryUpdatePosition(PlayerRuntime player, Position3 position);

    PlayerRuntimeSnapshot? RemoveByWorldConnection(Guid worldConnectionId);

    IReadOnlyList<PlayerRuntimeSnapshot> RemoveAll();
}
