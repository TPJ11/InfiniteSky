using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

using InfiniteSky.Protocol.Packets;

using Microsoft.Extensions.Logging;

namespace InfiniteSky.Networking;

/// <summary>
/// Owns one TCP client, its receive/send tasks, bounded send queue, and explicit transport state.
/// </summary>
public sealed partial class NetworkConnection : IAsyncDisposable
{
    private readonly Channel<QueuedFrame> _sendQueue;
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly IConnectionManager _manager;
    private readonly IIncomingPacketSink _sink;
    private readonly LegacyPacketRegistry _registry;
    private readonly LegacyReceiveAccumulator _receiver;
    private readonly NetworkingMetrics _metrics;
    private readonly ILogger<NetworkConnection> _logger;
    private readonly CancellationTokenSource _lifecycle =
        new();
    private readonly object _stateSync = new();
    private readonly TimeSpan _idleTimeout;
    private Task? _receiveTask;
    private Task? _sendTask;
    private NetworkConnectionState _state = NetworkConnectionState.Accepted;
    private bool _removed;

    public event Action<NetworkConnection>? Closed;

    public NetworkConnection(
        TcpClient client,
        NetworkEndpointKind endpointKind,
        IConnectionManager manager,
        IIncomingPacketSink sink,
        LegacyPacketRegistry registry,
        NetworkingMetrics metrics,
        ILogger<NetworkConnection> logger,
        NetworkingOptions options)
    {
        _client = client;
        EndpointKind = endpointKind;
        _stream = client.GetStream();
        _manager = manager;
        _sink = sink;
        _registry = registry;
        _metrics = metrics;
        _logger = logger;
        _idleTimeout = TimeSpan.FromSeconds(options.IdleTimeoutSeconds);
        _receiver = new LegacyReceiveAccumulator(
            options.MaxReceiveBufferBytes,
            options.MaxFrameBytes,
            registry);
        _sendQueue = Channel.CreateBounded<QueuedFrame>(
            new BoundedChannelOptions(options.SendQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
            });
        Id = NetworkConnectionId.New();
        RemoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
    }

    public NetworkConnectionId Id { get; }
    public NetworkEndpointKind EndpointKind { get; }
    public IPEndPoint? RemoteEndPoint { get; }

    public NetworkConnectionState State
    {
        get
        {
            lock (_stateSync)
            {
                return _state;
            }
        }
    }

    public void Start()
    {
        lock (_stateSync)
        {
            if (_state != NetworkConnectionState.Accepted)
            {
                throw new InvalidOperationException(
                    $"Cannot start connection in {_state} state.");
            }
            _state = NetworkConnectionState.Initialising;
        }

        _receiveTask = ReceiveLoopAsync();
        _sendTask = SendLoopAsync();

        lock (_stateSync)
        {
            _state = NetworkConnectionState.Connected;
        }

        _metrics.Accepted();
        if (_logger.IsEnabled(LogLevel.Information))
        {
            string connectionId = Id.ToString();
            string remoteEndpoint = RemoteEndPoint?.ToString() ?? "unknown";
            LogConnected(_logger, connectionId, remoteEndpoint);
        }
    }

    public async ValueTask<bool> SendAsync(
        ReadOnlyMemory<byte> frame,
        CancellationToken cancellationToken)
    {
        if (frame.Length == 0)
        {
            return false;
        }

        if (State is NetworkConnectionState.Closing
            or NetworkConnectionState.Closed
            or NetworkConnectionState.Faulted)
        {
            return false;
        }

        var queued = new QueuedFrame(
            frame.ToArray(),
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        bool enqueued = false;
        try
        {
            await _sendQueue.Writer.WriteAsync(queued, cancellationToken);
            enqueued = true;
            return await queued.Completion!.Task.WaitAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            if (enqueued)
            {
                await CloseAsync(CancellationToken.None);
            }
            return false;
        }
    }

    public bool TrySend(ReadOnlyMemory<byte> frame)
    {
        if (frame.Length == 0)
        {
            return false;
        }

        if (State is NetworkConnectionState.Closing
            or NetworkConnectionState.Closed
            or NetworkConnectionState.Faulted)
        {
            return false;
        }

        return _sendQueue.Writer.TryWrite(new QueuedFrame(frame.ToArray(), null));
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        await CloseCoreAsync(NetworkConnectionState.Closed, cancellationToken);
    }

    private async Task CloseCoreAsync(
        NetworkConnectionState finalState,
        CancellationToken cancellationToken)
    {
        lock (_stateSync)
        {
            if (_state is NetworkConnectionState.Closing
                or NetworkConnectionState.Closed
                or NetworkConnectionState.Faulted)
            {
                return;
            }
            _state = NetworkConnectionState.Closing;
        }

        _sendQueue.Writer.TryComplete();
        await _lifecycle.CancelAsync();

        try
        {
            await _stream.DisposeAsync();
        }
        catch (ObjectDisposedException)
        {
        }
        _client.Dispose();

        lock (_stateSync)
        {
            _state = finalState;
        }

        RemoveOnce();
        Closed?.Invoke(this);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            string connectionId = Id.ToString();
            string state = finalState.ToString();
            LogClosed(_logger, connectionId, state);
        }
    }

    private void RemoveOnce()
    {
        if (_removed)
        {
            return;
        }
        _removed = true;
        if (_manager.TryRemove(Id))
        {
            _metrics.Disconnected();
        }
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[4096];

        try
        {
            while (!_lifecycle.IsCancellationRequested)
            {
                using var idle = _idleTimeout > TimeSpan.Zero
                    ? new CancellationTokenSource(_idleTimeout)
                    : null;
                using var readCancellation =
                    idle is null
                        ? CancellationTokenSource.CreateLinkedTokenSource(_lifecycle.Token)
                        : CancellationTokenSource.CreateLinkedTokenSource(
                            _lifecycle.Token,
                            idle.Token);

                int read;
                try
                {
                    read = await _stream.ReadAsync(
                        buffer.AsMemory(),
                        readCancellation.Token);
                }
                catch (OperationCanceledException) when (
                    idle is not null
                    && idle.IsCancellationRequested
                    && !_lifecycle.IsCancellationRequested)
                {
                    LogIdleTimeout(_logger, Id.ToString());
                    await CloseCoreAsync(
                        NetworkConnectionState.Faulted,
                        CancellationToken.None);
                    return;
                }

                if (read == 0)
                {
                    await CloseCoreAsync(
                        NetworkConnectionState.Closed,
                        CancellationToken.None);
                    return;
                }

                _metrics.BytesReceived(read);
                _receiver.Append(buffer.AsSpan(0, read));

                await ProcessReceiveBufferAsync();
            }
        }
        catch (OperationCanceledException) when (_lifecycle.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _metrics.ReceiveFailure();
            LogReceiveFailure(_logger, Id.ToString(), exception);
            await CloseCoreAsync(
                NetworkConnectionState.Faulted,
                CancellationToken.None);
        }
    }

    private async Task ProcessReceiveBufferAsync()
    {
        while (true)
        {
            FrameReadResult result =
                _receiver.TryReadFrame(out LegacyProtocolFrame? frame, out byte unknownOpcode);

            if (result == FrameReadResult.Incomplete)
            {
                return;
            }

            if (result == FrameReadResult.UnknownOpcode)
            {
                await _sink.OnUnknownOpcodeAsync(
                    this,
                    unknownOpcode,
                    _lifecycle.Token);
                continue;
            }

            if (frame is null)
            {
                return;
            }

            _metrics.FrameReceived();
            await _sink.OnFrameAsync(
                this,
                frame,
                _lifecycle.Token);
        }
    }

    private async Task SendLoopAsync()
    {
        try
        {
            await foreach (QueuedFrame queued in _sendQueue.Reader.ReadAllAsync(_lifecycle.Token))
            {
                try
                {
                    await _stream.WriteAsync(queued.Bytes, _lifecycle.Token);
                    await _stream.FlushAsync(_lifecycle.Token);
                    _metrics.BytesSent(queued.Bytes.Length);
                    _metrics.FrameSent();
                    queued.Completion?.TrySetResult(true);
                }
                catch
                {
                    queued.Completion?.TrySetResult(false);
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (_lifecycle.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _metrics.SendFailure();
            LogSendFailure(_logger, Id.ToString(), exception);
            await CloseCoreAsync(
                NetworkConnectionState.Faulted,
                CancellationToken.None);
        }
        finally
        {
            while (_sendQueue.Reader.TryRead(out QueuedFrame? queued))
            {
                queued.Completion?.TrySetResult(false);
            }
        }
    }

    private sealed record QueuedFrame(
        ReadOnlyMemory<byte> Bytes,
        TaskCompletionSource<bool>? Completion);

    public async ValueTask DisposeAsync()
    {
        await CloseAsync(CancellationToken.None);
        _lifecycle.Dispose();

        if (_receiveTask is not null)
        {
            await Task.WhenAny(_receiveTask, Task.Delay(TimeSpan.FromSeconds(5)));
        }
        if (_sendTask is not null)
        {
            await Task.WhenAny(_sendTask, Task.Delay(TimeSpan.FromSeconds(5)));
        }
    }

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "Accepted TCP connection {ConnectionId} from {RemoteEndpoint}.")]
    private static partial void LogConnected(
        ILogger logger,
        string connectionId,
        string remoteEndpoint);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Closed TCP connection {ConnectionId} with state {State}.")]
    private static partial void LogClosed(
        ILogger logger,
        string connectionId,
        string state);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Error,
        Message = "Receive failure on TCP connection {ConnectionId}.")]
    private static partial void LogReceiveFailure(
        ILogger logger,
        string connectionId,
        Exception exception);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Error,
        Message = "Send failure on TCP connection {ConnectionId}.")]
    private static partial void LogSendFailure(
        ILogger logger,
        string connectionId,
        Exception exception);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Warning,
        Message = "TCP connection {ConnectionId} reached the idle timeout.")]
    private static partial void LogIdleTimeout(
        ILogger logger,
        string connectionId);
}
