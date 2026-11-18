using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyWorldHandshake(string Name, byte Slot, uint MapId)
{
    public const int WireSize = 18;

    public static LegacyWorldHandshake Decode(LegacyBinaryReader reader)
        => new(reader.ReadFixedString(13), reader.ReadByte(), reader.ReadUInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteFixedString(Name.AsSpan(), 13);
        writer.WriteByte(Slot);
        writer.WriteUInt32(MapId);
    }
}
