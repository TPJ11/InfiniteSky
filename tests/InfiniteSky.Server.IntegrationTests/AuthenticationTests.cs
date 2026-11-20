using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

using InfiniteSky.Application.Authentication;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Networking;
using InfiniteSky.Protocol.Binary;
using InfiniteSky.Protocol.Packets;
using InfiniteSky.Protocol.Structures;
using InfiniteSky.Server.Authentication;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task ValidCredentialsCreateOneSessionAndZeroStoredCredentialBuffer()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository { AccountLevel = 81 };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));
        Guid connectionId = Guid.NewGuid();

        AuthenticationResult result = await service.AuthenticateAsync(
            connectionId,
            "PlayerName",
            "Secret123",
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.Authenticated, result.Outcome);
        Assert.NotNull(result.Session);
        Assert.Equal(connectionId, result.Session.ConnectionId);
        Assert.Equal(new AccountId(17), result.Session.AccountId);
        Assert.Equal(81, result.Session.AccountLevel);
        Assert.NotEqual(Guid.Empty, result.Session.SessionId);
        Assert.Equal(AuthenticationConnectionState.Authenticated, service.GetConnectionState(connectionId));
        Assert.True(sessions.TryGetByAccount(new AccountId(17), out AuthenticatedSession? found));
        Assert.Equal(result.Session, found);
        Assert.Equal(1, repository.Active);
        Assert.All(repository.LastReturnedCredential!, value => Assert.Equal(0, value));
    }

    private sealed class FailingSessionStore : IAuthenticationSessionStore
    {
        public int Count => 0;

        public SessionRegistrationStatus TryCreate(
            AccountId accountId,
            string username,
            double accountLevel,
            Guid connectionId,
            DateTimeOffset createdAt,
            out AuthenticatedSession? session)
        {
            session = null;
            return SessionRegistrationStatus.Unavailable;
        }

        public bool TryGetByConnection(Guid connectionId, out AuthenticatedSession? session)
        {
            session = null;
            return false;
        }

        public bool TryGetByAccount(AccountId accountId, out AuthenticatedSession? session)
        {
            session = null;
            return false;
        }

        public bool RemoveByConnection(Guid connectionId, out AuthenticatedSession? session)
        {
            session = null;
            return false;
        }
    }

    [Fact]
    public async Task UnknownAccountAndIncorrectPasswordUseDistinctLegacyOutcomes()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository();
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));

        Guid incorrectConnectionId = Guid.NewGuid();
        AuthenticationResult incorrect = await service.AuthenticateAsync(
            incorrectConnectionId,
            "PlayerName",
            "wrong",
            CancellationToken.None);
        AuthenticationResult missing = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "unknown",
            "wrong",
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.IncorrectPassword, incorrect.Outcome);
        Assert.Equal(AuthenticationOutcome.AccountNotFound, missing.Outcome);
        Assert.Equal(
            AuthenticationConnectionState.Unauthenticated,
            service.GetConnectionState(incorrectConnectionId));
        Assert.Equal(0, sessions.Count);
        Assert.Equal(0, repository.Active);
        Assert.All(repository.LastReturnedCredential!, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task SimultaneousConnectionsCannotCreateDuplicateAccountSessions()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository { BlockFindUntilTwoCalls = true };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));

        Task<AuthenticationResult> first = service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None).AsTask();
        Task<AuthenticationResult> second = service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None).AsTask();

        await repository.TwoLookupsStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        repository.ReleaseLookups();
        AuthenticationResult[] results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.Outcome == AuthenticationOutcome.Authenticated);
        Assert.Single(results, result => result.Outcome == AuthenticationOutcome.AlreadyLoggedIn);
        Assert.Equal(1, sessions.Count);
    }

    [Fact]
    public async Task DuplicateRequestOnOneConnectionDoesNotStartConcurrentLookup()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository { BlockFindUntilReleased = true };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));
        Guid connectionId = Guid.NewGuid();

        Task<AuthenticationResult> first = service.AuthenticateAsync(
            connectionId,
            "PlayerName",
            "Secret123",
            CancellationToken.None).AsTask();
        await repository.FirstLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        AuthenticationResult duplicate = await service.AuthenticateAsync(
            connectionId,
            "PlayerName",
            "Secret123",
            CancellationToken.None);
        repository.ReleaseLookups();
        AuthenticationResult completed = await first;

        Assert.Equal(AuthenticationOutcome.RequestInProgress, duplicate.Outcome);
        Assert.Equal(AuthenticationOutcome.Authenticated, completed.Outcome);
        Assert.Equal(1, repository.LookupCount);
    }

    [Fact]
    public async Task DisconnectDuringLookupCancelsAuthenticationAndCreatesNoSession()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository { BlockFindUntilReleased = true };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));
        Guid connectionId = Guid.NewGuid();

        Task<AuthenticationResult> attempt = service.AuthenticateAsync(
            connectionId,
            "PlayerName",
            "Secret123",
            CancellationToken.None).AsTask();
        await repository.FirstLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        service.ConnectionClosed(connectionId);
        AuthenticationResult result = await attempt.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(AuthenticationOutcome.ConnectionClosed, result.Outcome);
        Assert.Equal(0, sessions.Count);
        Assert.Null(service.GetConnectionState(connectionId));
    }

    [Fact]
    public async Task AuthenticationTimeoutReturnsCannotAuthenticateOutcomeWithoutSession()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository { BlockFindUntilReleased = true };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromMilliseconds(25));

        AuthenticationResult result = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(AuthenticationOutcome.TimedOut, result.Outcome);
        Assert.Equal(0, sessions.Count);
    }

    [Fact]
    public async Task TimeoutDuringActiveUpdateRollsBackSessionAndActiveFlag()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository
        {
            BlockActiveUpdateUntilCancelled = true,
        };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromMilliseconds(25));

        AuthenticationResult result = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(AuthenticationOutcome.TimedOut, result.Outcome);
        Assert.Equal(0, sessions.Count);
        Assert.Equal(0, repository.Active);
    }

    [Fact]
    public async Task InvalidFieldsAndRepositoryFailureFailClosed()
    {
        using var metrics = new AuthenticationMetrics();
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            new TestAccountRepository { FailLookup = true },
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));

        AuthenticationResult invalid = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerNameIsTooLong",
            "Secret123",
            CancellationToken.None);
        AuthenticationResult unavailable = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.InvalidRequest, invalid.Outcome);
        Assert.Equal(AuthenticationOutcome.CannotAuthenticate, unavailable.Outcome);
        Assert.Equal(0, sessions.Count);
    }

    [Fact]
    public async Task MissingLegacyPasswordIsRejectedWithoutCredentialComparison()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository { AccountPassword = null };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));

        AuthenticationResult result = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.IncorrectPassword, result.Outcome);
        Assert.Null(repository.LastReturnedCredential);
        Assert.Equal(0, sessions.Count);
    }

    [Fact]
    public async Task LegacyLatin1CredentialTextIsComparedWithoutNormalization()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository
        {
            AccountUsername = "Jöhn",
            AccountPassword = "Päss",
        };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));

        var requestWriter = new LegacyBinaryWriter();
        new LegacyLoginRequest("Jöhn", "Päss").Encode(requestWriter);
        LegacyLoginRequest decoded = LegacyLoginRequest.Decode(
            new LegacyBinaryReader(requestWriter.WrittenMemory));
        AuthenticationResult result = await service.AuthenticateAsync(
            Guid.NewGuid(),
            decoded.Username,
            decoded.Password,
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.Authenticated, result.Outcome);
    }

    [Fact]
    public async Task RepositoryAndSessionCreationFailuresDoNotLeaveSessions()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository { FailLookup = true };
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));

        AuthenticationResult lookupFailure = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None);
        repository.FailLookup = false;
        repository.FailActiveUpdate = true;
        AuthenticationResult updateFailure = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.CannotAuthenticate, lookupFailure.Outcome);
        Assert.Equal(AuthenticationOutcome.CannotAuthenticate, updateFailure.Outcome);
        Assert.Equal(0, sessions.Count);
    }

    [Fact]
    public async Task SessionStoreFailureDoesNotSetAccountActive()
    {
        using var metrics = new AuthenticationMetrics();
        var repository = new TestAccountRepository();
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            new FailingSessionStore(),
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));

        AuthenticationResult result = await service.AuthenticateAsync(
            Guid.NewGuid(),
            "PlayerName",
            "Secret123",
            CancellationToken.None);

        Assert.Equal(AuthenticationOutcome.CannotAuthenticate, result.Outcome);
        Assert.Equal(0, repository.Active);
    }

    [Fact]
    public async Task LoginPacketUsesLegacyResponseAndCleansSessionOnDisconnect()
    {
        using var metrics = new AuthenticationMetrics();
        using var transportMetrics = new NetworkingMetrics();
        var repository = new TestAccountRepository();
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));
        var logger = new RecordingLogger<AuthenticationIncomingPacketSink>();
        var sink = new AuthenticationIncomingPacketSink(service, logger);
        var manager = new ConnectionManager(4);
        var options = new NetworkingOptions
        {
            MaxReceiveBufferBytes = 10_240,
            MaxFrameBytes = 10_240,
            SendQueueCapacity = 8,
            IdleTimeoutSeconds = 0,
            ConnectionShutdownTimeoutSeconds = 5,
        };
        var listener = new LegacyTcpListener(
            NetworkEndpointKind.Login,
            IPAddress.Loopback,
            0,
            manager,
            sink,
            LegacyCorePacketRegistry.CreateLogin(),
            transportMetrics,
            NullLogger<LegacyTcpListener>.Instance,
            NullLoggerFactory.Instance,
            options);
        await using (listener)
        {
            await listener.StartAsync(CancellationToken.None);
            IPEndPoint endpoint = Assert.IsType<IPEndPoint>(listener.LocalEndPoint);
            using var client = new TcpClient();
            await client.ConnectAsync(endpoint.Address, endpoint.Port);
            await AssertLoginGreetingAsync(client);

            await SendLoginAsync(client, "PlayerName", "wrong");
            byte[] rejectedReply = await ReadReplyAsync(client);
            Assert.Equal((byte)LegacyLoginStatus.IncorrectPassword, rejectedReply[1]);
            Assert.Equal(0, sessions.Count);

            await SendLoginAsync(client, "PlayerName", "Secret123");
            byte[] successReply = await ReadReplyAsync(client);
            var expectedWriter = new LegacyBinaryWriter();
            new LegacyLoginReply(LegacyLoginStatus.Success, "PlayerName")
                .Encode(expectedWriter);
            Assert.Equal(expectedWriter.ToArray(), successReply);
            Assert.Equal(1, sessions.Count);
            Assert.DoesNotContain("Secret123", string.Join(Environment.NewLine, logger.Messages));

            using var duplicateClient = new TcpClient();
            await duplicateClient.ConnectAsync(endpoint.Address, endpoint.Port);
            await AssertLoginGreetingAsync(duplicateClient);
            await SendLoginAsync(duplicateClient, "PlayerName", "Secret123");
            byte[] duplicateReply = await ReadReplyAsync(duplicateClient);
            Assert.Equal((byte)LegacyLoginStatus.AlreadyLoggedIn, duplicateReply[1]);
            Assert.Equal(
                0,
                await duplicateClient.GetStream()
                    .ReadAsync(new byte[1].AsMemory())
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Equal(1, sessions.Count);

            NetworkConnection connection = Assert.Single(
                manager.Connections,
                candidate => candidate.State == NetworkConnectionState.Connected);
            var closed = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            connection.Closed += _ => closed.TrySetResult();
            client.Close();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(0, sessions.Count);
        }
    }

    [Fact]
    public async Task WorldEndpointDoesNotInvokeLoginAuthentication()
    {
        using var metrics = new AuthenticationMetrics();
        using var transportMetrics = new NetworkingMetrics();
        var repository = new TestAccountRepository();
        var sessions = new InMemoryAuthenticationSessionStore(metrics);
        using var service = new AuthenticationService(
            repository,
            new LegacyPlaintextCredentialVerifier(),
            sessions,
            metrics,
            TimeProvider.System,
            TimeSpan.FromSeconds(5));
        var sink = new AuthenticationIncomingPacketSink(
            service,
            NullLogger<AuthenticationIncomingPacketSink>.Instance);
        var manager = new ConnectionManager(1);
        var options = new NetworkingOptions
        {
            MaxReceiveBufferBytes = 10_240,
            MaxFrameBytes = 10_240,
            SendQueueCapacity = 8,
            IdleTimeoutSeconds = 0,
            ConnectionShutdownTimeoutSeconds = 5,
        };
        var socketListener = new TcpListener(IPAddress.Loopback, 0);
        socketListener.Start();

        try
        {
            IPEndPoint endpoint = Assert.IsType<IPEndPoint>(socketListener.LocalEndpoint);
            using var client = new TcpClient();
            await client.ConnectAsync(endpoint.Address, endpoint.Port);
            TcpClient accepted = await socketListener.AcceptTcpClientAsync();
            await using var connection = new NetworkConnection(
                accepted,
                NetworkEndpointKind.World,
                manager,
                sink,
                LegacyCorePacketRegistry.CreateWorld(),
                transportMetrics,
                NullLogger<NetworkConnection>.Instance,
                options);
            Assert.True(manager.TryAdd(connection));

            await sink.OnFrameAsync(
                connection,
                new LegacyProtocolFrame(new byte[9], 0x03, new byte[91]),
                CancellationToken.None);

            Assert.Equal(0, repository.LookupCount);
            Assert.Equal(0, sessions.Count);
        }
        finally
        {
            socketListener.Stop();
        }
    }

    private static async Task SendLoginAsync(
        TcpClient client,
        string username,
        string password)
    {
        var payloadWriter = new LegacyBinaryWriter();
        new LegacyLoginRequest(username, password).Encode(payloadWriter);
        byte[] frame = new byte[9 + payloadWriter.Offset];
        frame[8] = LegacyLoginReply.PacketId;
        payloadWriter.WrittenSpan.CopyTo(frame.AsSpan(9));
        await client.GetStream().WriteAsync(frame);
    }

    private static async Task<byte[]> ReadReplyAsync(TcpClient client)
    {
        byte[] reply = new byte[LegacyLoginReply.WireSize];
        await client.GetStream().ReadExactlyAsync(reply);
        return reply;
    }

    private static async Task AssertLoginGreetingAsync(TcpClient client)
    {
        byte[] greeting = new byte[LegacyLoginGreeting.WireSize];
        await client.GetStream().ReadExactlyAsync(greeting);

        var expectedWriter = new LegacyBinaryWriter();
        new LegacyLoginGreeting(0, 1000).Encode(expectedWriter);
        Assert.Equal(expectedWriter.ToArray(), greeting);
    }

    private sealed class TestAccountRepository : IAccountAuthenticationRepository
    {
        private readonly TaskCompletionSource _twoLookupsStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstLookupStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseLookups =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _lookupCount;
        private int _active;

        public bool BlockFindUntilTwoCalls { get; init; }
        public bool BlockFindUntilReleased { get; init; }
        public bool BlockActiveUpdateUntilCancelled { get; init; }
        public bool FailLookup { get; set; }
        public bool FailActiveUpdate { get; set; }
        public string AccountUsername { get; init; } = "PlayerName";
        public string? AccountPassword { get; init; } = "Secret123";
        public double AccountLevel { get; init; }
        public int LookupCount => Volatile.Read(ref _lookupCount);
        public int Active => Volatile.Read(ref _active);
        public byte[]? LastReturnedCredential { get; private set; }
        public TaskCompletionSource TwoLookupsStarted => _twoLookupsStarted;
        public TaskCompletionSource FirstLookupStarted => _firstLookupStarted;

        public async ValueTask<AccountAuthenticationRecord?> FindByUsernameAsync(
            string username,
            CancellationToken cancellationToken)
        {
            if (FailLookup)
            {
                throw new AccountRepositoryException("Test account store unavailable.");
            }

            int currentCount = Interlocked.Increment(ref _lookupCount);
            _firstLookupStarted.TrySetResult();
            if (BlockFindUntilTwoCalls && currentCount >= 2)
            {
                _twoLookupsStarted.TrySetResult();
            }
            if ((BlockFindUntilTwoCalls && currentCount <= 2)
                || BlockFindUntilReleased)
            {
                await _releaseLookups.Task.WaitAsync(cancellationToken);
            }

            if (!string.Equals(username, AccountUsername, StringComparison.Ordinal))
            {
                return null;
            }

            LastReturnedCredential = AccountPassword is null
                ? null
                : Encoding.UTF8.GetBytes(AccountPassword);
            return new AccountAuthenticationRecord(
                new AccountId(17),
                AccountUsername,
                AccountLevel,
                LastReturnedCredential);
        }

        public async ValueTask SetActiveAsync(
            AccountId accountId,
            bool active,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailActiveUpdate)
            {
                throw new AccountRepositoryException("Test account update failed.");
            }

            Interlocked.Exchange(ref _active, active ? 1 : 0);
            if (active && BlockActiveUpdateUntilCancelled)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
        }

        public ValueTask ResetAllActiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Exchange(ref _active, 0);
            return ValueTask.CompletedTask;
        }

        public void ReleaseLookups() => _releaseLookups.TrySetResult();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NullLogger<T>.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Enqueue(formatter(state, exception));
        }
    }
}
