namespace InfiniteSky.Persistence.Tests;

public sealed class MongoPersistenceContextTests
{
    [Fact]
    public async Task DisabledContextDoesNotConnectAndRejectsDatabaseOperations()
    {
        var healthState = new MongoPersistenceHealthState(TimeProvider.System);
        using var metrics = new MongoPersistenceMetrics(healthState);
        var context = new MongoPersistenceContext(new MongoPersistenceSettings(), metrics);

        MongoPersistenceException pingFailure =
            await Assert.ThrowsAsync<MongoPersistenceException>(
                () => context.PingAsync(CancellationToken.None));
        MongoPersistenceException operationFailure =
            Assert.Throws<MongoPersistenceException>(
                () => context.CreateOperationToken(CancellationToken.None));

        Assert.Equal(MongoPersistenceFailureKind.Disabled, pingFailure.FailureKind);
        Assert.Equal(MongoPersistenceFailureKind.Disabled, operationFailure.FailureKind);
    }

    [Fact]
    public void RejectsConnectionStringDatabaseNameMismatchWithoutExposingUri()
    {
        var settings = new MongoPersistenceSettings
        {
            Enabled = true,
            ConnectionString = "mongodb://sensitive-user:sensitive-password@localhost/other",
            DatabaseName = "ts1",
        };

        MongoPersistenceException exception =
            Assert.Throws<MongoPersistenceException>(
                () =>
                {
                    var healthState = new MongoPersistenceHealthState(TimeProvider.System);
                    using var metrics = new MongoPersistenceMetrics(healthState);
                    _ = new MongoPersistenceContext(settings, metrics);
                });

        Assert.Equal(MongoPersistenceFailureKind.Configuration, exception.FailureKind);
        Assert.DoesNotContain("sensitive-password", exception.ToString(), StringComparison.Ordinal);
    }
}
