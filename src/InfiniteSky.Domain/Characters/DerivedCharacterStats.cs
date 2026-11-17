namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Runtime derived combat values produced by the original stat calculation module.
/// </summary>
public sealed record DerivedCharacterStats
{
    public double Damage { get; init; }
    public double Defense { get; init; }
    public double HitRate { get; init; }
    public double Dodge { get; init; }
    public double MaxHealth { get; init; }
    public double MaxChi { get; init; }
    public double Luck { get; init; }
    public double DeadlyRate { get; init; }
    public double ElementalDamage { get; init; }
    public double LightResistance { get; init; }
    public double ShadowResistance { get; init; }
    public double DarkResistance { get; init; }
}
