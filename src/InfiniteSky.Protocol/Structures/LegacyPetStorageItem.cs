using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyPetStorageItem(
    uint Id,
    uint Column,
    uint Row,
    uint Activity,
    uint Growth)
{
    public const int WireSize = 20;

    public static LegacyPetStorageItem Decode(LegacyBinaryReader reader)
        => new(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteUInt32(Column);
        writer.WriteUInt32(Row);
        writer.WriteUInt32(Activity);
        writer.WriteUInt32(Growth);
    }
}
