namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Source-backed zone selection and spatial-index settings.
/// </summary>
public sealed class ZonesOptions
{
    /// <summary>
    /// Gets or sets the original catalog IDs to load.
    /// </summary>
    public int[] Load { get; set; } = [];

    /// <summary>
    /// Gets or sets the bounded number of zones loaded concurrently.
    /// </summary>
    public int AsyncLoadLimit { get; set; } = 4;

    /// <summary>
    /// Gets or sets the legacy QuadTree depth.
    /// </summary>
    public int QuadTreeDepth { get; set; } = 6;

    /// <summary>
    /// Gets or sets the legacy QuadTree node split threshold.
    /// </summary>
    public int QuadTreeLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the legacy action broadcast candidate distance.
    /// </summary>
    public double ViewableActionDistance { get; set; } = 500;
}
