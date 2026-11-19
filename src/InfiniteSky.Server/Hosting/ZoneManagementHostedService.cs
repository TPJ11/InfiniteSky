using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Zones;
using InfiniteSky.GameData.Zones;
using InfiniteSky.Server.Configuration;
using InfiniteSky.World.Zones;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Adapts the zone manager to Generic Host startup and shutdown ordering.
/// </summary>
public sealed partial class ZoneManagementHostedService(
    ZoneManager manager,
    IOptions<InfiniteSkyOptions> options,
    ILogger<ZoneManagementHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ZonesOptions configured = options.Value.Zones;
        try
        {
            ZoneDescriptor[] zones = configured.Load
                .Select(id => LegacyZoneCatalog.Get(new ZoneId(id)))
                .ToArray();
            var managerOptions = new ZoneManagerOptions
            {
                LoadConcurrency = configured.AsyncLoadLimit,
                QuadTreeDepth = configured.QuadTreeDepth,
                QuadTreeNodeLimit = configured.QuadTreeLimit,
                ViewableActionDistance = configured.ViewableActionDistance,
            };

            await manager.StartAsync(zones, managerOptions, cancellationToken).ConfigureAwait(false);
            ZoneManagerSnapshot snapshot = manager.Current;
            if (snapshot.ConfiguredZoneCount == 0)
            {
                LogNoZonesConfigured(logger);
            }
            else
            {
                LogZonesInitialized(logger, snapshot.LoadedZoneCount, snapshot.ConfiguredZoneCount);
            }
        }
        catch (Exception exception)
        {
            LogZoneStartupFailed(logger, exception);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => manager.StopAsync(cancellationToken);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No zones are configured; world readiness will remain degraded.")]
    private static partial void LogNoZonesConfigured(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Initialized {LoadedZoneCount} of {ConfiguredZoneCount} configured zones.")]
    private static partial void LogZonesInitialized(
        ILogger logger,
        int loadedZoneCount,
        int configuredZoneCount);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Zone manager startup failed.")]
    private static partial void LogZoneStartupFailed(ILogger logger, Exception exception);
}
