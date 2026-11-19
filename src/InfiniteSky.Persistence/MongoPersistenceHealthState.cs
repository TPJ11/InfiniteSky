namespace InfiniteSky.Persistence;

public enum MongoPersistenceHealthStatus
{
    NotChecked,
    Disabled,
    Connected,
    AuthenticationFailure,
    Unavailable,
    OperationFailure,
}

public sealed record MongoPersistenceHealthSnapshot(
    MongoPersistenceHealthStatus Status,
    MongoPersistenceFailureKind? FailureKind,
    DateTimeOffset? LastSuccessfulCheck,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Thread-safe state from bounded MongoDB startup and periodic health probes.
/// </summary>
public sealed class MongoPersistenceHealthState
{
    private readonly Lock _sync = new();
    private readonly TimeProvider _timeProvider;
    private MongoPersistenceHealthStatus _status = MongoPersistenceHealthStatus.NotChecked;
    private MongoPersistenceFailureKind? _failureKind;
    private DateTimeOffset? _lastSuccessfulCheck;
    private DateTimeOffset _updatedAt;

    public MongoPersistenceHealthState(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _updatedAt = timeProvider.GetUtcNow();
    }

    public MongoPersistenceHealthSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                return new MongoPersistenceHealthSnapshot(
                    _status,
                    _failureKind,
                    _lastSuccessfulCheck,
                    _updatedAt);
            }
        }
    }

    public void MarkDisabled() => Update(MongoPersistenceHealthStatus.Disabled, null, false);

    public void MarkHealthy() => Update(MongoPersistenceHealthStatus.Connected, null, true);

    public void MarkFailure(MongoPersistenceFailureKind failureKind)
    {
        MongoPersistenceHealthStatus status = failureKind switch
        {
            MongoPersistenceFailureKind.Authentication => MongoPersistenceHealthStatus.AuthenticationFailure,
            MongoPersistenceFailureKind.Unavailable => MongoPersistenceHealthStatus.Unavailable,
            _ => MongoPersistenceHealthStatus.OperationFailure,
        };
        Update(status, failureKind, false);
    }

    private void Update(
        MongoPersistenceHealthStatus status,
        MongoPersistenceFailureKind? failureKind,
        bool successful)
    {
        lock (_sync)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            _status = status;
            _failureKind = failureKind;
            _updatedAt = now;
            if (successful)
            {
                _lastSuccessfulCheck = now;
            }
        }
    }
}
