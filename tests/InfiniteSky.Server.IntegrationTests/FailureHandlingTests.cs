using InfiniteSky.Application.Health;
using InfiniteSky.Server.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class FailureHandlingTests
{
    [Fact]
    public async Task ObserverFailureMarksApplicationFailedAndIsRethrown()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        builder.Services.AddSingleton<IApplicationLifecycleObserver, FailingLifecycleObserver>();
        using IHost host = builder.Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        IApplicationReadiness readiness = host.Services.GetRequiredService<IApplicationReadiness>();
        Assert.Equal(ApplicationLifecyclePhase.Failed, readiness.Current.Phase);
        Assert.False(readiness.Current.IsReady);

        var health = host.Services.GetRequiredService<HealthCheckService>();
        HealthReport report = await health.CheckHealthAsync(
            predicate: registration => registration.Tags.Contains("readiness"));
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }

    [Fact]
    public async Task ProcessEntryPointReturnsNonZeroForFatalConfigurationFailure()
    {
        int exitCode = await ServerApplication.RunAsync(
            ["--InfiniteSky:Database:Enabled", "true"]);

        Assert.Equal(1, exitCode);
    }

    private sealed class FailingLifecycleObserver : IApplicationLifecycleObserver
    {
        public ValueTask OnLifecycleTransitionAsync(
            ApplicationLifecycleTransition transition,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Deliberate lifecycle observer failure.");
        }
    }
}
