# Prompt 20 Integration Readiness

## Decision

**Not ready to begin the full-system integration stage.** The current repository has a buildable .NET host and tested foundation slices, but several required subsystems are missing, client-unreachable, or blocked on source artifacts. Starting a broad integration campaign now would exercise placeholder boundaries rather than the source-verified client journey. This is a readiness assessment only; Prompt 20 has not started.

## Readiness checklist

| Gate | State | Evidence / remaining work |
|---|---|---|
| Solution restore/build/test/format | Ready for bounded development | Prompt 19 validation: restore succeeds; full solution build has no warnings/errors; tests and format results are in `MIGRATION_STATUS.md`. |
| Dependency injection and host lifecycle | Foundation verified, domain integration incomplete | Existing host/DI/lifecycle tests pass. Required gameplay, data, world packet and administration handlers are not registered because they do not exist. |
| Configuration and network startup | Foundation implemented | Validated options and TCP listeners exist; networking remains disabled by default. World listener still lacks gameplay dispatch. |
| Authentication/session | Login slice implemented; legacy parity incomplete | Login packet and in-process session tests exist. Server-derived account `Level` is now carried by `AuthenticatedSession`; duplicate policy differs and live-world transfer is absent. |
| Character selection and world entry | Not ready | No client character list/create/delete/start packets, full Character wire codec, transfer coordinator, or world auth dispatch. |
| Persistence readiness | Not ready | Account/guild/location slices exist, but Mongo tests are opt-in/skipped, production BSON/collection metadata is unavailable, full character round-trip and save queue are absent. |
| Required game data/assets | Not ready | `.IMG` catalogs and `.WREGION` evidence are missing; MOB loading is disabled in the legacy root source. |
| World/zone/entity integration | Not ready | Zone lifecycle/spatial candidates and internal player activation exist, but network entry, transfer, entity population/replication and simulation are not wired. |
| Gameplay | Not ready | Pure XP/stat arithmetic is isolated; no integrated movement, combat, inventory, skills, progression packets, NPC/monster simulation, social or commerce flows. |
| Scripting/plugins | Not ready | No compatible root shared-VM or alternate FruitSalad runtime/API; no script event bridge. |
| Administration/moderation | Not ready | Account level is available in the session, but no world chat dispatch, command execution/response, active-character authorization context, or source-backed moderation service. |
| Original-client end-to-end transcript | Not ready | No complete original client login → character → world → gameplay → reconnect transcript or canonical captures. |
| External database compatibility | Not ready | Only synthetic BSON fixtures are available; integration container tests were skipped without Docker/opt-in. |
| Original Node.js source unchanged | Ready | Prompt 19 changes are limited to C# code/tests and migration documentation; no Node files were edited. |

## What is ready

- Foundation Generic Host and configuration validation.
- Login authentication request/reply slice and in-process session lifecycle tests.
- MongoDB account authentication repository boundary and targeted persistence slices.
- World/zone lifecycle and spatial infrastructure unit/host tests.
- Internal player activation and supported saved-location cleanup boundaries.
- A source-indexed feature matrix and [remaining gap register](./REMAINING_FEATURE_GAPS.md).

These are component-level results, not full-system integration or client compatibility.

## Required preconditions before Prompt 20

1. Provide authorized representative game-data assets and sanitized legacy BSON samples.
2. Resolve/fixture required character, world-auth, world-state, and transfer packets.
3. Implement source-backed character list/selection/world-entry dispatch and lifecycle wiring.
4. Establish persistence ownership and safe character/item/guild save behavior.
5. Connect zone/player/entity state to actual packet output and disconnect cleanup.
6. Implement only the gameplay/social/admin features whose source/data/protocol dependencies are verified.
7. Re-run the parity matrix and verify every planned integration path has executable, isolated tests.

The full-system integration work remains gated on explicit user approval; no Prompt 20 work is authorized or started by this report.
