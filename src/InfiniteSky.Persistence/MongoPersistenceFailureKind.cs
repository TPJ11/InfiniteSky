namespace InfiniteSky.Persistence;

/// <summary>
/// Safe, non-sensitive MongoDB failure categories for logs and health reporting.
/// </summary>
public enum MongoPersistenceFailureKind
{
    Configuration,
    Disabled,
    Authentication,
    Unavailable,
    AccountNotFound,
    DuplicateKey,
    Serialization,
    MalformedDocument,
    IndexConflict,
    IndexMissing,
    Database,
    OperationCancelled,
}
