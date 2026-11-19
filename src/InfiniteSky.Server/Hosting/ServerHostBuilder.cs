using InfiniteSky.Application.Authentication;
using InfiniteSky.Application.Characters;
using InfiniteSky.Application.Guilds;
using InfiniteSky.Application.Players;
using InfiniteSky.GameData.Zones;
using InfiniteSky.Networking;
using InfiniteSky.Persistence;
using InfiniteSky.Server.Authentication;
using InfiniteSky.Server.Configuration;
using InfiniteSky.World;
using InfiniteSky.World.Players;
using InfiniteSky.World.Zones;

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
        builder.Services.AddSingleton<MongoPersistenceSettings>(provider =>
        {
            DatabaseOptions database =
                provider.GetRequiredService<IOptions<InfiniteSkyOptions>>().Value.Database;
            return new MongoPersistenceSettings
            {
                Enabled = database.Enabled,
                ConnectionString = database.ConnectionString,
                DatabaseName = database.DatabaseName,
                AccountCollectionName = database.AccountCollectionName,
                CharacterCollectionName = database.CharacterCollectionName,
                GuildCollectionName = database.GuildCollectionName,
                EnsureIndexesOnStartup = database.EnsureIndexesOnStartup,
                ResetActiveAccountsOnStartup = database.ResetActiveAccountsOnStartup,
                ServerSelectionTimeoutSeconds = database.ServerSelectionTimeoutSeconds,
                ConnectTimeoutSeconds = database.ConnectTimeoutSeconds,
                OperationTimeoutSeconds = database.OperationTimeoutSeconds,
                HealthCheckIntervalSeconds = database.HealthCheckIntervalSeconds,
                HealthCheckTimeoutSeconds = database.HealthCheckTimeoutSeconds,
                MinConnectionPoolSize = database.MinConnectionPoolSize,
                MaxConnectionPoolSize = database.MaxConnectionPoolSize,
            };
        });
        builder.Services.AddSingleton<MongoPersistenceMetrics>();
        builder.Services.AddSingleton<MongoPersistenceHealthState>();
        builder.Services.AddSingleton<MongoPersistenceContext>();
        builder.Services.AddSingleton<MongoAccountAuthenticationRepository>();
        builder.Services.AddSingleton<IAccountAuthenticationRepository>(
            provider => provider.GetRequiredService<MongoAccountAuthenticationRepository>());
        builder.Services.AddSingleton<MongoGuildRepository>();
        builder.Services.AddSingleton<IGuildRepository>(
            provider => provider.GetRequiredService<MongoGuildRepository>());
        builder.Services.AddSingleton<MongoCharacterRepository>();
        builder.Services.AddSingleton<ICharacterRepository>(
            provider => provider.GetRequiredService<MongoCharacterRepository>());
        builder.Services.AddSingleton<MongoPersistenceHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            provider => provider.GetRequiredService<MongoPersistenceHostedService>());
        builder.Services.AddSingleton<ZoneMetrics>();
        builder.Services.AddSingleton<IZoneRegistry, ZoneRegistry>();
        builder.Services.AddSingleton<IZoneResourceLoader>(provider =>
        {
            IHostEnvironment environment = provider.GetRequiredService<IHostEnvironment>();
            return new LegacyZoneResourceLoader(Path.Combine(environment.ContentRootPath, "data"));
        });
        builder.Services.AddSingleton<ZoneManager>();
        builder.Services.AddSingleton<IZoneManager>(
            provider => provider.GetRequiredService<ZoneManager>());
        builder.Services.AddSingleton<IWorldZoneReadiness>(
            provider => provider.GetRequiredService<ZoneManager>());
        builder.Services.AddSingleton<ZoneManagementHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            provider => provider.GetRequiredService<ZoneManagementHostedService>());
        builder.Services.AddSingleton<WorldRuntime>();
        builder.Services.AddSingleton<WorldMetrics>();
        builder.Services.AddSingleton<WorldClock>();
        builder.Services.AddSingleton<WorldHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            provider => provider.GetRequiredService<WorldHostedService>());
        builder.Services.AddSingleton<PlayerMetrics>();
        builder.Services.AddSingleton<IPlayerManager, PlayerManager>();
        builder.Services.AddSingleton<PlayerActivationService>(provider =>
        {
            InfiniteSkyOptions options =
                provider.GetRequiredService<IOptions<InfiniteSkyOptions>>().Value;
            return new PlayerActivationService(
                provider.GetRequiredService<IAuthenticationSessionStore>(),
                provider.GetRequiredService<ICharacterRepository>(),
                provider.GetRequiredService<IPlayerManager>(),
                provider.GetRequiredService<IZoneManager>(),
                provider.GetRequiredService<PlayerMetrics>(),
                options.Characters.ServerName);
        });
        builder.Services.AddSingleton<PlayerManagementHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            provider => provider.GetRequiredService<PlayerManagementHostedService>());
        builder.Services.AddSingleton<AuthenticationMetrics>();
        builder.Services.AddSingleton<ICredentialVerifier, LegacyPlaintextCredentialVerifier>();
        builder.Services.AddSingleton<IAuthenticationSessionStore, InMemoryAuthenticationSessionStore>();
        builder.Services.AddSingleton<IAuthenticationService>(provider =>
        {
            InfiniteSkyOptions options =
                provider.GetRequiredService<IOptions<InfiniteSkyOptions>>().Value;
            return new AuthenticationService(
                provider.GetRequiredService<IAccountAuthenticationRepository>(),
                provider.GetRequiredService<ICredentialVerifier>(),
                provider.GetRequiredService<IAuthenticationSessionStore>(),
                provider.GetRequiredService<AuthenticationMetrics>(),
                provider.GetRequiredService<TimeProvider>(),
                TimeSpan.FromSeconds(options.Authentication.TimeoutSeconds));
        });
        builder.Services.AddSingleton<IIncomingPacketSink, AuthenticationIncomingPacketSink>();
        builder.Services.AddSingleton<NetworkHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            provider => provider.GetRequiredService<NetworkHostedService>());
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
