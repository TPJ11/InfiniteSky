namespace InfiniteSky.Application.Authentication;

public enum AccountRepositoryFailureKind
{
    Unknown,
    Disabled,
    Configuration,
    Authentication,
    Unavailable,
    DuplicateKey,
    Serialization,
    MalformedDocument,
    AccountNotFound,
    IndexFailure,
}
