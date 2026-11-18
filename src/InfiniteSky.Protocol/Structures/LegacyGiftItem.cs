using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyGiftItem(uint Id, uint Amount)
{
    public const int WireSize = 8;

    public static LegacyGiftItem Decode(LegacyBinaryReader reader)
        => new(reader.ReadUInt32(), reader.ReadUInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteUInt32(Amount);
    }
}
