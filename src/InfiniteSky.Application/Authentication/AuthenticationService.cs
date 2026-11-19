using System.Collections.Concurrent;
using System.Security.Cryptography;

using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Applies the legacy login credential rule and creates one in-process session per account.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService, IDisposable
{
    private const int MaximumCredentialCharacters = 14;

    private readonly IAccountAuthenticationRepository _accounts;
    private readonly ICredentialVerifier _credentialVerifier;
    private readonly IAuthenticationSessionStore _sessions;
    private readonly AuthenticationMetrics _metrics;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _authenticationTimeout;
    private readonly ConcurrentDictionary<Guid, ConnectionContext> _connections = new();

    public AuthenticationService(
        IAccountAuthenticationRepository accounts,
        ICredentialVerifier credentialVerifier,
        IAuthenticationSessionStore sessions,
        AuthenticationMetrics metrics,
        TimeProvider timeProvider,
        TimeSpan authenticationTimeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            authenticationTimeout,
            TimeSpan.Zero);
        _accounts = accounts;
        _credentialVerifier = credentialVerifier;
        _sessions = sessions;
        _metrics = metrics;
        _timeProvider = timeProvider;
        _authenticationTimeout = authenticationTimeout;
    }

    public async ValueTask<AuthenticationResult> AuthenticateAsync(
        Guid connectionId,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        _metrics.Attempted();

        if (connectionId == Guid.Empty
            || username is null
            || password is null
            || !IsLegacyField(username)
            || !IsLegacyField(password))
        {
            return Record(new AuthenticationResult(AuthenticationOutcome.InvalidRequest));
        }

        ConnectionContext context = _connections.GetOrAdd(
            connectionId,
            static _ => new ConnectionContext());

        if (!context.TryBeginAttempt(out AuthenticationConnectionState initialState))
        {
            AuthenticationOutcome outcome = initialState switch
            {
                AuthenticationConnectionState.Authenticated => AuthenticationOutcome.AlreadyAuthenticated,
                AuthenticationConnectionState.Closed => AuthenticationOutcome.ConnectionClosed,
                _ => AuthenticationOutcome.RequestInProgress,
            };
            if (outcome == AuthenticationOutcome.AlreadyAuthenticated)
            {
                _metrics.DuplicateLogin();
            }
            return Record(new AuthenticationResult(outcome));
        }

        AuthenticatedSession? createdSession = null;
        bool attemptCompleted = false;
        using CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                context.ClosedToken);
        linkedCancellation.CancelAfter(_authenticationTimeout);

        try
        {
            AccountAuthenticationRecord? account;
            try
            {
                account = await _accounts.FindByUsernameAsync(
                    username,
                    linkedCancellation.Token);
            }
            catch (AccountRepositoryException)
            {
                _metrics.RepositoryFailed();
                return Record(new AuthenticationResult(AuthenticationOutcome.CannotAuthenticate));
            }

            if (context.IsClosed || cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Record(new AuthenticationResult(AuthenticationOutcome.ConnectionClosed));
            }
            if (linkedCancellation.IsCancellationRequested)
            {
                return Record(new AuthenticationResult(AuthenticationOutcome.TimedOut));
            }

            if (account is null)
            {
                return Record(new AuthenticationResult(AuthenticationOutcome.AccountNotFound));
            }

            try
            {
                if (context.IsClosed || cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Record(new AuthenticationResult(AuthenticationOutcome.ConnectionClosed));
                }
                if (linkedCancellation.IsCancellationRequested)
                {
                    return Record(new AuthenticationResult(AuthenticationOutcome.TimedOut));
                }

                if (!_credentialVerifier.Verify(password, account.LegacyPasswordUtf8))
                {
                    return Record(new AuthenticationResult(AuthenticationOutcome.IncorrectPassword));
                }

                SessionRegistrationStatus registration = _sessions.TryCreate(
                    account.AccountId,
                    account.Username,
                    connectionId,
                    _timeProvider.GetUtcNow(),
                    out AuthenticatedSession? session);

                if (registration != SessionRegistrationStatus.Created || session is null)
                {
                    if (registration is SessionRegistrationStatus.AccountAlreadyActive
                        or SessionRegistrationStatus.ConnectionAlreadyAuthenticated)
                    {
                        _metrics.DuplicateLogin();
                        return Record(new AuthenticationResult(AuthenticationOutcome.AlreadyLoggedIn));
                    }

                    _metrics.SessionStoreFailed();
                    return Record(new AuthenticationResult(AuthenticationOutcome.CannotAuthenticate));
                }
                createdSession = session;

                if (context.IsClosed || cancellationToken.IsCancellationRequested)
                {
                    _sessions.RemoveByConnection(connectionId, out _);
                    createdSession = null;
                    cancellationToken.ThrowIfCancellationRequested();
                    return Record(new AuthenticationResult(AuthenticationOutcome.ConnectionClosed));
                }
                if (linkedCancellation.IsCancellationRequested)
                {
                    _sessions.RemoveByConnection(connectionId, out _);
                    createdSession = null;
                    return Record(new AuthenticationResult(AuthenticationOutcome.TimedOut));
                }

                try
                {
                    await _accounts.SetActiveAsync(
                        account.AccountId,
                        active: true,
                        linkedCancellation.Token);
                }
                catch (AccountRepositoryException)
                {
                    _sessions.RemoveByConnection(connectionId, out _);
                    createdSession = null;
                    _metrics.RepositoryFailed();
                    return Record(new AuthenticationResult(AuthenticationOutcome.CannotAuthenticate));
                }
                catch (OperationCanceledException)
                {
                    _sessions.RemoveByConnection(connectionId, out _);
                    createdSession = null;
                    await ResetAccountActiveAsync(account.AccountId);
                    if (context.IsClosed)
                    {
                        return Record(new AuthenticationResult(AuthenticationOutcome.ConnectionClosed));
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        return Record(new AuthenticationResult(AuthenticationOutcome.TimedOut));
                    }
                    throw;
                }
                catch
                {
                    _sessions.RemoveByConnection(connectionId, out _);
                    createdSession = null;
                    throw;
                }

                if (context.IsClosed || cancellationToken.IsCancellationRequested)
                {
                    _sessions.RemoveByConnection(connectionId, out _);
                    createdSession = null;
                    await ResetAccountActiveAsync(account.AccountId);
                    cancellationToken.ThrowIfCancellationRequested();
                    return Record(new AuthenticationResult(AuthenticationOutcome.ConnectionClosed));
                }
                if (linkedCancellation.IsCancellationRequested)
                {
                    _sessions.RemoveByConnection(connectionId, out _);
                    createdSession = null;
                    await ResetAccountActiveAsync(account.AccountId);
                    return Record(new AuthenticationResult(AuthenticationOutcome.TimedOut));
                }

                if (!context.CompleteAttempt(session))
                {
                    _sessions.RemoveByConnection(connectionId, out _);
                    createdSession = null;
                    await ResetAccountActiveAsync(account.AccountId);
                    return Record(new AuthenticationResult(AuthenticationOutcome.ConnectionClosed));
                }

                attemptCompleted = true;
                createdSession = null;
                return Record(new AuthenticationResult(AuthenticationOutcome.Authenticated, session));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(account.LegacyPasswordUtf8);
            }
        }
        catch (OperationCanceledException) when (context.IsClosed)
        {
            if (createdSession is not null)
            {
                _sessions.RemoveByConnection(connectionId, out _);
            }
            return Record(new AuthenticationResult(AuthenticationOutcome.ConnectionClosed));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (createdSession is not null)
            {
                _sessions.RemoveByConnection(connectionId, out _);
            }
            return Record(new AuthenticationResult(AuthenticationOutcome.TimedOut));
        }
        finally
        {
            if (!attemptCompleted)
            {
                context.CompleteAttempt(session: null);
            }
        }
    }

    public void ConnectionClosed(Guid connectionId)
    {
        if (_connections.TryRemove(connectionId, out ConnectionContext? context))
        {
            context.Dispose();
        }

        _sessions.RemoveByConnection(connectionId, out _);
    }

    public AuthenticationConnectionState? GetConnectionState(Guid connectionId)
    {
        return _connections.TryGetValue(connectionId, out ConnectionContext? context)
            ? context.State
            : null;
    }

    public void Dispose()
    {
        foreach (Guid connectionId in _connections.Keys)
        {
            ConnectionClosed(connectionId);
        }
    }

    private AuthenticationResult Record(AuthenticationResult result)
    {
        if (result.Outcome == AuthenticationOutcome.Authenticated)
        {
            _metrics.Succeeded();
        }
        else if (result.Outcome is not AuthenticationOutcome.ConnectionClosed
            and not AuthenticationOutcome.RequestInProgress
            and not AuthenticationOutcome.AlreadyAuthenticated)
        {
            _metrics.Rejected();
        }

        return result;
    }

    private async ValueTask ResetAccountActiveAsync(AccountId accountId)
    {
        using var timeout = new CancellationTokenSource(_authenticationTimeout);
        await _accounts.SetActiveAsync(
            accountId,
            active: false,
            timeout.Token);
    }

    private static bool IsLegacyField(string value)
    {
        if (value.Length > MaximumCredentialCharacters)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character > byte.MaxValue)
            {
                return false;
            }
        }

        return true;
    }

    private sealed class ConnectionContext : IDisposable
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _closed = new();
        private bool _isClosed;
        private bool _closeCancellationComplete;
        private bool _disposed;
        private bool _attemptInProgress;
        private AuthenticatedSession? _session;

        public CancellationToken ClosedToken => _closed.Token;

        public AuthenticationConnectionState State
        {
            get
            {
                lock (_sync)
                {
                    if (_isClosed)
                    {
                        return AuthenticationConnectionState.Closed;
                    }
                    if (_session is not null)
                    {
                        return AuthenticationConnectionState.Authenticated;
                    }
                    return _attemptInProgress
                        ? AuthenticationConnectionState.Authenticating
                        : AuthenticationConnectionState.Unauthenticated;
                }
            }
        }

        public bool IsClosed
        {
            get
            {
                lock (_sync)
                {
                    return _isClosed;
                }
            }
        }

        public bool TryBeginAttempt(out AuthenticationConnectionState state)
        {
            lock (_sync)
            {
                if (_isClosed)
                {
                    state = AuthenticationConnectionState.Closed;
                    return false;
                }
                if (_session is not null)
                {
                    state = AuthenticationConnectionState.Authenticated;
                    return false;
                }
                if (_attemptInProgress)
                {
                    state = AuthenticationConnectionState.Authenticating;
                    return false;
                }

                _attemptInProgress = true;
                state = AuthenticationConnectionState.Authenticating;
                return true;
            }
        }

        public bool CompleteAttempt(AuthenticatedSession? session)
        {
            bool dispose = false;
            bool completed;
            lock (_sync)
            {
                if (_isClosed)
                {
                    _attemptInProgress = false;
                    completed = false;
                    if (_closeCancellationComplete && !_disposed)
                    {
                        _disposed = true;
                        dispose = true;
                    }
                }
                else
                {
                    _attemptInProgress = false;
                    _session = session;
                    completed = true;
                }
            }

            if (dispose)
            {
                _closed.Dispose();
            }

            return completed;
        }

        public void Close()
        {
            bool cancel;
            lock (_sync)
            {
                if (_isClosed)
                {
                    return;
                }
                _isClosed = true;
                _session = null;
                cancel = true;
            }

            if (cancel)
            {
                _closed.Cancel();
            }

            bool dispose = false;
            lock (_sync)
            {
                _closeCancellationComplete = true;
                if (!_attemptInProgress && !_disposed)
                {
                    _disposed = true;
                    dispose = true;
                }
            }

            if (dispose)
            {
                _closed.Dispose();
            }
        }

        public void Dispose() => Close();
    }
}
