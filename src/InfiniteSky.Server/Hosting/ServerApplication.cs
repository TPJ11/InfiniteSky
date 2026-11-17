using Microsoft.Extensions.Hosting;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Runs the composed host and owns the process-level failure boundary.
/// </summary>
public static class ServerApplication
{
    /// <summary>
    /// Runs the host and returns a process exit code.
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        IHost? host = null;

        try
        {
            host = ServerHostBuilder.Create(args).Build();
            await host.RunAsync();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            host?.Dispose();
        }
    }
}
