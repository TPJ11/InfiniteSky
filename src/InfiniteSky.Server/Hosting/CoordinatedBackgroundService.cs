using InfiniteSky.Application.Health;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Base convention for cancellable future background services.
/// </summary>
public abstract partial class CoordinatedBackgroundService(
    ApplicationLifecycleCoordinator lifecycle,
    ILogger logger) : BackgroundService
{
    /// <summary>
    /// Runs service-specific work until cancellation is requested.
    /// </summary>
    protected abstract Task ExecuteServiceAsync(CancellationToken stoppingToken);

    /// <inheritdoc />
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ExecuteServiceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogCancelled(logger, GetType().Name);
        }
        catch (Exception exception)
        {
            lifecycle.MarkFailed($"{GetType().Name} failed.");
            LogFailed(logger, GetType().Name, exception);
            throw;
        }
    }

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Debug,
        Message = "Background service {ServiceName} observed cancellation.")]
    private static partial void LogCancelled(ILogger logger, string serviceName);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Critical,
        Message = "Background service {ServiceName} failed.")]
    private static partial void LogFailed(
        ILogger logger,
        string serviceName,
        Exception exception);
}
