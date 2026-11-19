using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.Application.Characters;

/// <summary>
/// Loads account-owned legacy characters and persists the world-location fields owned by disconnect handling.
/// </summary>
public interface ICharacterRepository
{
    ValueTask<IReadOnlyList<Character>> GetForAccountAsync(
        AccountId accountId,
        string serverName,
        CancellationToken cancellationToken);

    ValueTask<Character?> GetOwnedAsync(
        CharacterId characterId,
        AccountId accountId,
        string serverName,
        CancellationToken cancellationToken);

    ValueTask SaveWorldLocationAsync(
        CharacterId characterId,
        AccountId accountId,
        string serverName,
        ZoneId zoneId,
        Position3 position,
        CancellationToken cancellationToken);
}
