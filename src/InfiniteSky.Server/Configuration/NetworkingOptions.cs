namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Reserved configuration for the login and world listeners.
/// </summary>
public sealed class NetworkingOptions
{
    /// <summary>
    /// Gets or sets whether networking should be enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the listener bind address.
    /// </summary>
    public string BindAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the login listener port.
    /// </summary>
    public int LoginPort { get; set; } = 11091;

    /// <summary>
    /// Gets or sets the world listener port.
    /// </summary>
    public int WorldPort { get; set; } = 11092;
}
