namespace InfiniteSky.World.Zones;

/// <summary>
/// Lifecycle states for a configured zone.
/// </summary>
public enum ZoneLifecycleState
{
    Created,
    Initializing,
    Initialized,
    Starting,
    Running,
    Stopping,
    Stopped,
    Failed,
}
