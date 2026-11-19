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

    /// <summary>
    /// Maximum simultaneously registered transport connections.
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 100;

    /// <summary>
    /// Maximum receive bytes retained for an incomplete packet stream.
    /// </summary>
    public int MaxReceiveBufferBytes { get; set; } = 10_240;

    /// <summary>
    /// Maximum accepted packet/frame size in receive framing.
    /// </summary>
    public int MaxFrameBytes { get; set; } = 10_240;

    /// <summary>
    /// Maximum queued outbound frames per connection.
    /// </summary>
    public int SendQueueCapacity { get; set; } = 64;

    /// <summary>
    /// Idle read timeout in seconds; 0 disables transport idle timeout.
    /// </summary>
    public int IdleTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum time used to close active connections during shutdown.
    /// </summary>
    public int ConnectionShutdownTimeoutSeconds { get; set; } = 30;
}
