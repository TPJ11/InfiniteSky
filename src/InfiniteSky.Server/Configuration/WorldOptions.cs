using System.ComponentModel.DataAnnotations;

namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Reserved world simulation configuration.
/// </summary>
public sealed class WorldOptions
{
    /// <summary>
    /// Gets or sets the target legacy world cadence in milliseconds.
    /// </summary>
    [Range(1, 10_000)]
    public int TickMilliseconds { get; set; } = 100;
}
