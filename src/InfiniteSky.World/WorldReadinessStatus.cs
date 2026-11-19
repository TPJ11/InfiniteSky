namespace InfiniteSky.World;

public enum WorldReadinessStatus
{
    NotInitialized,
    Initializing,
    NotReady,
    Degraded,
    Ready,
    Stopping,
    Stopped,
    Failed,
}
