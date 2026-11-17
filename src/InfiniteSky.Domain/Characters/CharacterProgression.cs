namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Persistent level, experience, and progression point values.
/// </summary>
public sealed record CharacterProgression
{
    public int Level { get; init; } = 1;
    public long Experience { get; init; }
    public int Honor { get; init; }
    public int SkillPoints { get; init; }
    public int StatPoints { get; init; }
    public int StatBonus { get; init; }
    public int OtherIngame { get; init; }
    public long PlayTime { get; init; }
    public int ContributionPoints { get; init; }
}
