namespace InfiniteSky.World;

/// <summary>
/// Emits the observed 100 ms cadence only after world dependencies are ready.
/// </summary>
public sealed class WorldClock(
    WorldRuntime runtime,
    WorldMetrics metrics,
    TimeProvider timeProvider,
    IEnumerable<IWorldTickHandler> handlers)
{
    private readonly IWorldTickHandler[] _handlers = handlers.ToArray();

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        WorldRuntimeSnapshot snapshot = runtime.Current;
        if (!snapshot.IsReady || snapshot.Configuration is null)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return;
        }

        using var timer = new PeriodicTimer(snapshot.Configuration.TickInterval, timeProvider);
        long previous = timeProvider.GetTimestamp();

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            long current = timeProvider.GetTimestamp();
            var tick = new WorldTick(timeProvider.GetElapsedTime(previous, current));
            previous = current;

            runtime.RecordTick(tick.Elapsed);
            metrics.RecordTick(tick.Elapsed);
            foreach (IWorldTickHandler handler in _handlers)
            {
                await handler.OnWorldTickAsync(tick, cancellationToken);
            }
        }
    }
}
