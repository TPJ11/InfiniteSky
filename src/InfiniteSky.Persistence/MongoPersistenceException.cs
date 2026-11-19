namespace InfiniteSky.Persistence;

/// <summary>
/// A MongoDB persistence failure with a sanitized message.
/// </summary>
public sealed class MongoPersistenceException : Exception
{
    public MongoPersistenceException(MongoPersistenceFailureKind failureKind)
        : base(GetMessage(failureKind))
    {
        FailureKind = failureKind;
    }

    public MongoPersistenceFailureKind FailureKind { get; }

    private static string GetMessage(MongoPersistenceFailureKind failureKind)
    {
        return failureKind switch
        {
            MongoPersistenceFailureKind.Configuration => "MongoDB configuration is invalid.",
            MongoPersistenceFailureKind.Disabled => "MongoDB persistence is disabled.",
            MongoPersistenceFailureKind.Authentication => "MongoDB authentication or authorization failed.",
            MongoPersistenceFailureKind.Unavailable => "MongoDB is unavailable.",
            MongoPersistenceFailureKind.AccountNotFound => "The account no longer exists.",
            MongoPersistenceFailureKind.DuplicateKey => "MongoDB rejected a duplicate key.",
            MongoPersistenceFailureKind.Serialization => "A MongoDB BSON serialization operation failed.",
            MongoPersistenceFailureKind.MalformedDocument => "A MongoDB document is malformed.",
            MongoPersistenceFailureKind.IndexConflict => "The account username index is incompatible.",
            MongoPersistenceFailureKind.IndexMissing => "The required account username index is missing.",
            MongoPersistenceFailureKind.OperationCancelled => "The MongoDB operation was cancelled.",
            _ => "A MongoDB persistence operation failed.",
        };
    }
}
