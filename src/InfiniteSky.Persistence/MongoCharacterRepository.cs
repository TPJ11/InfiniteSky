using InfiniteSky.Application.Characters;
using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

namespace InfiniteSky.Persistence;

/// <summary>
/// Loads legacy character documents and performs non-destructive world-location updates.
/// </summary>
public sealed partial class MongoCharacterRepository : ICharacterRepository
{
    private readonly MongoPersistenceContext _context;
    private readonly MongoPersistenceHealthState _healthState;
    private readonly MongoPersistenceMetrics _metrics;
    private readonly ILogger<MongoCharacterRepository> _logger;

    public MongoCharacterRepository(
        MongoPersistenceContext context,
        MongoPersistenceHealthState healthState,
        MongoPersistenceMetrics metrics,
        ILogger<MongoCharacterRepository> logger)
    {
        _context = context;
        _healthState = healthState;
        _metrics = metrics;
        _logger = logger;
    }

    public async ValueTask<IReadOnlyList<Character>> GetForAccountAsync(
        AccountId accountId,
        string serverName,
        CancellationToken cancellationToken)
    {
        const string operation = "character.load_account";
        long start = MongoPersistenceMetrics.Start();
        try
        {
            using CancellationTokenSource timeout = _context.CreateOperationToken(cancellationToken);
            FilterDefinition<BsonDocument> filter = AccountServerFilter(accountId, serverName);
            using IAsyncCursor<BsonDocument> cursor = await _context.CharacterDocuments
                .Find(filter)
                .Limit(3)
                .ToCursorAsync(timeout.Token);
            List<BsonDocument> documents = await cursor.ToListAsync(timeout.Token);
            var characters = documents.Select(MongoCharacterDocumentMapper.Map).ToArray();

            _healthState.MarkHealthy();
            _metrics.Complete(operation, start);
            return characters;
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
            MongoPersistenceFailureKind failureKind = MongoPersistenceErrorClassifier.Classify(exception);
            if (failureKind != MongoPersistenceFailureKind.Disabled)
            {
                _healthState.MarkFailure(failureKind);
            }

            _metrics.Complete(operation, start, failureKind);
            LogLoadFailed(_logger, failureKind.ToString());
            throw new MongoPersistenceException(failureKind);
        }
    }

    public async ValueTask<Character?> GetOwnedAsync(
        CharacterId characterId,
        AccountId accountId,
        string serverName,
        CancellationToken cancellationToken)
    {
        const string operation = "character.load_owned";
        long start = MongoPersistenceMetrics.Start();
        try
        {
            using CancellationTokenSource timeout = _context.CreateOperationToken(cancellationToken);
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", new BsonInt64(characterId.Value)),
                AccountServerFilter(accountId, serverName));
            BsonDocument? document = await _context.CharacterDocuments
                .Find(filter)
                .FirstOrDefaultAsync(timeout.Token);

            Character? character = document is null ? null : MongoCharacterDocumentMapper.Map(document);
            _healthState.MarkHealthy();
            _metrics.Complete(operation, start);
            return character;
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
            MongoPersistenceFailureKind failureKind = MongoPersistenceErrorClassifier.Classify(exception);
            if (failureKind != MongoPersistenceFailureKind.Disabled)
            {
                _healthState.MarkFailure(failureKind);
            }

            _metrics.Complete(operation, start, failureKind);
            LogLoadFailed(_logger, failureKind.ToString());
            throw new MongoPersistenceException(failureKind);
        }
    }

    public async ValueTask SaveWorldLocationAsync(
        CharacterId characterId,
        AccountId accountId,
        string serverName,
        ZoneId zoneId,
        Position3 position,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        if (!double.IsFinite(position.X) || !double.IsFinite(position.Y) || !double.IsFinite(position.Z))
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Saved character coordinates must be finite.");
        }

        const string operation = "character.save_world_location";
        long start = MongoPersistenceMetrics.Start();
        try
        {
            using CancellationTokenSource timeout = _context.CreateOperationToken(cancellationToken);
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", new BsonInt64(characterId.Value)),
                AccountServerFilter(accountId, serverName));
            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                .Set("MapID", new BsonDouble(zoneId.Value))
                .Set("RealX", PersistedCoordinate(position.X))
                .Set("RealY", PersistedCoordinate(position.Y))
                .Set("RealZ", PersistedCoordinate(position.Z));
            UpdateResult result = await _context.CharacterDocuments.UpdateOneAsync(
                filter,
                update,
                cancellationToken: timeout.Token);

            if (result.MatchedCount != 1)
            {
                throw new MongoPersistenceException(MongoPersistenceFailureKind.CharacterNotFound);
            }

            _healthState.MarkHealthy();
            _metrics.Complete(operation, start);
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
            MongoPersistenceFailureKind failureKind = MongoPersistenceErrorClassifier.Classify(exception);
            if (failureKind != MongoPersistenceFailureKind.Disabled)
            {
                _healthState.MarkFailure(failureKind);
            }

            _metrics.Complete(operation, start, failureKind);
            LogSaveFailed(_logger, failureKind.ToString());
            throw exception is MongoPersistenceException persistenceException
                ? persistenceException
                : new MongoPersistenceException(failureKind);
        }
    }

    private static FilterDefinition<BsonDocument> AccountServerFilter(
        AccountId accountId,
        string serverName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        return Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("AccountID", new BsonInt64(accountId.Value)),
            Builders<BsonDocument>.Filter.Eq("ServerName", serverName));
    }

    private static BsonDouble PersistedCoordinate(double value) => new(Math.Truncate(value));

    [LoggerMessage(
        EventId = 4130,
        Level = LogLevel.Warning,
        Message = "MongoDB character read failed: Failure={FailureKind}")]
    private static partial void LogLoadFailed(ILogger logger, string failureKind);

    [LoggerMessage(
        EventId = 4131,
        Level = LogLevel.Warning,
        Message = "MongoDB character world-location save failed: Failure={FailureKind}")]
    private static partial void LogSaveFailed(ILogger logger, string failureKind);
}
