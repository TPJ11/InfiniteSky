using InfiniteSky.World;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InfiniteSky.Server.Health;

/// <summary>
/// Reports actual world runtime readiness, not merely host registration.
/// </summary>
public sealed class WorldHealthCheck(WorldRuntime runtime) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        WorldRuntimeSnapshot snapshot = runtime.Current;
        var data = new Dictionary<string, object>
        {
            ["LifecycleState"] = snapshot.State.ToString(),
            ["Readiness"] = snapshot.Readiness.ToString(),
            ["GuildCount"] = snapshot.GuildCount,
            ["LoadedZoneCount"] = snapshot.LoadedZoneCount,
            ["TickCount"] = snapshot.TickCount,
        };
        if (snapshot.Blockers.Count > 0)
        {
            data["Blockers"] = string.Join(",", snapshot.Blockers);
        }

        HealthCheckResult result = snapshot.Readiness switch
        {
            WorldReadinessStatus.Ready =>
                HealthCheckResult.Healthy("The world runtime is ready.", data),
            WorldReadinessStatus.Degraded =>
                HealthCheckResult.Degraded(
                    "The world runtime is running with unavailable dependencies.",
                    data: data),
            WorldReadinessStatus.Stopping or WorldReadinessStatus.Stopped
                or WorldReadinessStatus.Failed =>
                HealthCheckResult.Unhealthy(
                    "The world runtime is not available.",
                    data: data),
            _ =>
                HealthCheckResult.Degraded(
                    "The world runtime is initializing.",
                    data: data),
        };

        return Task.FromResult(result);
    }
}
