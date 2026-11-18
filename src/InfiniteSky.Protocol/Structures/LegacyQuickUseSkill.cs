using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyQuickUseSkill(uint Id, uint Level)
{
    public const int WireSize = 8;

    public static LegacyQuickUseSkill Decode(LegacyBinaryReader reader)
        => new(reader.ReadUInt32(), reader.ReadUInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteUInt32(Level);
    }
}
