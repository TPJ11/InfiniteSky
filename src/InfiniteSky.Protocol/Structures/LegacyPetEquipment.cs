using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyPetEquipment(uint Id, ushort Activity, uint Growth)
{
    public const int WireSize = 12;

    public static LegacyPetEquipment Decode(LegacyBinaryReader reader)
        => new(reader.ReadUInt32(), reader.ReadUInt16(), reader.ReadUInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteUInt16(Activity);
        writer.WriteUInt32(Growth);
    }
}
