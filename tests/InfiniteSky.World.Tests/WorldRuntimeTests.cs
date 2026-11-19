using InfiniteSky.Domain.Guilds;

namespace InfiniteSky.World.Tests;

public sealed class WorldRuntimeTests
{
    [Fact]
    public void ReportsDegradedReadinessAndProvidesCaseSensitiveGuildLookup()
    {
        var runtime = new WorldRuntime(TimeProvider.System);
        var guild = new Guild("LegacyGuild")
        {
            Level = 3,
            HasGuildEmblem = true,
        };
        guild.Members.Add(new GuildMember("Master", GuildMemberRole.Master));
        guild.Members.Add(null);

        Start(runtime, [guild], zonesAvailable: false, loadedZoneCount: 0);

        WorldRuntimeSnapshot snapshot = runtime.Current;
        Assert.Equal(WorldLifecycleState.Running, snapshot.State);
        Assert.Equal(WorldReadinessStatus.Degraded, snapshot.Readiness);
        Assert.False(snapshot.IsReady);
        Assert.Contains(WorldReadinessBlocker.ZoneRuntimeUnavailable, snapshot.Blockers);
        Assert.Equal(0, snapshot.LoadedZoneCount);

        WorldGuildSnapshot? loadedGuild = runtime.FindGuildByName("LegacyGuild");
        Assert.NotNull(loadedGuild);
        Assert.Equal(3, loadedGuild.Level);
        Assert.Equal(2, loadedGuild.Members.Count);
        Assert.Null(loadedGuild.Members[1]);
        Assert.Null(runtime.FindGuildByName("legacyguild"));
    }

    [Fact]
    public void ReadyWorldCanStopIdempotently()
    {
        var runtime = new WorldRuntime(TimeProvider.System);
        Start(runtime, [], zonesAvailable: true, loadedZoneCount: 2);

        Assert.True(runtime.Current.IsReady);
        Assert.Equal(2, runtime.Current.LoadedZoneCount);

        runtime.BeginStop();
        runtime.BeginStop();
        runtime.MarkStopped();
        runtime.MarkStopped();

        Assert.Equal(WorldLifecycleState.Stopped, runtime.Current.State);
        Assert.Equal(WorldReadinessStatus.Stopped, runtime.Current.Readiness);
        Assert.False(runtime.Current.IsReady);
    }

    [Fact]
    public void RejectsInvalidTransitionsAndDuplicateGuildNames()
    {
        var runtime = new WorldRuntime(TimeProvider.System);
        runtime.BeginInitialization();

        var duplicateGuilds = new[]
        {
            new Guild("Duplicate"),
            new Guild("Duplicate"),
        };
        Assert.Throws<InvalidOperationException>(
            () => runtime.CompleteInitialization(
                new WorldConfiguration(100),
                duplicateGuilds,
                guildDataAvailable: true,
                zonesAvailable: true,
                loadedZoneCount: 1));
        Assert.Throws<WorldTransitionException>(runtime.BeginStart);
        Assert.Equal(WorldLifecycleState.Initializing, runtime.Current.State);
    }

    [Fact]
    public void RecordsElapsedTicksOnlyWhileRunning()
    {
        var runtime = new WorldRuntime(TimeProvider.System);
        runtime.BeginInitialization();
        runtime.CompleteInitialization(
            new WorldConfiguration(100),
            [],
            guildDataAvailable: true,
            zonesAvailable: true,
            loadedZoneCount: 1);
        runtime.BeginStart();
        runtime.MarkRunning();
        runtime.RecordTick(TimeSpan.FromMilliseconds(98));

        Assert.Equal(1, runtime.Current.TickCount);
        Assert.Equal(98, runtime.Current.LastTickMilliseconds);

        runtime.BeginStop();
        Assert.Throws<WorldTransitionException>(
            () => runtime.RecordTick(TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public void RejectsInvalidTickConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldConfiguration(0));
        Assert.Equal(100, new WorldConfiguration(100).TickMilliseconds);
    }

    private static void Start(
        WorldRuntime runtime,
        IEnumerable<Guild> guilds,
        bool zonesAvailable,
        int loadedZoneCount)
    {
        runtime.BeginInitialization();
        runtime.CompleteInitialization(
            new WorldConfiguration(100),
            guilds,
            guildDataAvailable: true,
            zonesAvailable,
            loadedZoneCount);
        runtime.BeginStart();
        runtime.MarkRunning();
    }
}
