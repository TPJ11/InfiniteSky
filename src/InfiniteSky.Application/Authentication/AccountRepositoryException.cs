namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Signals an account-store failure without embedding credentials in the message.
/// </summary>
public sealed class AccountRepositoryException : Exception
{
    public AccountRepositoryException(string message)
        : base(message)
    {
    }

    public AccountRepositoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
