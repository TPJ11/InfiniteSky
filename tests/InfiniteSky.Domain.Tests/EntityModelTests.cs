using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Definitions;
using InfiniteSky.Domain.Entities;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Items;

namespace InfiniteSky.Domain.Tests;

public sealed class EntityModelTests
{
    [Fact]
    public void MonsterUsesDefinitionIdentityAndHealthDefaults()
    {
        var definition = new MonsterDefinition
        {
            Id = new MonsterDefinitionId(7),
            Name = "Monster",
            Health = 100,
            WalkSpeed = 2,
            RunSpeed = 4,
        };
        var monster = new MonsterEntity(new EntityId(10), definition);

        Assert.Equal((uint)7, monster.State.OtherId);
        Assert.Equal(100, monster.State.Health);
        Assert.Equal(monster.State.Health, monster.MaximumHealth);
        Assert.Equal(1, monster.State.Life);
    }

    [Fact]
    public void NpcUsesOriginalRuntimeHealthDefault()
    {
        var definition = new NpcDefinition
        {
            Id = new NpcDefinitionId(50),
            Name = "NPC",
            PageCount = 2,
            Chat1 = "Hello",
        };
        var npc = new NpcEntity(new EntityId(11), definition);

        Assert.Equal(1, npc.State.Health);
        Assert.Equal("Hello", npc.Definition.Chat1);
        Assert.NotSame(definition, npc);
    }

    [Fact]
    public void WorldItemUsesSourceSpawnDefaults()
    {
        var definition = new ItemDefinition
        {
            Id = new ItemDefinitionId(2),
            Name = "Potion",
            ItemType = ItemType.Common,
        };
        var item = new WorldItemEntity(new EntityId(12), definition);

        Assert.Equal(1, item.Amount);
        Assert.Equal(1, item.State.Life);
        Assert.Equal(1, item.JustSpawned);
        Assert.Equal(string.Empty, item.OwnerName);
    }

    [Fact]
    public void RuntimeCharacterUsesNullForOriginalNegativeTargetSentinels()
    {
        var state = new CharacterRuntimeState();

        Assert.Null(state.AccountId);
        Assert.Null(state.CharacterId);
        Assert.Null(state.UniqueId);
        Assert.Null(state.TargetObjectIndex);
        Assert.Null(state.TargetObjectUniqueNumber);
        Assert.Equal(0, state.ProposedLocation.X);
    }

    [Fact]
    public void EntityInstancesUseIdentityRatherThanValueEquality()
    {
        var definition = new NpcDefinition
        {
            Id = new NpcDefinitionId(1),
            Name = "NPC",
        };

        var first = new NpcEntity(new EntityId(1), definition);
        var second = new NpcEntity(new EntityId(1), definition);

        Assert.NotSame(first, second);
        Assert.NotEqual(first, second);
    }
}
