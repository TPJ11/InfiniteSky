namespace InfiniteSky.World.Tests;

public sealed class WorldClockTests
{
    [Fact]
    public async Task EmitsElapsedTicksSequentiallyAndObservesCancellation()
    {
        var runtime = new WorldRuntime(TimeProvider.System);
        runtime.BeginInitialization();
        runtime.CompleteInitialization(
            new WorldConfiguration(5),
            [],
            guildDataAvailable: true,
            zonesAvailable: true,
            loadedZoneCount: 1);
        runtime.BeginStart();
        runtime.MarkRunning();

        using var metrics = new WorldMetrics(runtime);
        using var cancellation = new CancellationTokenSource();
        var handler = new SlowTickHandler(targetCount: 3, cancellation);
        var clock = new WorldClock(runtime, metrics, TimeProvider.System, [handler]);

        Task run = clock.RunAsync(cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => run.WaitAsync(TimeSpan.FromSeconds(5)));
        await handler.Completed;
        Assert.Equal(3, runtime.Current.TickCount);
        Assert.Equal(1, handler.MaximumConcurrentCalls);
        Assert.All(handler.Ticks, tick => Assert.True(tick.Elapsed > TimeSpan.Zero));
    }

    [Fact]
    public async Task DoesNotEmitTicksWhenWorldDependenciesAreUnavailable()
    {
        var runtime = new WorldRuntime(TimeProvider.System);
        runtime.BeginInitialization();
        runtime.CompleteInitialization(
            new WorldConfiguration(5),
            [],
            guildDataAvailable: false,
            zonesAvailable: false,
            loadedZoneCount: 0);
        runtime.BeginStart();
        runtime.MarkRunning();

        using var metrics = new WorldMetrics(runtime);
        var clock = new WorldClock(runtime, metrics, TimeProvider.System, []);
        using var cancellation = new CancellationTokenSource();

        Task run = clock.RunAsync(cancellation.Token);
        await Task.Delay(50);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(0, runtime.Current.TickCount);
    }

    private sealed class SlowTickHandler(
        int targetCount,
        CancellationTokenSource cancellation) : IWorldTickHandler
    {
        private readonly TaskCompletionSource _completed = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeCalls;
        private int _completedCalls;
        private int _maximumConcurrentCalls;

        public Task Completed => _completed.Task;
        public List<WorldTick> Ticks { get; } = [];
        public int MaximumConcurrentCalls => _maximumConcurrentCalls;

        public async ValueTask OnWorldTickAsync(
            WorldTick tick,
            CancellationToken cancellationToken)
        {
            int activeCalls = Interlocked.Increment(ref _activeCalls);
            UpdateMaximum(activeCalls);
            Ticks.Add(tick);
            await Task.Delay(25, cancellationToken);
            Interlocked.Decrement(ref _activeCalls);

            if (Interlocked.Increment(ref _completedCalls) == targetCount)
            {
                _completed.TrySetResult();
                cancellation.Cancel();
            }
        }

        private void UpdateMaximum(int activeCalls)
        {
            int maximum;
            do
            {
                maximum = _maximumConcurrentCalls;
                if (activeCalls <= maximum)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(
                ref _maximumConcurrentCalls,
                activeCalls,
                maximum) != maximum);
        }
    }
}
