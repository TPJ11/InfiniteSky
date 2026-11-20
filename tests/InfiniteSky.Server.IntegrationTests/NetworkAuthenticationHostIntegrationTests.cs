using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

using InfiniteSky.Application.Authentication;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Protocol.Binary;
using InfiniteSky.Protocol.Structures;
using InfiniteSky.Server.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class NetworkAuthenticationHostIntegrationTests
{
    [Fact]
    public async Task HostRoutesSocketLoginThroughAuthenticationAndCleansUpOnShutdown()
    {
        int loginPort = GetUnusedPort();
        int worldPort = GetUnusedPortExcluding(loginPort);
        HostApplicationBuilder builder = CreateNetworkedHostBuilder(loginPort, worldPort);
        builder.Services.RemoveAll<IAccountAuthenticationRepository>();
        builder.Services.AddSingleton<IAccountAuthenticationRepository>(
            new TestAccountAuthenticationRepository());
        using IHost host = builder.Build();

        await host.StartAsync();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, loginPort);
        await SendLoginAsync(client, "HostPlayer", "Secret123");

        byte[] reply = new byte[LegacyLoginReply.WireSize];
        await client.GetStream().ReadExactlyAsync(reply)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(3));

        var expected = new LegacyBinaryWriter();
        new LegacyLoginReply(LegacyLoginStatus.Success, "HostPlayer").Encode(expected);
        Assert.Equal(expected.ToArray(), reply);

        IAuthenticationSessionStore sessions =
            host.Services.GetRequiredService<IAuthenticationSessionStore>();
        NetworkHostedService networking =
            host.Services.GetRequiredService<NetworkHostedService>();
        Assert.Equal(1, sessions.Count);
        Assert.Equal(1, networking.Connections.Count);

        using (var duplicateClient = new TcpClient())
        {
            await duplicateClient.ConnectAsync(IPAddress.Loopback, loginPort);
            await SendLoginAsync(duplicateClient, "HostPlayer", "Secret123");
            byte[] duplicateReply = await ReadReplyAsync(duplicateClient);
            Assert.Equal((byte)LegacyLoginStatus.AlreadyLoggedIn, duplicateReply[1]);
            Assert.Equal(
                0,
                await duplicateClient.GetStream()
                    .ReadAsync(new byte[1].AsMemory())
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(3)));
        }

        Assert.Equal(1, sessions.Count);
        Assert.Equal(1, networking.Connections.Count);

        client.Close();
        await WaitUntilAsync(
            () => sessions.Count == 0 && networking.Connections.Count == 0,
            TimeSpan.FromSeconds(3));

        using var reconnectedClient = new TcpClient();
        await reconnectedClient.ConnectAsync(IPAddress.Loopback, loginPort);
        await SendLoginAsync(reconnectedClient, "HostPlayer", "Secret123");
        byte[] reconnectReply = await ReadReplyAsync(reconnectedClient);
        Assert.Equal((byte)LegacyLoginStatus.Success, reconnectReply[1]);
        Assert.Equal(1, sessions.Count);
        Assert.Equal(1, networking.Connections.Count);

        await host.StopAsync();

        Assert.Equal(0, sessions.Count);
        Assert.Equal(0, networking.Connections.Count);
    }

    [Fact]
    public async Task WorldListenerBindFailureReleasesLoginListenerDuringStartupRollback()
    {
        using var occupiedWorldPort = new TcpListener(IPAddress.Loopback, 0);
        occupiedWorldPort.Start();
        int worldPort = ((IPEndPoint)occupiedWorldPort.LocalEndpoint).Port;
        int loginPort = GetUnusedPortExcluding(worldPort);
        HostApplicationBuilder builder = CreateNetworkedHostBuilder(loginPort, worldPort);
        using IHost host = builder.Build();

        await Assert.ThrowsAnyAsync<SocketException>(() => host.StartAsync());

        using var releasedLoginPort = new TcpListener(IPAddress.Loopback, loginPort);
        releasedLoginPort.Start();
    }

    private static HostApplicationBuilder CreateNetworkedHostBuilder(
        int loginPort,
        int worldPort)
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InfiniteSky:Networking:Enabled"] = "true",
            ["InfiniteSky:Networking:BindAddress"] = IPAddress.Loopback.ToString(),
            ["InfiniteSky:Networking:LoginPort"] =
                loginPort.ToString(CultureInfo.InvariantCulture),
            ["InfiniteSky:Networking:WorldPort"] =
                worldPort.ToString(CultureInfo.InvariantCulture),
            ["InfiniteSky:Networking:IdleTimeoutSeconds"] = "0",
            ["InfiniteSky:Networking:ConnectionShutdownTimeoutSeconds"] = "3",
            ["InfiniteSky:Application:ShutdownTimeoutSeconds"] = "5",
        });
        return builder;
    }

    private static int GetUnusedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static int GetUnusedPortExcluding(int excludedPort)
    {
        int port;
        do
        {
            port = GetUnusedPort();
        }
        while (port == excludedPort);

        return port;
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
        await client.GetStream().ReadExactlyAsync(reply)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(3));
        return reply;
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellation.Token);
        }
    }

    private sealed class TestAccountAuthenticationRepository : IAccountAuthenticationRepository
    {
        public ValueTask<AccountAuthenticationRecord?> FindByUsernameAsync(
            string username,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(username, "HostPlayer", StringComparison.Ordinal))
            {
                return ValueTask.FromResult<AccountAuthenticationRecord?>(null);
            }

            return ValueTask.FromResult<AccountAuthenticationRecord?>(
                new AccountAuthenticationRecord(
                    new AccountId(912),
                    username,
                    0,
                    Encoding.UTF8.GetBytes("Secret123")));
        }

        public ValueTask SetActiveAsync(
            AccountId accountId,
            bool active,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        public ValueTask ResetAllActiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }
}
