using InfiniteSky.Application.Health;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Zones;
using InfiniteSky.GameData.Zones;
using InfiniteSky.Server.Hosting;
using InfiniteSky.World;
using InfiniteSky.World.Zones;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class FoundationHostTests
{
    [Fact]
    public async Task HostStartsWithReadyFoundationAndStopsCleanly()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();

        await host.StartAsync();

        IApplicationReadiness readiness = host.Services.GetRequiredService<IApplicationReadiness>();
        Assert.True(readiness.Current.IsReady);
        Assert.Equal(ApplicationLifecyclePhase.Running, readiness.Current.Phase);
        Assert.Equal("Valid", readiness.Current.Checks["Configuration"]);
        Assert.Equal("Running", readiness.Current.Checks["Hosting"]);
        Assert.Equal("DisabledByFoundation", readiness.Current.Checks["Networking"]);
        Assert.Equal("DisabledByConfiguration", readiness.Current.Checks["Database"]);

        await host.StopAsync();

        Assert.False(readiness.Current.IsReady);
        Assert.Equal(ApplicationLifecyclePhase.Stopped, readiness.Current.Phase);
    }

    [Fact]
    public async Task HostedServiceAndReadinessShareOneLifecycleInstance()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();

        var lifecycle = host.Services.GetRequiredService<ApplicationLifecycleCoordinator>();
        FoundationHostedService hostedService = host.Services.GetRequiredService<FoundationHostedService>();
        IApplicationReadiness readiness = host.Services.GetRequiredService<IApplicationReadiness>();

        Assert.Same(lifecycle, readiness);

        await host.StartAsync();
        Assert.Same(
            hostedService,
            Assert.Single(host.Services.GetServices<IHostedService>().OfType<FoundationHostedService>()));
        await host.StopAsync();
    }

    [Fact]
    public async Task WorldHostedServiceRunsBetweenPersistenceAndNetworking()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();
        Type[] hostedServiceTypes = host.Services.GetServices<IHostedService>()
            .Select(service => service.GetType())
            .ToArray();

        Assert.True(
            Array.IndexOf(hostedServiceTypes, typeof(MongoPersistenceHostedService))
            < Array.IndexOf(hostedServiceTypes, typeof(WorldHostedService)));
        Assert.True(
            Array.IndexOf(hostedServiceTypes, typeof(WorldHostedService))
            < Array.IndexOf(hostedServiceTypes, typeof(NetworkHostedService)));

        await host.StartAsync();
        WorldRuntime runtime = host.Services.GetRequiredService<WorldRuntime>();
        Assert.Equal(WorldLifecycleState.Running, runtime.Current.State);
        Assert.Equal(WorldReadinessStatus.Degraded, runtime.Current.Readiness);
        Assert.Contains(WorldReadinessBlocker.ZoneRuntimeUnavailable, runtime.Current.Blockers);
        Assert.Equal(0, runtime.Current.LoadedZoneCount);

        await host.StopAsync();
        Assert.Equal(WorldLifecycleState.Stopped, runtime.Current.State);
    }

    [Theory]
    [InlineData(true, HealthStatus.Healthy)]
    [InlineData(false, HealthStatus.Degraded)]
    public async Task ConfiguredZonesStartBeforeWorldAndSupplyRealReadiness(
        bool resourcesExist,
        HealthStatus expectedZoneHealth)
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InfiniteSky:Zones:Load:0"] = "1",
        });
        builder.Services.RemoveAll<IZoneResourceLoader>();
        builder.Services.AddSingleton<IZoneResourceLoader>(new EmptyZoneResourceLoader(resourcesExist));
        using IHost host = builder.Build();

        Type[] hostedServiceTypes = host.Services.GetServices<IHostedService>()
            .Select(service => service.GetType())
            .ToArray();
        Assert.True(
            Array.IndexOf(hostedServiceTypes, typeof(ZoneManagementHostedService))
            < Array.IndexOf(hostedServiceTypes, typeof(WorldHostedService)));

        await host.StartAsync();

        IZoneManager manager = host.Services.GetRequiredService<IZoneManager>();
        WorldRuntime runtime = host.Services.GetRequiredService<WorldRuntime>();
        Assert.True(manager.IsReady);
        Assert.Equal(1, manager.LoadedZoneCount);
        Assert.Equal(1, runtime.Current.LoadedZoneCount);
        Assert.DoesNotContain(WorldReadinessBlocker.ZoneRuntimeUnavailable, runtime.Current.Blockers);
        Assert.Contains(WorldReadinessBlocker.GuildDataUnavailable, runtime.Current.Blockers);
        HealthReportEntry zoneHealth = (await host.Services.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(predicate: check => check.Name == "zones-readiness"))
            .Entries["zones-readiness"];
        Assert.Equal(expectedZoneHealth, zoneHealth.Status);
        Assert.Equal(resourcesExist ? 0 : 1, zoneHealth.Data["ZonesMissingMoveRegionFiles"]);

        await host.StopAsync();

        Assert.Equal(ZoneManagerState.Stopped, manager.Current.State);
        Assert.Empty(manager.GetZones());
    }

    [Fact]
    public void HostBuildThrowsForInvalidFoundationConfiguration()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InfiniteSky:World:TickMilliseconds"] = "0",
        });
        Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(
            () => builder.Build());
    }

    [Fact]
    public async Task HealthChecksDistinguishReadinessAndLiveness()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();
        var health = host.Services.GetRequiredService<HealthCheckService>();

        HealthReport initialReadiness = await health.CheckHealthAsync(
            predicate: registration => registration.Tags.Contains("readiness"));
        Assert.Equal(HealthStatus.Degraded, initialReadiness.Status);

        await host.StartAsync();

        HealthReport runningReadiness = await health.CheckHealthAsync(
            predicate: registration => registration.Tags.Contains("readiness"));
        HealthReport runningLiveness = await health.CheckHealthAsync(
            predicate: registration => registration.Tags.Contains("liveness"));
        Assert.Equal(HealthStatus.Degraded, runningReadiness.Status);
        Assert.Equal(HealthStatus.Healthy, runningLiveness.Status);

        await host.StopAsync();

        HealthReport stoppedReadiness = await health.CheckHealthAsync(
            predicate: registration => registration.Tags.Contains("readiness"));
        Assert.Equal(HealthStatus.Unhealthy, stoppedReadiness.Status);
    }

    [Fact]
    public async Task UnreachableMongoDatabaseFailsStartupAndIsNotReportedHealthy()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InfiniteSky:Database:Enabled"] = "true",
            ["InfiniteSky:Database:ConnectionString"] = "mongodb://127.0.0.1:1/ts1",
            ["InfiniteSky:Database:ServerSelectionTimeoutSeconds"] = "1",
            ["InfiniteSky:Database:ConnectTimeoutSeconds"] = "1",
            ["InfiniteSky:Database:HealthCheckTimeoutSeconds"] = "1",
            ["InfiniteSky:Database:OperationTimeoutSeconds"] = "1",
            ["InfiniteSky:Database:ResetActiveAccountsOnStartup"] = "false",
        });
        using IHost host = builder.Build();

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.DoesNotContain("mongodb://", exception.ToString(), StringComparison.Ordinal);
        HealthReportEntry databaseHealth = (await host.Services
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(predicate: check => check.Name == "mongodb-persistence"))
            .Entries["mongodb-persistence"];
        Assert.Equal(HealthStatus.Unhealthy, databaseHealth.Status);
        Assert.Equal("Unavailable", databaseHealth.Data["FailureKind"]);
    }

    [Fact]
    public async Task HostPublishesTheFullLifecycleSequence()
    {
        List<ApplicationLifecyclePhase> observedPhases = [];
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        builder.Services.AddSingleton<IApplicationLifecycleObserver>(
            new RecordingLifecycleObserver(observedPhases));
        using IHost host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        Assert.Equal(
            [
                ApplicationLifecyclePhase.Initialising,
                ApplicationLifecyclePhase.Starting,
                ApplicationLifecyclePhase.Running,
                ApplicationLifecyclePhase.Stopping,
                ApplicationLifecyclePhase.Stopped,
            ],
            observedPhases);
    }

    private sealed class RecordingLifecycleObserver(
        ICollection<ApplicationLifecyclePhase> phases) : IApplicationLifecycleObserver
    {
        public ValueTask OnLifecycleTransitionAsync(
            ApplicationLifecycleTransition transition,
            CancellationToken cancellationToken)
        {
            phases.Add(transition.Current);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class EmptyZoneResourceLoader(bool filesExist) : IZoneResourceLoader
    {
        public Task<ZoneResourceData> LoadAsync(ZoneId zoneId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ZoneResourceData(
                Array.Empty<SpawnDefinition>(),
                Array.Empty<InfiniteSky.Protocol.Structures.LegacyWorldRegion>(),
                Array.Empty<InfiniteSky.Protocol.Structures.LegacyWorldRegion>(),
                filesExist,
                filesExist,
                filesExist));
        }
    }
}
