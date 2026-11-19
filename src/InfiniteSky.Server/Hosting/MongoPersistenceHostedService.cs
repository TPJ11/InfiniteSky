using InfiniteSky.Application.Authentication;
using InfiniteSky.Application.Health;
using InfiniteSky.Persistence;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Verifies MongoDB before network startup and performs bounded periodic health probes.
/// </summary>
public sealed partial class MongoPersistenceHostedService : BackgroundService
{
    private readonly IAccountAuthenticationRepository _accounts;
    private readonly ApplicationLifecycleCoordinator _lifecycle;
    private readonly ILogger<MongoPersistenceHostedService> _logger;
    private readonly MongoPersistenceContext _context;
    private readonly MongoPersistenceHealthState _healthState;
    private readonly MongoPersistenceSettings _settings;

    public MongoPersistenceHostedService(
        IAccountAuthenticationRepository accounts,
        ApplicationLifecycleCoordinator lifecycle,
        MongoPersistenceContext context,
        MongoPersistenceHealthState healthState,
        MongoPersistenceSettings settings,
        ILogger<MongoPersistenceHostedService> logger)
    {
        _accounts = accounts;
        _lifecycle = lifecycle;
        _context = context;
        _healthState = healthState;
        _settings = settings;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            _healthState.MarkDisabled();
            _lifecycle.SetCheck("Database", "DisabledByConfiguration");
            LogDisabled(_logger);
            return;
        }

        await InitializeAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_settings.HealthCheckIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using CancellationTokenSource timeout =
                CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_settings.HealthCheckTimeoutSeconds));

            try
            {
                await _context.PingAsync(timeout.Token);
                _healthState.MarkHealthy();
                _lifecycle.SetCheck("Database", "Connected");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (
                exception is MongoException
                    or MongoPersistenceException
                    or BsonException
                    or TimeoutException
                    or OperationCanceledException)
            {
                MongoPersistenceFailureKind failureKind =
                    exception is OperationCanceledException
                        ? MongoPersistenceFailureKind.Unavailable
                        : MongoPersistenceErrorClassifier.Classify(exception);
                _healthState.MarkFailure(failureKind);
                _lifecycle.SetCheck("Database", failureKind.ToString());
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    LogProbeFailed(_logger, failureKind.ToString());
                }
            }
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (CancellationTokenSource timeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(_settings.HealthCheckTimeoutSeconds));
                await _context.PingAsync(timeout.Token);
            }

            using (CancellationTokenSource indexTimeout =
                _context.CreateOperationToken(cancellationToken))
            {
                await _context.VerifyAccountUsernameIndexAsync(
                    _settings.EnsureIndexesOnStartup,
                    indexTimeout.Token);
            }

            if (_settings.ResetActiveAccountsOnStartup)
            {
                await _accounts.ResetAllActiveAsync(cancellationToken);
            }

            _healthState.MarkHealthy();
            _lifecycle.SetCheck("Database", "Connected");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is MongoException
                or MongoPersistenceException
                or AccountRepositoryException
                or BsonException
                or TimeoutException
                or OperationCanceledException)
        {
            MongoPersistenceFailureKind failureKind =
                exception switch
                {
                    OperationCanceledException => MongoPersistenceFailureKind.Unavailable,
                    AccountRepositoryException repositoryException =>
                        MapRepositoryFailure(repositoryException.FailureKind),
                    _ => MongoPersistenceErrorClassifier.Classify(exception),
                };
            string failureName = GetFailureName(failureKind);
            _healthState.MarkFailure(failureKind);
            _lifecycle.SetCheck("Database", failureName);
            if (_logger.IsEnabled(LogLevel.Critical))
            {
                LogStartupFailed(_logger, failureName);
            }
            throw new InvalidOperationException(
                $"MongoDB startup failed ({failureName}).");
        }
    }

    private static string GetFailureName(MongoPersistenceFailureKind failureKind)
    {
        return failureKind switch
        {
            MongoPersistenceFailureKind.Configuration => "Configuration",
            MongoPersistenceFailureKind.Disabled => "Disabled",
            MongoPersistenceFailureKind.Authentication => "Authentication",
            MongoPersistenceFailureKind.Unavailable => "Unavailable",
            MongoPersistenceFailureKind.AccountNotFound => "AccountNotFound",
            MongoPersistenceFailureKind.DuplicateKey => "DuplicateKey",
            MongoPersistenceFailureKind.Serialization => "Serialization",
            MongoPersistenceFailureKind.MalformedDocument => "MalformedDocument",
            MongoPersistenceFailureKind.IndexConflict => "IndexConflict",
            MongoPersistenceFailureKind.IndexMissing => "IndexMissing",
            MongoPersistenceFailureKind.OperationCancelled => "OperationCancelled",
            _ => "Database",
        };
    }

    private static MongoPersistenceFailureKind MapRepositoryFailure(
        AccountRepositoryFailureKind failureKind)
    {
        return failureKind switch
        {
            AccountRepositoryFailureKind.Disabled => MongoPersistenceFailureKind.Disabled,
            AccountRepositoryFailureKind.Configuration => MongoPersistenceFailureKind.Configuration,
            AccountRepositoryFailureKind.Authentication => MongoPersistenceFailureKind.Authentication,
            AccountRepositoryFailureKind.Unavailable => MongoPersistenceFailureKind.Unavailable,
            AccountRepositoryFailureKind.DuplicateKey => MongoPersistenceFailureKind.DuplicateKey,
            AccountRepositoryFailureKind.Serialization => MongoPersistenceFailureKind.Serialization,
            AccountRepositoryFailureKind.MalformedDocument => MongoPersistenceFailureKind.MalformedDocument,
            AccountRepositoryFailureKind.AccountNotFound => MongoPersistenceFailureKind.AccountNotFound,
            AccountRepositoryFailureKind.IndexFailure => MongoPersistenceFailureKind.IndexConflict,
            _ => MongoPersistenceFailureKind.Database,
        };
    }

    [LoggerMessage(
        EventId = 4110,
        Level = LogLevel.Information,
        Message = "MongoDB persistence is disabled by configuration.")]
    private static partial void LogDisabled(ILogger logger);

    [LoggerMessage(
        EventId = 4111,
        Level = LogLevel.Critical,
        Message = "MongoDB startup failed: Failure={FailureKind}")]
    private static partial void LogStartupFailed(ILogger logger, string failureKind);

    [LoggerMessage(
        EventId = 4112,
        Level = LogLevel.Warning,
        Message = "MongoDB health probe failed: Failure={FailureKind}")]
    private static partial void LogProbeFailed(ILogger logger, string failureKind);
}
