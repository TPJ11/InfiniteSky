using InfiniteSky.Application.Diagnostics;
using InfiniteSky.Application.Health;
using InfiniteSky.Server.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Publishes readiness after hosted services start and removes it before they stop.
/// </summary>
public sealed partial class ApplicationReadinessHostedService : IHostedService
{
    private readonly InfiniteSkyDiagnostics _diagnostics;
    private readonly IHostEnvironment _environment;
    private readonly ApplicationLifecycleCoordinator _lifecycle;
    private readonly ILogger<ApplicationReadinessHostedService> _logger;
    private readonly InfiniteSkyOptions _options;

    /// <summary>
    /// Initializes the readiness boundary.
    /// </summary>
    public ApplicationReadinessHostedService(
        IOptions<InfiniteSkyOptions> options,
        IHostEnvironment environment,
        ApplicationLifecycleCoordinator lifecycle,
        InfiniteSkyDiagnostics diagnostics,
        ILogger<ApplicationReadinessHostedService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _lifecycle = lifecycle;
        _diagnostics = diagnostics;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using System.Diagnostics.Activity? activity =
                _diagnostics.Activities.StartActivity("application.lifecycle.start");

            await _lifecycle.TransitionAsync(
                ApplicationLifecyclePhase.Running,
                "All hosted services have started.",
                cancellationToken);

            ApplicationReadinessSnapshot snapshot = _lifecycle.Current;
            LogStarted(
                _logger,
                _options.Application.Name,
                _environment.EnvironmentName,
                _options.Gameplay.CompatibilityProfile,
                snapshot.Checks["Networking"],
                snapshot.Checks["Database"]);
        }
        catch (Exception exception)
        {
            _lifecycle.MarkFailed("Application readiness startup failed.");
            LogStartupFailed(_logger, exception);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_lifecycle.Current.Phase == ApplicationLifecyclePhase.Failed)
        {
            return;
        }

        try
        {
            using System.Diagnostics.Activity? activity =
                _diagnostics.Activities.StartActivity("application.lifecycle.stop");

            await _lifecycle.TransitionAsync(
                ApplicationLifecyclePhase.Stopping,
                "Stopping new work before draining registered services.",
                cancellationToken);
        }
        catch (Exception exception)
        {
            _lifecycle.MarkFailed("Application readiness shutdown failed.");
            LogShutdownFailed(_logger, exception);
            throw;
        }
    }

    [LoggerMessage(
        EventId = 1020,
        Level = LogLevel.Information,
        Message = "InfiniteSky foundation started: Name={ApplicationName}, Environment={Environment}, "
            + "CompatibilityProfile={CompatibilityProfile}, Networking={NetworkingState}, Database={DatabaseState}")]
    private static partial void LogStarted(
        ILogger logger,
        string applicationName,
        string environment,
        string compatibilityProfile,
        string networkingState,
        string databaseState);

    [LoggerMessage(
        EventId = 1021,
        Level = LogLevel.Critical,
        Message = "InfiniteSky readiness startup failed.")]
    private static partial void LogStartupFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1022,
        Level = LogLevel.Critical,
        Message = "InfiniteSky readiness shutdown failed.")]
    private static partial void LogShutdownFailed(ILogger logger, Exception exception);
}
