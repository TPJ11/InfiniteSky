using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Definitions;

/// <summary>
/// Runtime pet contribution data attached to selected item definitions.
/// </summary>
public sealed record PetStatistics
{
    public double Health { get; init; }
    public double Chi { get; init; }
    public double Damage { get; init; }
    public double Defense { get; init; }
    public double Dodge { get; init; }
    public double HitRate { get; init; }
    public double ElementalDamage { get; init; }
    public double ElementalDefense { get; init; }
    public long MaximumGrowth { get; init; } = 40_000_000;
    public IReadOnlyDictionary<SkillDefinitionId, int> Skills { get; init; } =
        new Dictionary<SkillDefinitionId, int>();
}
