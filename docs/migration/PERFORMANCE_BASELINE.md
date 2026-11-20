# Prompt 23 Performance Baseline

## Snapshot

| Field | Value |
|---|---|
| Pre-optimization source commit | `76815fdeca856de07a1fb85c5cbe682a87bc3532` |
| Branch | `master` |
| Worktree at stage start | Clean |
| OS | Windows 11 Pro, 64-bit, build `10.0.26300` |
| CPU | 12th Gen Intel(R) Core(TM) i9-12900K; 24 logical processors |
| Physical memory | 31.75 GiB; 17.71 GiB free at initial environment capture |
| .NET SDK | `10.0.401` |
| .NET runtime used | `.NET 10.0.12` |
| Architecture | x64 |
| Network under test | IPv4 loopback; no external interface traffic |
| Database | Disabled; no MongoDB or Docker available |

Memory availability and host load are point-in-time observations on a potentially shared development machine, not a controlled dedicated benchmark host.

The worktree was clean at commit capture. The performance harness and its solution entry were added afterward; no .NET runtime source had been modified for the pre-optimization measurements. The original Node.js implementation was not changed.

## Configuration and implementation coverage

The checked-in [server settings](../../src/InfiniteSky.Server/appsettings.json) disable networking and MongoDB by default. The baseline harness explicitly enables login/world TCP listeners on ephemeral loopback ports, sets `MaxConcurrentConnections` to the checked-in default of 100, disables the database, and replaces only the account repository with generated in-memory test accounts. The default receive/send/frame bounds are retained.

No zones are configured, and the world remains degraded/not ready. The migration implements source-compatible TCP framing, login greeting, login request decoding, authentication, session creation, reply encoding, and disconnect cleanup. The measured workload does not include character selection, world handoff, player entry, movement, visibility, combat, chat, inventory, gameplay persistence, scripts, or administration. See [verification readiness](./VERIFICATION_READINESS.md) and [integration harness scope](./INTEGRATION_TEST_HARNESS.md).

The production diagnostics already include network accepted/disconnected/frame/byte/failure counters, authentication outcome/session counters, world tick and spatial query histograms, player activation counts, and Mongo operation metrics. They do not currently provide per-packet processing time, send/receive queue depth, login-duration histograms, or gameplay/script execution timing. The harness adds process-level measurements for its own test run and does not add a monitoring platform.

## Source references

- [Server composition](../../src/InfiniteSky.Server/Hosting/ServerHostBuilder.cs#L29) wires the host, authentication service, session store, network hosted service, world/zone services, and optional MongoDB persistence. [NetworkConnection.StartAsync](../../src/InfiniteSky.Networking/NetworkConnection.cs#L85) owns per-socket task startup.
- [LegacyReceiveAccumulator.Append](../../src/InfiniteSky.Networking/LegacyReceiveAccumulator.cs#L32) handles received bytes. The [login sink](../../src/InfiniteSky.Server/Authentication/AuthenticationIncomingPacketSink.cs#L33) emits the greeting and dispatches login frames; [`LegacyLoginRequest`](../../src/InfiniteSky.Protocol/Structures/LegacyLoginRequest.cs#L5) and [`LegacyLoginReply`](../../src/InfiniteSky.Protocol/Structures/LegacyLoginReply.cs#L10) define the measured wire structures. [AuthenticationService.AuthenticateAsync](../../src/InfiniteSky.Application/Authentication/AuthenticationService.cs#L42) performs account lookup, credential verification, and session creation.
- [Default server networking/database settings](../../src/InfiniteSky.Server/appsettings.json#L16) provide the 100-connection test ceiling and disabled-by-default listeners/database.
- Existing diagnostic sources are [network](../../src/InfiniteSky.Networking/NetworkingMetrics.cs#L8), [authentication](../../src/InfiniteSky.Application/Authentication/AuthenticationMetrics.cs#L8), [world](../../src/InfiniteSky.World/WorldMetrics.cs#L8), [zone/spatial](../../src/InfiniteSky.World/Zones/ZoneMetrics.cs#L8), [player](../../src/InfiniteSky.World/Players/PlayerMetrics.cs#L8), and [Mongo persistence](../../src/InfiniteSky.Persistence/MongoPersistenceMetrics.cs#L9). The [world clock](../../src/InfiniteSky.World/WorldClock.cs#L6) and [zone spatial index](../../src/InfiniteSky.World/Spatial/ZoneSpatialIndex.cs#L12) exist as components but were not exercised as server gameplay workloads.
- The new [ordered receive regression test](../../tests/InfiniteSky.Server.IntegrationTests/NetworkingTests.cs#L175) records the single-reader ordering requirement and its real-socket verification.

## Pre-change correctness baseline

Before adding the harness or changing runtime code:

| Command | Result | Elapsed |
|---|---|---:|
| `dotnet restore InfiniteSky.sln` | Succeeded | 1.02 s |
| `dotnet build InfiniteSky.sln --no-restore` | Passed; 0 warnings, 0 errors | 4.47 s |
| `dotnet test InfiniteSky.sln --no-build` | 187 passed, 0 failed, 2 skipped | 5.75 s |

The two skipped tests are the opt-in MongoDB Testcontainers tests. Their skip does not verify live database behavior. The final project test totals were Domain 50, Protocol 25, GameData 5, World 22, Persistence 23 passed/2 skipped, and Server integration 62.

## Reproducible measurements

The harness and exact profiles are described in [PERFORMANCE_TEST_PLAN.md](./PERFORMANCE_TEST_PLAN.md). Each profile used one warmup cohort, three measured repetitions, and a five-second idle interval after a successful simultaneous authentication burst. The pre-change results below are the measurements captured before correcting the duplicate receive/send loop starts in `NetworkConnection.StartAsync`.

All requested connections authenticated successfully at each tested point in each repetition. All connections and sessions were zero after cleanup. Network receive/send failure counters were zero in these bounded burst/idle runs. These are login connections/sessions, not active players.

| Simultaneous connections/sessions | Auth completions/s: median (min–max) | Login p50 ms: median (min–max) | Login p95 ms: median (min–max) | Idle CPU % of 24 logical processors: median (min–max) | Idle allocation bytes/s: median (min–max) |
|---:|---:|---:|---:|---:|---:|
| 1 | 1,153 (492–1,548) | 0.786 (0.586–1.636) | 0.786 (0.586–1.636) | 0.078 (0.013–0.078) | 1,055 (766–4,161) |
| 10 | 3,382 (3,299–4,005) | 0.452 (0.250–1.254) | 1.653 (0.744–1.684) | 0.104 (0–0.104) | 1,007 (893–1,082) |
| 50 | 5,415 (3,704–6,294) | 0.451 (0.314–0.528) | 1.280 (1.096–1.845) | 0.273 (0.013–0.365) | 1,155 (1,005–1,164) |
| 100 | 4,577 (4,144–7,323) | 0.420 (0.250–0.477) | 0.727 (0.611–0.761) | 0.273 (0–0.377) | 1,150 (1,006–2,210) |

Authentication throughput is the number of simultaneous clients divided by the elapsed time until all login replies arrived. It is a single burst measurement, not sustained packet throughput. CPU/allocation values describe the five-second idle interval with authenticated sockets open, not the login burst.

The login p99 values, GC counts, post-GC managed heap, working set, thread-pool counts, and cleanup/error counters are emitted in the harness JSON output. No generation collections occurred during the five-second idle intervals. Process memory includes both host and client in the same process, and the managed heap is sampled after forced collections outside the timed interval. These values do not establish leak freedom.

## Baseline limitations

- The Windows dynamic TCP port range is `49152-65535`. An initial repeated reconnect/churn harness exhausted local ephemeral ports before its 50-client run; those incomplete results were discarded. The final harness uses one bounded concurrent cohort per repetition and then holds the authenticated sockets idle.
- The three repetitions and five-second holds are short measurements on one development host. Burst throughput and tail latency vary between repetitions; no stable server-wide capacity or production SLA is inferred.
- `dotnet-counters`, `dotnet-trace`, and `dotnet-gcdump` are not installed. No stack-based CPU profile was captured. CPU/memory/allocation figures are process-level measurements from the harness.
- Live MongoDB, original Node.js differential execution, original-client testing, production game-data loading, and full world/gameplay load testing were unavailable or out of scope for this implemented slice.
