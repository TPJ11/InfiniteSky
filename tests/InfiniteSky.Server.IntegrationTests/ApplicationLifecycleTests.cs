using InfiniteSky.Application.Health;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class ApplicationLifecycleTests
{
    [Fact]
    public async Task LifecycleFollowsTheDefinedTransitionSequence()
    {
        var coordinator = new ApplicationLifecycleCoordinator([], TimeProvider.System);

        await coordinator.TransitionAsync(
            ApplicationLifecyclePhase.Initialising,
            "initialising",
            CancellationToken.None);
        await coordinator.TransitionAsync(
            ApplicationLifecyclePhase.Starting,
            "starting",
            CancellationToken.None);
        await coordinator.TransitionAsync(
            ApplicationLifecyclePhase.Running,
            "running",
            CancellationToken.None);

        Assert.True(coordinator.Current.IsReady);
        Assert.Equal(ApplicationLifecyclePhase.Running, coordinator.Current.Phase);

        await coordinator.TransitionAsync(
            ApplicationLifecyclePhase.Stopping,
            "stopping",
            CancellationToken.None);
        await coordinator.TransitionAsync(
            ApplicationLifecyclePhase.Stopped,
            "stopped",
            CancellationToken.None);

        Assert.False(coordinator.Current.IsReady);
        Assert.Equal(ApplicationLifecyclePhase.Stopped, coordinator.Current.Phase);
    }

    [Fact]
    public async Task InvalidTransitionIsRejectedWithoutChangingState()
    {
        var coordinator = new ApplicationLifecycleCoordinator([], TimeProvider.System);

        var exception = await Assert.ThrowsAsync<InvalidApplicationLifecycleTransitionException>(
            () => coordinator.TransitionAsync(
                ApplicationLifecyclePhase.Running,
                "skip transitions",
                CancellationToken.None));

        Assert.Equal(ApplicationLifecyclePhase.Created, exception.Current);
        Assert.Equal(ApplicationLifecyclePhase.Running, exception.Requested);
        Assert.Equal(ApplicationLifecyclePhase.Created, coordinator.Current.Phase);
    }

    [Fact]
    public async Task ObserversAreNotifiedInRegistrationOrder()
    {
        List<string> order = [];
        var first = new RecordingLifecycleObserver("first", order);
        var second = new RecordingLifecycleObserver("second", order);
        var coordinator = new ApplicationLifecycleCoordinator([first, second], TimeProvider.System);

        await coordinator.TransitionAsync(
            ApplicationLifecyclePhase.Initialising,
            "test",
            CancellationToken.None);

        Assert.Equal(["first:Initialising", "second:Initialising"], order);
    }

    [Fact]
    public async Task MarkingFailureMakesReadinessUnavailable()
    {
        var coordinator = new ApplicationLifecycleCoordinator([], TimeProvider.System);
        await coordinator.TransitionAsync(
            ApplicationLifecyclePhase.Initialising,
            "initialising",
            CancellationToken.None);

        coordinator.MarkFailed("test failure");

        Assert.False(coordinator.Current.IsReady);
        Assert.Equal(ApplicationLifecyclePhase.Failed, coordinator.Current.Phase);
    }

    private sealed class RecordingLifecycleObserver(
        string name,
        ICollection<string> order) : IApplicationLifecycleObserver
    {
        public ValueTask OnLifecycleTransitionAsync(
            ApplicationLifecycleTransition transition,
            CancellationToken cancellationToken)
        {
            order.Add($"{name}:{transition.Current}");
            return ValueTask.CompletedTask;
        }
    }
}
