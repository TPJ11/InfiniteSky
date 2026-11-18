namespace InfiniteSky.Protocol;

/// <summary>
/// Raised when a protocol value cannot be encoded into its declared wire shape.
/// </summary>
public sealed class LegacyProtocolWriteException : LegacyProtocolException
{
    public LegacyProtocolWriteException(string message)
        : base(message)
    {
    }
}
