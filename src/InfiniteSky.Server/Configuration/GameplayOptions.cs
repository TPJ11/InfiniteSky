namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Reserved gameplay compatibility configuration.
/// </summary>
public sealed class GameplayOptions
{
    /// <summary>
    /// Gets or sets the active behavior profile.
    /// </summary>
    public string CompatibilityProfile { get; set; } = "LegacyParity";
}
