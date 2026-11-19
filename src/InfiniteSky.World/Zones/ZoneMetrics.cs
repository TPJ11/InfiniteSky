using System.Diagnostics.Metrics;

namespace InfiniteSky.World.Zones;

/// <summary>
/// Low-cardinality zone and spatial diagnostics.
/// </summary>
public sealed class ZoneMetrics : IDisposable
{
    private readonly Meter _meter = new("InfiniteSky.World.Zones");
    private readonly UpDownCounter<long> _registeredZones;
    private readonly UpDownCounter<long> _spatialObjects;
    private readonly Counter<long> _lifecycleFailures;
    private readonly Histogram<double> _initializationMilliseconds;
    private readonly Histogram<double> _queryMilliseconds;

    public ZoneMetrics()
    {
        _registeredZones = _meter.CreateUpDownCounter<long>("infinite_sky.zone.registered");
        _spatialObjects = _meter.CreateUpDownCounter<long>("infinite_sky.zone.spatial_objects");
        _lifecycleFailures = _meter.CreateCounter<long>("infinite_sky.zone.lifecycle_failures");
        _initializationMilliseconds = _meter.CreateHistogram<double>("infinite_sky.zone.initialization_ms");
        _queryMilliseconds = _meter.CreateHistogram<double>("infinite_sky.zone.spatial_query_ms");
    }

    public void RecordZoneRegistration(long delta) => _registeredZones.Add(delta);
    public void RecordSpatialObjectDelta(long delta) => _spatialObjects.Add(delta);
    public void RecordZoneLifecycleFailure(long count = 1) => _lifecycleFailures.Add(count);
    public void RecordZoneInitialization(TimeSpan elapsed) => _initializationMilliseconds.Record(elapsed.TotalMilliseconds);
    public void RecordSpatialQuery(TimeSpan elapsed) => _queryMilliseconds.Record(elapsed.TotalMilliseconds);

    public void Dispose() => _meter.Dispose();
}
