using InfiniteSky.Domain.Guilds;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace InfiniteSky.Persistence.Tests;

public sealed class MongoGuildDocumentMapperTests
{
    [Fact]
    public void MapsLegacyGuildFieldsAndRetainsNullMemberSlots()
    {
        BsonDocument document = ReadFixture();
        byte[] before = document.ToBson();

        Guild guild = MongoGuildDocumentMapper.Map(document);

        Assert.Equal("LegacyGuild", guild.Name);
        Assert.Equal(3, guild.Level);
        Assert.True(guild.HasGuildEmblem);
        Assert.Equal(3, guild.Members.Count);
        Assert.Equal(new GuildMember("GuildMaster", GuildMemberRole.Master), guild.Members[0]);
        Assert.Null(guild.Members[1]);
        Assert.Equal(
            new GuildMember("GuildAssistant", GuildMemberRole.Assistant),
            guild.Members[2]);
        Assert.Equal(
            before,
            BsonSerializer.Deserialize<BsonDocument>(document.ToBson()).ToBson());
    }

    [Fact]
    public void AppliesOnlyDocumentedMongooseDefaultsForMissingFields()
    {
        var document = new BsonDocument("Name", "DefaultGuild");

        Guild guild = MongoGuildDocumentMapper.Map(document);

        Assert.Equal(0, guild.Level);
        Assert.False(guild.HasGuildEmblem);
        Assert.Empty(guild.Members);
    }

    [Fact]
    public void RejectsNullMemberArrayAndUnknownRoleValues()
    {
        var nullMembers = new BsonDocument
        {
            { "Name", "Malformed" },
            { "Members", BsonNull.Value },
        };
        var unknownRole = new BsonDocument
        {
            { "Name", "Malformed" },
            {
                "Members",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "Name", "Member" },
                        { "LeaderFlag", 3 },
                    },
                }
            },
        };

        AssertMalformed(nullMembers);
        AssertMalformed(unknownRole);
    }

    private static BsonDocument ReadFixture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "legacy-guild-document.json");
        return BsonDocument.Parse(File.ReadAllText(path));
    }

    private static void AssertMalformed(BsonDocument document)
    {
        MongoPersistenceException exception = Assert.Throws<MongoPersistenceException>(
            () => MongoGuildDocumentMapper.Map(document));
        Assert.Equal(MongoPersistenceFailureKind.MalformedDocument, exception.FailureKind);
    }
}
