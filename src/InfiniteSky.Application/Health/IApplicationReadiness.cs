namespace InfiniteSky.Application.Health;

/// <summary>
/// Exposes application readiness without owning or probing a specific external service.
/// </summary>
public interface IApplicationReadiness
{
    /// <summary>
    /// Gets the current readiness snapshot.
    /// </summary>
    ApplicationReadinessSnapshot Current { get; }
}
