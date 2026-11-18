using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyLoginRequest(string Username, string Password)
{
    public const int WireSize = 91;

    public static LegacyLoginRequest Decode(LegacyBinaryReader reader)
    {
        string username = reader.ReadFixedString(14);
        string password = reader.ReadFixedString(14);
        reader.Skip(63);
        return new LegacyLoginRequest(username, password);
    }

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteFixedString(Username.AsSpan(), 14);
        writer.WriteFixedString(Password.AsSpan(), 14);
        Span<byte> padding = writer.GetSpan(63);
        padding.Clear();
        writer.Advance(63);
    }
}
