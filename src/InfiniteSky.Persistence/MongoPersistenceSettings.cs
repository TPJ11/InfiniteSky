namespace InfiniteSky.Persistence;

/// <summary>
/// MongoDB settings projected from server configuration at the composition boundary.
/// </summary>
public sealed class MongoPersistenceSettings
{
    public bool Enabled { get; init; }
    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "ts1";
    public string AccountCollectionName { get; init; } = "account_mongooses";
    public string GuildCollectionName { get; init; } = "guilds";
    public bool EnsureIndexesOnStartup { get; init; } = true;
    public bool ResetActiveAccountsOnStartup { get; init; } = true;
    public int ServerSelectionTimeoutSeconds { get; init; } = 5;
    public int ConnectTimeoutSeconds { get; init; } = 5;
    public int OperationTimeoutSeconds { get; init; } = 10;
    public int HealthCheckIntervalSeconds { get; init; } = 30;
    public int HealthCheckTimeoutSeconds { get; init; } = 3;
    public int MinConnectionPoolSize { get; init; }
    public int MaxConnectionPoolSize { get; init; } = 100;
}
