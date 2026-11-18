using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacySmallPetStorageItem(
    uint Id,
    uint Activity,
    uint Growth)
{
    public const int WireSize = 12;

    public static LegacySmallPetStorageItem Decode(LegacyBinaryReader reader)
        => new(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteUInt32(Activity);
        writer.WriteUInt32(Growth);
    }
}
