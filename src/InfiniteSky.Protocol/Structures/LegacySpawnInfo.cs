using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacySpawnInfo(
    uint UniqueId,
    uint Id,
    LegacyStandardVector3 Location,
    float Direction)
{
    public const int WireSize = 24;

    public static LegacySpawnInfo Decode(LegacyBinaryReader reader)
        => new(
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            LegacyStandardVector3.Decode(reader),
            reader.ReadFloat32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(UniqueId);
        writer.WriteUInt32(Id);
        Location.Encode(writer);
        writer.WriteFloat32(Direction);
    }
}
