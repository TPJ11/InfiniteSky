namespace InfiniteSky.Domain.Values;

/// <summary>
/// Stores direction in the degree-valued numeric form used by original state objects.
/// </summary>
public readonly record struct DirectionDegrees(double Value)
{
    /// <summary>
    /// Gets the zero direction.
    /// </summary>
    public static DirectionDegrees Zero => new(0);
}
