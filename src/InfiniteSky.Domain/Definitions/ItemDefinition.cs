using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Items;

namespace InfiniteSky.Domain.Definitions;

/// <summary>
/// Named, source-supported fields from an original item information record.
/// Unknown record offsets intentionally remain absent.
/// </summary>
public sealed record ItemDefinition
{
    public required ItemDefinitionId Id { get; init; }
    public required string Name { get; init; }
    public ItemRarity Rarity { get; init; }
    public ItemType ItemType { get; init; }
    public uint DisplayItem2D { get; init; }
    public uint Level { get; init; }
    public uint Clan { get; init; }
    public uint PurchasePrice { get; init; }
    public uint SalePrice { get; init; }
    public int Capacity { get; init; }
    public int LevelRequirement { get; init; }
    public int HonorPointRequirement { get; init; }
    public int Strength { get; init; }
    public int Dexterity { get; init; }
    public int Vitality { get; init; }
    public int Chi { get; init; }
    public int Luck { get; init; }
    public int Damage { get; init; }
    public int Defense { get; init; }
    public int LightDamage { get; init; }
    public int ShadowDamage { get; init; }
    public int DarkDamage { get; init; }
    public int LightResistance { get; init; }
    public int ShadowResistance { get; init; }
    public int DarkResistance { get; init; }
    public int ChanceToHit { get; init; }
    public int ChanceToDodge { get; init; }
    public int PercentToDeadlyBlow { get; init; }
    public int SkillBonusId1 { get; init; }
    public int SkillBonusId2 { get; init; }
    public int SkillBonusId3 { get; init; }
    public int SkillBonusAmount1 { get; init; }
    public int SkillBonusAmount2 { get; init; }
    public int SkillBonusAmount3 { get; init; }
    public int ValueType { get; init; }
    public int Value1 { get; init; }
    public int Refinement { get; init; }
    public int ChanceToEarnExperienceFromFinalHit { get; init; }
    public int ExperienceBonusPercentFromFinalHit { get; init; }
    public int DecreaseChiConsumption { get; init; }
    public int DodgeDeadlyBlow { get; init; }
    public int IncreaseAllSkillMastery { get; init; }
    public string Description1 { get; init; } = string.Empty;
    public string Description2 { get; init; } = string.Empty;
    public string Description3 { get; init; } = string.Empty;
    public PetStatistics? PetStatistics { get; init; }
}
