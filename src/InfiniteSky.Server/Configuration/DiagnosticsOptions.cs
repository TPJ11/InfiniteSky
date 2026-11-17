namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Configures vendor-independent diagnostics names.
/// </summary>
public sealed class DiagnosticsOptions
{
    /// <summary>
    /// Gets or sets the logical service name used by activity and meter sources.
    /// </summary>
    public string ServiceName { get; set; } = "InfiniteSky";
}
