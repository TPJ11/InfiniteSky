using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

/// <summary>
/// Normal IEEE-754 CVec3 encoding for standards-compliant/provided byte fixtures.
/// </summary>
public readonly record struct LegacyStandardVector3(float X, float Y, float Z)
{
    public const int WireSize = 12;

    public static LegacyStandardVector3 Decode(LegacyBinaryReader reader)
        => new(reader.ReadFloat32(), reader.ReadFloat32(), reader.ReadFloat32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteFloat32(X);
        writer.WriteFloat32(Y);
        writer.WriteFloat32(Z);
    }
}
