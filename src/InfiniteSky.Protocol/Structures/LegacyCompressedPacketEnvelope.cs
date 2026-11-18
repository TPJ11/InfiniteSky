using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

/// <summary>
/// Uncompressed state envelope used by the original makeCompressedPacket implementation.
/// Compressed variants remain unimplemented and unresolved.
/// </summary>
public readonly record struct LegacyCompressedPacketEnvelope(
    byte PacketId,
    bool IsCompressed)
{
    public const int WireSize = 2;

    public static LegacyCompressedPacketEnvelope Decode(LegacyBinaryReader reader)
        => new(reader.ReadByte(), reader.ReadByte() != 0);

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteByte(PacketId);
        writer.WriteByte(IsCompressed ? (byte)1 : (byte)0);
    }
}
