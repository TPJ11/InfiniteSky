using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyBottle(
    uint Id,
    ReadOnlyMemory<byte> Capacity)
{
    public const int WireSize = 12;

    public static LegacyBottle Decode(LegacyBinaryReader reader)
    {
        uint id = reader.ReadUInt32();
        var capacity = reader.ReadBytes(8);
        return new LegacyBottle(id, capacity);
    }

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Id);
        byte[] bytes = Capacity.ToArray();
        if (bytes.Length != 8)
        {
            throw new LegacyProtocolWriteException("LegacyBottle capacity must contain exactly eight bytes.");
        }
        writer.WriteBytes(bytes);
    }
}
