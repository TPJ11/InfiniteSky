using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.World.Players;

/// <summary>
/// Immutable view of an active player's identity and world membership.
/// </summary>
public sealed record PlayerRuntimeSnapshot(
    CharacterId CharacterId,
    AccountId AccountId,
    Guid AuthenticationSessionId,
    Guid WorldConnectionId,
    ZoneId ZoneId,
    EntityId EntityId,
    Position3 Position);
