using InfiniteSky.Application.Authentication;
using InfiniteSky.Domain.Identifiers;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

namespace InfiniteSky.Persistence;

/// <summary>
/// Implements the Prompt 11 account contract using the original field names and BSON.
/// </summary>
public sealed partial class MongoAccountAuthenticationRepository
    : IAccountAuthenticationRepository
{
    private readonly MongoPersistenceContext _context;
    private readonly MongoPersistenceHealthState _healthState;
    private readonly MongoPersistenceMetrics _metrics;
    private readonly ILogger<MongoAccountAuthenticationRepository> _logger;

    public MongoAccountAuthenticationRepository(
        MongoPersistenceContext context,
        MongoPersistenceHealthState healthState,
        MongoPersistenceMetrics metrics,
        ILogger<MongoAccountAuthenticationRepository> logger)
    {
        _context = context;
        _healthState = healthState;
        _metrics = metrics;
        _logger = logger;
    }

    public ValueTask<AccountAuthenticationRecord?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(username);
        return ExecuteAsync(
            "account.find",
            async token =>
            {
                FilterDefinition<BsonDocument> filter =
                    Builders<BsonDocument>.Filter.Eq("Username", username);
                ProjectionDefinition<BsonDocument> projection =
                    Builders<BsonDocument>.Projection
                        .Include("_id")
                        .Include("Username")
                        .Include("Password");
                var options = new FindOptions<BsonDocument, BsonDocument>
                {
                    Collation = new Collation("simple"),
                    Projection = projection,
                };
                using IAsyncCursor<BsonDocument> cursor =
                    await _context.AccountDocuments.FindAsync(
                        filter,
                        options,
                        cancellationToken: token);
                BsonDocument? document = await cursor.FirstOrDefaultAsync(token);
                return document is null
                    ? null
                    : LegacyAccountAuthenticationDocumentMapper.Map(document);
            },
            cancellationToken);
    }

    public async ValueTask SetActiveAsync(
        AccountId accountId,
        bool active,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            "account.set_active",
            async token =>
            {
                FilterDefinition<BsonDocument> filter =
                    Builders<BsonDocument>.Filter.Eq("_id", new BsonInt64(accountId.Value));
                UpdateDefinition<BsonDocument> update =
                    Builders<BsonDocument>.Update.Set("active", new BsonInt32(active ? 1 : 0));
                UpdateResult result = await _context.AccountDocuments.UpdateOneAsync(
                    filter,
                    update,
                    cancellationToken: token);
                if (result.MatchedCount == 0)
                {
                    throw new MongoPersistenceException(MongoPersistenceFailureKind.AccountNotFound);
                }

                return true;
            },
            cancellationToken);
    }

    public async ValueTask ResetAllActiveAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            "account.reset_active",
            async token =>
            {
                UpdateDefinition<BsonDocument> update =
                    Builders<BsonDocument>.Update.Set("active", new BsonInt32(0));
                await _context.AccountDocuments.UpdateManyAsync(
                    Builders<BsonDocument>.Filter.Empty,
                    update,
                    cancellationToken: token);
                return true;
            },
            cancellationToken);
    }

    private async ValueTask<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        long start = MongoPersistenceMetrics.Start();
        try
        {
            using CancellationTokenSource timeout =
                _context.CreateOperationToken(cancellationToken);
            T result = await action(timeout.Token);
            _healthState.MarkHealthy();
            _metrics.Complete(operation, start);
            return result;
        }
        catch (OperationCanceledException)
        {
            _metrics.Complete(operation, start, MongoPersistenceFailureKind.OperationCancelled);
            throw;
        }
        catch (Exception exception) when (
            exception is MongoException
                or MongoPersistenceException
                or BsonException
                or TimeoutException)
        {
            MongoPersistenceFailureKind failureKind =
                MongoPersistenceErrorClassifier.Classify(exception);
            if (failureKind != MongoPersistenceFailureKind.Disabled)
            {
                _healthState.MarkFailure(failureKind);
            }
            _metrics.Complete(operation, start, failureKind);
            LogOperationFailed(_logger, operation, failureKind.ToString());
            throw new AccountRepositoryException(
                "MongoDB account operation failed.",
                MapAccountFailureKind(failureKind));
        }
    }

    private static AccountRepositoryFailureKind MapAccountFailureKind(
        MongoPersistenceFailureKind failureKind)
    {
        return failureKind switch
        {
            MongoPersistenceFailureKind.Disabled => AccountRepositoryFailureKind.Disabled,
            MongoPersistenceFailureKind.Configuration => AccountRepositoryFailureKind.Configuration,
            MongoPersistenceFailureKind.Authentication => AccountRepositoryFailureKind.Authentication,
            MongoPersistenceFailureKind.Unavailable => AccountRepositoryFailureKind.Unavailable,
            MongoPersistenceFailureKind.DuplicateKey => AccountRepositoryFailureKind.DuplicateKey,
            MongoPersistenceFailureKind.Serialization => AccountRepositoryFailureKind.Serialization,
            MongoPersistenceFailureKind.MalformedDocument => AccountRepositoryFailureKind.MalformedDocument,
            MongoPersistenceFailureKind.AccountNotFound => AccountRepositoryFailureKind.AccountNotFound,
            MongoPersistenceFailureKind.IndexConflict or MongoPersistenceFailureKind.IndexMissing =>
                AccountRepositoryFailureKind.IndexFailure,
            _ => AccountRepositoryFailureKind.Unknown,
        };
    }

    [LoggerMessage(
        EventId = 4100,
        Level = LogLevel.Warning,
        Message = "MongoDB persistence operation failed: Operation={Operation}, Failure={FailureKind}")]
    private static partial void LogOperationFailed(
        ILogger logger,
        string operation,
        string failureKind);
}
