using InfiniteSky.Application.Authentication;

using Microsoft.Extensions.Logging.Abstractions;

using MongoDB.Bson;
using MongoDB.Driver;

using Testcontainers.MongoDb;

namespace InfiniteSky.Persistence.Tests;

public sealed class MongoAccountAuthenticationRepositoryIntegrationTests
{
    [MongoIntegrationFact]
    public async Task MongoRepositoryReadsAndUpdatesLegacyAccountDocuments()
    {
        await using var container = new MongoDbBuilder("mongo:7.0").Build();
        await container.StartAsync();

        string databaseName = $"infinitesky_test_{Guid.NewGuid():N}";
        string collectionName = "account_mongooses";
        var client = new MongoClient(container.GetConnectionString());
        IMongoDatabase database = client.GetDatabase(databaseName);
        IMongoCollection<BsonDocument> accounts =
            database.GetCollection<BsonDocument>(collectionName);
        IMongoCollection<BsonDocument> guildDocuments =
            database.GetCollection<BsonDocument>("guilds");
        var settings = new MongoPersistenceSettings
        {
            Enabled = true,
            ConnectionString = container.GetConnectionString(),
            DatabaseName = databaseName,
            AccountCollectionName = collectionName,
            OperationTimeoutSeconds = 15,
            MaxConnectionPoolSize = 10,
        };
        var healthState = new MongoPersistenceHealthState(TimeProvider.System);
        using var metrics = new MongoPersistenceMetrics(healthState);
        var context = new MongoPersistenceContext(settings, metrics);

        await context.PingAsync(CancellationToken.None);
        await context.VerifyAccountUsernameIndexAsync(
            createWhenMissing: true,
            CancellationToken.None);

        var accountDocument = new BsonDocument
        {
            { "_id", 17 },
            { "Username", "LegacyFixture" },
            { "Password", "fixture-only-password" },
            { "active", 0 },
            { "UnmappedLegacyField", new BsonDocument("keep", true) },
        };
        await accounts.InsertOneAsync(accountDocument);
        await guildDocuments.InsertOneAsync(new BsonDocument
        {
            { "Name", "IntegrationGuild" },
            { "Level", 2 },
            { "hasGuildEmblem", true },
            {
                "Members",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "Name", "GuildLeader" },
                        { "LeaderFlag", 2 },
                    },
                }
            },
        });

        var repository = new MongoAccountAuthenticationRepository(
            context,
            healthState,
            metrics,
            NullLogger<MongoAccountAuthenticationRepository>.Instance);
        var guildRepository = new MongoGuildRepository(
            context,
            healthState,
            metrics,
            NullLogger<MongoGuildRepository>.Instance);
        IReadOnlyList<InfiniteSky.Domain.Guilds.Guild> guilds =
            await guildRepository.LoadAllAsync(CancellationToken.None);
        Assert.Equal("IntegrationGuild", Assert.Single(guilds).Name);

        AccountAuthenticationRecord? account = await repository.FindByUsernameAsync(
            "LegacyFixture",
            CancellationToken.None);

        Assert.NotNull(account);
        Assert.Equal(17, account.AccountId.Value);
        Assert.Equal("LegacyFixture", account.Username);
        Assert.Equal(
            System.Text.Encoding.UTF8.GetBytes("fixture-only-password"),
            account.LegacyPasswordUtf8);

        await repository.SetActiveAsync(
            account.AccountId,
            active: true,
            CancellationToken.None);
        BsonDocument stored = await accounts.Find(
            Builders<BsonDocument>.Filter.Eq("_id", 17)).SingleAsync();
        Assert.Equal(BsonType.Int32, stored["active"].BsonType);
        Assert.Equal(1, stored["active"].AsInt32);
        Assert.Equal(new BsonDocument("keep", true), stored["UnmappedLegacyField"]);

        MongoWriteException duplicateKey = await Assert.ThrowsAsync<MongoWriteException>(
            async () => await accounts.InsertOneAsync(new BsonDocument
            {
                { "_id", 18 },
                { "Username", "LegacyFixture" },
                { "Password", "another-fixture-password" },
            }));
        Assert.Equal(
            MongoPersistenceFailureKind.DuplicateKey,
            MongoPersistenceErrorClassifier.Classify(duplicateKey));

        await repository.ResetAllActiveAsync(CancellationToken.None);
        stored = await accounts.Find(Builders<BsonDocument>.Filter.Eq("_id", 17)).SingleAsync();
        Assert.Equal(0, stored["active"].AsInt32);

        Assert.Null(await repository.FindByUsernameAsync("not-present", CancellationToken.None));

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await repository.FindByUsernameAsync(
                "LegacyFixture",
                cancelled.Token));
    }
}
