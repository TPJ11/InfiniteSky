using System.Diagnostics.Metrics;

namespace InfiniteSky.World.Players;

/// <summary>
/// Low-cardinality player activation, cleanup, and persistence metrics.
/// </summary>
public sealed class PlayerMetrics : IDisposable
{
    private readonly Meter _meter = new("InfiniteSky.World.Players");
    private readonly UpDownCounter<long> _activePlayers;
    private readonly Counter<long> _activationFailures;
    private readonly Counter<long> _saveFailures;

    public PlayerMetrics()
    {
        _activePlayers = _meter.CreateUpDownCounter<long>("infinite_sky.players.active");
        _activationFailures = _meter.CreateCounter<long>("infinite_sky.players.activation_failures");
        _saveFailures = _meter.CreateCounter<long>("infinite_sky.players.save_failures");
    }

    public void RecordActivePlayerDelta(long delta) => _activePlayers.Add(delta);
    public void RecordActivationFailure() => _activationFailures.Add(1);
    public void RecordSaveFailure() => _saveFailures.Add(1);

    public void Dispose() => _meter.Dispose();
}
