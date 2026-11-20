# Final Feature Reconciliation

## Source of truth and method

The original root Node.js runtime is the authoritative behavior reference. `FruitSalad/` is a distinct alternate/experimental runtime and is not merged into root behavior. This reconciliation uses the 232 feature rows in [FEATURE_PARITY_MATRIX.md](../FEATURE_PARITY_MATRIX.md), existing source-coverage and gap reports, current C# projects/tests, Prompt 01–24 reports/checkpoints, and Prompt 25 baseline. A report's existence is not treated as proof; only linked implementation and test evidence support status.

The matrix was parsed by feature ID with no duplicates. Current status totals are:

| Status | Features |
|---|---:|
| `NOT_STARTED` | 136 |
| `IN_PROGRESS` | 8 |
| `IMPLEMENTED` | 48 |
| `VERIFIED` | 14 |
| `BLOCKED` | 25 |
| `NOT_APPLICABLE` | 1 |
| **Total** | **232** |

No Prompt 25 feature status was promoted. The 14 `VERIFIED` entries are bounded tests of the exact feature criteria, not evidence of full server parity.

## Subsystem reconciliation

| Subsystem | Current matrix status counts | Verification evidence | Main remaining limitation | Release impact |
|---|---|---|---|---|
| Infrastructure | 11 `NOT_STARTED`, 3 `IMPLEMENTED`, 4 `VERIFIED` | Generic Host, config, startup/shutdown and host tests | No production deployment/service wrapper; readiness is available through registered health checks but no HTTP endpoint exists | Partial foundation only |
| Networking | 4 `NOT_STARTED`, 3 `IMPLEMENTED`, 3 `VERIFIED` | Loopback TCP framing, bounds, ordering, cleanup, and login host tests | World listener has no client gameplay dispatch; raw TCP is not encrypted | Blocks public/playable release |
| Protocol | 25 `NOT_STARTED`, 1 `IN_PROGRESS`, 2 `IMPLEMENTED`, 5 `VERIFIED` | Binary primitives and narrow source-derived login/handshake/structure fixtures | Character/world tails, most request/response families and compressed state unresolved | Blocks client compatibility |
| Authentication | 2 `IMPLEMENTED`, 2 `VERIFIED` | Loopback login outcome, session and source-derived packet tests | Legacy plaintext storage and cleartext transport; no original client or live DB | High-severity release blocker |
| Sessions | 3 `IMPLEMENTED` | Same-process duplicate and cleanup tests | Process-local ownership; full character/world transfer not connected | Blocks complete login journey |
| Persistence | 8 `NOT_STARTED`, 1 `IN_PROGRESS`, 5 `IMPLEMENTED` | Synthetic BSON mapping and component tests; 2 Mongo integration tests skipped | No representative production BSON, full round-trip, or live Mongo validation | Blocks data acceptance |
| Game data | 11 `NOT_STARTED` | Tracked NPC parsing fixture and unit tests | `.IMG` and `.WREGION` corpus absent; source catalog incomplete | Blocks gameplay/zone behavior |
| Domain | 5 `NOT_STARTED`, 10 `IMPLEMENTED` | Model/value tests and isolated source-derived XP/stat arithmetic tests | No connected game-data, handler, persistence or runtime simulation | Foundation, not playable |
| World engine | 10 `NOT_STARTED`, 3 `IMPLEMENTED` | Lifecycle, readiness, clock component tests | No integrated zone simulation or gameplay tick consumers | Blocks world gameplay |
| Zones/spatial | 4 `NOT_STARTED`, 6 `IMPLEMENTED` | Loader, lifecycle, spatial boundaries/concurrency tests | No portal transitions, entity spawning, terrain/navigation, or real `.WREGION` corpus | Blocks map/client flow |
| Entities/players | 8 `NOT_STARTED`, 11 `IMPLEMENTED`, 2 `BLOCKED` | Internal ownership, activation, registry, persistence-location and cleanup component tests | No packet-driven selection/activation, full character codec, spawn/despawn or gameplay | Blocks playable state |
| Gameplay | 35 `NOT_STARTED`, 2 `IN_PROGRESS`, 1 `BLOCKED` | Isolated XP/stat calculations only | Movement/combat/inventory/skills/NPC/quests/social/economy not client reachable; original code includes stubs/dormant paths | Critical completeness blocker |
| Scripting/plugins | 4 `NOT_STARTED`, 8 `BLOCKED`, 1 `NOT_APPLICABLE` | Source inventory and trust-boundary docs; no runtime tests | No .NET script runtime/plugin bridge; root shared Node globals and FruitSalad RPC differ | Blocks legacy script parity |
| Administration | 13 `BLOCKED` | Source command inventory and account-level session mapping tests | No world chat dispatch, command execution, operator CLI or moderation service | Blocks admin parity |
| Integration | 11 `NOT_STARTED`, 4 `IN_PROGRESS`, 1 `BLOCKED` | Host/socket login; 100-connection login harness; component regression tests | No complete client journey, live data/database, gameplay or original-client run | Blocks migration acceptance |

## Behavioral parity disposition

- **Protocol:** Only source-backed structures and a limited login path are implemented and covered. The 18-byte handshake request fixture does not implement its world response or transfer.
- **Authentication:** Legacy login status behavior is implemented for a narrow path and exercised with a test repository. Mongo account persistence is implemented but not live-verified.
- **Characters/persistence:** Account-owned reads, selected character ownership, and targeted saved-location update are internal components. No login character-slot packet or world entry packet handler connects them.
- **Zones/world:** A single root world and static zone manager exist; optional resource handling and spatial candidates are covered. This is not a simulation.
- **Gameplay:** Only two isolated arithmetic slices are in progress. Do not treat them as migrated player progression.
- **Scripting/administration:** Audited and documented, not implemented in the .NET runtime.
- **Original client:** No authorized original game client build was available. No original-client compatibility claim is made.

For source stubs, experimental paths, unported data, protocol ambiguities and acceptance criteria per ID, see [REMAINING_FEATURE_GAPS.md](REMAINING_FEATURE_GAPS.md), [SOURCE_COVERAGE_AUDIT.md](SOURCE_COVERAGE_AUDIT.md), and subsystem reports under `docs/migration/`.
