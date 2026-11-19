using System.Net;
using System.Net.Sockets;

using InfiniteSky.Protocol.Packets;

using Microsoft.Extensions.Logging;

namespace InfiniteSky.Networking;

/// <summary>
/// Starts/stops one TCP listener and owns its accepted connection tasks.
/// </summary>
public sealed partial class LegacyTcpListener : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly NetworkEndpointKind _kind;
    private readonly IConnectionManager _manager;
    private readonly IIncomingPacketSink _sink;
    private readonly LegacyPacketRegistry _registry;
    private readonly NetworkingMetrics _metrics;
    private readonly ILogger<LegacyTcpListener> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly NetworkingOptions _options;
    private readonly CancellationTokenSource _lifecycle = new();
    private readonly HashSet<NetworkConnection> _connections = [];
    private readonly object _sync = new();
    private Task? _acceptTask;
    private bool _started;
    private bool _disposed;

    public LegacyTcpListener(
        NetworkEndpointKind kind,
        IPAddress address,
        int port,
        IConnectionManager manager,
        IIncomingPacketSink sink,
        LegacyPacketRegistry registry,
        NetworkingMetrics metrics,
        ILogger<LegacyTcpListener> logger,
        ILoggerFactory loggerFactory,
        NetworkingOptions options)
    {
        _kind = kind;
        _listener = new TcpListener(address, port);
        _manager = manager;
        _sink = sink;
        _registry = registry;
        _metrics = metrics;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _options = options;
    }

    public IPEndPoint? LocalEndPoint =>
        _listener.LocalEndpoint as IPEndPoint;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_started)
            {
                throw new InvalidOperationException(
                    $"{_kind} TCP listener is already started.");
            }
            _started = true;
        }

        try
        {
            _listener.Start();
            _acceptTask = AcceptLoopAsync();
            if (_logger.IsEnabled(LogLevel.Information))
            {
                string kind = _kind.ToString();
                string endPoint = _listener.LocalEndpoint.ToString() ?? "unknown";
                LogStarted(
                    _logger,
                    kind,
                    endPoint);
            }
        }
        catch
        {
            _listener.Stop();
            lock (_sync)
            {
                _started = false;
            }
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_started)
            {
                return;
            }
            _started = false;
        }

        await _lifecycle.CancelAsync();
        _listener.Stop();

        NetworkConnection[] connections;
        lock (_sync)
        {
            connections = _connections.ToArray();
        }

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(
            _options.ConnectionShutdownTimeoutSeconds));

        foreach (NetworkConnection connection in connections)
        {
            await connection.CloseAsync(timeout.Token);
        }

        if (_acceptTask is not null)
        {
            await Task.WhenAny(
                _acceptTask,
                Task.Delay(
                    TimeSpan.FromSeconds(_options.ConnectionShutdownTimeoutSeconds),
                    cancellationToken));
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            string kind = _kind.ToString();
            LogStopped(_logger, kind);
        }
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_lifecycle.IsCancellationRequested)
            {
                TcpClient client =
                    await _listener.AcceptTcpClientAsync(_lifecycle.Token);

                var connection = new NetworkConnection(
                    client,
                    _manager,
                    _sink,
                    _registry,
                    _metrics,
                    _loggerFactory.CreateLogger<NetworkConnection>(),
                    _options);
                connection.Closed += OnConnectionClosed;

                if (!_manager.TryAdd(connection))
                {
                    client.Dispose();
                    LogRejected(_logger, connection.Id.ToString());
                    continue;
                }

                lock (_sync)
                {
                    _connections.Add(connection);
                }

                connection.Start();
            }
        }
        catch (OperationCanceledException) when (_lifecycle.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (_lifecycle.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogListenerFailure(_logger, _kind.ToString(), exception);
        }
    }

    private void OnConnectionClosed(NetworkConnection connection)
    {
        lock (_sync)
        {
            _connections.Remove(connection);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        await StopAsync(CancellationToken.None);
        _lifecycle.Dispose();
    }

    [LoggerMessage(
        EventId = 3020,
        Level = LogLevel.Information,
        Message = "Started {EndpointKind} TCP listener on {EndPoint}.")]
    private static partial void LogStarted(
        ILogger logger,
        string endpointKind,
        string endPoint);

    [LoggerMessage(
        EventId = 3021,
        Level = LogLevel.Information,
        Message = "Stopped {EndpointKind} TCP listener.")]
    private static partial void LogStopped(
        ILogger logger,
        string endpointKind);

    [LoggerMessage(
        EventId = 3022,
        Level = LogLevel.Warning,
        Message = "Rejected TCP connection {ConnectionId}: connection limit reached.")]
    private static partial void LogRejected(
        ILogger logger,
        string connectionId);

    [LoggerMessage(
        EventId = 3023,
        Level = LogLevel.Error,
        Message = "TCP listener {EndpointKind} failed.")]
    private static partial void LogListenerFailure(
        ILogger logger,
        string endpointKind,
        Exception exception);
}
