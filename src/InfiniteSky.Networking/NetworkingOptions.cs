namespace InfiniteSky.Networking;

/// <summary>
/// Framework-neutral TCP transport limits and endpoint settings.
/// </summary>
public sealed record NetworkingOptions
{
    public string BindAddress { get; init; } = "127.0.0.1";
    public int LoginPort { get; init; } = 11091;
    public int WorldPort { get; init; } = 11092;
    public int MaxConcurrentConnections { get; init; } = 100;
    public int MaxReceiveBufferBytes { get; init; } = 10_240;
    public int MaxFrameBytes { get; init; } = 10_240;
    public int SendQueueCapacity { get; init; } = 64;
    public int IdleTimeoutSeconds { get; init; } = 120;
    public int ConnectionShutdownTimeoutSeconds { get; init; } = 30;
}
