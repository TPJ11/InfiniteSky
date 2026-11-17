namespace InfiniteSky.Domain.Values;

/// <summary>
/// A mutable-compatible numeric projection of the JavaScript CVec3 values.
/// </summary>
public readonly record struct Position3(double X, double Y, double Z)
{
    /// <summary>
    /// Gets the all-zero origin used by the original CVec3 defaults.
    /// </summary>
    public static Position3 Zero => new(0, 0, 0);
}
