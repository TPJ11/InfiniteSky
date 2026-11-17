namespace InfiniteSky.Domain.Definitions;

/// <summary>
/// Named modifier values in one of the two original skill data blocks.
/// </summary>
public sealed record SkillModifiers
{
    public double ChiCost { get; init; }
    public double DegreeOfDefensiveSkill { get; init; }
    public double ChiRecovery { get; init; }
    public double ChanceToAcupressure { get; init; }
    public double ChanceToUnstun { get; init; }
    public double AirWalkDistance { get; init; }
    public double EnergyBall { get; init; }
    public double DamageIncreased { get; init; }
    public double Unknown1 { get; init; }
    public double AttackRangeApplied { get; init; }
    public double DamageApplied { get; init; }
    public double OnlyForLightDamage { get; init; }
    public double OnlyForShadowDamage { get; init; }
    public double OnlyForDarkDamage { get; init; }
    public double ChanceToHitApplied { get; init; }
    public double EffectiveDuration { get; init; }
    public double IncreasedDamage { get; init; }
    public double IncreasedDefense { get; init; }
    public double Unknown2 { get; init; }
    public double CastTime { get; init; }
    public double IncreasedLightResistance { get; init; }
    public double IncreasedShadowResistance { get; init; }
    public double IncreasedDarkResistance { get; init; }
    public double IncreasedChanceToHit { get; init; }
    public double IncreasedChanceToDodge { get; init; }
    public double IncreasedMovementSpeed { get; init; }
    public double IncreasedAttackSpeed { get; init; }
    public double IncreasedLuck { get; init; }
    public double EnchancedChanceToDeadlyBlow { get; init; }
    public double ChanceToReturnDamage { get; init; }
    public double IncreasedAcupressureDefense { get; init; }
    public double ChanceToRemoveIncreaseEffect { get; init; }
    public double HealthRegenerationPoints { get; init; }
    public double ChiRegenerationPoints { get; init; }
}
