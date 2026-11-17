using InfiniteSky.Application.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InfiniteSky.Server.Health;

/// <summary>
/// Reports whether the foundation process is alive and usable.
/// </summary>
public sealed class FoundationLivenessHealthCheck(
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
            ApplicationLifecyclePhase.Failed or ApplicationLifecyclePhase.Stopped =>
                HealthCheckResult.Unhealthy(
                    "The application lifecycle is terminal.",
                    data: CreateData(snapshot)),
            ApplicationLifecyclePhase.Stopping =>
                HealthCheckResult.Degraded(
                    "The application is shutting down.",
                    data: CreateData(snapshot)),
            _ =>
                HealthCheckResult.Healthy(data: CreateData(snapshot)),
        };

        return Task.FromResult(result);
    }

    private static Dictionary<string, object> CreateData(
        ApplicationReadinessSnapshot snapshot) =>
        new Dictionary<string, object>
        {
            ["Phase"] = snapshot.Phase,
            ["Reason"] = snapshot.Reason,
        };
}
