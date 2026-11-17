namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Numeric HP and Chi values persisted on a character.
/// </summary>
public sealed record CharacterVitals
{
    public double? Health { get; init; }
    public double? Chi { get; init; }
}
