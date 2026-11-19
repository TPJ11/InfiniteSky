namespace InfiniteSky.World;

public sealed record WorldConfiguration
{
    public WorldConfiguration(int tickMilliseconds)
    {
        if (tickMilliseconds is < 1 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(tickMilliseconds));
        }

        TickMilliseconds = tickMilliseconds;
    }

    public int TickMilliseconds { get; }
    public TimeSpan TickInterval => TimeSpan.FromMilliseconds(TickMilliseconds);
}
