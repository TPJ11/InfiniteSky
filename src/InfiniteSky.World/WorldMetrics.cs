using System.Diagnostics.Metrics;

namespace InfiniteSky.World;

/// <summary>
/// Low-cardinality world lifecycle and cadence measurements.
/// </summary>
public sealed class WorldMetrics : IDisposable
{
    public const string MeterName = "InfiniteSky.World";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _ticks;
    private readonly Counter<long> _lifecycleFailures;
    private readonly Histogram<double> _tickDurationMilliseconds;
    private readonly Histogram<double> _initializationDurationMilliseconds;

    public WorldMetrics(WorldRuntime runtime)
    {
        _ticks = _meter.CreateCounter<long>("infinite_sky_world_ticks_total");
        _lifecycleFailures = _meter.CreateCounter<long>("infinite_sky_world_lifecycle_failures_total");
        _tickDurationMilliseconds =
            _meter.CreateHistogram<double>("infinite_sky_world_tick_duration_milliseconds");
        _initializationDurationMilliseconds =
            _meter.CreateHistogram<double>("infinite_sky_world_initialization_duration_milliseconds");
        _meter.CreateObservableGauge(
            "infinite_sky_world_ready",
            () => runtime.Current.IsReady ? 1 : 0);
        _meter.CreateObservableGauge(
            "infinite_sky_world_guilds",
            () => runtime.Current.GuildCount);
        _meter.CreateObservableGauge(
            "infinite_sky_world_loaded_zones",
            () => runtime.Current.LoadedZoneCount);
    }

    public void RecordTick(TimeSpan elapsed)
    {
        _ticks.Add(1);
        _tickDurationMilliseconds.Record(elapsed.TotalMilliseconds);
    }

    public void RecordInitialization(TimeSpan elapsed)
    {
        _initializationDurationMilliseconds.Record(elapsed.TotalMilliseconds);
    }

    public void RecordLifecycleFailure() => _lifecycleFailures.Add(1);

    public void Dispose() => _meter.Dispose();
}
