namespace InfiniteSky.Domain.Identifiers;

/// <summary>
/// Identifies a persistent character.
/// </summary>
public readonly record struct CharacterId
{
    /// <summary>
    /// Initializes the identifier.
    /// </summary>
    public CharacterId(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Value = value;
    }

    /// <summary>
    /// Gets the source character counter value.
    /// </summary>
    public long Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
