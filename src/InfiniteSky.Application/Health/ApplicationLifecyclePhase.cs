namespace InfiniteSky.Application.Health;

/// <summary>
/// Describes the lifecycle phase exposed by the application readiness boundary.
/// </summary>
public enum ApplicationLifecyclePhase
{
    Created,
    Initialising,
    Starting,
    Running,
    Stopping,
    Stopped,
    Failed,
}
