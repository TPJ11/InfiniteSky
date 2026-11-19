using InfiniteSky.World.Zones;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InfiniteSky.Server.Health;

/// <summary>
/// Reports whether configured zones are initialized and registered.
/// </summary>
public sealed class ZoneHealthCheck(IZoneManager manager) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = cancellationToken;
        ZoneManagerSnapshot snapshot = manager.Current;
        ZoneRuntimeSnapshot[] zones = manager.GetZones()
            .Select(static zone => zone.Current)
            .ToArray();
        int missingNpcResources = zones.Count(static zone => !zone.HasNpcSpawnFile);
        int missingMoveRegionResources = zones.Count(static zone => !zone.HasMoveRegionFile);
        int missingSafeRegionResources = zones.Count(static zone => !zone.HasSafeRegionFile);
        Dictionary<string, object> data = new()
        {
            ["LifecycleState"] = snapshot.State.ToString(),
            ["ConfiguredZoneCount"] = snapshot.ConfiguredZoneCount,
            ["LoadedZoneCount"] = snapshot.LoadedZoneCount,
            ["FailedZoneCount"] = snapshot.FailedZoneCount,
            ["Ready"] = snapshot.IsReady,
            ["ZonesMissingNpcSpawnFiles"] = missingNpcResources,
            ["ZonesMissingMoveRegionFiles"] = missingMoveRegionResources,
            ["ZonesMissingSafeRegionFiles"] = missingSafeRegionResources,
        };

        HealthCheckResult result = snapshot.State switch
        {
            ZoneManagerState.Failed => HealthCheckResult.Unhealthy(
                "Zone initialization or shutdown failed.",
                data: data),
            ZoneManagerState.Stopped => HealthCheckResult.Unhealthy(
                "Zone manager is stopped.",
                data: data),
            _ when snapshot.IsReady
                && (missingNpcResources > 0
                    || missingMoveRegionResources > 0
                    || missingSafeRegionResources > 0) => HealthCheckResult.Degraded(
                "Zones are running, but optional legacy resources are missing.",
                data: data),
            _ when snapshot.IsReady => HealthCheckResult.Healthy(
                "Configured zones are running.",
                data),
            _ => HealthCheckResult.Degraded(
                snapshot.ConfiguredZoneCount == 0
                    ? "No zones are configured."
                    : "Configured zones are not all ready.",
                data: data),
        };

        return Task.FromResult(result);
    }
}
