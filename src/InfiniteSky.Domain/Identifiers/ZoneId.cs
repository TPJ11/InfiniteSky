namespace InfiniteSky.Domain.Identifiers;

/// <summary>
/// Identifies a map/zone. Zero appears in original return-to-town requests.
/// </summary>
public readonly record struct ZoneId
{
    /// <summary>
    /// Initializes the identifier.
    /// </summary>
    public ZoneId(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Value = value;
    }

    /// <summary>
    /// Gets the zone/map identifier.
    /// </summary>
    public int Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
