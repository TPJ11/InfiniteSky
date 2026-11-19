using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

using Microsoft.Extensions.Logging.Abstractions;

using MongoDB.Bson;
using MongoDB.Driver;

using Testcontainers.MongoDb;

namespace InfiniteSky.Persistence.Tests;

public sealed class MongoCharacterRepositoryIntegrationTests
{
    [MongoIntegrationFact]
    public async Task RepositoryEnforcesOwnershipAndUpdatesOnlyLegacyWorldLocationFields()
    {
        await using var container = new MongoDbBuilder("mongo:7.0").Build();
        await container.StartAsync();

        string databaseName = $"infinitesky_characters_{Guid.NewGuid():N}";
        string collectionName = "character_mongooses";
        var client = new MongoClient(container.GetConnectionString());
        IMongoCollection<BsonDocument> documents =
            client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionName);
        var settings = new MongoPersistenceSettings
        {
            Enabled = true,
            ConnectionString = container.GetConnectionString(),
            DatabaseName = databaseName,
            CharacterCollectionName = collectionName,
            OperationTimeoutSeconds = 15,
            MaxConnectionPoolSize = 10,
        };
        var healthState = new MongoPersistenceHealthState(TimeProvider.System);
        using var metrics = new MongoPersistenceMetrics(healthState);
        using var context = new MongoPersistenceContext(settings, metrics);
        await context.PingAsync(CancellationToken.None);

        var source = new BsonDocument
        {
            { "_id", new BsonDouble(107) },
            { "AccountID", new BsonDouble(17) },
            { "ServerName", "Development" },
            { "Name", "PlayerFixture" },
            { "MapID", new BsonDouble(1) },
            { "RealX", new BsonDouble(-630) },
            { "RealY", new BsonDouble(50) },
            { "RealZ", new BsonDouble(-815) },
            { "Health", new BsonDouble(800.5) },
            { "UnmappedLegacyField", new BsonDocument("keep", true) },
        };
        await documents.InsertOneAsync(source);
        var repository = new MongoCharacterRepository(
            context,
            healthState,
            metrics,
            NullLogger<MongoCharacterRepository>.Instance);

        var accountCharacters = await repository.GetForAccountAsync(
            new AccountId(17),
            "Development",
            CancellationToken.None);
        Assert.Equal("PlayerFixture", Assert.Single(accountCharacters).Name);
        Assert.Null(await repository.GetOwnedAsync(
            new CharacterId(107),
            new AccountId(18),
            "Development",
            CancellationToken.None));
        Assert.NotNull(await repository.GetOwnedAsync(
            new CharacterId(107),
            new AccountId(17),
            "Development",
            CancellationToken.None));

        await repository.SaveWorldLocationAsync(
            new CharacterId(107),
            new AccountId(17),
            "Development",
            new ZoneId(4),
            new Position3(-630.7, 50.2, -815.9),
            CancellationToken.None);

        BsonDocument stored = await documents.Find(
            Builders<BsonDocument>.Filter.Eq("_id", new BsonDouble(107))).SingleAsync();
        Assert.Equal(BsonType.Double, stored["MapID"].BsonType);
        Assert.Equal(4d, stored["MapID"].AsDouble);
        Assert.Equal(-630d, stored["RealX"].AsDouble);
        Assert.Equal(50d, stored["RealY"].AsDouble);
        Assert.Equal(-815d, stored["RealZ"].AsDouble);
        Assert.Equal(800.5d, stored["Health"].AsDouble);
        Assert.Equal(new BsonDocument("keep", true), stored["UnmappedLegacyField"]);

        MongoPersistenceException notFound = await Assert.ThrowsAsync<MongoPersistenceException>(
            async () => await repository.SaveWorldLocationAsync(
                new CharacterId(108),
                new AccountId(17),
                "Development",
                new ZoneId(4),
                Position3.Zero,
                CancellationToken.None));
        Assert.Equal(MongoPersistenceFailureKind.CharacterNotFound, notFound.FailureKind);
    }
}
