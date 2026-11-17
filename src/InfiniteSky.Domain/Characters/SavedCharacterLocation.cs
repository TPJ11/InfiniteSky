using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Preserves the independently optional map and coordinate properties of a saved character.
/// </summary>
public sealed record SavedCharacterLocation
{
    public ZoneId? ZoneId { get; init; }
    public double? X { get; init; }
    public double? Y { get; init; }
    public double? Z { get; init; }

    /// <summary>
    /// Gets a position only when all three source coordinates are present.
    /// </summary>
    public Position3? Position =>
        X.HasValue && Y.HasValue && Z.HasValue
            ? new Position3(X.Value, Y.Value, Z.Value)
            : null;
}
