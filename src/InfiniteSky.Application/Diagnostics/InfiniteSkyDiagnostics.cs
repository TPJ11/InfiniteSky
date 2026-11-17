using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace InfiniteSky.Application.Diagnostics;

/// <summary>
/// Owns vendor-independent activity and metric entry points for the application process.
/// </summary>
public sealed class InfiniteSkyDiagnostics : IDisposable
{
    /// <summary>
    /// Initializes the diagnostics sources.
    /// </summary>
    public InfiniteSkyDiagnostics(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        Activities = new ActivitySource($"{serviceName}.Activities");
        Metrics = new Meter($"{serviceName}.Metrics");
    }

    /// <summary>
    /// Gets the activity source used by future infrastructure and gameplay operations.
    /// </summary>
    public ActivitySource Activities { get; }

    /// <summary>
    /// Gets the meter used for future explicit process metrics.
    /// </summary>
    public Meter Metrics { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Activities.Dispose();
        Metrics.Dispose();
    }
}
