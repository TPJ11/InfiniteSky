namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Persistent appearance values supplied at character creation.
/// </summary>
public sealed record CharacterAppearance
{
    public Clan Clan { get; init; }
    public Gender Gender { get; init; }
    public int Hair { get; init; }
    public int Face { get; init; }
}
