# InfiniteSky Migration Status

## Authority and update rules

This file is the authoritative dashboard for the .NET 10 migration. Update it only from the feature matrix and linked test evidence.

- The .NET 10 solution foundation, core domain models, binary protocol foundation, TCP infrastructure, authentication flow, MongoDB account/guild-read/character-location persistence slices, world/zone infrastructure, and internal player activation boundary are implemented where evidence supports it. Prompt 16 has isolated XP and stat-allocation arithmetic slices in progress; full packet, data, entity simulation, database, and gameplay migration remains incomplete.
- A feature may be `VERIFIED` only after its test/acceptance evidence exists and is linked.
- `IMPLEMENTED` is not `VERIFIED`.
- Blockers are documented below, in the matrix notes, and in `docs/migration/MIGRATION_RISKS.md`.

## Current summary

| Metric | Count |
|---|---:|
| Total features | 227 |
| NOT_STARTED | 159 |
| IN_PROGRESS | 4 |
| IMPLEMENTED | 48 |
| VERIFIED | 14 |
| BLOCKED | 2 |
| NOT_APPLICABLE | 0 |
| Overall verified progress | 6.2% |

Prompt 15 records full character/world protocol integration and full original-client player/entity compatibility as `BLOCKED` in the matrix because required codecs, derived stats, fixtures, and client evidence are absent. Foundation evidence is recorded in [FOUNDATION_IMPLEMENTATION.md](docs/architecture/FOUNDATION_IMPLEMENTATION.md).

## Subsystem progress

| Subsystem | Total | Implemented | Verified | Blocked | Progress |
|---|---:|---:|---:|---:|---:|
| Infrastructure | 18 | 3 | 4 | 0 | 38.9% |
| Networking | 10 | 3 | 3 | 0 | 60% |
| Protocol | 33 | 2 | 5 | 0 | 21.2% |
| Authentication | 4 | 2 | 2 | 0 | 100% |
| Sessions | 3 | 3 | 0 | 0 | 100% |
| Persistence | 14 | 5 | 0 | 0 | 35.7% |
| Game data | 11 | 0 | 0 | 0 | 0% |
| Domain | 15 | 10 | 0 | 0 | 66.7% |
| World engine | 13 | 3 | 0 | 0 | 23.1% |
| Zones/spatial | 10 | 6 | 0 | 0 | 60% |
| Entities/players | 21 | 11 | 0 | 2 | 52.4% |
| Gameplay | 38 | 0 | 0 | 0 | 0% |
| Scripting/plugins | 8 | 0 | 0 | 0 | 0% |
| Administration | 13 | 0 | 0 | 0 | 0% |
| Integration/compatibility | 16 | 0 | 0 | 0 | 0% |
| **Total** | **227** | **48** | **14** | **2** | **27.3%** |

## Completed domain model work

- Strong identifiers for accounts, characters, entities, zones, items, skills, monsters, and NPCs.
- `Position3`, grid, direction, and rotation values preserving source numeric semantics.
- Account, character, equipment, inventory, skills, stats, guild, per-server bank, and runtime character state.
- Item, skill, monster, NPC, and experience definitions from named original record fields.
- Monster/NPC/world-item runtime entities and zone spawn/region descriptors.
- 50 domain tests covering compatibility models, defaults, null/sparse arrays, identifiers, enum values, numerics, definitions, identity, XP progression, and stat allocation arithmetic.
- `LegacyExperienceAwarder` and `LegacyStatPointAllocator` source-arithmetic calculations with synthetic threshold, point-band, missing-record, cap and allocation-key tests; not connected to runtime, packet handling, persistence, or original game data.

Full persistence round-trip, world simulation, complete packet lifecycle, and gameplay algorithms remain `NOT_STARTED`, `IN_PROGRESS`, or `BLOCKED` as listed in the feature matrix.

## Completed binary protocol work

- Bounded little-endian reader/writer with signed/unsigned 8/16/32, float32, fixed strings, fixed bytes, offsets, and clear read/write exceptions.
- Explicit normal float and original scalar `res+1` restruct float compatibility.
- Nine-byte receive-frame parser and fixed opcode registry/core fixed descriptors.
- Login greeting, login request, world handshake, zone change, map reply, pin, equipment, storage, gift, quick-use, spawn, WREGION, attack, and uncompressed state-envelope structures.
- Original Node fixtures for login greeting, login request/reply, and scalar CVec3 floats.
- 24 protocol tests pass, including fixed string, boundaries, frame parsing, opcode duplicate, and exact structure round trips.

The complete Character/world packet tails and compressed state remain unresolved.

## Completed networking work

- TCP-only login and world listeners with validated loopback/address and configured ports.
- Shared connection manager with unique IDs, states, limits, exact removal, and disposal.
- Nine-byte receive framing with fixed-payload opcode registry integration.
- Fragmented/coalesced/multi-frame receive accumulator with bounded buffering.
- One ordered bounded send channel per connection.
- Remote disconnect, idle timeout, unknown opcode, receive/send failure, and listener shutdown handling.
- Login authentication packet sink with outcome/session diagnostics; world/gameplay packet handlers remain future.
- Real networking metrics for active/accepted/disconnected connections, frames, bytes, and failures.
- Loopback tests for listener, receive, send, limits, and remote disconnect.

## Completed authentication and session foundation

- Login endpoint `0x03` decoding and 41-byte legacy result encoding, with success and account-not-found hex fixtures.
- Application-layer plaintext credential comparison matching the observed legacy string rule; the transient password byte buffer is zeroed and credentials are excluded from logs and sessions.
- Per-connection authentication state, timeout/cancellation handling, account/session boundaries, and a process-local session store.
- Login connection-close cleanup and duplicate-account prevention, including concurrent same-account login tests.
- Structured authentication outcomes and real attempt/success/rejection/failure/duplicate/session metrics.
- The production MongoDB repository is registered; database access remains opt-in and live legacy-data compatibility is not verified.

Login character slots, world transfer, and distributed sessions remain incomplete. The legacy plaintext password storage and the stricter in-process duplicate policy are documented in the authentication and session implementation reports.

## Completed MongoDB persistence slice

- Official MongoDB C# driver with one DI-owned client, validated configuration, bounded startup/health probes, explicit error categories, and driver retry reads/writes disabled.
- Raw BSON account lookup and field-only `active` updates behind Prompt 11's repository interface; missing/null credential behavior fails closed.
- Configurable unique username index verification/bootstrap, with no index drops or schema migration.
- Startup `active=0` reset matches the root Node behavior and is configurable; it affects the entire selected account collection.
- BSON fixture, mapping/config/DI/error/redaction tests, and an opt-in ephemeral Testcontainers integration test.
- `dotnet restore` and full solution build pass. Runnable tests pass; the MongoDB container test is skipped unless `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true` is set. The fixture is synthetic, not a production database export.

This is not full persistence parity. Prompt 15 adds character reads and targeted saved-location writes only; whole-character round-trip, counters, guild writes/repair, email, banks, inventory, gameplay writes, save coordination, and data comparison remain incomplete. The inferred `account_mongooses`, `character_mongooses`, and `guilds` collection names, deployed index state, BSON numeric subtypes, server version, and authentication database must be confirmed before production use.

## World management foundation (Prompt 13)

- Implemented the root server's single-world lifecycle, readiness reporting, and host ordering; no world ID, multi-world registry, or world selection was found in the root Node startup.
- Added the source-backed 100 ms default cadence as validated `InfiniteSky:World:TickMilliseconds` configuration. At the Prompt 13 checkpoint the production clock remained idle because no zone provider existed; Prompt 14 now supplies the real provider.
- Added the asynchronous MongoDB guild bootstrap and the legacy guild field mapper. The inferred `guilds` collection name and live BSON compatibility remain unverified.
- Added the `IWorldZoneReadiness` boundary without a fake zone. Prompt 14 now registers a static zone manager; networking/world entry, zone simulation, players, and gameplay remain incomplete.
- `WLD-011..013` are `IMPLEMENTED`, not `VERIFIED`; tests establish unit/host integration behavior but do not establish original-client or live-database parity. See [WORLD_MANAGEMENT_IMPLEMENTATION.md](docs/migration/WORLD_MANAGEMENT_IMPLEMENTATION.md) and [WORLD_LIFECYCLE.md](docs/migration/WORLD_LIFECYCLE.md).
- Validation from Prompt 13: solution restore and build succeeded with zero warnings; 125 tests passed, 1 opt-in MongoDB test was skipped, and full-solution format verification passed.

## Zone management and spatial infrastructure (Prompt 14)

- Added the exact 62-ID static catalog from `config.json-dist`; the default C# zone list remains empty and unknown IDs are not synthesized.
- Added cancellable `.NPC` and signed `.WREGION` readers. Missing files remain optional as in the original loader; malformed files fail zone startup. NPC rows are parsed as definitions only; MOB population remains disabled.
- Added zone lifecycle, bounded startup, registry/duplicate checks, clean shutdown of runtime-owned resources, X/Z QuadTree queries, source action-distance candidates, health, and metrics.
- The zone manager now supplies `IWorldZoneReadiness` and starts before the world. Empty configuration remains degraded; a configured nonempty zone set can clear only the zone blocker, not guild/persistence blockers.
- `ZON-001..003`, `ZON-006`, and `ZON-009..010` are `IMPLEMENTED`, not `VERIFIED`. `ZON-004..005` and `ZON-007..008` remain `NOT_STARTED`; this stage does not implement portals/transfers, entity spawning, safe-zone policy, terrain, or navigation.
- The repository tracks 52 `.NPC` and 56 `.MOB` spawn files but has no `.WREGION` files. Region tests therefore use structure-derived fixtures and do not establish live map compatibility.
- See [ZONE_MANAGEMENT_IMPLEMENTATION.md](docs/migration/ZONE_MANAGEMENT_IMPLEMENTATION.md), [ZONE_LIFECYCLE.md](docs/migration/ZONE_LIFECYCLE.md), [SPATIAL_SYSTEMS.md](docs/migration/SPATIAL_SYSTEMS.md), and [ZONE_VISIBILITY_AND_TRANSITIONS.md](docs/migration/ZONE_VISIBILITY_AND_TRANSITIONS.md).
- Final Prompt 14 validation: `dotnet restore` succeeded; the complete solution built with zero warnings/errors; 144 tests passed and one opt-in MongoDB test was skipped; `dotnet format --verify-no-changes` passed. No original Node.js source or legacy game-data asset files changed.

## Player and entity infrastructure (Prompt 15)

- Added a Mongo character mapper/repository using raw BSON, account/server-scoped reads, the root three-character result limit, and targeted `MapID`/`RealX/Y/Z` updates. Unknown BSON fields are not deserialized or overwritten.
- Added authenticated character listing/activation boundaries. Activation requires current session, matching account ownership, complete saved position/map, and a running zone; no automatic selection or fallback spawn is used.
- Added a thread-safe player registry keyed separately by character, authentication session, world connection, and `(zone, EntityId)`. Runtime IDs remain `uint` and zone-local; persistent IDs are not reused as entity IDs.
- Added atomic spatial registration with zone shutdown, player position update/removal, nearby-player candidate queries, disconnect snapshots, and graceful location save/cleanup before world and zones stop.
- No client character/world packets, full Character wire codec, derived stats, HP/Chi mutation, real network disconnect hook, NPC/monster runtime spawning, or gameplay simulation were added. `ENT-020` and `ENT-021` remain `BLOCKED`; full `PER-003` remains `IN_PROGRESS`.
- See [PLAYER_ENTITY_IMPLEMENTATION.md](docs/migration/PLAYER_ENTITY_IMPLEMENTATION.md), [ENTITY_LIFECYCLE.md](docs/migration/ENTITY_LIFECYCLE.md), [CHARACTER_PERSISTENCE.md](docs/migration/CHARACTER_PERSISTENCE.md), [PLAYER_SESSION_AND_WORLD_ENTRY.md](docs/migration/PLAYER_SESSION_AND_WORLD_ENTRY.md), and [ENTITY_VISIBILITY_INTEGRATION.md](docs/migration/ENTITY_VISIBILITY_INTEGRATION.md).
- Prompt 15 verification: `dotnet restore InfiniteSky.sln` succeeded; the complete solution built with zero warnings/errors; all 156 runnable tests passed and the two opt-in MongoDB container tests were skipped; `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` passed. Docker is unavailable, so no live MongoDB test or production BSON compatibility is claimed.

## Outstanding domain gaps

- `MonstersKilled` has no typed key/usage evidence and remains deferred.
- Packet character tails, NPC/monster unknown records, and quest state are not modeled.
- Most domain models still lack persistence/protocol mappers; the character raw-BSON adapter is a narrow read/targeted-write slice with synthetic fixtures, not a full round-trip. Affected matrix rows remain `IMPLEMENTED` or `IN_PROGRESS`, not `VERIFIED`.
- Numeric widths at packet boundaries require later adapter validation.

## Critical outstanding features

The immediate critical path is:

1. `PRO-001..004`, `PRO-015`, `PRO-017`, `PRO-032` — framing and unresolved wire structures.
2. `DAT-001..008` — original game data and world data readers.
3. `DOM-007..008` and persistence/protocol mappings for implemented models — progression, stats, and boundary conversion. `GP-004` only has isolated arithmetic; it needs the missing experience catalog and integration.
4. `PER-003`, `PER-008`, `PER-009`, `PER-011..012` — full character/BSON round-trip and safe save behavior.
5. `NET-008..009`, `PER-003..012` — session transfer, character/gameplay persistence, and full schema compatibility.
6. `GP-001..003`, `INT-001` — end-to-end character/world entry.
7. `GP-005..038` — remaining gameplay; implement in dependency order only after source, game data and packet evidence are available.
8. `WLD-001..010`, `ZON-004..005`, `ZON-007..008`, `ENT-001..002` — zone actors, portal/transfer behavior, entity bootstrap, safe-region policy, terrain, and world behavior beyond the implemented management/spatial boundary.
9. `INT-006..009` — real data/client compatibility and performance evidence.

## Blocked research areas

The following matrix features are explicitly `BLOCKED` or remain research gaps:

- compressed state envelope and canonical `.pac` capture (`PRO-032..033`);
- complete Character/world 0x17 tails (`PRO-015..017`);
- authoritative movement/collision data (`GP-008`, `ZON-008`);
- natural monster AI/spawn and player respawn semantics (`GP-012`, `GP-023..024`);
- quest protocol and data (`GP-025`);
- generic NPC interaction packet/data (`GP-026`);
- experience/item/skill/monster information images and original-data gameplay vectors (`DAT-002..005`);
- complete gameplay packet request/response layouts and original-client transcripts (`GP-007..038`);
- unsupported external plugins (`SCR-007`);
- representative production BSON and client data (`INT-006`, `INT-008`, `PER-003`);
- complete Character/world packet integration and full player/entity compatibility (`ENT-020..021`);
- supported scope of FruitSalad process/editor tooling (`INF-014..015`, `INT-011..012`).

## Prompt 16 — gameplay

Source audit and implementation status are documented in [GAMEPLAY_IMPLEMENTATION_PLAN.md](docs/migration/GAMEPLAY_IMPLEMENTATION_PLAN.md) and [GAMEPLAY_SYSTEMS_IMPLEMENTATION.md](docs/migration/GAMEPLAY_SYSTEMS_IMPLEMENTATION.md). `GP-004` and `GP-005` are `IN_PROGRESS`: pure XP and stat-allocation arithmetic are covered by synthetic tests but have no game-data catalog, handlers, derived-stat/vitals integration, packet output, or persistence. All other gameplay features remain unimplemented; source defects and unknowns are documented instead of copied as assumed rules. Validation: restore passed; full build passed with 0 warnings/errors; full suite 178 passed, 0 failed, 2 opt-in MongoDB tests skipped; format verification passed. Prompt 17 is not started.

## Dependency warnings

- Do not widen gameplay services beyond independently source-backed calculations until required domain, game data, packet, and persistence fixtures are available.
- Do not expose a host as protocol-compatible before Stage 5.
- Do not enable corrected gameplay defaults.
- Do not port scripting/eval into production.
- Do not claim database migration readiness without dry-run and representative BSON comparison.
- Do not mark any P0 feature `VERIFIED` without linked executable evidence.
