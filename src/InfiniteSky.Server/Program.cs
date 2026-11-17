using InfiniteSky.Server.Hosting;

namespace InfiniteSky.Server;

/// <summary>
/// Process entry point for the InfiniteSky foundation host.
/// </summary>
public static class Program
{
    /// <summary>
    /// Builds and runs the Generic Host.
    /// </summary>
    public static Task<int> Main(string[] args)
    {
        return ServerApplication.RunAsync(args);
    }
}
