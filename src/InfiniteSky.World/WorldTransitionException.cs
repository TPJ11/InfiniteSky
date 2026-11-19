namespace InfiniteSky.World;

public sealed class WorldTransitionException : InvalidOperationException
{
    public WorldTransitionException(WorldLifecycleState current, WorldLifecycleState next)
        : base($"World lifecycle transition {current} -> {next} is invalid.")
    {
        Current = current;
        Next = next;
    }

    public WorldLifecycleState Current { get; }
    public WorldLifecycleState Next { get; }
}
