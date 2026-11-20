# Prompt 22 Verification Readiness

## Decision

The .NET solution has a repeatable, passing foundation test suite and source-derived evidence for a limited set of protocol, authentication, persistence-mapping, domain and world-management slices. It is **not ready to claim server-wide behavioral parity**, live persistence compatibility, or original-client compatibility.

This report records Prompt 22 readiness and remains applicable to the current migration. Prompt 23 was subsequently approved and ran a limited loopback login/session benchmark; that evidence does not make the server ready for representative full-server performance testing.

## Evidence availability

| Evidence level | Available? | Scope |
|---|---|---|
| A — original client compared with .NET | No | No original client build/provenance was available and no client was run. |
| B — Node.js differential execution | No | Node is installed, but legacy dependencies/configuration/test target are absent; no Node process ran. |
| C — source-derived fixture | Yes, limited | Login and handshake byte fixtures; synthetic BSON; tracked `.NPC` file. Several checked-in protocol fixture files are not consumed by tests. |
| D — source-derived rules/state tests | Yes, limited | Authentication decisions, domain arithmetic/models, spatial boundaries, lifecycle and failure transitions. |
| E — implementation-only tests | Yes | Host composition, configuration, DI, network lifecycle and diagnostics behaviors. |

## Subsystem readiness

| Subsystem | Current evidence | Remaining blocker | Readiness |
|---|---|---|---|
| Infrastructure/configuration | Host lifecycle, configuration, listener ordering and selected failure paths | Full original startup order, all dependency failures and production readiness semantics | Partial |
| Networking/framing | Loopback TCP, fragmentation/coalescing, bounds, disconnect and send serialization | Queue saturation, idle timeout expiration, world packet dispatch and external-client behavior | Partial |
| Protocol | Source-derived login/greeting fixtures and one new world-handshake request fixture | Character/world state layouts, most opcodes, canonical compressed captures and full fixture execution | Partial |
| Authentication/sessions | Simulated login and deterministic session/repository tests | Live account repository execution and exact legacy stale-active/world-socket duplicate policy | Partial |
| MongoDB persistence | BSON mappers and configuration/failure tests | Both Testcontainers tests skipped; no production BSON, live query/index/recovery evidence | Blocked for live compatibility |
| Game data | `.NPC` and synthetic region parsing | `.IMG` catalogs and `.WREGION` corpus missing; no full world bootstrap | Blocked for full data compatibility |
| Domain/world/zones | Model, lifecycle, spatial and player activation component tests | Full gameplay, client visibility, transfer, movement and actual persistence connections | Partial |
| Gameplay | Synthetic XP and stat allocation arithmetic only | Original data, handlers, formulas/packets, deterministic integration and persistence | Not ready |
| Scripting/plugins | No C# scripting implementation or tests | Supported runtime/API boundary and original source behavior remain blocked | Not ready |
| Administration | No command dispatcher/handler tests | World chat ingress, command scope, authorization and effects remain blocked | Not ready |
| End-to-end | Loopback login path with test account persistence | Character selection, world/zone entry, gameplay, Mongo and client flow | Not ready |
| Differential/original client | None | Missing runtime/client prerequisites and executable workflow | Blocked |
| Concurrency/failure recovery | Selected deterministic concurrency/failure cases | Races and recovery across DB, player gameplay, inventory, scripting and shutdown are absent | Partial |
| Performance | Prompt 23 adds a measured real-socket login/authentication baseline at 1/10/50/100 connections and five-second idle sessions | World/gameplay workflows, live MongoDB, complete protocol traffic, representative data, and long-duration workloads are absent | Not ready for full-server performance claims |

## Reliability and limitations

- Three full-suite repeat runs before the Prompt 22 test addition completed with the same baseline counts: 186 passed, 0 failed, 2 opt-in MongoDB tests skipped. A separate coverage run also completed with those counts.
- The new handshake fixture test passed in isolation. One final full-suite run is required after documentation/test changes; its result is recorded in [COMPREHENSIVE_REGRESSION_REPORT.md](./COMPREHENSIVE_REGRESSION_REPORT.md).
- No flaky failure was observed in those repeated runs. `WorldClockTests` uses system time and short delays, and listener tests reserve then release ephemeral ports; repeated success does not prove these tests are deterministic under all CI load.
- Coverage collector output is available, but line/branch coverage is not parity evidence. No target threshold is defined.
- No claim of resource-leak freedom, production database safety, full field preservation, original-client interoperability or production readiness is supported.

## Gate

The Prompt 23 host/login measurements are valid only for the implemented foundation and are documented in [PERFORMANCE_BENCHMARK_RESULTS.md](./PERFORMANCE_BENCHMARK_RESULTS.md). They do not represent the migrated MMORPG server. Full-server performance work still requires implemented source-supported world/gameplay workflows, representative fixtures/data, isolated persistence evidence, and workload-specific measurements. The original-client, Node differential, and live MongoDB compatibility gates remain unresolved.
