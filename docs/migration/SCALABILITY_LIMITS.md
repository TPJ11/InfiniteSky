# Prompt 23 Scalability Limits

## Measured limit

The highest successfully tested workload was **100 concurrent loopback TCP connections and 100 authenticated in-memory sessions**, held open for approximately five seconds after the login burst, with three repetitions. The server configuration used the checked-in `MaxConcurrentConnections` default of 100. No attempt above that configured limit was made.

This is a tested login/session foundation limit only. **Active players tested: 0. Players per zone tested: 0.** The server does not yet expose character selection, world handoff, client-bound world entry, movement, visibility, or gameplay through the network path.

| Concurrent login connections / sessions | Successful auth per repetition | Auth completions/s median (range) | p95 login latency median (range) | Errors | Cleanup |
|---:|---|---:|---:|---:|---|
| 1 | 1/1, all three runs | 1,153 before; 1,173 after | 0.786 ms before; 0.769 ms after | 0 receive/send failures in measured cohort runs | 0 connections, 0 sessions |
| 10 | 10/10, all three runs | 3,382 before; 4,026 after | 1.653 ms before; 0.754 ms after | 0 receive/send failures in measured cohort runs | 0 connections, 0 sessions |
| 50 | 50/50, all three runs | 5,415 before; 5,279 after | 1.280 ms before; 1.225 ms after | 0 receive/send failures in measured cohort runs | 0 connections, 0 sessions |
| 100 | 100/100, all three runs | 4,577 before; 6,448 after | 0.727 ms before; 1.212 ms after | 0 receive/send failures in measured cohort runs | 0 connections, 0 sessions |

Before/after values are three-run medians from the exact profile described in [PERFORMANCE_BENCHMARK_RESULTS.md](./PERFORMANCE_BENCHMARK_RESULTS.md). They are not a capacity curve: the host and client simulator share a process and the ranges vary. No speedup is inferred.

## Resource and failure limits

- The test host is Windows 11 Pro build `10.0.26300`, 12th Gen Intel Core i9-12900K, 24 logical processors, .NET SDK `10.0.401` and runtime `10.0.12`.
- Traffic is IPv4 loopback; MongoDB is disabled and the account repository is an in-memory test substitute.
- The maximum tested open connection/session count is 100; the configuration limit, not a measured resource saturation point, bounded the test.
- During the five-second idle interval at 100 sessions, process CPU medians were 0.273% before and 0.234% after when normalized across 24 logical processors. Allocations, heap, and working set include both host and simulated clients; see the benchmark results for ranges.
- The measured idle intervals had zero generation collections, zero pending thread-pool work items at repetition end, and zero recorded receive/send failures. This does not measure activity or GC in the short login burst, nor prove long-term leak freedom.
- No failure threshold or overload behavior was reached or tested. Send-queue saturation, the 101st connection, slow clients, burst retries, database failures, and server shutdown under load were not exercised.
- No highest tested gameplay player count or players-per-zone value exists; do not project one from the connection count.

## Limiting factors and untested cases

The 100-connection test point is simply the configured maximum. The evidence does not show whether CPU, memory, OS socket resources, MongoDB, world simulation, packet throughput, or another factor would limit a larger deployment. An earlier unbounded reconnect/churn experiment exhausted the Windows dynamic TCP port range; it was discarded, and the final harness uses bounded cohorts.

The following limits remain unknown or blocked:

- character loading, world entry, movement, broadcasts, visibility, dense/distributed zones, and world tick latency;
- combat, inventory, economy, chat, quests, NPC/monster updates, and persistence under load;
- live MongoDB operation latency, pooling, indexes, outage response, and save throughput;
- script execution, plugin/event pressure, administrative operations, and cross-zone contention;
- long-duration heap retention, connection churn soak, thread-pool starvation, queue growth, and overload response;
- Node.js comparison and original-client load.

The harness and reproduction steps are in [PERFORMANCE_TEST_PLAN.md](./PERFORMANCE_TEST_PLAN.md). Full-server or production-scale scalability claims remain unsupported.
