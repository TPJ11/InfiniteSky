using System.Net;

using InfiniteSky.Application.Health;
using InfiniteSky.Networking;
using InfiniteSky.Protocol.Packets;
using InfiniteSky.Server.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Starts/stops login and world TCP listeners as application-hosted transport services.
/// It creates no player, authentication, or session state.
/// </summary>
public sealed partial class NetworkHostedService : IHostedService, IAsyncDisposable
{
    private readonly InfiniteSky.Networking.NetworkingOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<NetworkHostedService> _logger;
    private readonly ConnectionManager _manager;
    private readonly NetworkingMetrics _metrics;
    private readonly LoggingIncomingPacketSink _sink;
    private readonly InfiniteSkyOptions _rootOptions;
    private readonly ApplicationLifecycleCoordinator _lifecycle;
    private readonly List<LegacyTcpListener> _listeners = [];
    private bool _disposed;

    public NetworkHostedService(
        IOptions<InfiniteSkyOptions> rootOptions,
        ILoggerFactory loggerFactory,
        ILogger<NetworkHostedService> logger,
        ApplicationLifecycleCoordinator lifecycle)
    {
        InfiniteSkyOptions root = rootOptions.Value;
        _rootOptions = root;
        _lifecycle = lifecycle;
        _options = new InfiniteSky.Networking.NetworkingOptions
        {
            BindAddress = root.Networking.BindAddress,
            LoginPort = root.Networking.LoginPort,
            WorldPort = root.Networking.WorldPort,
            MaxConcurrentConnections = root.Networking.MaxConcurrentConnections,
            MaxReceiveBufferBytes = root.Networking.MaxReceiveBufferBytes,
            MaxFrameBytes = root.Networking.MaxFrameBytes,
            SendQueueCapacity = root.Networking.SendQueueCapacity,
            IdleTimeoutSeconds = root.Networking.IdleTimeoutSeconds,
            ConnectionShutdownTimeoutSeconds = root.Networking.ConnectionShutdownTimeoutSeconds,
        };
        _loggerFactory = loggerFactory;
        _logger = logger;
        _manager = new ConnectionManager(_options.MaxConcurrentConnections);
        _metrics = new NetworkingMetrics();
        _sink = new LoggingIncomingPacketSink(
            loggerFactory.CreateLogger<LoggingIncomingPacketSink>());
    }

    /// <summary>
    /// Gets the actual connection manager for tests and future application boundaries.
    /// </summary>
    public IConnectionManager Connections => _manager;

    public NetworkingMetrics Metrics => _metrics;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        InfiniteSkyOptions root = _rootOptions;

        if (!root.Networking.Enabled)
        {
            LogDisabled(_logger);
            return;
        }

        if (!IPAddress.TryParse(root.Networking.BindAddress, out IPAddress? address))
        {
            throw new InvalidOperationException(
                "BindAddress must be a valid IP address.");
        }

        LegacyPacketRegistry loginRegistry =
            LegacyCorePacketRegistry.CreateLogin();
        LegacyPacketRegistry worldRegistry =
            LegacyCorePacketRegistry.CreateWorld();

        var login = new LegacyTcpListener(
            NetworkEndpointKind.Login,
            address,
            root.Networking.LoginPort,
            _manager,
            _sink,
            loginRegistry,
            _metrics,
            _loggerFactory.CreateLogger<LegacyTcpListener>(),
            _loggerFactory,
            _options);

        var world = new LegacyTcpListener(
            NetworkEndpointKind.World,
            address,
            root.Networking.WorldPort,
            _manager,
            _sink,
            worldRegistry,
            _metrics,
            _loggerFactory.CreateLogger<LegacyTcpListener>(),
            _loggerFactory,
            _options);

        _listeners.Add(login);
        _listeners.Add(world);

        try
        {
            await login.StartAsync(cancellationToken);
            await world.StartAsync(cancellationToken);
            _lifecycle.SetCheck("Networking", "Listening");
        }
        catch
        {
            await StopAsync(CancellationToken.None);
            throw;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            string loginEndPoint = login.LocalEndPoint?.ToString() ?? "unknown";
            string worldEndPoint = world.LocalEndPoint?.ToString() ?? "unknown";
            LogStarted(_logger, loginEndPoint, worldEndPoint);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (LegacyTcpListener listener in _listeners)
        {
            await listener.DisposeAsync();
        }
        _listeners.Clear();
        _lifecycle.SetCheck("Networking", "Stopped");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        await StopAsync(CancellationToken.None);
        _metrics.Dispose();
    }

    [LoggerMessage(
        EventId = 3030,
        Level = LogLevel.Information,
        Message = "Networking is disabled; login and world listeners were not started.")]
    private static partial void LogDisabled(ILogger logger);

    [LoggerMessage(
        EventId = 3031,
        Level = LogLevel.Information,
        Message = "Started login listener on {LoginEndPoint} and world listener on {WorldEndPoint}.")]
    private static partial void LogStarted(
        ILogger logger,
        string loginEndPoint,
        string worldEndPoint);
}
