using InfiniteSky.Persistence;
using InfiniteSky.Server.Configuration;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Health;

/// <summary>
/// Reports an explicit disabled, unchecked, connected, or failed MongoDB state.
/// </summary>
public sealed class MongoPersistenceHealthCheck(
    IOptions<InfiniteSkyOptions> options,
    MongoPersistenceHealthState healthState) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        MongoPersistenceHealthSnapshot snapshot = healthState.Current;
        var data = new Dictionary<string, object>
        {
            ["Status"] = snapshot.Status.ToString(),
        };
        if (snapshot.FailureKind is not null)
        {
            data["FailureKind"] = snapshot.FailureKind.Value.ToString();
        }
        if (snapshot.LastSuccessfulCheck is not null)
        {
            data["LastSuccessfulCheck"] = snapshot.LastSuccessfulCheck.Value;
        }

        if (!options.Value.Database.Enabled
            || snapshot.Status == MongoPersistenceHealthStatus.Disabled)
        {
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    "MongoDB persistence is disabled and has not been checked.",
                    data: data));
        }

        if (snapshot.Status == MongoPersistenceHealthStatus.Connected)
        {
            return Task.FromResult(HealthCheckResult.Healthy("MongoDB is operational.", data));
        }

        return Task.FromResult(
            HealthCheckResult.Unhealthy(
                snapshot.Status == MongoPersistenceHealthStatus.NotChecked
                    ? "MongoDB has not been checked."
                    : "MongoDB is not operational.",
                data: data));
    }
}
