namespace InfiniteSky.Application.Health;

/// <summary>
/// Raised when a caller requests a lifecycle state change not permitted by the state machine.
/// </summary>
public sealed class InvalidApplicationLifecycleTransitionException : InvalidOperationException
{
    /// <summary>
    /// Initializes the exception.
    /// </summary>
    public InvalidApplicationLifecycleTransitionException(
        ApplicationLifecyclePhase current,
        ApplicationLifecyclePhase requested)
        : base($"Cannot transition application lifecycle from {current} to {requested}.")
    {
        Current = current;
        Requested = requested;
    }

    /// <summary>
    /// Gets the phase active when the invalid transition was requested.
    /// </summary>
    public ApplicationLifecyclePhase Current { get; }

    /// <summary>
    /// Gets the invalid requested phase.
    /// </summary>
    public ApplicationLifecyclePhase Requested { get; }
}
