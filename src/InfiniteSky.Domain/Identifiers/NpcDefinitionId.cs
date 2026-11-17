namespace InfiniteSky.Domain.Identifiers;

/// <summary>
/// Identifies an NPC definition.
/// </summary>
public readonly record struct NpcDefinitionId
{
    /// <summary>
    /// Initializes the identifier.
    /// </summary>
    public NpcDefinitionId(uint value) => Value = value;

    /// <summary>
    /// Gets the unsigned definition identifier.
    /// </summary>
    public uint Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
