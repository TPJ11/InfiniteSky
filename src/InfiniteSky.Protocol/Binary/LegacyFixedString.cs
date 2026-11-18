using System.Text;

namespace InfiniteSky.Protocol.Binary;

/// <summary>
/// Implements the byte-pass-through fixed-string behaviour of the original restruct code.
/// </summary>
public static class LegacyFixedString
{
    private static readonly Encoding Encoding = Encoding.Latin1;

    /// <summary>
    /// Reads a fixed width, stopping at the first zero and ignoring bytes after it.
    /// </summary>
    public static string Read(ReadOnlySpan<byte> bytes)
    {
        int terminator = bytes.IndexOf((byte)0);
        ReadOnlySpan<byte> meaningful = terminator >= 0 ? bytes[..terminator] : bytes;
        return Encoding.GetString(meaningful);
    }

    /// <summary>
    /// Encodes and writes a fixed-width string using truncation/padding consistent with restruct.
    /// Returns the number of source bytes that fit.
    /// </summary>
    public static int Write(LegacyBinaryWriter writer, ReadOnlySpan<char> value, int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        Span<byte> destination = writer.GetSpan(width);
        destination.Clear();
        int used = Math.Min(value.Length, width);
        for (int i = 0; i < used; i++)
        {
            destination[i] = unchecked((byte)value[i]);
        }
        writer.Advance(width);
        return used;
    }

    /// <summary>
    /// Encodes a fixed-width string with a one-byte-per-character compatibility rule.
    /// </summary>
    public static int GetLegacyByteCount(ReadOnlySpan<char> value)
    {
        int count = 0;
        foreach (char character in value)
        {
            count += 1;
        }
        return count;
    }
}
