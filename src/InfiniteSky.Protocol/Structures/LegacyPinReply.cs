using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyPinReply(byte PacketId, byte Status)
{
    public const int WireSize = 2;

    public static LegacyPinReply Decode(LegacyBinaryReader reader)
        => new(reader.ReadByte(), reader.ReadByte());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteByte(PacketId);
        writer.WriteByte(Status);
    }
}
