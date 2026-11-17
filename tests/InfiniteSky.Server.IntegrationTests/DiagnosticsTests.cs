using System.Diagnostics;

using InfiniteSky.Application.Diagnostics;
using InfiniteSky.Server.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class DiagnosticsTests
{
    [Fact]
    public async Task LifecycleStartEmitsAnActivity()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        using IHost host = builder.Build();
        InfiniteSkyDiagnostics diagnostics =
            host.Services.GetRequiredService<InfiniteSkyDiagnostics>();
        List<string> activityNames = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == diagnostics.Activities.Name,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => activityNames.Add(activity.OperationName),
        };
        ActivitySource.AddActivityListener(listener);

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains("application.lifecycle.start", activityNames);
        Assert.Equal("InfiniteSky.Activities", diagnostics.Activities.Name);
        Assert.Equal("InfiniteSky.Metrics", diagnostics.Metrics.Name);
    }

    [Fact]
    public void DiagnosticsDisposalStopsActivityCreation()
    {
        var diagnostics = new InfiniteSkyDiagnostics("InfiniteSky.DisposalTest");
        diagnostics.Dispose();

        Assert.Null(diagnostics.Activities.StartActivity("after-dispose"));
    }
}
