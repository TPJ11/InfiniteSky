namespace InfiniteSky.Domain.Identifiers;

/// <summary>
/// Identifies a monster definition.
/// </summary>
public readonly record struct MonsterDefinitionId
{
    /// <summary>
    /// Initializes the identifier.
    /// </summary>
    public MonsterDefinitionId(uint value) => Value = value;

    /// <summary>
    /// Gets the unsigned definition identifier.
    /// </summary>
    public uint Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
