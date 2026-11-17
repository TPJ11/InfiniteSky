using System.ComponentModel.DataAnnotations;

namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Application identity and lifecycle configuration.
/// </summary>
public sealed class ApplicationOptions
{
    /// <summary>
    /// Gets or sets the operator-facing server name.
    /// </summary>
    [Required]
    public string Name { get; set; } = "InfiniteSky";

    /// <summary>
    /// Gets or sets the maximum graceful shutdown duration in seconds.
    /// </summary>
    [Range(1, 300)]
    public int ShutdownTimeoutSeconds { get; set; } = 30;
}
