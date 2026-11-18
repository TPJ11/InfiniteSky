using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyMapLoadReply(byte PacketId, byte Status, string Ip, uint Port)
{
    public const int WireSize = 22;

    public static LegacyMapLoadReply Decode(LegacyBinaryReader reader)
    {
        byte packetId = reader.ReadByte();
        byte status = reader.ReadByte();
        string ip = reader.ReadFixedString(16);
        uint port = reader.ReadUInt32();
        return new LegacyMapLoadReply(packetId, status, ip, port);
    }

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteByte(PacketId);
        writer.WriteByte(Status);
        writer.WriteFixedString(Ip.AsSpan(), 16);
        writer.WriteUInt32(Port);
    }
}
