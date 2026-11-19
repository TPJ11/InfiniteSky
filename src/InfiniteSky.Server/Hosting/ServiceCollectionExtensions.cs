using InfiniteSky.Application.Diagnostics;
using InfiniteSky.Application.Health;
using InfiniteSky.Server.Configuration;
using InfiniteSky.Server.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Registers the server foundation using explicit, auditable lifetimes.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application lifecycle, readiness, diagnostics, and host coordination services.
    /// </summary>
    public static IServiceCollection AddInfiniteSkyInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ApplicationLifecycleCoordinator>();
        services.AddSingleton<IApplicationReadiness>(
            provider => provider.GetRequiredService<ApplicationLifecycleCoordinator>());
        services.AddSingleton<InfiniteSkyDiagnostics>(provider =>
        {
            InfiniteSkyOptions options = provider.GetRequiredService<IOptions<InfiniteSkyOptions>>().Value;
            return new InfiniteSkyDiagnostics(options.Diagnostics.ServiceName);
        });
        services.AddSingleton<FoundationHostedService>();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(
            provider => provider.GetRequiredService<FoundationHostedService>());

        services.AddHealthChecks()
            .AddCheck<FoundationLivenessHealthCheck>(
                "foundation-liveness",
                tags: ["liveness"])
            .AddCheck<FoundationReadinessHealthCheck>(
                "foundation-readiness",
                tags: ["readiness"])
            .AddCheck<MongoPersistenceHealthCheck>(
                "mongodb-persistence",
                tags: ["readiness", "database"])
            .AddCheck<WorldHealthCheck>(
                "world-readiness",
                tags: ["readiness", "world"]);

        return services;
    }
}
