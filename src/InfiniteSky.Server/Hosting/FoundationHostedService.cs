using InfiniteSky.Application.Health;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Owns the initialization and final-stop boundaries of the foundation host lifecycle.
/// </summary>
public sealed partial class FoundationHostedService : IHostedService
{
    private readonly ApplicationLifecycleCoordinator _lifecycle;
    private readonly ILogger<FoundationHostedService> _logger;

    /// <summary>
    /// Initializes the lifecycle service.
    /// </summary>
    public FoundationHostedService(
        ApplicationLifecycleCoordinator lifecycle,
        ILogger<FoundationHostedService> logger)
    {
        _lifecycle = lifecycle;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _lifecycle.SetCheck("Configuration", "Valid");
            _lifecycle.SetCheck("Networking", "DisabledByFoundation");
            _lifecycle.SetCheck("Database", "DisabledByFoundation");

            await _lifecycle.TransitionAsync(
                ApplicationLifecyclePhase.Initialising,
                "Initializing application infrastructure.",
                cancellationToken);
            await _lifecycle.TransitionAsync(
                ApplicationLifecyclePhase.Starting,
                "Starting hosted infrastructure.",
                cancellationToken);
        }
        catch (Exception exception)
        {
            _lifecycle.MarkFailed("Application startup failed.");
            LogStartupFailed(_logger, exception);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_lifecycle.Current.Phase == ApplicationLifecyclePhase.Failed)
        {
            LogStopped(_logger);
            return;
        }

        try
        {
            await _lifecycle.TransitionAsync(
                ApplicationLifecyclePhase.Stopped,
                "Application infrastructure stopped.",
                cancellationToken);
            LogStopped(_logger);
        }
        catch (Exception exception)
        {
            _lifecycle.MarkFailed("Application shutdown failed.");
            LogShutdownFailed(_logger, exception);
            throw;
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "InfiniteSky foundation stopped.")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Critical,
        Message = "InfiniteSky foundation startup failed.")]
    private static partial void LogStartupFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Critical,
        Message = "InfiniteSky foundation shutdown failed.")]
    private static partial void LogShutdownFailed(ILogger logger, Exception exception);
}
