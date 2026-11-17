using InfiniteSky.Server.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Composes the Generic Host without placing service registration in the program entry point.
/// </summary>
public static class ServerHostBuilder
{
    /// <summary>
    /// Creates a configured host builder for the InfiniteSky foundation.
    /// </summary>
    public static HostApplicationBuilder Create(string[]? args = null)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args ?? []);

        builder.Configuration.AddEnvironmentVariables(prefix: "INFINITESKY_");

        builder.Services.AddOptions<InfiniteSkyOptions>()
            .BindConfiguration(InfiniteSkyOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddOptions<HostOptions>()
            .Configure<IOptions<InfiniteSkyOptions>>((hostOptions, options) =>
            {
                hostOptions.ShutdownTimeout =
                    TimeSpan.FromSeconds(options.Value.Application.ShutdownTimeoutSeconds);
                hostOptions.ServicesStartConcurrently = options.Value.Hosting.StartServicesConcurrently;
                hostOptions.ServicesStopConcurrently = options.Value.Hosting.StopServicesConcurrently;
                hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
            });

        builder.Services.AddSingleton<IValidateOptions<InfiniteSkyOptions>, InfiniteSkyOptionsValidator>();
        builder.Services.AddInfiniteSkyInfrastructure();
        builder.Services.AddSingleton<ApplicationReadinessHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            provider => provider.GetRequiredService<ApplicationReadinessHostedService>());
        builder.ConfigureContainer(
            new DefaultServiceProviderFactory(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            }));

        return builder;
    }
}
