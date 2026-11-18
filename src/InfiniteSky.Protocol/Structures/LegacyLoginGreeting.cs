using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyLoginGreeting(byte Unknown, uint PlayerCount)
{
    public const byte PacketId = 0x00;
    public const int WireSize = 21;

    public static LegacyLoginGreeting Decode(LegacyBinaryReader reader)
    {
        _ = reader.ReadByte();
        byte unknown = reader.ReadByte();
        uint playerCount = reader.ReadUInt32();
        reader.Skip(15);
        return new LegacyLoginGreeting(unknown, playerCount);
    }

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteByte(PacketId);
        writer.WriteByte(Unknown);
        writer.WriteUInt32(PlayerCount);
        Span<byte> padding = writer.GetSpan(15);
        padding.Clear();
        writer.Advance(15);
    }
}
