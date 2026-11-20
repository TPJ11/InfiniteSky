# Prompt 23 Performance Optimizations

## Retained change: one receive loop and one send loop per connection

### Finding

Before the change, `NetworkConnection.StartAsync` invoked `ReceiveLoopAsync` before the connection-open callback and invoked it again after that callback. It also invoked `SendLoopAsync` twice and overwrote `_sendTask`, leaving the first tasks untracked. This contradicted the documented single receive/single send pipeline, caused concurrent reads against the same `LegacyReceiveAccumulator`, and allowed multiple readers to drain the send channel and write the same stream.

The login greeting must be sent during `OnConnectionOpenedAsync`, so the send loop remains started once before that callback. Receive processing starts once after the callback returns, preserving greeting-before-request ordering.

### Change and compatibility risk

Removed only the premature receive-loop start and the duplicate send-loop start. Packet layouts, validation, socket options, queue capacity, protocol ordering, authentication, and gameplay rules were not changed. The single-reader/single-writer invariant follows the original source's serialized socket processing and the existing C# transport design.

### Evidence and verification

- Added `TcpListenerProcessesMultipleFramesOnOneOrderedReceiveLoop`, which sends 64 source-sized `0x03` frames over one real TCP connection and asserts ordered, non-concurrent handler dispatch. The writes are separate; TCP coalescing is not assumed.
- The new test **failed before the change** with a five-second timeout while waiting for all frames.
- The same test **passed after the change**.
- In the measured 1/10/50/100 connection profiles, all login/authentication attempts succeeded before and after, failure counters remained zero, and connections/sessions returned to zero.
- The exact before/after throughput and latency results are in [PERFORMANCE_BENCHMARK_RESULTS.md](./PERFORMANCE_BENCHMARK_RESULTS.md). They are mixed and noisy; no consistent speedup is claimed. The change is retained for correctness, ordering, and task ownership rather than a claimed benchmark win.

## Other optimization candidates

No measured hotspot justified changes to frame parsing/serialization, session lookup, world/zone processing, spatial queries, gameplay, MongoDB access, scripting, administration, or garbage collection. Those paths were not optimized.

No new production metrics, monitoring platform, cache, packet batching, queue policy, parallel world processing, unsafe memory handling, or protocol behavior was introduced. Existing `System.Diagnostics.Metrics` instrumentation covers selected network/auth/world/zone/player/Mongo events; per-packet duration, queue depth, and gameplay/script durations remain uninstrumented. The available measurements and environment limits are documented in [PERFORMANCE_BASELINE.md](./PERFORMANCE_BASELINE.md).

## Rejected or deferred work

| Candidate | Decision | Evidence / blocker |
|---|---|---|
| Packet buffer pooling or zero-copy parsing | Deferred | No stack/allocation profile identifies packet copies as a bottleneck; exact protocol behavior must remain unchanged. |
| Session/player lookup changes | Deferred | No lookup bottleneck observed; player-world lifecycle is not connected to the client path. |
| Spatial index or zone-loop changes | Deferred | Only component tests exist; no representative zone corpus or reachable player visibility workload is available. |
| Mongo query/cache/index changes | Deferred | MongoDB is disabled/unavailable; no live query evidence. |
| Gameplay, scripting, or command optimization | Blocked | These production workflows are not implemented or reachable through the server network path. |
| Added monitoring platform or production-wide instrumentation | Not added | Existing diagnostics plus bounded harness measurements suffice for the narrow test; no platform approval or overhead evidence exists. |
