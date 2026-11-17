namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Reserved zone loading configuration.
/// </summary>
public sealed class ZonesOptions
{
    /// <summary>
    /// Gets or sets the zones requested for future loading.
    /// </summary>
    public int[] Load { get; set; } = [];
}
