using InfiniteSky.Application.Players;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Saves and unregisters active player locations before world zones are shut down.
/// </summary>
public sealed partial class PlayerManagementHostedService(
    PlayerActivationService players,
    ILogger<PlayerManagementHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await players.ShutdownAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogPlayerShutdownFailed(logger, exception);
            throw;
        }
    }

    [LoggerMessage(
        EventId = 4320,
        Level = LogLevel.Error,
        Message = "Player manager shutdown failed.")]
    private static partial void LogPlayerShutdownFailed(ILogger logger, Exception exception);
}
