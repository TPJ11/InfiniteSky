namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Controls Generic Host startup and shutdown coordination.
/// </summary>
public sealed class HostingOptions
{
    /// <summary>
    /// Gets or sets whether hosted services start concurrently.
    /// </summary>
    public bool StartServicesConcurrently { get; set; }

    /// <summary>
    /// Gets or sets whether hosted services stop concurrently.
    /// </summary>
    public bool StopServicesConcurrently { get; set; }
}
