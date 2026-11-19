using MongoDB.Bson;
using MongoDB.Driver;

namespace InfiniteSky.Persistence;

/// <summary>
/// Owns the long-lived MongoClient and exposes bounded database operations.
/// </summary>
public sealed class MongoPersistenceContext : IDisposable
{
    private readonly MongoClient? _client;
    private readonly IMongoDatabase? _database;
    private readonly MongoPersistenceMetrics _metrics;
    private readonly MongoPersistenceSettings _settings;

    public MongoPersistenceContext(
        MongoPersistenceSettings settings,
        MongoPersistenceMetrics metrics)
    {
        _settings = settings;
        _metrics = metrics;

        if (!settings.Enabled)
        {
            return;
        }

        ValidateEnabledSettings(settings);

        MongoUrl url;
        try
        {
            url = MongoUrl.Create(settings.ConnectionString);
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException or MongoConfigurationException)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.Configuration);
        }

        if (!string.IsNullOrEmpty(url.DatabaseName)
            && !string.Equals(url.DatabaseName, settings.DatabaseName, StringComparison.Ordinal))
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.Configuration);
        }

        MongoClientSettings clientSettings;
        try
        {
            clientSettings = MongoClientSettings.FromUrl(url);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or MongoConfigurationException)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.Configuration);
        }

        clientSettings.ServerSelectionTimeout =
            TimeSpan.FromSeconds(settings.ServerSelectionTimeoutSeconds);
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(settings.ConnectTimeoutSeconds);
        clientSettings.MinConnectionPoolSize = settings.MinConnectionPoolSize;
        clientSettings.MaxConnectionPoolSize = settings.MaxConnectionPoolSize;
        clientSettings.RetryReads = false;
        clientSettings.RetryWrites = false;

        try
        {
            _client = new MongoClient(clientSettings);
            _database = _client.GetDatabase(settings.DatabaseName);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or MongoConfigurationException)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.Configuration);
        }
    }

    public MongoPersistenceSettings Settings => _settings;

    internal IMongoCollection<BsonDocument> AccountDocuments
    {
        get
        {
            if (_database is null)
            {
                throw new MongoPersistenceException(MongoPersistenceFailureKind.Disabled);
            }

            return _database.GetCollection<BsonDocument>(_settings.AccountCollectionName);
        }
    }

    public CancellationTokenSource CreateOperationToken(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.Disabled);
        }

        CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.OperationTimeoutSeconds));
        return timeout;
    }

    public async Task PingAsync(CancellationToken cancellationToken)
    {
        await MeasureAsync(
            "health.ping",
            async () =>
            {
                if (_client is null)
                {
                    throw new MongoPersistenceException(MongoPersistenceFailureKind.Disabled);
                }

                await _database!.RunCommandAsync<BsonDocument>(
                    new BsonDocument("ping", 1),
                    cancellationToken: cancellationToken);
            });
    }

    public async Task VerifyAccountUsernameIndexAsync(
        bool createWhenMissing,
        CancellationToken cancellationToken)
    {
        await MeasureAsync(
            "index.account_username",
            () => VerifyAccountUsernameIndexCoreAsync(createWhenMissing, cancellationToken));
    }

    private async Task VerifyAccountUsernameIndexCoreAsync(
        bool createWhenMissing,
        CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> accounts = AccountDocuments;
        IReadOnlyList<BsonDocument> indexes = await ReadIndexesAsync(accounts, cancellationToken);
        BsonDocument? usernameIndex = FindUsernameIndex(indexes);
        if (usernameIndex is not null)
        {
            if (!IsCompatibleUsernameIndex(usernameIndex))
            {
                throw new MongoPersistenceException(MongoPersistenceFailureKind.IndexConflict);
            }

            return;
        }

        if (!createWhenMissing)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.IndexMissing);
        }

        var index = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("Username"),
            new CreateIndexOptions { Name = "Username_1", Unique = true });
        await accounts.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);

        indexes = await ReadIndexesAsync(accounts, cancellationToken);
        usernameIndex = FindUsernameIndex(indexes);
        if (usernameIndex is null || !IsCompatibleUsernameIndex(usernameIndex))
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.IndexConflict);
        }
    }

    public void Dispose() => _client?.Dispose();

    private async Task MeasureAsync(string operation, Func<Task> action)
    {
        long start = MongoPersistenceMetrics.Start();
        try
        {
            await action();
            _metrics.Complete(operation, start);
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
            exception is MongoException or MongoPersistenceException or BsonException or TimeoutException)
        {
            _metrics.Complete(
                operation,
                start,
                MongoPersistenceErrorClassifier.Classify(exception));
            throw;
        }
    }

    private static bool IsCompatibleUsernameIndex(BsonDocument index)
    {
        if (!index.GetValue("unique", false).ToBoolean()
            || index.GetValue("sparse", false).ToBoolean()
            || index.Contains("partialFilterExpression"))
        {
            return false;
        }

        if (index.TryGetValue("collation", out BsonValue? collationValue)
            && collationValue.BsonType == BsonType.Document
            && (!collationValue.AsBsonDocument.TryGetValue("locale", out BsonValue? locale)
                || locale.AsString != "simple"))
        {
            return false;
        }

        return true;
    }

    private static async Task<IReadOnlyList<BsonDocument>> ReadIndexesAsync(
        IMongoCollection<BsonDocument> collection,
        CancellationToken cancellationToken)
    {
        using IAsyncCursor<BsonDocument> cursor =
            await collection.Indexes.ListAsync(cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    private static BsonDocument? FindUsernameIndex(IEnumerable<BsonDocument> indexes)
    {
        foreach (BsonDocument index in indexes)
        {
            if (!index.TryGetValue("key", out BsonValue? keysValue)
                || keysValue.BsonType != BsonType.Document)
            {
                continue;
            }

            BsonDocument keys = keysValue.AsBsonDocument;
            if (keys.ElementCount == 1
                && keys.TryGetValue("Username", out BsonValue? direction)
                && direction.IsNumeric
                && direction.ToDouble() == 1)
            {
                return index;
            }
        }

        return null;
    }

    private static void ValidateEnabledSettings(MongoPersistenceSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString)
            || string.IsNullOrWhiteSpace(settings.DatabaseName)
            || string.IsNullOrWhiteSpace(settings.AccountCollectionName)
            || settings.ServerSelectionTimeoutSeconds < 1
            || settings.ConnectTimeoutSeconds < 1
            || settings.OperationTimeoutSeconds < 1
            || settings.MinConnectionPoolSize < 0
            || settings.MaxConnectionPoolSize < 1
            || settings.MinConnectionPoolSize > settings.MaxConnectionPoolSize)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.Configuration);
        }
    }
}
