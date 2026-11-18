namespace InfiniteSky.Protocol.Packets;

/// <summary>
/// A protocol descriptor for a receive opcode and either no payload or an exact payload size.
/// </summary>
public sealed record LegacyPacketDefinition(
    byte Opcode,
    string Name,
    LegacyPacketKind Kind,
    int PayloadSize)
{
    public LegacyPacketDefinition(byte opcode, string name)
        : this(opcode, name, LegacyPacketKind.HeaderOnly, 0)
    {
    }

    public LegacyPacketDefinition(byte opcode, string name, int payloadSize)
        : this(opcode, name, LegacyPacketKind.FixedPayload, payloadSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadSize);
    }
}
