namespace InfiniteSky.Protocol;

/// <summary>
/// Base exception for bounded binary protocol operations.
/// </summary>
public class LegacyProtocolException : Exception
{
    public LegacyProtocolException(string message)
        : base(message)
    {
    }

    public LegacyProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
