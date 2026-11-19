namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Legacy character-shard selection configuration.
/// </summary>
public sealed class CharactersOptions
{
    /// <summary>
    /// Gets or sets the legacy <c>ServerName</c> value used to isolate character records.
    /// </summary>
    public string ServerName { get; set; } = "Development";
}
