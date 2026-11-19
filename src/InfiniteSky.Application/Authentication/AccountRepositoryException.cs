namespace InfiniteSky.Application.Authentication;

/// <summary>
/// Signals an account-store failure without embedding credentials in the message.
/// </summary>
public sealed class AccountRepositoryException : Exception
{
    public AccountRepositoryException(string message)
        : base(message)
    {
        FailureKind = AccountRepositoryFailureKind.Unknown;
    }

    public AccountRepositoryException(
        string message,
        AccountRepositoryFailureKind failureKind)
        : base(message)
    {
        FailureKind = failureKind;
    }

    public AccountRepositoryException(string message, Exception innerException)
        : base(message, innerException)
    {
        FailureKind = AccountRepositoryFailureKind.Unknown;
    }

    public AccountRepositoryFailureKind FailureKind { get; }
}
