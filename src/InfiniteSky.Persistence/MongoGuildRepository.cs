using InfiniteSky.Application.Guilds;
using InfiniteSky.Domain.Guilds;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

namespace InfiniteSky.Persistence;

/// <summary>
/// Reads the guild documents loaded by the original world bootstrap.
/// </summary>
public sealed partial class MongoGuildRepository : IGuildRepository
{
    private readonly MongoPersistenceContext _context;
    private readonly MongoPersistenceHealthState _healthState;
    private readonly MongoPersistenceMetrics _metrics;
    private readonly ILogger<MongoGuildRepository> _logger;

    public MongoGuildRepository(
        MongoPersistenceContext context,
        MongoPersistenceHealthState healthState,
        MongoPersistenceMetrics metrics,
        ILogger<MongoGuildRepository> logger)
    {
        _context = context;
        _healthState = healthState;
        _metrics = metrics;
        _logger = logger;
    }

    public async ValueTask<IReadOnlyList<Guild>> LoadAllAsync(
        CancellationToken cancellationToken)
    {
        const string operation = "guild.load_all";
        long start = MongoPersistenceMetrics.Start();

        try
        {
            using CancellationTokenSource timeout =
                _context.CreateOperationToken(cancellationToken);
            using IAsyncCursor<BsonDocument> cursor =
                await _context.GuildDocuments.Find(Builders<BsonDocument>.Filter.Empty)
                    .ToCursorAsync(timeout.Token);
            List<BsonDocument> documents = await cursor.ToListAsync(timeout.Token);

            var guilds = new List<Guild>(documents.Count);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (BsonDocument document in documents)
            {
                Guild guild = MongoGuildDocumentMapper.Map(document);
                if (!names.Add(guild.Name))
                {
                    throw new MongoPersistenceException(
                        MongoPersistenceFailureKind.MalformedDocument);
                }

                guilds.Add(guild);
            }

            _healthState.MarkHealthy();
            _metrics.Complete(operation, start);
            return guilds;
        }
        catch (OperationCanceledException)
        {
            _metrics.Complete(
                operation,
                start,
                MongoPersistenceFailureKind.OperationCancelled);
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
            LogLoadFailed(_logger, failureKind.ToString());
            throw new MongoPersistenceException(failureKind);
        }
    }

    [LoggerMessage(
        EventId = 4120,
        Level = LogLevel.Warning,
        Message = "MongoDB guild bootstrap failed: Failure={FailureKind}")]
    private static partial void LogLoadFailed(ILogger logger, string failureKind);
}
