using InfiniteSky.Domain.Accounts;
using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Guilds;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Items;
using InfiniteSky.Domain.Skills;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.Domain.Tests;

public sealed class CharacterModelTests
{
    [Fact]
    public void CharacterUsesSourceBackedMongooseDefaults()
    {
        var character = new Character(new CharacterId(1), new AccountId(1), "Player");

        Assert.Equal(1, character.Progression.Level);
        Assert.Equal(0, character.Progression.Experience);
        Assert.Null(character.GuildName);
        Assert.Null(character.Equipment.Weapon);
        Assert.Null(character.Equipment.Pet);
        Assert.Empty(character.Inventory);
        Assert.Empty(character.SkillList);
    }

    [Fact]
    public void CharacterCanRepresentGuanyinStartingTemplate()
    {
        var character = new Character(new CharacterId(1), new AccountId(10), "GuanyinStart")
        {
            Appearance = new CharacterAppearance
            {
                Clan = Clan.Guanyin,
                Gender = Gender.Male,
                Hair = 0,
                Face = 0,
            },
            Attributes = new CharacterAttributes
            {
                Strength = 1,
                Chi = 1,
                Dexterity = 1,
                Vitality = 1,
            },
            Economy = new CharacterEconomy { Silver = 50_000 },
            SavedLocation = new SavedCharacterLocation
            {
                ZoneId = new ZoneId(1),
                X = -630,
                Y = 50,
                Z = -815,
            },
        };

        character.SkillList.Add(new SkillEntry(new SkillDefinitionId(1), 1));
        character.SkillList.Add(new SkillEntry(new SkillDefinitionId(2), 1));
        character.Inventory.Add(
            new InventoryItem
            {
                ItemId = new ItemDefinitionId(2),
                Amount = 99,
                Position = new GridPosition(0, 0),
            });

        Assert.Equal(Clan.Guanyin, character.Appearance.Clan);
        Assert.Equal(new Position3(-630, 50, -815), character.SavedLocation.Position);
        Assert.Equal(50_000, character.Economy.Silver);
        Assert.Equal([new SkillEntry(new SkillDefinitionId(1), 1), new SkillEntry(new SkillDefinitionId(2), 1)], character.SkillList);
    }

    [Fact]
    public void CollectionsPreserveOrderingAndEmptySlots()
    {
        var character = new Character(new CharacterId(1), new AccountId(1), "Slots");
        var first = new InventoryItem { ItemId = new ItemDefinitionId(2), Amount = 1 };
        var second = new InventoryItem { ItemId = new ItemDefinitionId(26), Amount = 2 };

        character.Inventory.Add(first);
        character.Inventory.Add(null);
        character.Inventory.Add(second);

        Assert.Equal([first, null, second], character.Inventory);
        Assert.Null(character.Inventory[1]);
    }

    [Fact]
    public void SavedLocationPreservesPartialCoordinates()
    {
        var partial = new SavedCharacterLocation
        {
            ZoneId = new ZoneId(6),
            X = 1,
            Z = 3,
        };

        Assert.Equal(1, partial.X);
        Assert.Null(partial.Y);
        Assert.Null(partial.Position);
    }

    [Fact]
    public void CharacterEntitiesUseReferenceIdentity()
    {
        var first = new Character(new CharacterId(1), new AccountId(1), "Same");
        var second = new Character(new CharacterId(1), new AccountId(1), "Same");

        Assert.NotEqual(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void AccountAndPerServerBankUseExplicitNullSemantics()
    {
        var account = new Account(new AccountId(1), "Account")
        {
            CharacterIndividuals = new Dictionary<string, AccountServerData>(),
        };

        Assert.Equal("0000", account.Pin);
        Assert.Empty(account.CharacterIndividuals);
        Assert.Null(new Account(new AccountId(2), "Missing").CharacterIndividuals);
    }

    [Fact]
    public void EquipmentPreservesNormalAndPetVariants()
    {
        var equipment = new CharacterEquipment
        {
            Weapon = new EquipmentItem
            {
                ItemId = new ItemDefinitionId(100),
                Enchant = 3,
                Combine = 2,
            },
            Pet = new PetEquipment
            {
                ItemId = new ItemDefinitionId(9_800),
                Activity = 100,
                Growth = 240_000_000,
            },
        };

        Assert.Equal(3, equipment.Weapon.Enchant);
        Assert.Equal(2, equipment.Weapon.Combine);
        Assert.Equal(240_000_000, equipment.Pet.Growth);
    }

    [Fact]
    public void GuildMemberOrderAndRolesUseSourceValues()
    {
        var guild = new Guild("Guild");
        guild.Members.Add(new GuildMember("Master", GuildMemberRole.Master));
        guild.Members.Add(new GuildMember("Member", GuildMemberRole.Member));

        Assert.Equal(
            ["Master", "Member"],
            guild.Members.Select(member => Assert.IsType<GuildMember>(member).Name));
        Assert.Equal(GuildMemberRole.Master, Assert.IsType<GuildMember>(guild.Members[0]).Role);
    }
}
