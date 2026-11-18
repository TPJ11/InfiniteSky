using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyWorldRegion(
    int Unknown1,
    int ZoneId,
    int Unknown2,
    int Unknown3,
    int X,
    int Y,
    int Z,
    int Radius)
{
    public const int WireSize = 32;

    public static LegacyWorldRegion Decode(LegacyBinaryReader reader)
        => new(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteInt32(Unknown1);
        writer.WriteInt32(ZoneId);
        writer.WriteInt32(Unknown2);
        writer.WriteInt32(Unknown3);
        writer.WriteInt32(X);
        writer.WriteInt32(Y);
        writer.WriteInt32(Z);
        writer.WriteInt32(Radius);
    }
}
