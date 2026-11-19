using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace InfiniteSky.Persistence;

/// <summary>
/// Low-cardinality MongoDB operation counters and durations.
/// </summary>
public sealed class MongoPersistenceMetrics : IDisposable
{
    public const string MeterName = "InfiniteSky.Persistence";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _operations;
    private readonly Counter<long> _failures;
    private readonly Histogram<double> _durationSeconds;

    public MongoPersistenceMetrics(MongoPersistenceHealthState healthState)
    {
        _operations = _meter.CreateCounter<long>("infinite_sky_mongodb_operations_total");
        _failures = _meter.CreateCounter<long>("infinite_sky_mongodb_failures_total");
        _durationSeconds = _meter.CreateHistogram<double>("infinite_sky_mongodb_operation_duration_seconds");
        _meter.CreateObservableGauge(
            "infinite_sky_mongodb_health_status",
            () => (int)healthState.Current.Status,
            description: "MongoDB health status: 0=not checked, 1=disabled, 2=connected, 3=authentication failure, 4=unavailable, 5=operation failure.");
    }

    public static long Start() => Stopwatch.GetTimestamp();

    public void Complete(
        string operation,
        long startTimestamp,
        MongoPersistenceFailureKind? failureKind = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        TagList tags = new()
        {
            { "operation", operation },
        };
        if (failureKind is not null)
        {
            tags.Add("failure", failureKind.Value.ToString());
            _failures.Add(1, tags);
        }

        _operations.Add(1, tags);
        _durationSeconds.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds, tags);
    }

    public void Dispose() => _meter.Dispose();
}
