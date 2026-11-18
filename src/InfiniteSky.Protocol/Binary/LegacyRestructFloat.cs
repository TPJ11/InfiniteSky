using System.Buffers.Binary;

namespace InfiniteSky.Protocol.Binary;

/// <summary>
/// Implements the scalar float packing bug and standard decoding behaviour in the original restruct library.
/// </summary>
public static class LegacyRestructFloat
{
    /// <summary>
    /// Writes a scalar float exactly as top-level restruct scalar float32l.pack does:
    /// the uint32 bit pattern is incremented once before little-endian output.
    /// </summary>
    public static void WriteScalarBugged(LegacyBinaryWriter writer, float value)
    {
        Span<byte> destination = writer.GetSpan(sizeof(uint));
        uint bits = unchecked((uint)BitConverter.SingleToInt32Bits(value));
        BinaryPrimitives.WriteUInt32LittleEndian(destination, unchecked(bits + 1));
        writer.Advance(sizeof(uint));
    }

    /// <summary>
    /// Reads scalar bytes through the normal IEEE-754 little-endian interpretation.
    /// </summary>
    public static float ReadScalar(LegacyBinaryReader reader)
    {
        uint bits = reader.ReadUInt32();
        return BitConverter.Int32BitsToSingle(unchecked((int)bits));
    }

    /// <summary>
    /// Writes a normal scalar float. It is separate from the legacy bugged representation.
    /// </summary>
    public static void WriteStandard(LegacyBinaryWriter writer, float value)
    {
        writer.WriteFloat32(value);
    }
}
