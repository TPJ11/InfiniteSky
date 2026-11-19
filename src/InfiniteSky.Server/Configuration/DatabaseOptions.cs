namespace InfiniteSky.Server.Configuration;

/// <summary>
/// MongoDB connection, account persistence, and operational settings.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Gets or sets whether MongoDB persistence is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string Provider { get; set; } = "MongoDB";

    /// <summary>
    /// Gets or sets the MongoDB connection string, normally supplied by environment variables or secrets.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name. The legacy root configuration uses <c>ts1</c>.
    /// </summary>
    public string DatabaseName { get; set; } = "ts1";

    /// <summary>
    /// Gets or sets the account collection name. Verify this Mongoose-derived default against the deployment.
    /// </summary>
    public string AccountCollectionName { get; set; } = "account_mongooses";

    /// <summary>
    /// Gets or sets the guild collection name. Verify this Mongoose-derived default against the deployment.
    /// </summary>
    public string GuildCollectionName { get; set; } = "guilds";

    /// <summary>
    /// Gets or sets whether the required account username index is created when missing.
    /// Existing incompatible indexes are never dropped or changed.
    /// </summary>
    public bool EnsureIndexesOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets whether startup resets the legacy account <c>active</c> flag for all documents.
    /// </summary>
    public bool ResetActiveAccountsOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets the MongoDB server-selection timeout.
    /// </summary>
    public int ServerSelectionTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the MongoDB connection timeout.
    /// </summary>
    public int ConnectTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum duration of a MongoDB operation.
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the interval between MongoDB health probes.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the timeout for a MongoDB health probe.
    /// </summary>
    public int HealthCheckTimeoutSeconds { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minimum MongoDB connection-pool size.
    /// </summary>
    public int MinConnectionPoolSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum MongoDB connection-pool size.
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;
}
