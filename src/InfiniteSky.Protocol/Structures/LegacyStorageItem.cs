using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyStorageItem(
    uint Id,
    uint Column,
    uint Row,
    uint Amount,
    byte Enchant,
    byte Combine,
    ushort Unknown)
{
    public const int WireSize = 20;

    public static LegacyStorageItem Decode(LegacyBinaryReader reader)
        => new(
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadUInt16());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteUInt32(Column);
        writer.WriteUInt32(Row);
        writer.WriteUInt32(Amount);
        writer.WriteByte(Enchant);
        writer.WriteByte(Combine);
        writer.WriteUInt16(Unknown);
    }
}
