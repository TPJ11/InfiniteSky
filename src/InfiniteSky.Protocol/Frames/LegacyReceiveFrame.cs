namespace InfiniteSky.Protocol.Frames;

/// <summary>
/// Helpers for the original 9-byte receive-header/framed payload convention.
/// </summary>
public static class LegacyReceiveFrame
{
    public const int HeaderSize = 9;
    public const int OpcodeOffset = 8;

    public static bool TryParse(
        ReadOnlySpan<byte> buffer,
        int payloadSize,
        out byte opcode,
        out ReadOnlySpan<byte> payload,
        out int consumed)
    {
        payload = default;
        opcode = 0;
        consumed = 0;

        if (payloadSize < 0)
        {
            return false;
        }

        int total = HeaderSize + payloadSize;
        if (buffer.Length < total)
        {
            return false;
        }

        opcode = buffer[OpcodeOffset];
        payload = buffer.Slice(HeaderSize, payloadSize);
        consumed = total;
        return true;
    }

    public static int TotalSize(int payloadSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadSize);
        return checked(HeaderSize + payloadSize);
    }
}
