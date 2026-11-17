namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Reserved persistence configuration. No database connection is made by the foundation.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Gets or sets whether the future database adapter should be enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string Provider { get; set; } = "MongoDB";

    /// <summary>
    /// Gets or sets the database connection string, normally supplied by environment variables or secrets.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
