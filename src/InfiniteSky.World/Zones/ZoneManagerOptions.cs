namespace InfiniteSky.World.Zones;

/// <summary>
/// Source-backed queue and QuadTree settings for zone startup.
/// </summary>
public sealed record ZoneManagerOptions
{
    public int LoadConcurrency { get; init; } = 4;
    public int QuadTreeDepth { get; init; } = 6;
    public int QuadTreeNodeLimit { get; init; } = 10;
    public double ViewableActionDistance { get; init; } = 500;
}
