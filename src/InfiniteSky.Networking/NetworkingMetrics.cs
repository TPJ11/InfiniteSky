using System.Diagnostics.Metrics;

namespace InfiniteSky.Networking;

/// <summary>
/// Real vendor-independent metrics for transport events.
/// </summary>
public sealed class NetworkingMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _accepted;
    private readonly Counter<long> _disconnected;
    private readonly Counter<long> _receiveFailures;
    private readonly Counter<long> _sendFailures;
    private readonly Counter<long> _framesReceived;
    private readonly Counter<long> _framesSent;
    private readonly Counter<long> _bytesReceived;
    private readonly Counter<long> _bytesSent;
    private readonly UpDownCounter<long> _active;

    public NetworkingMetrics()
    {
        _meter = new Meter("InfiniteSky.Networking");
        _accepted = _meter.CreateCounter<long>("infinite_sky.network.accepted");
        _disconnected = _meter.CreateCounter<long>("infinite_sky.network.disconnected");
        _receiveFailures = _meter.CreateCounter<long>("infinite_sky.network.receive_failures");
        _sendFailures = _meter.CreateCounter<long>("infinite_sky.network.send_failures");
        _framesReceived = _meter.CreateCounter<long>("infinite_sky.network.frames_received");
        _framesSent = _meter.CreateCounter<long>("infinite_sky.network.frames_sent");
        _bytesReceived = _meter.CreateCounter<long>("infinite_sky.network.bytes_received");
        _bytesSent = _meter.CreateCounter<long>("infinite_sky.network.bytes_sent");
        _active = _meter.CreateUpDownCounter<long>("infinite_sky.network.active");
    }

    public void Accepted() { _accepted.Add(1); _active.Add(1); }
    public void Disconnected() { _disconnected.Add(1); _active.Add(-1); }
    public void ReceiveFailure() => _receiveFailures.Add(1);
    public void SendFailure() => _sendFailures.Add(1);
    public void FrameReceived() => _framesReceived.Add(1);
    public void FrameSent() => _framesSent.Add(1);
    public void BytesReceived(int count) => _bytesReceived.Add(count);
    public void BytesSent(int count) => _bytesSent.Add(count);
    public void Dispose() => _meter.Dispose();
}
