namespace InfiniteSky.Protocol;

/// <summary>
/// Raised when incoming protocol bytes cannot satisfy a read.
/// </summary>
public sealed class LegacyProtocolReadException : LegacyProtocolException
{
    public LegacyProtocolReadException(int offset, int requested, int remaining)
        : base($"Cannot read {requested} byte(s) at offset {offset}; only {remaining} byte(s) remain.")
    {
        Offset = offset;
        Requested = requested;
        Remaining = remaining;
    }

    public int Offset { get; }
    public int Requested { get; }
    public int Remaining { get; }
}
