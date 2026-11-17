namespace InfiniteSky.Domain.Identifiers;

/// <summary>
/// Identifies an item definition from the original item information file.
/// </summary>
public readonly record struct ItemDefinitionId
{
    /// <summary>
    /// Initializes the identifier.
    /// </summary>
    public ItemDefinitionId(uint value) => Value = value;

    /// <summary>
    /// Gets the unsigned definition identifier.
    /// </summary>
    public uint Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
