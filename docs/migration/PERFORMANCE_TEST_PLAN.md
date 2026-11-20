# Prompt 23 Performance Test Plan

## Scope and gate

This plan exercises only the implemented host-composed login/authentication path over real loopback TCP. It is not a representative MMORPG gameplay workload: character selection, world handoff, client-bound world entry, movement, combat, chat, scripting, administration, and player persistence are not available through packet handlers. No SLA, target player count, or production workload distribution is inferred.

The harness is [`tools/InfiniteSky.PerformanceHarness`](../../tools/InfiniteSky.PerformanceHarness/Program.cs). It uses the production `ServerHostBuilder`, `NetworkHostedService`, framing, login greeting, login request, authentication service, session store, and reply. Only the account repository is replaced with isolated in-memory test accounts; MongoDB is explicitly disabled.

## Environment and inputs

- Runtime: .NET 10 Release build, loopback IPv4 only.
- Listeners: ephemeral loopback ports; the server's maximum concurrent connection setting is set to the checked-in default of 100.
- Database: disabled; no MongoDB service or Docker is used.
- Game data: no zones are configured. The harness does not claim that world readiness or simulation is ready.
- Client traffic: source-compatible 21-byte login greeting, 9-byte legacy receive header plus the 91-byte login request, and the 41-byte legacy login response. The structures are defined in [`LegacyLoginGreeting`](../../src/InfiniteSky.Protocol/Structures/LegacyLoginGreeting.cs#L5), [`LegacyLoginRequest`](../../src/InfiniteSky.Protocol/Structures/LegacyLoginRequest.cs#L5), and [`LegacyLoginReply`](../../src/InfiniteSky.Protocol/Structures/LegacyLoginReply.cs#L10).
- Accounts: generated, isolated test identities backed by an in-memory implementation of `IAccountAuthenticationRepository`; no production accounts or secrets are used.

## Workloads

| Profile | Status | Method | Interpretation |
|---|---|---|---|
| Concurrent login burst followed by idle authenticated sockets | Runnable | Open 1, 10, 50, or 100 real TCP clients concurrently; authenticate each with a unique test identity; keep all sockets and sessions open for the configured idle measurement interval; disconnect and verify cleanup | Measures the implemented login/auth/session path and idle connection retention only. These connection counts are test points, not player-count targets. |
| Idle unauthenticated connections | Not included | Not required for the implemented login workflow | Login connection-open sends a greeting; it does not represent an active player. |
| Character selection, world entry, movement, chat, combat, inventory, persistence, scripting, and administration | Blocked | No supported end-to-end handlers exist | Do not emulate missing functionality with arbitrary packets or mocks. |
| Dense/distributed zones and visibility | Blocked | No client-reachable world/player simulation or representative region corpus | Component tests are not server load tests. |
| Mongo latency, outage, or database throughput | Blocked | MongoDB is unavailable and database access is disabled for this harness | No database performance or recovery claim. |
| Queue saturation / malformed-packet stress | Not run | Separate policy and workload required | Normal-load results do not imply overload behavior. |

The 1/10/50/100 test points increase to the configured connection ceiling, rather than asserting an external player target. One warmup cohort and three measured repetitions are used. Each measured cohort is held idle for five seconds by default; `--duration-seconds` can be set from 1 through 60. These short runs establish only the stated short-window result, not a soak or long-term stability.

## Metrics

Each measured repetition writes one JSON line:

- attempted and successful connections/authentications;
- concurrent login burst elapsed time, completions per second, and p50/p95/p99 per-client login latency;
- active connections and sessions during the idle interval and after cleanup;
- process CPU time normalized by logical processor count, process working set, managed-heap snapshots after forced collections, allocation during the idle interval, and generation collection counts;
- networking receive/send failure counters, thread-pool thread count, and pending work items.

CPU and memory are observed in the process containing both the server host and the simulated clients. They are not isolated server-only resource measurements. Heap snapshots use forced full collections outside the timed idle interval; generation collection counts are sampled before the final forced collection. The login burst's latency/throughput and the later idle interval are separate measurements.

No separate microbenchmark is included. The load result must not be interpreted as packet codec, spatial index, world tick, MongoDB, or complete-server performance. No stack-based CPU profile is available in this environment (`dotnet-trace`, `dotnet-counters`, and `dotnet-gcdump` are not installed); no new monitoring platform is introduced.

## Commands

From the repository root in PowerShell:

```powershell
dotnet restore InfiniteSky.sln
dotnet build tools\InfiniteSky.PerformanceHarness\InfiniteSky.PerformanceHarness.csproj --no-restore -c Release
dotnet run --no-build -c Release --project tools\InfiniteSky.PerformanceHarness -- --clients 1 --repetitions 3 --warmup 1 --duration-seconds 5
dotnet run --no-build -c Release --project tools\InfiniteSky.PerformanceHarness -- --clients 10 --repetitions 3 --warmup 5 --duration-seconds 5
dotnet run --no-build -c Release --project tools\InfiniteSky.PerformanceHarness -- --clients 50 --repetitions 3 --warmup 5 --duration-seconds 5
dotnet run --no-build -c Release --project tools\InfiniteSky.PerformanceHarness -- --clients 100 --repetitions 3 --warmup 5 --duration-seconds 5
```

The harness enforces 1-100 measured clients, matching the test configuration ceiling. Capture the JSON lines and record the commit, hardware, runtime, configuration, warmup, repetition count, and measurement interval alongside them.

## Failure criteria and cleanup

A run fails if any client cannot connect, receives an unexpected greeting/reply, fails source-compatible authentication, loses its authenticated session during the idle interval, or if connections/sessions do not return to zero after disconnect. Failures are surfaced as a non-zero process exit or exception; the harness does not silently count failed clients as successful work.

The harness closes every simulated socket, waits up to five seconds for connection/session cleanup, and stops the host. A failed batch still disposes successfully established clients. Test runs are local-only and do not touch production services.

## Correctness checks

After any runtime change, run:

```powershell
dotnet test InfiniteSky.sln --no-build
dotnet format InfiniteSky.sln --verify-no-changes --no-restore
git diff --check
```

The existing loopback network tests cover concurrent sends, frame fragmentation/coalescing, buffer limits, and disconnect cleanup. The focused real-socket login integration test covers the exact greeting and login reply. There is no original client, live Node.js differential server, MongoDB, or complete gameplay path for a compatibility/load comparison.
