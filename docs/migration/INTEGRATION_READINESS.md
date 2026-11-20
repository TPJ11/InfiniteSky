# Prompt 20 Integration Readiness

## Decision

**Prompt 20 established a bounded host/network/login integration baseline; Prompt 21 confirmed the server is still not ready for original-client testing.** The integrated socket path ends at authenticated account session creation. Prompt 21 added the source-derived login greeting and tested it with simulated loopback clients, but no original game-client build or attributable client capture was available. Character/world protocols, gameplay, persistence-backed client journeys and original game assets remain absent, incomplete, or blocked.

## Readiness checklist

| Gate | State | Evidence / remaining work |
|---|---|---|
| Baseline restore/build/tests/format | Verified for current tree | Prompt 20 restore, full build and format passed; 186 tests pass and 2 opt-in Mongo tests are skipped. See [integration baseline](./INTEGRATION_BASELINE.md) and [migration status](../../MIGRATION_STATUS.md). |
| Host composition and lifecycle | Verified within tests | Host startup/shutdown and hosted-service ordering pass. Default configuration starts with networking/database disabled and world readiness degraded; no production-style process/deployment smoke test was run. |
| Networking/protocol/authentication | Partial integration verified | Real host/TCP login, response, session, duplicate rejection, reconnect and shutdown cleanup pass with a test account repository. World packets are not dispatched; the world listener may bind while world readiness is degraded (`R-052`); Mongo-backed login remains unverified. |
| Startup failure/rollback | Verified within tests | Invalid options and unavailable Mongo fail startup; an occupied world port rolls back the already-started login listener. Post-start listener failure recovery is untested. |
| Character selection/loading | Not ready | No client character list/create/delete/start handlers or full Character wire codec. |
| World entry and zone transition | Not ready | No world authentication/transfer dispatch, client-bound activation, visibility initialization, or client notifications. |
| Persistence readiness | Not ready for integrated workflows | Account/guild/location slices exist, but Mongo tests are opt-in/skipped; representative production BSON, full character round-trip and coordinated saves are absent. |
| Required game data/assets | Not ready | Repository has spawn `.MOB`/`.NPC` and `.pac` files but no required `.IMG` catalogs or `.WREGION` corpus; MOB runtime loading is incomplete. |
| World/zone/entity integration | Not ready | Zone lifecycle/spatial components and internal player activation are tested, but network entry, transfer, entity replication and simulation are not connected. |
| Gameplay | Not ready | XP/stat arithmetic remains isolated; movement, combat, inventory, skills, NPC/monster simulation, social/economy flows are not client-reachable. |
| Scripting/plugins | Not ready | No compatible root shared-VM or alternate FruitSalad runtime/API or script event bridge. |
| Administration/moderation | Not ready | Account `Level` is in the session, but world chat dispatch, command execution/response, active-character context and moderation services are absent. |
| Original-client end-to-end transcript | Not ready | No complete login → character → world → gameplay → reconnect transcript or canonical client captures. |
| External database compatibility | Not verified | Only synthetic BSON fixtures are available; the 2 opt-in Mongo integration tests were skipped. |
| Original Node.js source unchanged | Verified | Prompt 20 changed only the C# test harness and migration documentation; no original Node.js source or production data was edited. |

## What is ready

- Foundation Generic Host and configuration validation.
- Host-composed loopback login request/reply, session lifecycle, duplicate login, reconnect and shutdown tests using a deterministic test repository.
- Partial network startup rollback when the world port is occupied.
- MongoDB account authentication repository boundary and targeted persistence slices.
- World/zone lifecycle and spatial infrastructure unit/host tests.
- Internal player activation and supported saved-location cleanup boundaries.
- A source-indexed feature matrix and [remaining gap register](./REMAINING_FEATURE_GAPS.md).

These results verify a narrow login integration and several host boundaries, not full-system integration, live MongoDB compatibility, or original-client compatibility. See [full-system integration status](./FULL_SYSTEM_INTEGRATION.md), [scenarios](./END_TO_END_TEST_SCENARIOS.md), and the [test harness](./INTEGRATION_TEST_HARNESS.md).

## Required preconditions before original-client testing

1. Provide an authorized original client build with provenance, representative game-data assets, and sanitized legacy BSON samples.
2. Resolve and fixture the required character, world-auth, world-state, and transfer packets.
3. Implement source-backed character list/selection/world-entry dispatch and lifecycle wiring.
4. Establish persistence ownership and safe character/item/guild save behavior.
5. Connect zone/player/entity state to actual packet output and disconnect cleanup.
6. Implement only gameplay/social/admin features whose source, data and protocol dependencies are verified.
7. Execute isolated MongoDB integration tests and verify every planned client workflow with the original client over actual sockets.

The above blockers make original-client testing premature. Prompt 21 source-derived work is complete, but no original-client compatibility claim is made. Do not begin Prompt 22 without explicit approval.
