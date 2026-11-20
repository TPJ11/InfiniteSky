using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Single-process session ownership for the current monolithic server.
/// </summary>
public sealed class InMemoryAuthenticationSessionStore : IAuthenticationSessionStore
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, AuthenticatedSession> _byConnection = [];
    private readonly Dictionary<AccountId, AuthenticatedSession> _byAccount = [];
    private readonly AuthenticationMetrics _metrics;

    public InMemoryAuthenticationSessionStore(AuthenticationMetrics metrics)
    {
        _metrics = metrics;
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _byConnection.Count;
            }
        }
    }

    public SessionRegistrationStatus TryCreate(
        AccountId accountId,
        string username,
        double accountLevel,
        Guid connectionId,
        DateTimeOffset createdAt,
        out AuthenticatedSession? session)
    {
        ArgumentNullException.ThrowIfNull(username);
        if (!double.IsFinite(accountLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(accountLevel));
        }
        session = null;

        if (connectionId == Guid.Empty)
        {
            return SessionRegistrationStatus.Unavailable;
        }

        lock (_sync)
        {
            if (_byAccount.ContainsKey(accountId))
            {
                return SessionRegistrationStatus.AccountAlreadyActive;
            }

            if (_byConnection.ContainsKey(connectionId))
            {
                return SessionRegistrationStatus.ConnectionAlreadyAuthenticated;
            }

            session = new AuthenticatedSession(
                Guid.NewGuid(),
                accountId,
                username,
                accountLevel,
                connectionId,
                createdAt);
            _byAccount.Add(accountId, session);
            _byConnection.Add(connectionId, session);
            _metrics.SessionAdded();
            return SessionRegistrationStatus.Created;
        }
    }

    public bool TryGetByConnection(Guid connectionId, out AuthenticatedSession? session)
    {
        lock (_sync)
        {
            return _byConnection.TryGetValue(connectionId, out session);
        }
    }

    public bool TryGetByAccount(AccountId accountId, out AuthenticatedSession? session)
    {
        lock (_sync)
        {
            return _byAccount.TryGetValue(accountId, out session);
        }
    }

    public bool RemoveByConnection(Guid connectionId, out AuthenticatedSession? session)
    {
        lock (_sync)
        {
            if (!_byConnection.Remove(connectionId, out session) || session is null)
            {
                return false;
            }

            _byAccount.Remove(session.AccountId);
            _metrics.SessionRemoved();
            return true;
        }
    }
}
