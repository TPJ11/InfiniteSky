using InfiniteSky.Server.Hosting;

using Microsoft.Extensions.Hosting;

namespace InfiniteSky.Server;

/// <summary>
/// Process entry point for the InfiniteSky foundation host.
/// </summary>
public static class Program
{
    /// <summary>
    /// Builds and runs the Generic Host.
    /// </summary>
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create(args);
        await builder.Build().RunAsync();
    }
}
