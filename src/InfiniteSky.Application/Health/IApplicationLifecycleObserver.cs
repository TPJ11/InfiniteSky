namespace InfiniteSky.Application.Health;

/// <summary>
/// Receives ordered asynchronous lifecycle notifications without owning the Generic Host.
/// </summary>
public interface IApplicationLifecycleObserver
{
    /// <summary>
    /// Handles an accepted lifecycle transition.
    /// </summary>
    ValueTask OnLifecycleTransitionAsync(
        ApplicationLifecycleTransition transition,
        CancellationToken cancellationToken);
}
