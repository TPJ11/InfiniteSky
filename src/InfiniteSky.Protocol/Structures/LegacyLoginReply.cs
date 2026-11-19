using System.Text;

using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

/// <summary>
/// The direct 41-byte login reply assembled by the original Node.js handler.
/// </summary>
public readonly record struct LegacyLoginReply(
    LegacyLoginStatus Status,
    string? Username = null)
{
    public const byte PacketId = 0x03;
    public const int WireSize = 41;

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteByte(PacketId);
        writer.WriteByte((byte)Status);

        Span<byte> reserved = writer.GetSpan(12);
        reserved.Clear();
        writer.Advance(12);

        Span<byte> usernameAndTail = writer.GetSpan(27);
        usernameAndTail.Clear();
        if (Status == LegacyLoginStatus.Success && Username is not null)
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            encoder.Convert(
                Username.AsSpan(),
                usernameAndTail,
                flush: true,
                out _,
                out _,
                out _);
            "****"u8.CopyTo(usernameAndTail[22..]);
        }
        writer.Advance(27);
    }
}
