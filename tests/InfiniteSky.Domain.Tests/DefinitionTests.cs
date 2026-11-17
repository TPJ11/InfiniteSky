using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Constants;
using InfiniteSky.Domain.Definitions;
using InfiniteSky.Domain.Guilds;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Items;
using InfiniteSky.Domain.Skills;
using InfiniteSky.Domain.Values;
using InfiniteSky.Domain.Zones;

namespace InfiniteSky.Domain.Tests;

public sealed class DefinitionTests
{
    [Fact]
    public void ItemEnumValuesMatchOriginalItemTypeConstants()
    {
        Assert.Equal(1, (int)ItemType.SilverCoins);
        Assert.Equal(6, (int)ItemType.CalabashBottle);
        Assert.Equal(13, (int)ItemType.Sword);
        Assert.Equal(21, (int)ItemType.Scepter);
        Assert.Equal(22, (int)ItemType.Pet);
        Assert.Equal(24, (int)ItemType.AssistAgain);
    }

    [Fact]
    public void RarityEquipmentSkillAndGuildValuesArePreserved()
    {
        Assert.Equal(0, (int)Clan.Guanyin);
        Assert.Equal(1, (int)Clan.Fujin);
        Assert.Equal(2, (int)Clan.Jinong);
        Assert.Equal(0, (int)Gender.Male);
        Assert.Equal(1, (int)Gender.Female);
        Assert.Equal(0, (int)ItemRarity.Common);
        Assert.Equal(4, (int)ItemRarity.Elite);
        Assert.Equal(0, (int)EquipmentSlot.Amulet);
        Assert.Equal(8, (int)EquipmentSlot.Pet);
        Assert.Equal(2, (int)SkillCategory.Attack);
        Assert.Equal(0, (int)GuildMemberRole.Member);
        Assert.Equal(2, (int)GuildMemberRole.Master);
    }

    [Fact]
    public void SkillModifiersPreservePostLoadFractionalValues()
    {
        // Synthetic values verify the post-load fractional representation, not game-balance values.
        var modifiers = new SkillModifiers
        {
            ChiCost = 12.5,
            EffectiveDuration = 1.25,
            IncreasedDamage = -0.5,
        };
        var skill = new SkillDefinition
        {
            Id = new SkillDefinitionId(1),
            Name = "Skill",
            Category = SkillCategory.General,
            ModifiersStart = modifiers,
            MaximumLevel = 10,
        };

        Assert.Equal(12.5, skill.ModifiersStart.ChiCost);
        Assert.Equal(1.25, skill.ModifiersStart.EffectiveDuration);
        Assert.Equal(-0.5, skill.ModifiersStart.IncreasedDamage);
    }

    [Fact]
    public void MonsterDefinitionPreservesSignedAndUnsignedSourceFields()
    {
        // Synthetic signed values verify lossless representation of the int32 source declaration.
        var monster = new MonsterDefinition
        {
            Id = new MonsterDefinitionId(100),
            Name = "Monster",
            Level = 145,
            Health = 10_000,
            AttackPower = -2,
            DefensePower = 30,
            WalkSpeed = -1,
        };

        Assert.Equal((uint)145, monster.Level);
        Assert.Equal((uint)10_000, monster.Health);
        Assert.Equal(-2, monster.AttackPower);
        Assert.Equal(-1, monster.WalkSpeed);
    }

    [Fact]
    public void SpawnAndRegionRepresentOriginalRecordFields()
    {
        var spawn = new SpawnDefinition(
            9,
            100,
            new Position3(-1.5, 2.5, 3.5),
            359.5);
        var region = new WorldRegion(
            0,
            new ZoneId(6),
            1,
            2,
            -100,
            50,
            300,
            25);

        Assert.Equal((uint)9, spawn.UniqueId);
        Assert.Equal(359.5, spawn.Direction);
        Assert.Equal(-100, region.X);
        Assert.Equal(25, region.Radius);
    }

    [Fact]
    public void LegacyCollectionSizesMatchSourceConstants()
    {
        Assert.Equal(3, LegacyCollectionSizes.CharacterSlots);
        Assert.Equal(64, LegacyCollectionSizes.InventoryItems);
        Assert.Equal(28, LegacyCollectionSizes.PrivateStorageItems);
        Assert.Equal(56, LegacyCollectionSizes.BankItems);
        Assert.Equal(4, LegacyCollectionSizes.QuickUseItems);
        Assert.Equal(30, LegacyCollectionSizes.SkillListEntries);
        Assert.Equal(24, LegacyCollectionSizes.SkillBarEntries);
        Assert.Equal(10, LegacyCollectionSizes.FriendSlots);
    }

    [Fact]
    public void ExperienceAndPetDefinitionsPreserveSourceValues()
    {
        var experience = new ExperienceDefinition
        {
            Level = 1,
            ExperienceStart = 0,
            ExperienceEnd = 221,
            SkillPoint = 1,
            GuanyinHealth = 500,
            FujinHealth = 450,
            JinongHealth = 550,
        };
        var petStats = new PetStatistics
        {
            Damage = 1_000,
            HitRate = 1_000,
            MaximumGrowth = 240_000_000,
        };
        var item = new ItemDefinition
        {
            Id = new ItemDefinitionId(9_800),
            Name = "Pet",
            ItemType = ItemType.Pet,
            PetStatistics = petStats,
        };

        Assert.Equal((uint)221, experience.ExperienceEnd);
        Assert.Equal((uint)500, experience.GuanyinHealth);
        Assert.Same(petStats, item.PetStatistics);
        Assert.Equal(240_000_000, item.PetStatistics.MaximumGrowth);
    }
}
