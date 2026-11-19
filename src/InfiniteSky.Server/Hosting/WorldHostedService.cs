using System.Diagnostics;

using InfiniteSky.Application.Guilds;
using InfiniteSky.Application.Health;
using InfiniteSky.Domain.Guilds;
using InfiniteSky.Persistence;
using InfiniteSky.Server.Configuration;
using InfiniteSky.World;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Loads world bootstrap data before networking and owns the world cadence until shutdown.
/// </summary>
public sealed partial class WorldHostedService : CoordinatedBackgroundService
{
    private readonly IGuildRepository _guildRepository;
    private readonly MongoPersistenceSettings _persistenceSettings;
    private readonly WorldRuntime _runtime;
    private readonly WorldClock _clock;
    private readonly WorldMetrics _metrics;
    private readonly WorldConfiguration _configuration;
    private readonly IWorldZoneReadiness[] _zoneReadinessProviders;
    private readonly ApplicationLifecycleCoordinator _lifecycle;
    private readonly ILogger<WorldHostedService> _logger;
    private int _startAttempted;

    public WorldHostedService(
        IGuildRepository guildRepository,
        MongoPersistenceSettings persistenceSettings,
        WorldRuntime runtime,
        WorldClock clock,
        WorldMetrics metrics,
        IOptions<InfiniteSkyOptions> options,
        IEnumerable<IWorldZoneReadiness> zoneReadinessProviders,
        ApplicationLifecycleCoordinator lifecycle,
        ILogger<WorldHostedService> logger)
        : base(lifecycle, logger)
    {
        _guildRepository = guildRepository;
        _persistenceSettings = persistenceSettings;
        _runtime = runtime;
        _clock = clock;
        _metrics = metrics;
        _configuration = new WorldConfiguration(options.Value.World.TickMilliseconds);
        _zoneReadinessProviders = zoneReadinessProviders.ToArray();
        _lifecycle = lifecycle;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _startAttempted, 1, 0) != 0)
        {
            if (_runtime.Current.State == WorldLifecycleState.Running)
            {
                return;
            }

            throw new InvalidOperationException("World startup has already been attempted.");
        }

        long start = Stopwatch.GetTimestamp();
        _runtime.BeginInitialization();

        try
        {
            IReadOnlyList<Guild> guilds = [];
            bool guildDataAvailable = false;
            if (_persistenceSettings.Enabled)
            {
                try
                {
                    guilds = await _guildRepository.LoadAllAsync(cancellationToken);
                    guildDataAvailable = true;
                }
                catch (MongoPersistenceException exception)
                {
                    if (exception.FailureKind == MongoPersistenceFailureKind.OperationCancelled
                        && cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    LogGuildBootstrapUnavailable(_logger, exception.FailureKind.ToString());
                }
                catch (OperationCanceledException) when (
                    !cancellationToken.IsCancellationRequested)
                {
                    LogGuildBootstrapUnavailable(
                        _logger,
                        MongoPersistenceFailureKind.Unavailable.ToString());
                }
            }

            if (_zoneReadinessProviders.Any(provider => provider.LoadedZoneCount < 0))
            {
                throw new InvalidOperationException("A world zone readiness provider reported a negative count.");
            }

            int loadedZoneCount = checked(
                _zoneReadinessProviders.Sum(provider => provider.LoadedZoneCount));
            bool zonesAvailable = _zoneReadinessProviders.Length > 0
                && _zoneReadinessProviders.All(provider => provider.IsReady);

            _runtime.CompleteInitialization(
                _configuration,
                guilds,
                guildDataAvailable,
                zonesAvailable,
                loadedZoneCount);
            _runtime.BeginStart();
            _runtime.MarkRunning();
            _metrics.RecordInitialization(Stopwatch.GetElapsedTime(start));

            SetLifecycleCheck();
            if (_logger.IsEnabled(LogLevel.Information))
            {
                WorldRuntimeSnapshot snapshot = _runtime.Current;
                LogStarted(
                    _logger,
                    snapshot.Readiness,
                    snapshot.GuildCount,
                    snapshot.Blockers.Count);
            }

            await base.StartAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkStartupFailure();
            throw;
        }
        catch (Exception exception)
        {
            MarkStartupFailure();
            LogStartupFailed(_logger, exception);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        WorldLifecycleState state = _runtime.Current.State;
        if (state is WorldLifecycleState.Created
            or WorldLifecycleState.Stopped
            or WorldLifecycleState.Failed)
        {
            await base.StopAsync(cancellationToken);
            return;
        }

        _runtime.BeginStop();
        _lifecycle.SetCheck("World", WorldReadinessStatus.Stopping.ToString());
        try
        {
            await base.StopAsync(cancellationToken);
            _runtime.MarkStopped();
            SetLifecycleCheck();
            LogStopped(_logger);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _runtime.MarkFailed();
            _metrics.RecordLifecycleFailure();
            _lifecycle.SetCheck("World", WorldReadinessStatus.Failed.ToString());
            _lifecycle.MarkFailed("World shutdown failed.");
            LogShutdownFailed(_logger, exception);
            throw;
        }
    }

    protected override async Task ExecuteServiceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _clock.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            _runtime.MarkFailed();
            _metrics.RecordLifecycleFailure();
            _lifecycle.SetCheck("World", WorldReadinessStatus.Failed.ToString());
            throw;
        }
    }

    private void MarkStartupFailure()
    {
        _runtime.MarkFailed();
        _metrics.RecordLifecycleFailure();
        _lifecycle.SetCheck("World", WorldReadinessStatus.Failed.ToString());
        _lifecycle.MarkFailed("World startup failed.");
    }

    private void SetLifecycleCheck()
    {
        WorldRuntimeSnapshot snapshot = _runtime.Current;
        _lifecycle.SetCheck("World", snapshot.Readiness.ToString());
    }

    [LoggerMessage(
        EventId = 4200,
        Level = LogLevel.Information,
        Message = "World runtime started: Readiness={Readiness}, Guilds={GuildCount}, BlockerCount={BlockerCount}")]
    private static partial void LogStarted(
        ILogger logger,
        WorldReadinessStatus readiness,
        int guildCount,
        int blockerCount);

    [LoggerMessage(
        EventId = 4201,
        Level = LogLevel.Warning,
        Message = "World guild bootstrap is unavailable: Failure={FailureKind}")]
    private static partial void LogGuildBootstrapUnavailable(
        ILogger logger,
        string failureKind);

    [LoggerMessage(
        EventId = 4202,
        Level = LogLevel.Critical,
        Message = "World startup failed.")]
    private static partial void LogStartupFailed(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 4203,
        Level = LogLevel.Information,
        Message = "World runtime stopped.")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(
        EventId = 4204,
        Level = LogLevel.Critical,
        Message = "World shutdown failed.")]
    private static partial void LogShutdownFailed(
        ILogger logger,
        Exception exception);
}
