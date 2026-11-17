using InfiniteSky.Application.Health;
using InfiniteSky.Server.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        Assert.Equal(ApplicationLifecyclePhase.Ready, readiness.Current.Phase);
        Assert.Equal("Valid", readiness.Current.Checks["Configuration"]);
        Assert.Equal("Running", readiness.Current.Checks["Hosting"]);
        Assert.Equal("DisabledByFoundation", readiness.Current.Checks["Networking"]);
        Assert.Equal("DisabledByFoundation", readiness.Current.Checks["Database"]);

        await host.StopAsync();

        Assert.False(readiness.Current.IsReady);
        Assert.Equal(ApplicationLifecyclePhase.Stopped, readiness.Current.Phase);
    }

    [Fact]
    public async Task HostedServiceAndReadinessShareOneLifecycleInstance()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();

        FoundationHostedService hostedService = host.Services.GetRequiredService<FoundationHostedService>();
        IApplicationReadiness readiness = host.Services.GetRequiredService<IApplicationReadiness>();

        Assert.Same(hostedService, readiness);

        await host.StartAsync();
        Assert.Same(hostedService, Assert.Single(host.Services.GetServices<IHostedService>()));
        await host.StopAsync();
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
}
