namespace InfiniteSky.Domain.Identifiers;

/// <summary>
/// Identifies a runtime QuadTree/node entity.
/// </summary>
public readonly record struct EntityId
{
    /// <summary>
    /// Initializes the wire-visible node identifier.
    /// </summary>
    public EntityId(uint value) => Value = value;

    /// <summary>
    /// Gets the unsigned runtime identifier.
    /// </summary>
    public uint Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
