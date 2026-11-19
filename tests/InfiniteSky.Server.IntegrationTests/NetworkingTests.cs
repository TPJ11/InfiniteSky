using System.Net;
using System.Net.Sockets;

using InfiniteSky.Networking;
using InfiniteSky.Protocol.Packets;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class NetworkingTests
{
    [Fact]
    public void ReceiveAccumulatorHandlesPartialAndCoalescedFrames()
    {
        LegacyPacketRegistry registry = LegacyCorePacketRegistry.CreateLogin();
        var accumulator = new LegacyReceiveAccumulator(10240, 10240, registry);
        byte[] frame = CreateFrame(0x03, 91);

        accumulator.Append(frame.AsSpan(0, 17));
        Assert.Equal(FrameReadResult.Incomplete,
            accumulator.TryReadFrame(out _, out _));

        accumulator.Append(frame.AsSpan(17));
        Assert.Equal(FrameReadResult.Complete,
            accumulator.TryReadFrame(out LegacyProtocolFrame? complete, out _));
        Assert.NotNull(complete);
        Assert.Equal((byte)0x03, complete.Opcode);
        Assert.Equal(91, complete.Payload.Length);
    }

    [Fact]
    public void ReceiveAccumulatorHandlesMultipleFrames()
    {
        LegacyPacketRegistry registry = LegacyCorePacketRegistry.CreateLogin();
        var accumulator = new LegacyReceiveAccumulator(10240, 10240, registry);
        byte[] first = CreateFrame(0x04, 8);
        byte[] second = CreateFrame(0x04, 8);
        accumulator.Append(first);
        accumulator.Append(second);

        Assert.Equal(FrameReadResult.Complete,
            accumulator.TryReadFrame(out LegacyProtocolFrame? one, out _));
        Assert.Equal(FrameReadResult.Complete,
            accumulator.TryReadFrame(out LegacyProtocolFrame? two, out _));
        Assert.NotNull(one);
        Assert.NotNull(two);
        Assert.Equal(0, accumulator.Count);
    }

    [Fact]
    public void ReceiveAccumulatorDiscardsUnknownOpcodeHeader()
    {
        LegacyPacketRegistry registry = LegacyCorePacketRegistry.CreateLogin();
        var accumulator = new LegacyReceiveAccumulator(10240, 10240, registry);
        byte[] frame = CreateHeader(0xFE);

        accumulator.Append(frame);
        Assert.Equal(FrameReadResult.UnknownOpcode,
            accumulator.TryReadFrame(out _, out byte opcode));
        Assert.Equal((byte)0xFE, opcode);
        Assert.Equal(0, accumulator.Count);
    }

    [Fact]
    public void ReceiveAccumulatorRejectsBufferOverflow()
    {
        var accumulator = new LegacyReceiveAccumulator(
            16,
            16,
            LegacyCorePacketRegistry.CreateLogin());

        Assert.Throws<InvalidOperationException>(
            () => accumulator.Append(new byte[17]));
    }

    [Fact]
    public void ReceiveAccumulatorRejectsFrameExceedingConfiguredLimit()
    {
        var accumulator = new LegacyReceiveAccumulator(
            10240,
            99,
            LegacyCorePacketRegistry.CreateLogin());

        accumulator.Append(CreateHeader(0x03));

        Assert.Throws<InvalidDataException>(
            () => accumulator.TryReadFrame(out _, out _));
    }

    [Fact]
    public async Task TcpListenerAcceptsCompleteFrameAndSendsResponse()
    {
        LegacyPacketRegistry registry = LegacyCorePacketRegistry.CreateLogin();
        var manager = new ConnectionManager(10);
        var metrics = new NetworkingMetrics();
        var sink = new RecordingIncomingSink();
        var loggerFactory = NullLoggerFactory.Instance;
        var options = new NetworkingOptions
        {
            MaxReceiveBufferBytes = 10240,
            SendQueueCapacity = 16,
            IdleTimeoutSeconds = 0,
            ConnectionShutdownTimeoutSeconds = 5,
        };

        var listener = new LegacyTcpListener(
            NetworkEndpointKind.Login,
            IPAddress.Loopback,
            0,
            manager,
            sink,
            registry,
            metrics,
            loggerFactory.CreateLogger<LegacyTcpListener>(),
            loggerFactory,
            options);

        await listener.StartAsync(CancellationToken.None);
        IPEndPoint endpoint = Assert.IsType<IPEndPoint>(listener.LocalEndPoint);

        using var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port);

        await WaitUntilAsync(
            () => manager.Count == 1,
            TimeSpan.FromSeconds(2));

        byte[] incoming = CreateFrame(0x03, 91);
        await client.GetStream().WriteAsync(incoming);

        LegacyProtocolFrame frame =
            await sink.Frame.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal((byte)0x03, frame.Opcode);
        Assert.Equal(91, frame.Payload.Length);

        NetworkConnection connection =
            Assert.Single(manager.Connections);
        byte[] response = [0x03, 0x02, .. new byte[39]];
        Assert.True(await connection.SendAsync(response, CancellationToken.None));

        byte[] receive = new byte[response.Length];
        await client.GetStream().ReadExactlyAsync(receive);
        Assert.Equal(response, receive);

        byte[][] concurrentFrames = Enumerable.Range(1, 8)
            .Select(value => Enumerable.Repeat((byte)value, 128).ToArray())
            .ToArray();
        bool[] queued = await Task.WhenAll(
            concurrentFrames.Select(frame =>
                connection.SendAsync(frame, CancellationToken.None).AsTask()));
        Assert.All(queued, Assert.True);

        byte[] concurrentReceive = new byte[concurrentFrames.Length * 128];
        await client.GetStream().ReadExactlyAsync(concurrentReceive);
        var observedMarkers = new HashSet<byte>();
        for (int offset = 0; offset < concurrentReceive.Length; offset += 128)
        {
            byte marker = concurrentReceive[offset];
            Assert.All(
                concurrentReceive.AsSpan(offset, 128).ToArray(),
                value => Assert.Equal(marker, value));
            observedMarkers.Add(marker);
        }
        Assert.Equal(
            concurrentFrames.Select(frame => frame[0]).Order(),
            observedMarkers.Order());

        await listener.StopAsync(CancellationToken.None);
        await listener.DisposeAsync();
    }

    [Fact]
    public async Task TcpListenerRejectsConnectionAtLimitAndDisconnectsExcess()
    {
        LegacyPacketRegistry registry = LegacyCorePacketRegistry.CreateLogin();
        var manager = new ConnectionManager(1);
        var metrics = new NetworkingMetrics();
        var sink = new RecordingIncomingSink();
        var loggerFactory = NullLoggerFactory.Instance;
        var options = new NetworkingOptions
        {
            MaxReceiveBufferBytes = 10240,
            SendQueueCapacity = 16,
            IdleTimeoutSeconds = 0,
            ConnectionShutdownTimeoutSeconds = 5,
        };

        var listener = new LegacyTcpListener(
            NetworkEndpointKind.World,
            IPAddress.Loopback,
            0,
            manager,
            sink,
            registry,
            metrics,
            loggerFactory.CreateLogger<LegacyTcpListener>(),
            loggerFactory,
            options);

        await listener.StartAsync(CancellationToken.None);
        IPEndPoint endpoint = Assert.IsType<IPEndPoint>(listener.LocalEndPoint);

        using var first = new TcpClient();
        await first.ConnectAsync(endpoint.Address, endpoint.Port);
        await WaitUntilAsync(() => manager.Count == 1, TimeSpan.FromSeconds(2));

        using var second = new TcpClient();
        await second.ConnectAsync(endpoint.Address, endpoint.Port);
        await WaitUntilAsync(() => manager.Count == 1, TimeSpan.FromSeconds(2));

        await listener.StopAsync(CancellationToken.None);
        await listener.DisposeAsync();
    }

    [Fact]
    public async Task RemoteDisconnectRemovesConnectionExactlyOnce()
    {
        LegacyPacketRegistry registry = LegacyCorePacketRegistry.CreateLogin();
        var manager = new ConnectionManager(10);
        var metrics = new NetworkingMetrics();
        var sink = new RecordingIncomingSink();
        var loggerFactory = NullLoggerFactory.Instance;
        var options = new NetworkingOptions
        {
            MaxReceiveBufferBytes = 10240,
            SendQueueCapacity = 16,
            IdleTimeoutSeconds = 0,
            ConnectionShutdownTimeoutSeconds = 5,
        };

        var listener = new LegacyTcpListener(
            NetworkEndpointKind.Login,
            IPAddress.Loopback,
            0,
            manager,
            sink,
            registry,
            metrics,
            loggerFactory.CreateLogger<LegacyTcpListener>(),
            loggerFactory,
            options);

        await listener.StartAsync(CancellationToken.None);
        IPEndPoint endpoint = Assert.IsType<IPEndPoint>(listener.LocalEndPoint);

        var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port);
        await WaitUntilAsync(() => manager.Count == 1, TimeSpan.FromSeconds(2));
        client.Close();

        await WaitUntilAsync(() => manager.Count == 0, TimeSpan.FromSeconds(2));
        await listener.StopAsync(CancellationToken.None);
        await listener.DisposeAsync();
    }

    private static byte[] CreateFrame(byte opcode, int payloadSize)
    {
        var frame = new byte[9 + payloadSize];
        frame[8] = opcode;
        return frame;
    }

    private static byte[] CreateHeader(byte opcode)
    {
        var header = new byte[9];
        header[8] = opcode;
        return header;
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(10, cancellation.Token);
        }
    }

    private sealed class RecordingIncomingSink : IIncomingPacketSink
    {
        public TaskCompletionSource<LegacyProtocolFrame> Frame { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask OnFrameAsync(
            NetworkConnection connection,
            LegacyProtocolFrame frame,
            CancellationToken cancellationToken)
        {
            Frame.TrySetResult(frame);
            return ValueTask.CompletedTask;
        }

        public ValueTask OnUnknownOpcodeAsync(
            NetworkConnection connection,
            byte opcode,
            CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
