namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Strongly typed root configuration for the server.
/// </summary>
public sealed class InfiniteSkyOptions
{
    /// <summary>
    /// The configuration section consumed from appsettings and environment variables.
    /// </summary>
    public const string SectionName = "InfiniteSky";

    /// <summary>
    /// Gets or sets application lifecycle options.
    /// </summary>
    public ApplicationOptions Application { get; set; } = new();

    /// <summary>
    /// Gets or sets host lifecycle coordination options.
    /// </summary>
    public HostingOptions Hosting { get; set; } = new();

    /// <summary>
    /// Gets or sets process diagnostics options.
    /// </summary>
    public DiagnosticsOptions Diagnostics { get; set; } = new();

    /// <summary>
    /// Gets or sets future listener options.
    /// </summary>
    public NetworkingOptions Networking { get; set; } = new();

    /// <summary>
    /// Gets or sets authentication safeguards.
    /// </summary>
    public AuthenticationOptions Authentication { get; set; } = new();

    /// <summary>
    /// Gets or sets future persistence options.
    /// </summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Gets or sets world runtime options.
    /// </summary>
    public WorldOptions World { get; set; } = new();

    /// <summary>
    /// Gets or sets zone loading options.
    /// </summary>
    public ZonesOptions Zones { get; set; } = new();

    /// <summary>
    /// Gets or sets gameplay compatibility options.
    /// </summary>
    public GameplayOptions Gameplay { get; set; } = new();
}
