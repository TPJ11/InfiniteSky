namespace InfiniteSky.Networking;

/// <summary>
/// Delivers a decoded frame boundary to future application packet handlers.
/// No authentication or gameplay behavior belongs here.
/// </summary>
public interface IIncomingPacketSink
{
    ValueTask OnFrameAsync(
        NetworkConnection connection,
        LegacyProtocolFrame frame,
        CancellationToken cancellationToken);

    ValueTask OnUnknownOpcodeAsync(
        NetworkConnection connection,
        byte opcode,
        CancellationToken cancellationToken);
}
