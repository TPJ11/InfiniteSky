using InfiniteSky.Application.Authentication;
using InfiniteSky.Application.Characters;
using InfiniteSky.Application.Diagnostics;
using InfiniteSky.Application.Health;
using InfiniteSky.Application.Players;
using InfiniteSky.Persistence;
using InfiniteSky.Server.Hosting;
using InfiniteSky.World.Players;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void ApplicationBoundaryServicesAreSingletons()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();

        using IServiceScope firstScope = host.Services.CreateScope();
        using IServiceScope secondScope = host.Services.CreateScope();

        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<IApplicationReadiness>(),
            secondScope.ServiceProvider.GetRequiredService<IApplicationReadiness>());
        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<InfiniteSkyDiagnostics>(),
            secondScope.ServiceProvider.GetRequiredService<InfiniteSkyDiagnostics>());
    }

    [Fact]
    public void BuildValidatesRegisteredServices()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();

        Assert.IsType<FoundationHostedService>(
            Assert.Single(host.Services.GetServices<IHostedService>().OfType<FoundationHostedService>()));
    }

    [Fact]
    public void ProductionAccountRepositoryIsMongoBackedAndSingleton()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();

        IAccountAuthenticationRepository first =
            host.Services.GetRequiredService<IAccountAuthenticationRepository>();
        IAccountAuthenticationRepository second =
            host.Services.GetRequiredService<IAccountAuthenticationRepository>();

        Assert.IsType<MongoAccountAuthenticationRepository>(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void ProductionCharacterAndPlayerServicesAreRegisteredAsSingletons()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();

        ICharacterRepository firstRepository =
            host.Services.GetRequiredService<ICharacterRepository>();
        ICharacterRepository secondRepository =
            host.Services.GetRequiredService<ICharacterRepository>();
        IPlayerManager firstManager = host.Services.GetRequiredService<IPlayerManager>();
        IPlayerManager secondManager = host.Services.GetRequiredService<IPlayerManager>();

        Assert.IsType<MongoCharacterRepository>(firstRepository);
        Assert.Same(firstRepository, secondRepository);
        Assert.IsType<PlayerManager>(firstManager);
        Assert.Same(firstManager, secondManager);
        Assert.NotNull(host.Services.GetRequiredService<PlayerActivationService>());
    }

    [Fact]
    public void PlayerCleanupRunsAfterNetworkingAndBeforeZoneShutdown()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();
        Type[] hostedServiceTypes = host.Services.GetServices<IHostedService>()
            .Select(service => service.GetType())
            .ToArray();

        Assert.True(
            Array.IndexOf(hostedServiceTypes, typeof(ZoneManagementHostedService))
            < Array.IndexOf(hostedServiceTypes, typeof(WorldHostedService)));
        Assert.True(
            Array.IndexOf(hostedServiceTypes, typeof(WorldHostedService))
            < Array.IndexOf(hostedServiceTypes, typeof(PlayerManagementHostedService)));
        Assert.True(
            Array.IndexOf(hostedServiceTypes, typeof(PlayerManagementHostedService))
            < Array.IndexOf(hostedServiceTypes, typeof(NetworkHostedService)));
    }
}
