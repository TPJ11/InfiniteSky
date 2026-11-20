using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using InfiniteSky.Application.Authentication;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Protocol.Binary;
using InfiniteSky.Protocol.Structures;
using InfiniteSky.Server.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

internal static class Program
{
    private const int MaximumConnections = 100;
    private const string Password = "PerfPass1";

    public static async Task<int> Main(string[] args)
    {
        HarnessOptions options = HarnessOptions.Parse(args);
        int loginPort = GetUnusedPort();
        int worldPort = GetUnusedPortExcluding(loginPort);

        HostApplicationBuilder builder = ServerHostBuilder.Create([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InfiniteSky:Networking:Enabled"] = "true",
            ["InfiniteSky:Networking:BindAddress"] = IPAddress.Loopback.ToString(),
            ["InfiniteSky:Networking:LoginPort"] = loginPort.ToString(CultureInfo.InvariantCulture),
            ["InfiniteSky:Networking:WorldPort"] = worldPort.ToString(CultureInfo.InvariantCulture),
            ["InfiniteSky:Networking:MaxConcurrentConnections"] =
                MaximumConnections.ToString(CultureInfo.InvariantCulture),
            ["InfiniteSky:Networking:IdleTimeoutSeconds"] = "0",
            ["InfiniteSky:Networking:ConnectionShutdownTimeoutSeconds"] = "5",
            ["InfiniteSky:Database:Enabled"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:InfiniteSky.Networking.NetworkConnection"] = "Critical",
            ["Logging:LogLevel:InfiniteSky.Server.Hosting.ZoneManagementHostedService"] = "Critical",
        });
        builder.Services.RemoveAll<IAccountAuthenticationRepository>();
        builder.Services.AddSingleton<IAccountAuthenticationRepository>(
            new LoadAccountAuthenticationRepository());

        using var counters = new PerformanceCounters();
        using IHost host = builder.Build();
        await host.StartAsync();

        try
        {
            NetworkHostedService network =
                host.Services.GetRequiredService<NetworkHostedService>();
            IAuthenticationSessionStore sessions =
                host.Services.GetRequiredService<IAuthenticationSessionStore>();
            var endpoint = new IPEndPoint(IPAddress.Loopback, loginPort);

            if (options.WarmupClients > 0)
            {
                AuthenticatedClient[] warmup =
                    await RunConcurrentBatchAsync(endpoint, options.WarmupClients);
                DisposeClients(warmup);
                await WaitForCleanupAsync(network, sessions);
            }

            for (int repetition = 1; repetition <= options.Repetitions; repetition++)
            {
                BatchMeasurement measurement = await RunMeasurementAsync(
                    endpoint,
                    options,
                    network,
                    sessions,
                    counters);
                Console.WriteLine(JsonSerializer.Serialize(
                    CreateResult(options, repetition, measurement, network, sessions)));
            }
        }
        finally
        {
            await host.StopAsync();
        }

        return 0;
    }

    private static async Task<BatchMeasurement> RunMeasurementAsync(
        IPEndPoint endpoint,
        HarnessOptions options,
        NetworkHostedService network,
        IAuthenticationSessionStore sessions,
        PerformanceCounters counters)
    {
        using Process process = Process.GetCurrentProcess();
        ForceFullCollection();
        long receiveFailuresBefore = counters.ReceiveFailures;
        long sendFailuresBefore = counters.SendFailures;
        AuthenticatedClient[] clients = [];
        TimeSpan burstElapsed = TimeSpan.Zero;
        TimeSpan idleDuration = TimeSpan.Zero;
        TimeSpan cpuDuringIdle = TimeSpan.Zero;
        long allocatedDuringIdle = 0;
        long managedBeforeIdle = 0;
        long managedAfterIdle = 0;
        long workingSetBeforeIdle = 0;
        long workingSetAfterIdle = 0;
        int[] collectionsDuringIdle = [0, 0, 0];
        int activeConnectionsDuringIdle = 0;
        int activeSessionsDuringIdle = 0;

        try
        {
            var burstTimer = Stopwatch.StartNew();
            clients = await RunConcurrentBatchAsync(endpoint, options.Clients);
            burstTimer.Stop();
            burstElapsed = burstTimer.Elapsed;

            if (network.Connections.Count != clients.Length
                || sessions.Count != clients.Length)
            {
                throw new InvalidOperationException(
                    "The active connection and authenticated session counts did not match the successful clients.");
            }

            ForceFullCollection();
            managedBeforeIdle = GC.GetGCMemoryInfo().HeapSizeBytes;
            process.Refresh();
            TimeSpan cpuBeforeIdle = process.TotalProcessorTime;
            long allocatedBeforeIdle = GC.GetTotalAllocatedBytes(precise: true);
            workingSetBeforeIdle = process.WorkingSet64;
            int[] collectionsBeforeIdle =
                [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
            var idleTimer = Stopwatch.StartNew();
            await Task.Delay(TimeSpan.FromSeconds(options.DurationSeconds));
            idleTimer.Stop();
            idleDuration = idleTimer.Elapsed;

            activeConnectionsDuringIdle = network.Connections.Count;
            activeSessionsDuringIdle = sessions.Count;
            if (activeConnectionsDuringIdle != clients.Length
                || activeSessionsDuringIdle != clients.Length)
            {
                throw new InvalidOperationException(
                    "An authenticated connection or session ended during the idle measurement.");
            }

            process.Refresh();
            cpuDuringIdle = process.TotalProcessorTime - cpuBeforeIdle;
            allocatedDuringIdle =
                GC.GetTotalAllocatedBytes(precise: true) - allocatedBeforeIdle;
            workingSetAfterIdle = process.WorkingSet64;
            int[] collectionsAfterIdle =
                [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
            collectionsDuringIdle =
                Enumerable.Range(0, 3)
                    .Select(generation =>
                        collectionsAfterIdle[generation] - collectionsBeforeIdle[generation])
                    .ToArray();
            ForceFullCollection();
            managedAfterIdle = GC.GetGCMemoryInfo().HeapSizeBytes;
        }
        finally
        {
            DisposeClients(clients);
            await WaitForCleanupAsync(network, sessions);
        }

        return new BatchMeasurement(
            burstElapsed,
            clients,
            idleDuration,
            cpuDuringIdle,
            allocatedDuringIdle,
            managedBeforeIdle,
            managedAfterIdle,
            workingSetBeforeIdle,
            workingSetAfterIdle,
            collectionsDuringIdle,
            activeConnectionsDuringIdle,
            activeSessionsDuringIdle,
            counters.ReceiveFailures - receiveFailuresBefore,
            counters.SendFailures - sendFailuresBefore,
            ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount);
    }

    private static void ForceFullCollection()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
    }

    private static async Task<AuthenticatedClient[]> RunConcurrentBatchAsync(
        IPEndPoint endpoint,
        int clientCount)
    {
        Task<AuthenticatedClient>[] clients = Enumerable.Range(1, clientCount)
            .Select(index => RunLoginClientAsync(endpoint, MakeUsername(index)))
            .ToArray();
        try
        {
            return await Task.WhenAll(clients);
        }
        catch
        {
            foreach (Task<AuthenticatedClient> task in clients)
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    task.Result.Dispose();
                }
            }

            throw;
        }
    }

    private static async Task<AuthenticatedClient> RunLoginClientAsync(
        IPEndPoint endpoint,
        string username)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var client = new TcpClient { NoDelay = true };
        Stopwatch timer = Stopwatch.StartNew();
        try
        {
            await client.ConnectAsync(endpoint.Address, endpoint.Port, timeout.Token);

            NetworkStream stream = client.GetStream();
            byte[] greeting = new byte[LegacyLoginGreeting.WireSize];
            await stream.ReadExactlyAsync(greeting.AsMemory(), timeout.Token);
            if (greeting[0] != LegacyLoginGreeting.PacketId)
            {
                throw new InvalidDataException("The login listener returned an unexpected greeting.");
            }

            var payload = new LegacyBinaryWriter();
            new LegacyLoginRequest(username, Password).Encode(payload);
            byte[] frame = new byte[9 + payload.Offset];
            frame[8] = LegacyLoginReply.PacketId;
            payload.WrittenSpan.CopyTo(frame.AsSpan(9));
            await stream.WriteAsync(frame.AsMemory(), timeout.Token);

            byte[] reply = new byte[LegacyLoginReply.WireSize];
            await stream.ReadExactlyAsync(reply.AsMemory(), timeout.Token);
            timer.Stop();
            if (reply[0] != LegacyLoginReply.PacketId
                || reply[1] != (byte)LegacyLoginStatus.Success)
            {
                throw new InvalidDataException(
                    "The login listener did not authenticate the isolated test account.");
            }

            return new AuthenticatedClient(client, timer.Elapsed);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static void DisposeClients(IEnumerable<AuthenticatedClient> clients)
    {
        foreach (AuthenticatedClient client in clients)
        {
            client.Dispose();
        }
    }

    private static async Task WaitForCleanupAsync(
        NetworkHostedService network,
        IAuthenticationSessionStore sessions)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (network.Connections.Count != 0 || sessions.Count != 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
    }

    private static object CreateResult(
        HarnessOptions options,
        int repetition,
        BatchMeasurement measurement,
        NetworkHostedService network,
        IAuthenticationSessionStore sessions)
    {
        double idleSeconds = measurement.IdleDuration.TotalSeconds;
        TimeSpan[] latencies = measurement.Clients
            .Select(static client => client.LoginLatency)
            .ToArray();
        return new
        {
            scenario = "source-compatible-login-burst-then-idle-authenticated-sessions",
            repetition,
            attemptedConnections = options.Clients,
            successfulAuthentications = measurement.Clients.Length,
            authenticationBurstMilliseconds = measurement.BurstElapsed.TotalMilliseconds,
            authenticationRequestsPerSecond =
                measurement.Clients.Length / measurement.BurstElapsed.TotalSeconds,
            latencyMilliseconds = new
            {
                p50 = PercentileMilliseconds(latencies, 0.50),
                p95 = PercentileMilliseconds(latencies, 0.95),
                p99 = PercentileMilliseconds(latencies, 0.99),
            },
            sustainedIdleSeconds = measurement.IdleDuration.TotalSeconds,
            processCpuPercentOfLogicalProcessors =
                measurement.CpuDuringIdle.TotalSeconds / idleSeconds / Environment.ProcessorCount * 100,
            allocatedBytesDuringIdle = measurement.AllocatedBytesDuringIdle,
            allocatedBytesPerSecondDuringIdle = measurement.AllocatedBytesDuringIdle / idleSeconds,
            managedHeapBytesBeforeIdle = measurement.ManagedHeapBeforeIdle,
            managedHeapBytesAfterIdle = measurement.ManagedHeapAfterIdle,
            workingSetBytesBeforeIdle = measurement.WorkingSetBeforeIdle,
            workingSetBytesAfterIdle = measurement.WorkingSetAfterIdle,
            generationCollectionsDuringIdle = measurement.GenerationCollectionsDuringIdle,
            activeConnectionsDuringIdle = measurement.ActiveConnectionsDuringIdle,
            activeSessionsDuringIdle = measurement.ActiveSessionsDuringIdle,
            threadPoolThreadCountAfter = measurement.ThreadPoolThreadCount,
            threadPoolPendingWorkItemsAfter = measurement.ThreadPoolPendingWorkItems,
            receiveFailures = measurement.ReceiveFailures,
            sendFailures = measurement.SendFailures,
            activeConnectionsAfterCleanup = network.Connections.Count,
            activeSessionsAfterCleanup = sessions.Count,
            processorCount = Environment.ProcessorCount,
            runtime = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
        };
    }

    private static double PercentileMilliseconds(TimeSpan[] latencies, double percentile)
    {
        double[] sorted = latencies
            .Select(static latency => latency.TotalMilliseconds)
            .Order()
            .ToArray();
        int index = Math.Clamp(
            (int)Math.Ceiling(percentile * sorted.Length) - 1,
            0,
            sorted.Length - 1);
        return sorted[index];
    }

    private static string MakeUsername(int index)
        => "B" + index.ToString("D13", CultureInfo.InvariantCulture);

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

    private sealed record BatchMeasurement(
        TimeSpan BurstElapsed,
        AuthenticatedClient[] Clients,
        TimeSpan IdleDuration,
        TimeSpan CpuDuringIdle,
        long AllocatedBytesDuringIdle,
        long ManagedHeapBeforeIdle,
        long ManagedHeapAfterIdle,
        long WorkingSetBeforeIdle,
        long WorkingSetAfterIdle,
        int[] GenerationCollectionsDuringIdle,
        int ActiveConnectionsDuringIdle,
        int ActiveSessionsDuringIdle,
        long ReceiveFailures,
        long SendFailures,
        int ThreadPoolThreadCount,
        long ThreadPoolPendingWorkItems);

    private sealed record AuthenticatedClient(TcpClient Connection, TimeSpan LoginLatency) : IDisposable
    {
        public void Dispose() => Connection.Dispose();
    }

    private sealed record HarnessOptions(
        int Clients,
        int Repetitions,
        int WarmupClients,
        int DurationSeconds)
    {
        public static HarnessOptions Parse(string[] args)
        {
            int clients = ReadInt(args, "--clients", 10, 1, MaximumConnections);
            int repetitions = ReadInt(args, "--repetitions", 3, 1, 100);
            int warmup = ReadInt(args, "--warmup", Math.Min(5, clients), 0, MaximumConnections);
            int duration = ReadInt(args, "--duration-seconds", 5, 1, 60);
            return new HarnessOptions(clients, repetitions, warmup, duration);
        }

        private static int ReadInt(
            string[] args,
            string name,
            int defaultValue,
            int minimum,
            int maximum)
        {
            int position = Array.IndexOf(args, name);
            if (position < 0)
            {
                return defaultValue;
            }
            if (position + 1 >= args.Length
                || !int.TryParse(args[position + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                || value < minimum
                || value > maximum)
            {
                throw new ArgumentException(
                    $"{name} must be an integer from {minimum} through {maximum}.");
            }

            return value;
        }
    }

    private sealed class LoadAccountAuthenticationRepository : IAccountAuthenticationRepository
    {
        public ValueTask<AccountAuthenticationRecord?> FindByUsernameAsync(
            string username,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (username.Length != 14
                || username[0] != 'B'
                || !long.TryParse(
                    username.AsSpan(1),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long accountId))
            {
                return ValueTask.FromResult<AccountAuthenticationRecord?>(null);
            }

            return ValueTask.FromResult<AccountAuthenticationRecord?>(
                new AccountAuthenticationRecord(
                    new AccountId(accountId),
                    username,
                    0,
                    Encoding.UTF8.GetBytes(Password)));
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

    private sealed class PerformanceCounters : IDisposable
    {
        private readonly MeterListener _listener = new();
        private long _receiveFailures;
        private long _sendFailures;

        public PerformanceCounters()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "InfiniteSky.Networking"
                    && instrument.Name is "infinite_sky.network.receive_failures"
                        or "infinite_sky.network.send_failures")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>(
                (instrument, measurement, _, _) =>
                {
                    if (instrument.Name == "infinite_sky.network.receive_failures")
                    {
                        Interlocked.Add(ref _receiveFailures, measurement);
                    }
                    else if (instrument.Name == "infinite_sky.network.send_failures")
                    {
                        Interlocked.Add(ref _sendFailures, measurement);
                    }
                });
            _listener.Start();
        }

        public long ReceiveFailures => Interlocked.Read(ref _receiveFailures);
        public long SendFailures => Interlocked.Read(ref _sendFailures);

        public void Dispose() => _listener.Dispose();
    }
}
