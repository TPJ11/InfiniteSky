namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Persistent modifier values present in the original character schema.
/// </summary>
public sealed record CharacterBonuses
{
    public int StrengthBonus { get; init; }
    public int DexterityBonus { get; init; }
    public int LuckBuff { get; init; }
    public int StrengthBuff { get; init; }
    public int ExperienceBuff { get; init; }
    public int AutoPillHealth { get; init; }
    public int AutoPillChi { get; init; }
    public int ElementalDamage { get; init; }
    public int ElementalDefense { get; init; }
    public int DarkDamage { get; init; }
    public int FactionDefenseBonus { get; init; }
    public int ChanceDodgeHit { get; init; }
    public int DamageBonus { get; init; }
    public int DailyOne { get; init; }
    public int DailyPlayerVersusPlayerKills { get; init; }
    public int DailyUnknown { get; init; }
    public int DailyUnknown2 { get; init; }
}
