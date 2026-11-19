using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.Application.Characters;

/// <summary>
/// A character's saved legacy world destination, independent of transient entity identity.
/// </summary>
public sealed record PlayerWorldLocation(
    CharacterId CharacterId,
    AccountId AccountId,
    string ServerName,
    ZoneId ZoneId,
    Position3 Position);
