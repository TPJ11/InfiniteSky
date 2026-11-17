using InfiniteSky.Application.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InfiniteSky.Server.Health;

/// <summary>
/// Reports readiness to accept future work without treating absent systems as healthy.
/// </summary>
public sealed class FoundationReadinessHealthCheck(
    IApplicationReadiness readiness) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ApplicationReadinessSnapshot snapshot = readiness.Current;
        HealthCheckResult result = snapshot.Phase switch
        {
            ApplicationLifecyclePhase.Running =>
                HealthCheckResult.Healthy(
                    "The foundation host is ready.",
                    CreateData(snapshot)),
            ApplicationLifecyclePhase.Failed or ApplicationLifecyclePhase.Stopped =>
                HealthCheckResult.Unhealthy(
                    "The foundation host cannot accept work.",
                    data: CreateData(snapshot)),
            _ =>
                HealthCheckResult.Degraded(
                    "The foundation host is transitioning.",
                    data: CreateData(snapshot)),
        };

        return Task.FromResult(result);
    }

    private static Dictionary<string, object> CreateData(
        ApplicationReadinessSnapshot snapshot)
    {
        var data = new Dictionary<string, object>
        {
            ["Phase"] = snapshot.Phase,
            ["Reason"] = snapshot.Reason,
        };

        foreach ((string key, string value) in snapshot.Checks)
        {
            data[key] = value;
        }

        return data;
    }
}
