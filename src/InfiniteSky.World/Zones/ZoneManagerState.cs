namespace InfiniteSky.World.Zones;

/// <summary>
/// Lifecycle states for the process-local zone manager.
/// </summary>
public enum ZoneManagerState
{
    Created,
    Initializing,
    Running,
    Stopping,
    Stopped,
    Failed,
}
