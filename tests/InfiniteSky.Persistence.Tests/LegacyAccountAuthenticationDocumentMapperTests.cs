using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace InfiniteSky.Persistence.Tests;

public sealed class LegacyAccountAuthenticationDocumentMapperTests
{
    [Fact]
    public void MapsLegacyFieldsWithoutChangingTheRawBsonDocument()
    {
        BsonDocument document = ReadFixture("legacy-account-document.json");
        byte[] before = document.ToBson();

        var account = LegacyAccountAuthenticationDocumentMapper.Map(document);

        Assert.Equal(17, account.AccountId.Value);
        Assert.Equal("LegacyFixture", account.Username);
        Assert.Equal(0, account.AccountLevel);
        Assert.Equal(
            Encoding.UTF8.GetBytes("fixture-only-password"),
            account.LegacyPasswordUtf8);
        Assert.Equal(BsonType.Int32, document["_id"].BsonType);
        Assert.Equal(BsonType.Int32, document["active"].BsonType);
        Assert.Equal(BsonType.DateTime, document["LastLogin"].BsonType);
        Assert.Equal(BsonType.Null, document["CharacterIndividuals"].BsonType);
        Assert.Equal(
            before,
            BsonSerializer.Deserialize<BsonDocument>(document.ToBson()).ToBson());
    }

    [Fact]
    public void MissingAndNullPasswordFieldsRemainAbsentCredentials()
    {
        var missing = new BsonDocument
        {
            { "_id", new BsonInt64(18) },
            { "Username", "MissingPassword" },
        };
        var nullPassword = new BsonDocument
        {
            { "_id", new BsonDouble(19d) },
            { "Username", "NullPassword" },
            { "Password", BsonNull.Value },
        };

        var missingAccount = LegacyAccountAuthenticationDocumentMapper.Map(missing);
        var nullAccount = LegacyAccountAuthenticationDocumentMapper.Map(nullPassword);

        Assert.Null(missingAccount.LegacyPasswordUtf8);
        Assert.Null(nullAccount.LegacyPasswordUtf8);
        Assert.Equal(18, missingAccount.AccountId.Value);
        Assert.Equal(19, nullAccount.AccountId.Value);
    }

    [Fact]
    public void MapsLegacyAccountLevelAcrossNumericBsonTypesAndDefaultsMissingOrNullToZero()
    {
        var int32Level = new BsonDocument
        {
            { "_id", 20 },
            { "Username", "Int32Level" },
            { "Level", new BsonInt32(12) },
        };
        var int64Level = new BsonDocument
        {
            { "_id", 21 },
            { "Username", "Int64Level" },
            { "Level", new BsonInt64(80) },
        };
        var doubleLevel = new BsonDocument
        {
            { "_id", 22 },
            { "Username", "DoubleLevel" },
            { "Level", new BsonDouble(80.5) },
        };
        var missingLevel = new BsonDocument
        {
            { "_id", 23 },
            { "Username", "MissingLevel" },
        };
        var nullLevel = new BsonDocument
        {
            { "_id", 24 },
            { "Username", "NullLevel" },
            { "Level", BsonNull.Value },
        };

        Assert.Equal(12, LegacyAccountAuthenticationDocumentMapper.Map(int32Level).AccountLevel);
        Assert.Equal(80, LegacyAccountAuthenticationDocumentMapper.Map(int64Level).AccountLevel);
        Assert.Equal(80.5, LegacyAccountAuthenticationDocumentMapper.Map(doubleLevel).AccountLevel);
        Assert.Equal(0, LegacyAccountAuthenticationDocumentMapper.Map(missingLevel).AccountLevel);
        Assert.Equal(0, LegacyAccountAuthenticationDocumentMapper.Map(nullLevel).AccountLevel);
    }

    [Theory]
    [InlineData(BsonType.String)]
    [InlineData(BsonType.Boolean)]
    public void RejectsNonNumericAccountLevel(BsonType levelType)
    {
        var document = new BsonDocument
        {
            { "_id", 25 },
            { "Username", "MalformedLevel" },
            {
                "Level",
                levelType == BsonType.String
                    ? BsonString.Create("80")
                    : BsonBoolean.Create(true)
            },
        };

        MongoPersistenceException exception =
            Assert.Throws<MongoPersistenceException>(
                () => LegacyAccountAuthenticationDocumentMapper.Map(document));

        Assert.Equal(MongoPersistenceFailureKind.MalformedDocument, exception.FailureKind);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsNonFiniteAccountLevel(double level)
    {
        var document = new BsonDocument
        {
            { "_id", 26 },
            { "Username", "MalformedLevel" },
            { "Level", new BsonDouble(level) },
        };

        MongoPersistenceException exception =
            Assert.Throws<MongoPersistenceException>(
                () => LegacyAccountAuthenticationDocumentMapper.Map(document));

        Assert.Equal(MongoPersistenceFailureKind.MalformedDocument, exception.FailureKind);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.5)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(9_223_372_036_854_775_808d)]
    public void RejectsInvalidNumericAccountIdentifiers(double value)
    {
        var document = new BsonDocument
        {
            { "_id", new BsonDouble(value) },
            { "Username", "Malformed" },
        };

        MongoPersistenceException exception =
            Assert.Throws<MongoPersistenceException>(
                () => LegacyAccountAuthenticationDocumentMapper.Map(document));

        Assert.Equal(MongoPersistenceFailureKind.MalformedDocument, exception.FailureKind);
    }

    [Fact]
    public void RejectsUnexpectedCredentialTypes()
    {
        var document = new BsonDocument
        {
            { "_id", 1 },
            { "Username", "Malformed" },
            { "Password", 123 },
        };

        Assert.Throws<MongoPersistenceException>(
            () => LegacyAccountAuthenticationDocumentMapper.Map(document));
    }

    [Fact]
    public void ClassifiesCancellationAuthenticationAndDuplicateFailures()
    {
        Assert.Equal(
            MongoPersistenceFailureKind.OperationCancelled,
            MongoPersistenceErrorClassifier.Classify(new OperationCanceledException()));
        Assert.Equal(
            MongoPersistenceFailureKind.Authentication,
            MongoPersistenceErrorClassifier.Classify(
                new MongoPersistenceException(MongoPersistenceFailureKind.Authentication)));
        Assert.Equal(
            MongoPersistenceFailureKind.Serialization,
            MongoPersistenceErrorClassifier.Classify(
                new BsonSerializationException("fixture")));
    }

    [Fact]
    public async Task RepositoryLogsDoNotIncludeUsernamePasswordOrConnectionString()
    {
        var logger = new RecordingLogger<MongoAccountAuthenticationRepository>();
        var settings = new MongoPersistenceSettings
        {
            Enabled = false,
            ConnectionString = "mongodb://sensitive-user:sensitive-password@localhost/ts1",
        };
        var healthState = new MongoPersistenceHealthState(TimeProvider.System);
        using var metrics = new MongoPersistenceMetrics(healthState);
        var context = new MongoPersistenceContext(settings, metrics);
        var repository = new MongoAccountAuthenticationRepository(
            context,
            healthState,
            metrics,
            logger);

        InfiniteSky.Application.Authentication.AccountRepositoryException exception =
            await Assert.ThrowsAsync<InfiniteSky.Application.Authentication.AccountRepositoryException>(
            async () => await repository.FindByUsernameAsync(
                "SensitiveUsername",
                CancellationToken.None));

        Assert.Equal(
            InfiniteSky.Application.Authentication.AccountRepositoryFailureKind.Disabled,
            exception.FailureKind);
        string messages = string.Join(Environment.NewLine, logger.Messages);
        Assert.DoesNotContain("SensitiveUsername", messages, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-password", messages, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-user", messages, StringComparison.Ordinal);
    }

    private static BsonDocument ReadFixture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return BsonDocument.Parse(File.ReadAllText(path));
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NullLogger<T>.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
