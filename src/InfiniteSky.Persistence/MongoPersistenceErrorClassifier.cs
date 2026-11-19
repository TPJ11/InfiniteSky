using MongoDB.Bson;
using MongoDB.Driver;

namespace InfiniteSky.Persistence;

/// <summary>
/// Classifies driver failures without exposing a connection string or document contents.
/// </summary>
public static class MongoPersistenceErrorClassifier
{
    public static MongoPersistenceFailureKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            MongoPersistenceException persistenceException => persistenceException.FailureKind,
            OperationCanceledException => MongoPersistenceFailureKind.OperationCancelled,
            BsonSerializationException => MongoPersistenceFailureKind.Serialization,
            MongoAuthenticationException => MongoPersistenceFailureKind.Authentication,
            MongoCommandException commandException when commandException.Code is 13 or 18 =>
                MongoPersistenceFailureKind.Authentication,
            MongoCommandException commandException when commandException.Code is 85 or 86 =>
                MongoPersistenceFailureKind.IndexConflict,
            MongoCommandException commandException when commandException.Code == 11000 =>
                MongoPersistenceFailureKind.DuplicateKey,
            MongoWriteException writeException when writeException.WriteError.Code == 11000 =>
                MongoPersistenceFailureKind.DuplicateKey,
            MongoConnectionException or TimeoutException => MongoPersistenceFailureKind.Unavailable,
            MongoException => MongoPersistenceFailureKind.Database,
            _ => MongoPersistenceFailureKind.Database,
        };
    }
}
