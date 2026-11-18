using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

/// <summary>
/// 12-byte CVec3. Encoding is a compatibility choice:
/// scalar struct fields use the original restruct float32 bug, while a standard decode method is available.
/// </summary>
public readonly record struct LegacyVector3(float X, float Y, float Z)
{
    public const int WireSize = 12;

    public static LegacyVector3 Decode(LegacyBinaryReader reader)
    {
        float x = LegacyRestructFloat.ReadScalar(reader);
        float y = LegacyRestructFloat.ReadScalar(reader);
        float z = LegacyRestructFloat.ReadScalar(reader);
        return new LegacyVector3(x, y, z);
    }

    public void Encode(LegacyBinaryWriter writer)
    {
        LegacyRestructFloat.WriteScalarBugged(writer, X);
        LegacyRestructFloat.WriteScalarBugged(writer, Y);
        LegacyRestructFloat.WriteScalarBugged(writer, Z);
    }
}
