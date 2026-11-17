using InfiniteSky.Application.Health;
using InfiniteSky.Server.Hosting;

using Microsoft.Extensions.Logging.Abstractions;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class BackgroundServiceTests
{
    [Fact]
    public async Task BackgroundServiceObservesCancellationAndStops()
    {
        ApplicationLifecycleCoordinator lifecycle = await CreateRunningLifecycleAsync();
        var service = new CancellationAwareService(lifecycle);

        await service.StartAsync(CancellationToken.None);
        await service.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        Assert.True(service.CancellationObserved);
    }

    [Fact]
    public async Task BackgroundServiceExceptionMarksLifecycleFailed()
    {
        ApplicationLifecycleCoordinator lifecycle = await CreateRunningLifecycleAsync();
        var service = new FailingService(lifecycle);

        await service.StartAsync(CancellationToken.None);
        await service.Executed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await WaitUntilAsync(
            () => lifecycle.Current.Phase == ApplicationLifecyclePhase.Failed,
            TimeSpan.FromSeconds(2));

        Assert.Equal(ApplicationLifecyclePhase.Failed, lifecycle.Current.Phase);
        await service.StopAsync(CancellationToken.None);
    }

    private static async Task<ApplicationLifecycleCoordinator> CreateRunningLifecycleAsync()
    {
        var lifecycle = new ApplicationLifecycleCoordinator([], TimeProvider.System);
        await lifecycle.TransitionAsync(
            ApplicationLifecyclePhase.Initialising,
            "test setup",
            CancellationToken.None);
        await lifecycle.TransitionAsync(
            ApplicationLifecyclePhase.Starting,
            "test setup",
            CancellationToken.None);
        await lifecycle.TransitionAsync(
            ApplicationLifecyclePhase.Running,
            "test setup",
            CancellationToken.None);
        return lifecycle;
    }

    private sealed class CancellationAwareService(
        ApplicationLifecycleCoordinator lifecycle)
        : CoordinatedBackgroundService(lifecycle, NullLogger.Instance)
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool CancellationObserved { get; private set; }

        protected override async Task ExecuteServiceAsync(CancellationToken stoppingToken)
        {
            Started.TrySetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                CancellationObserved = true;
                throw;
            }
        }
    }

    private sealed class FailingService(ApplicationLifecycleCoordinator lifecycle)
        : CoordinatedBackgroundService(lifecycle, NullLogger.Instance)
    {
        public TaskCompletionSource Executed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task ExecuteServiceAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            Executed.TrySetResult();
            throw new InvalidOperationException("Deliberate background service failure.");
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        while (!condition())
        {
            await Task.Delay(10, cancellation.Token);
        }
    }
}
