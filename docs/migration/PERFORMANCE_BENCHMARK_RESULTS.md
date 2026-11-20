# Prompt 23 Performance Benchmark Results

## Result summary

Measurements are limited to the source-compatible login/authentication socket path and five seconds of idle authenticated sessions per repetition. They do not measure active MMORPG players, world ticks, gameplay, packet/action throughput, spatial visibility, MongoDB, or production capacity.

The pre-change run was captured at runtime source commit `76815fdeca856de07a1fb85c5cbe682a87bc3532`. The post-change run used the same host, build mode, harness, connection counts, repetitions, warmup, and duration, with only the receive/send loop ownership fix applied in [`NetworkConnection.cs`](../../src/InfiniteSky.Networking/NetworkConnection.cs). Each cell below reports the median of three repetitions and, in parentheses, the minimum-to-maximum observed across those repetitions.

| Connections/sessions | Phase | Auth completions/s | Login p50 ms | Login p95 ms | Idle CPU % of 24 logical processors | Idle allocations bytes/s |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Before | 1,153 (492–1,548) | 0.786 (0.586–1.636) | 0.786 (0.586–1.636) | 0.078 (0.013–0.078) | 1,055 (766–4,161) |
| 1 | After | 1,173 (503–1,302) | 0.769 (0.690–1.599) | 0.769 (0.690–1.599) | 0.026 (0–0.078) | 968 (892–979) |
| 10 | Before | 3,382 (3,299–4,005) | 0.452 (0.250–1.254) | 1.653 (0.744–1.684) | 0.104 (0–0.104) | 1,007 (893–1,082) |
| 10 | After | 4,026 (1,819–4,280) | 0.516 (0.294–0.643) | 0.754 (0.592–2.413) | 0.104 (0.013–0.117) | 929 (818–1,087) |
| 50 | Before | 5,415 (3,704–6,294) | 0.451 (0.314–0.528) | 1.280 (1.096–1.845) | 0.273 (0.013–0.365) | 1,155 (1,005–1,164) |
| 50 | After | 5,279 (5,149–6,162) | 0.354 (0.268–2.329) | 1.225 (0.696–3.405) | 0.208 (0–0.326) | 1,024 (942–1,087) |
| 100 | Before | 4,577 (4,144–7,323) | 0.420 (0.250–0.477) | 0.727 (0.611–0.761) | 0.273 (0–0.377) | 1,150 (1,006–2,210) |
| 100 | After | 6,448 (5,528–7,345) | 0.646 (0.285–0.650) | 1.212 (0.962–2.047) | 0.234 (0–0.286) | 1,054 (957–1,114) |

The p99 latency values were emitted in JSON for every run. They are intentionally not collapsed into this table because the median of each percentile across only three repetitions can obscure per-repetition tail ordering. The harness output preserves all individual p50/p95/p99 values.

## Memory, GC, queue, and error observations

The numbers below are process-level observations for the combined host and simulated clients. Managed-heap values are snapshots after forced full collections outside the timed idle interval. Working set includes native and managed allocations and is reported as the observed range across the before/after idle snapshots and repetitions.

| Connections/sessions | Managed heap after idle, before / after | Combined process working set before / after | Gen 0/1/2 collections during idle, before / after | Network failures before / after | Active sessions during idle | Cleanup |
|---:|---:|---:|---|---:|---:|---|
| 1 | 2.77–2.95 / 2.77–2.96 MiB | 44.3–67.4 / 44.8–47.9 MiB | 0 / 0 | 0 / 0 | 1 / 1 | 0 connections, 0 sessions |
| 10 | 2.79–2.99 / 2.79–2.96 MiB | 45.1–75.6 / 45.5–51.2 MiB | 0 / 0 | 0 / 0 | 10 / 10 | 0 connections, 0 sessions |
| 50 | 2.69–5.40 / 3.42–3.62 MiB | 45.6–60.5 / 45.7–59.4 MiB | 0 / 0 | 0 / 0 | 50 / 50 | 0 connections, 0 sessions |
| 100 | 4.29–7.93 / 3.66–7.20 MiB | 47.0–63.6 / 45.8–62.2 MiB | 0 / 0 | 0 / 0 | 100 / 100 | 0 connections, 0 sessions |

Thread-pool pending work items were zero at the end of each repetition after cleanup. The harness measures allocation and GC only during the idle interval; it does not claim that the short authentication burst caused zero collections or that retained objects are leak-free.

## Interpretation

- The maximum tested workload was 100 simultaneous loopback TCP connections and 100 authenticated in-memory sessions, held for approximately five seconds, repeated three times. This equals the configured test limit; no larger connection count was attempted.
- Every attempt authenticated successfully and cleanup returned the connection/session counts to zero in all measured repetitions.
- The measured throughput medians move in mixed directions: approximately +1.7% at 1, +19.0% at 10, -2.5% at 50, and +40.9% at 100. At 100, median p95 login latency increased from 0.727 ms to 1.212 ms. The ranges overlap and vary materially between repetitions.
- Therefore, these measurements do **not** establish a consistent throughput or latency improvement from the code change. The change is retained for correctness and lifecycle ownership: it removes duplicate, untracked receive/send loops and restores the one-reader/one-writer pipeline. The regression test failed before the change and passed afterward.
- The idle CPU and allocation rates are near the measurement floor for this host and this idle workload; they are not indicators of packet-processing, world simulation, or gameplay CPU cost.
- No profiling evidence identified a codec, spatial-query, database, world-loop, scripting, or gameplay hotspot. No speculative optimization was made in those paths.

## Reproducibility and limitations

Use the commands and parameters in [PERFORMANCE_TEST_PLAN.md](./PERFORMANCE_TEST_PLAN.md). The harness writes JSON Lines containing each repetition's latency percentiles, CPU, allocations, heap/working-set snapshots, GC counts, active sessions, failures, and cleanup values. The test parameters describe an experiment, not actual player behavior.

This is a Windows 11 development-host result with .NET SDK 10.0.401/runtime 10.0.12, loopback transport, disabled MongoDB, and an in-memory account repository. It is not compared with Node.js or an original client. No world/player workload, sustained gameplay soak, dense-zone, distributed-zone, database failure, send-queue saturation, or overload behavior was tested.
