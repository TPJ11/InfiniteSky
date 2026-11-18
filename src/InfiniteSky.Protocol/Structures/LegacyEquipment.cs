using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyEquipment(
    uint Id,
    uint Capacity,
    byte Enchant,
    byte Combine,
    ushort Unknown)
{
    public const int WireSize = 12;

    public static LegacyEquipment Decode(LegacyBinaryReader reader)
        => new(
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadUInt16());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteUInt32(Capacity);
        writer.WriteByte(Enchant);
        writer.WriteByte(Combine);
        writer.WriteUInt16(Unknown);
    }
}
