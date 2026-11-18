using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyZoneChangeRequest(
    uint ZoneId,
    uint PortalId,
    uint Unknown1,
    uint Unknown2)
{
    public const int WireSize = 16;

    public static LegacyZoneChangeRequest Decode(LegacyBinaryReader reader)
        => new(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(ZoneId);
        writer.WriteUInt32(PortalId);
        writer.WriteUInt32(Unknown1);
        writer.WriteUInt32(Unknown2);
    }
}
