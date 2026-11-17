using System.Collections.ObjectModel;

using InfiniteSky.Application.Health;
using InfiniteSky.Server.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Hosting;

/// <summary>
/// Owns the foundation host lifecycle and reports readiness only for components that actually exist.
/// </summary>
public sealed partial class FoundationHostedService : IHostedService, IApplicationReadiness
{
    private static readonly IReadOnlyDictionary<string, string> StartingChecks =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            ["Configuration"] = "Starting",
            ["Hosting"] = "Starting",
            ["Networking"] = "NotImplemented",
            ["Database"] = "NotImplemented",
        });

    private readonly IHostEnvironment _environment;
    private readonly ILogger<FoundationHostedService> _logger;
    private readonly InfiniteSkyOptions _options;
    private readonly CancellationTokenRegistration _stoppingRegistration;
    private ApplicationReadinessSnapshot _current =
        new(false, ApplicationLifecyclePhase.Starting, StartingChecks);

    /// <summary>
    /// Initializes the lifecycle service.
    /// </summary>
    public FoundationHostedService(
        IOptions<InfiniteSkyOptions> options,
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        ILogger<FoundationHostedService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
        _stoppingRegistration = applicationLifetime.ApplicationStopping.Register(TransitionToStopping);
    }

    /// <inheritdoc />
    public ApplicationReadinessSnapshot Current => Volatile.Read(ref _current);

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var checks = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            ["Configuration"] = "Valid",
            ["Hosting"] = "Running",
            ["Networking"] = "DisabledByFoundation",
            ["Database"] = "DisabledByFoundation",
        });

        Volatile.Write(
            ref _current,
            new ApplicationReadinessSnapshot(true, ApplicationLifecyclePhase.Ready, checks));

        LogStarted(
            _logger,
            _options.Application.Name,
            _environment.EnvironmentName,
            _options.Gameplay.CompatibilityProfile,
            checks["Networking"],
            checks["Database"]);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        var checks = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            ["Configuration"] = "Valid",
            ["Hosting"] = "Stopped",
            ["Networking"] = "DisabledByFoundation",
            ["Database"] = "DisabledByFoundation",
        });

        Volatile.Write(
            ref _current,
            new ApplicationReadinessSnapshot(false, ApplicationLifecyclePhase.Stopped, checks));

        _stoppingRegistration.Dispose();
        LogStopped(_logger);
        return Task.CompletedTask;
    }

    private void TransitionToStopping()
    {
        var checks = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            ["Configuration"] = "Valid",
            ["Hosting"] = "Stopping",
            ["Networking"] = "DisabledByFoundation",
            ["Database"] = "DisabledByFoundation",
        });

        Volatile.Write(
            ref _current,
            new ApplicationReadinessSnapshot(false, ApplicationLifecyclePhase.Stopping, checks));
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "InfiniteSky foundation started: Name={ApplicationName}, Environment={Environment}, "
            + "CompatibilityProfile={CompatibilityProfile}, Networking={NetworkingState}, Database={DatabaseState}")]
    private static partial void LogStarted(
        ILogger logger,
        string applicationName,
        string environment,
        string compatibilityProfile,
        string networkingState,
        string databaseState);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "InfiniteSky foundation stopped.")]
    private static partial void LogStopped(ILogger logger);
}
