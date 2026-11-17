namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Persistent base character attribute points.
/// </summary>
public sealed record CharacterAttributes
{
    public int Strength { get; init; }
    public int Chi { get; init; }
    public int Dexterity { get; init; }
    public int Vitality { get; init; }
}
