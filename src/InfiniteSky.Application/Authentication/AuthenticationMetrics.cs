using System.Diagnostics.Metrics;

namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Metrics for real authentication and session events.
/// </summary>
public sealed class AuthenticationMetrics : IDisposable
{
    private readonly Meter _meter = new("InfiniteSky.Authentication");
    private readonly Counter<long> _attempts;
    private readonly Counter<long> _successes;
    private readonly Counter<long> _rejections;
    private readonly Counter<long> _repositoryFailures;
    private readonly Counter<long> _sessionStoreFailures;
    private readonly Counter<long> _duplicateLogins;
    private readonly UpDownCounter<long> _activeSessions;

    public AuthenticationMetrics()
    {
        _attempts = _meter.CreateCounter<long>("infinite_sky.authentication.attempts");
        _successes = _meter.CreateCounter<long>("infinite_sky.authentication.successes");
        _rejections = _meter.CreateCounter<long>("infinite_sky.authentication.rejections");
        _repositoryFailures = _meter.CreateCounter<long>("infinite_sky.authentication.repository_failures");
        _sessionStoreFailures = _meter.CreateCounter<long>("infinite_sky.authentication.session_store_failures");
        _duplicateLogins = _meter.CreateCounter<long>("infinite_sky.authentication.duplicate_logins");
        _activeSessions = _meter.CreateUpDownCounter<long>("infinite_sky.authentication.active_sessions");
    }

    public void Attempted() => _attempts.Add(1);
    public void Succeeded() => _successes.Add(1);
    public void Rejected() => _rejections.Add(1);
    public void RepositoryFailed() => _repositoryFailures.Add(1);
    public void SessionStoreFailed() => _sessionStoreFailures.Add(1);
    public void DuplicateLogin() => _duplicateLogins.Add(1);
    public void SessionAdded() => _activeSessions.Add(1);
    public void SessionRemoved() => _activeSessions.Add(-1);
    public void Dispose() => _meter.Dispose();
}
