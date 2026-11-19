using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;

using MongoDB.Bson;

namespace InfiniteSky.Persistence.Tests;

public sealed class MongoCharacterDocumentMapperTests
{
    [Fact]
    public void MapsSupportedLegacyCharacterFieldsAndLeavesRawBsonUntouched()
    {
        BsonDocument document = ReadFixture("legacy-character-document.json");
        byte[] before = document.ToBson();

        Character character = MongoCharacterDocumentMapper.Map(document);

        Assert.Equal(107, character.Id.Value);
        Assert.Equal(17, character.AccountId.Value);
        Assert.Equal("Development", character.ServerName);
        Assert.Equal("PlayerFixture", character.Name);
        Assert.Equal(Clan.Jinong, character.Appearance.Clan);
        Assert.Equal(Gender.Female, character.Appearance.Gender);
        Assert.Equal(12, character.Progression.Level);
        Assert.Equal(1234, character.Progression.Experience);
        Assert.Equal(11, character.Progression.ContributionPoints);
        Assert.Equal(12, character.Attributes.Strength);
        Assert.Equal(13, character.Attributes.Chi);
        Assert.Equal(50000, character.Economy.Silver);
        Assert.Equal(800.5, character.Vitals.Health);
        Assert.Null(character.Vitals.Chi);
        Assert.Equal(new ZoneId(1), character.SavedLocation.ZoneId);
        Assert.Equal(-630.7, character.SavedLocation.X);
        Assert.Equal(-815.9, character.SavedLocation.Z);
        Assert.Equal(103u, character.Equipment.Armor!.ItemId.Value);
        Assert.Equal(2, character.Equipment.Armor.Combine);
        Assert.Equal(901u, character.Equipment.Pet!.ItemId.Value);
        Assert.Equal(2, character.Inventory.Count);
        Assert.Null(character.Inventory[1]);
        Assert.Equal(4, character.QuickUseItems[0]!.Amount);
        Assert.Equal(5, character.QuickUseItems[1]!.Amount);
        Assert.Equal("FriendFixture", Assert.Single(character.Friends));
        Assert.Equal(3, character.Deaths);
        Assert.Equal(before, document.ToBson());
    }

    [Fact]
    public void MissingOptionalLegacyFieldsRemainOptional()
    {
        var document = new BsonDocument
        {
            { "_id", 1 },
            { "AccountID", 2 },
            { "Name", "Minimal" },
        };

        Character character = MongoCharacterDocumentMapper.Map(document);

        Assert.Equal(1, character.Progression.Level);
        Assert.Null(character.ServerName);
        Assert.Null(character.SavedLocation.ZoneId);
        Assert.Null(character.SavedLocation.Position);
        Assert.Null(character.Vitals.Health);
        Assert.Empty(character.Inventory);
        Assert.Empty(character.SkillBar);
    }

    [Fact]
    public void RejectsMalformedIdentityNumbersAndPartialInventoryCoordinates()
    {
        var fractionalId = new BsonDocument
        {
            { "_id", 1.5 },
            { "AccountID", 2 },
            { "Name", "Malformed" },
        };
        var partialPosition = new BsonDocument
        {
            { "_id", 1 },
            { "AccountID", 2 },
            { "Name", "Malformed" },
            {
                "Inventory",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "ID", 2 },
                        { "Column", 1 },
                    },
                }
            },
        };

        MongoPersistenceException badId = Assert.Throws<MongoPersistenceException>(
            () => MongoCharacterDocumentMapper.Map(fractionalId));
        MongoPersistenceException badPosition = Assert.Throws<MongoPersistenceException>(
            () => MongoCharacterDocumentMapper.Map(partialPosition));

        Assert.Equal(MongoPersistenceFailureKind.MalformedDocument, badId.FailureKind);
        Assert.Equal(MongoPersistenceFailureKind.MalformedDocument, badPosition.FailureKind);
    }

    private static BsonDocument ReadFixture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return BsonDocument.Parse(File.ReadAllText(path));
    }
}
