using InfiniteSky.Application.Authentication;
using InfiniteSky.Application.Diagnostics;
using InfiniteSky.Application.Health;
using InfiniteSky.Persistence;
using InfiniteSky.Server.Hosting;

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
}
