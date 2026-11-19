using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Thread-safe in-process store for authenticated login sessions.
/// </summary>
public interface IAuthenticationSessionStore
{
    int Count { get; }

    SessionRegistrationStatus TryCreate(
        AccountId accountId,
        string username,
        Guid connectionId,
        DateTimeOffset createdAt,
        out AuthenticatedSession? session);

    bool TryGetByConnection(Guid connectionId, out AuthenticatedSession? session);

    bool TryGetByAccount(AccountId accountId, out AuthenticatedSession? session);

    bool RemoveByConnection(Guid connectionId, out AuthenticatedSession? session);
}
