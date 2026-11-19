namespace InfiniteSky.World;

/// <summary>
/// Receives the legacy world cadence; zone actors can subscribe in a later migration stage.
/// </summary>
public interface IWorldTickHandler
{
    ValueTask OnWorldTickAsync(WorldTick tick, CancellationToken cancellationToken);
}
