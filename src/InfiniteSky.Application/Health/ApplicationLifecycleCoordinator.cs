using System.Collections.ObjectModel;

namespace InfiniteSky.Application.Health;

/// <summary>
/// Coordinates application lifecycle state independently from hosting and external infrastructure.
/// </summary>
public sealed class ApplicationLifecycleCoordinator : IApplicationReadiness
{
    private readonly Lock _sync = new();
    private readonly IApplicationLifecycleObserver[] _observers;
    private readonly TimeProvider _timeProvider;
    private ApplicationLifecyclePhase _phase = ApplicationLifecyclePhase.Created;
    private DateTimeOffset _updatedAt;
    private string _reason = "Application created.";
    private Dictionary<string, string> _checks = new(StringComparer.Ordinal)
    {
        ["Configuration"] = "NotValidated",
        ["Hosting"] = "Created",
        ["Networking"] = "NotImplemented",
        ["Database"] = "NotImplemented",
    };

    /// <summary>
    /// Initializes the coordinator.
    /// </summary>
    public ApplicationLifecycleCoordinator(
        IEnumerable<IApplicationLifecycleObserver> observers,
        TimeProvider timeProvider)
    {
        _observers = observers.ToArray();
        _timeProvider = timeProvider;
        _updatedAt = timeProvider.GetUtcNow();
    }

    /// <inheritdoc />
    public ApplicationReadinessSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                return CreateSnapshot();
            }
        }
    }

    /// <summary>
    /// Updates a named readiness check without changing lifecycle state.
    /// </summary>
    public void SetCheck(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        lock (_sync)
        {
            _checks[name] = value;
            _updatedAt = _timeProvider.GetUtcNow();
        }
    }

    /// <summary>
    /// Transitions to a valid state and notifies observers in registration order.
    /// </summary>
    public async Task TransitionAsync(
        ApplicationLifecyclePhase next,
        string reason,
        CancellationToken cancellationToken)
    {
        ApplicationLifecycleTransition transition;

        lock (_sync)
        {
            if (!IsAllowed(_phase, next))
            {
                throw new InvalidApplicationLifecycleTransitionException(_phase, next);
            }

            ApplicationLifecyclePhase previous = _phase;
            _phase = next;
            _reason = reason;
            _updatedAt = _timeProvider.GetUtcNow();
            _checks["Hosting"] = next.ToString();
            transition = new ApplicationLifecycleTransition(previous, next, reason, _updatedAt);
        }

        foreach (IApplicationLifecycleObserver observer in _observers)
        {
            await observer.OnLifecycleTransitionAsync(transition, cancellationToken);
        }
    }

    /// <summary>
    /// Marks the application as failed without notifying observers.
    /// </summary>
    public void MarkFailed(string reason)
    {
        lock (_sync)
        {
            if (_phase is ApplicationLifecyclePhase.Stopped or ApplicationLifecyclePhase.Failed)
            {
                return;
            }

            _phase = ApplicationLifecyclePhase.Failed;
            _reason = reason;
            _updatedAt = _timeProvider.GetUtcNow();
            _checks["Hosting"] = ApplicationLifecyclePhase.Failed.ToString();
        }
    }

    private static bool IsAllowed(
        ApplicationLifecyclePhase current,
        ApplicationLifecyclePhase next)
    {
        return (current, next) switch
        {
            (ApplicationLifecyclePhase.Created, ApplicationLifecyclePhase.Initialising) => true,
            (ApplicationLifecyclePhase.Initialising, ApplicationLifecyclePhase.Starting) => true,
            (ApplicationLifecyclePhase.Starting, ApplicationLifecyclePhase.Running) => true,
            (ApplicationLifecyclePhase.Running, ApplicationLifecyclePhase.Stopping) => true,
            (ApplicationLifecyclePhase.Stopping, ApplicationLifecyclePhase.Stopped) => true,
            (ApplicationLifecyclePhase.Created, ApplicationLifecyclePhase.Failed) => true,
            (ApplicationLifecyclePhase.Initialising, ApplicationLifecyclePhase.Failed) => true,
            (ApplicationLifecyclePhase.Starting, ApplicationLifecyclePhase.Failed) => true,
            (ApplicationLifecyclePhase.Running, ApplicationLifecyclePhase.Failed) => true,
            (ApplicationLifecyclePhase.Stopping, ApplicationLifecyclePhase.Failed) => true,
            _ => false,
        };
    }

    private ApplicationReadinessSnapshot CreateSnapshot()
    {
        var checks = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(_checks, StringComparer.Ordinal));

        return new ApplicationReadinessSnapshot(
            _phase == ApplicationLifecyclePhase.Running,
            _phase,
            _reason,
            _updatedAt,
            checks);
    }
}
