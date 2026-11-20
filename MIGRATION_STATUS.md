# InfiniteSky Migration Status

## Authority and update rules

This file is the authoritative dashboard for the .NET 10 migration. Update it only from the feature matrix and linked test evidence.

- The .NET 10 solution foundation, core domain models, binary protocol foundation, TCP infrastructure, authentication flow, MongoDB account/guild-read/character-location persistence slices, world/zone infrastructure, and internal player activation boundary are implemented where evidence supports it. Prompt 16 has isolated XP and stat-allocation arithmetic slices in progress; full packet, data, entity simulation, database, and gameplay migration remains incomplete. Prompt 17 audited legacy scripting and plugin behavior; no compatible runtime or native plugin API was added, and affected features are marked blocked/not started/not applicable in the matrix.
- Prompt 20 verifies a bounded host-composed TCP login/session path with test persistence, duplicate rejection, reconnect, shutdown cleanup, and listener-start rollback. It does not complete character/world/gameplay integration or verify live MongoDB/original-client compatibility.
- Prompt 21 added the source-backed login greeting to the .NET login connection path and verified its bytes with an existing golden fixture and loopback tests. No original client or live Node reference was available; real-client compatibility remains blocked.
- A feature may be `VERIFIED` only after its test/acceptance evidence exists and is linked.
- `IMPLEMENTED` is not `VERIFIED`.
- Blockers are documented below, in the matrix notes, and in `docs/migration/MIGRATION_RISKS.md`.

## Current summary

| Metric | Count |
|---|---:|
| Total features | 232 |
| NOT_STARTED | 137 |
| IN_PROGRESS | 7 |
| IMPLEMENTED | 48 |
| VERIFIED | 14 |
| BLOCKED | 25 |
| NOT_APPLICABLE | 1 |
| Overall verified progress | 6.0% |

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
| Gameplay | 38 | 0 | 0 | 1 | 0% |
| Scripting/plugins | 13 | 0 | 0 | 8 | 0% |
| Administration | 13 | 0 | 0 | 13 | 0% |
| Integration/compatibility | 16 | 0 | 0 | 1 | 0% |
| **Total** | **232** | **48** | **14** | **25** | **26.7%** |

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
10. `ADM-001..013` and `GP-032` — command dispatch, authoritative account privilege, world chat networking, and required administration/gameplay services.

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
- root shared-VM, host API, and module lifecycle compatibility (`SCR-001`, `SCR-004`, `SCR-008`, `SCR-011..013`); external plugin references with no source are `NOT_APPLICABLE` (`SCR-007`);
- representative production BSON and client data (`INT-006`, `INT-008`, `PER-003`);
- complete Character/world packet integration and full player/entity compatibility (`ENT-020..021`);
- supported scope of FruitSalad process/editor tooling (`INF-014..015`, `INT-011..012`).

## Prompt 16 — gameplay

Source audit and implementation status are documented in [GAMEPLAY_IMPLEMENTATION_PLAN.md](docs/migration/GAMEPLAY_IMPLEMENTATION_PLAN.md) and [GAMEPLAY_SYSTEMS_IMPLEMENTATION.md](docs/migration/GAMEPLAY_SYSTEMS_IMPLEMENTATION.md). `GP-004` and `GP-005` are `IN_PROGRESS`: pure XP and stat-allocation arithmetic are covered by synthetic tests but have no game-data catalog, handlers, derived-stat/vitals integration, packet output, or persistence. All other gameplay features remain unimplemented; source defects and unknowns are documented instead of copied as assumed rules. Validation: restore passed; full build passed with 0 warnings/errors; full suite 178 passed, 0 failed, 2 opt-in MongoDB tests skipped; format verification passed.

## Prompt 17 — scripting and plugins

The root Node.js VMScript, loaders, module dependency declarations, packet/command script directories, root lifecycle events, zone hook slots, configured plugin filenames, and alternate FruitSalad process/zone-script runtime are inventoried in [SCRIPTING_COMPATIBILITY_MATRIX.md](docs/migration/SCRIPTING_COMPATIBILITY_MATRIX.md). The selected migration strategy is native C# migration of individually source-verified server behavior; no embedded JavaScript runtime or generic plugin API was added because the root scripts execute against shared Node globals and unfinished live service APIs. No script or plugin is `IMPLEMENTED` or `VERIFIED`. `SCR-001..002`, `SCR-004..005`, `SCR-008`, and `SCR-011..013` are `BLOCKED`; `SCR-003`, `SCR-006`, `SCR-009..010` are `NOT_STARTED`; README-only external plugin references (`SCR-007`) are `NOT_APPLICABLE`. Prompt 18 separately documents the legacy commands but does not add a C# command runtime.

Decision, lifecycle, event, gameplay and security details are documented in [SCRIPTING_ENGINE_DECISION.md](docs/migration/SCRIPTING_ENGINE_DECISION.md), [SCRIPTING_IMPLEMENTATION.md](docs/migration/SCRIPTING_IMPLEMENTATION.md), [PLUGIN_LIFECYCLE.md](docs/migration/PLUGIN_LIFECYCLE.md), [SCRIPT_EVENT_SYSTEM.md](docs/migration/SCRIPT_EVENT_SYSTEM.md), [SCRIPTING_GAMEPLAY_INTEGRATION.md](docs/migration/SCRIPTING_GAMEPLAY_INTEGRATION.md), and [SCRIPTING_SECURITY_MODEL.md](docs/migration/SCRIPTING_SECURITY_MODEL.md). No original JavaScript files changed. No scripting tests or runtime integration tests are claimed because no execution behavior was implemented. Validation: restore passed; build passed with 0 warnings/errors; full suite 178 passed, 0 failed, 2 opt-in MongoDB tests skipped; format verification and `git diff --check` passed.

## Prompt 18 — commands and administration

The root Node command registry and dispatch behavior, command modules and aliases, numeric account-level checks, root console REPL, moderation stubs, and separate FruitSalad command tree are inventoried in [ADMIN_COMMAND_COMPATIBILITY_MATRIX.md](docs/migration/ADMIN_COMMAND_COMPATIBILITY_MATRIX.md). Supporting documents describe the command boundary, authorization, GM commands, moderation, server administration, and audit/security findings. No command framework, handler, privilege field, audit store, or operator CLI was added.

The source audit found that .NET world listeners do not dispatch the legacy normal/faction/whisper chat opcodes, and command effects depend on incomplete world, gameplay, packet, and persistence services. Prompt 19 has now added the server-loaded account `Level` to authentication records and authenticated sessions; this does not add world dispatch, character-bound command context, or authorization/response handling. Therefore `ADM-001..013` and `GP-032` remain `BLOCKED`; none is `IMPLEMENTED` or `VERIFIED`. The compatibility matrix distinguishes active behavior from stubs, broken/partial handlers, and absent features. No original Node.js source files changed and no command tests are claimed.

Prompt 18 validation: `dotnet restore InfiniteSky.sln` succeeded (dependencies were already current); the full solution build passed with 0 warnings/errors; all 178 runnable tests passed and the 2 opt-in MongoDB integration tests were skipped; `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` and `git diff --check` passed. A documentation link check found all 236 local Markdown links target existing files. These checks establish existing solution health, not command parity; no command-specific tests ran because no command execution code was added. No original Node.js source files changed.

## Prompt 19 — remaining systems and feature gaps

The source coverage audit was rechecked against the 232-row feature matrix and the existing C# projects/tests. The [remaining gap register](docs/migration/REMAINING_FEATURE_GAPS.md) indexes all 169 open matrix IDs; the [closure plan](docs/migration/FEATURE_GAP_CLOSURE_PLAN.md), [completeness report](docs/migration/FEATURE_COMPLETENESS_REPORT.md), [blockers](docs/migration/REMAINING_BLOCKERS.md), and [integration readiness](docs/migration/INTEGRATION_READINESS.md) record ordering and evidence. No C# TODO/NotImplementedException-style code markers were found; reviewed no-op lifecycle returns and initial `NotImplemented` readiness values are not claimed as completed runtime features.

Prompt 19 closed the account authority data-path gap: the Mongo authentication projection includes account `Level`, the raw BSON mapper preserves finite numeric representations and defaults missing/null to 0, and the authenticated session carries this server-derived field. Non-numeric/non-finite values fail closed. This is not command authorization or feature parity; `AUTH-003` and `SES-001` remain `IMPLEMENTED`, and all command/chat features remain blocked. No gameplay, packet, schema, or Node.js implementation was invented or modified.

Prompt 19 validation: `dotnet restore InfiniteSky.sln` succeeded; the full solution build passed with 0 warnings/errors; all 184 runnable tests passed and the 2 opt-in MongoDB integration tests were skipped; `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` and `git diff --check` passed. The focused authentication integration project passed 60/60; persistence passed 23 with 2 container tests skipped. A final repository-wide documentation scan confirmed all 781 local Markdown links target existing files. Original Node.js and JSON data files remain unchanged. Live MongoDB behavior is not verified.

## Prompt 20 — full-system integration and end-to-end baseline

The pre-change snapshot is recorded in [INTEGRATION_BASELINE.md](docs/migration/INTEGRATION_BASELINE.md): commit `073a7cbe7f00df0f752c9c6e9a629f6946cf4f56`, clean `master`, .NET SDK 10.0.401, restore/build/format success, 184 passing tests and 2 skipped opt-in MongoDB tests. Startup composition and dependency order are documented in [FULL_SYSTEM_INTEGRATION.md](docs/migration/FULL_SYSTEM_INTEGRATION.md).

Prompt 20 added a host-composed loopback test that uses the actual Generic Host, listeners, framing, login sink, authentication service, and session store with a deterministic test account repository. It verifies successful legacy login response, duplicate-account rejection, disconnect cleanup, reconnect, and session/connection cleanup on host shutdown. A second test verifies that failure to bind the world port releases the login listener during startup rollback. Persistence is substituted in the socket flow; these tests do not establish live MongoDB or character/world compatibility.

The scenario ledger is in [END_TO_END_TEST_SCENARIOS.md](docs/migration/END_TO_END_TEST_SCENARIOS.md); harness limits and commands are in [INTEGRATION_TEST_HARNESS.md](docs/migration/INTEGRATION_TEST_HARNESS.md); runtime settings are in [RUNTIME_CONFIGURATION.md](docs/migration/RUNTIME_CONFIGURATION.md); diagnosed test/setup failures and deferred failures are in [INTEGRATION_FAILURES_AND_FIXES.md](docs/migration/INTEGRATION_FAILURES_AND_FIXES.md). The [integration readiness report](docs/migration/INTEGRATION_READINESS.md) concludes the server is **not ready for original-client testing**. Character/world dispatch, active-player network ownership, data assets, gameplay, scripts, commands, and live persistence remain blocked or unimplemented. Prompt 21 has not started.

Matrix statuses `INT-001`, `INT-010`, and `INT-015` moved from `NOT_STARTED` to `IN_PROGRESS` for the tested login slice, host shutdown/reconnect evidence, and Prompt 20 documentation; their full acceptance criteria remain unmet. The matrix totals are now 138 `NOT_STARTED`, 7 `IN_PROGRESS`, 48 `IMPLEMENTED`, 14 `VERIFIED`, 24 `BLOCKED`, and 1 `NOT_APPLICABLE` (232 total). No overall server or full client journey was marked `VERIFIED`.

Prompt 20 validation: `dotnet restore InfiniteSky.sln` succeeded; `dotnet build InfiniteSky.sln --no-restore` passed with 0 warnings and 0 errors; `dotnet test InfiniteSky.sln --no-build` passed 186 tests with 0 failures and 2 opt-in MongoDB tests skipped (Domain 50, Protocol 24, GameData 5, World 22, Server integration 62, Persistence 23 passed); the focused host/socket test filter passed 2/2; `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` and `git diff --check` passed. A repository-wide scan checked 822 local Markdown file links with no broken targets. Original Node.js files and production data were not modified. Live MongoDB, character/world workflows, and original-client compatibility remain unverified.

## Prompt 21 — original-client compatibility testing

Repository/workspace inventory found legacy TSX private-server helper/launcher source and one old pcap, but no original game-client executable/build, client version, or required client asset set. The helper is not the game client, and the pcap has no documented client provenance; its payload was not copied into tests or documentation. No `node_modules`, working legacy `config.json`, package test script target, or local MongoDB service was available, so neither the original client nor the Node.js reference server was run.

Source comparison identified one directly evidenced login mismatch: Node sends the 21-byte `0x00` server-list greeting when a login socket connects, while the .NET listener previously did not send any initial application packet. The networking sink now has an awaited connection-open callback; the login sink emits `LegacyLoginGreeting(0, 1000)` before processing requests. The existing source-derived greeting fixture and loopback integration tests verify exact bytes and ordering. This is protocol/source and simulated-socket evidence only, not client-based verification. Successful .NET authentication still does not send character slots; no character/world success response was fabricated.

Seven Prompt 21 reports document the environment, compatibility matrix, capture inventory, packet fixture provenance, issues/fixes, test results, and readiness decision. [`CLIENT_COMPATIBILITY_READINESS.md`](docs/migration/CLIENT_COMPATIBILITY_READINESS.md) concludes real-client testing is `BLOCKED`. `INT-008` moved from `NOT_STARTED` to `BLOCKED` because an original client build is unavailable. The matrix totals are now 137 `NOT_STARTED`, 7 `IN_PROGRESS`, 48 `IMPLEMENTED`, 14 `VERIFIED`, 25 `BLOCKED`, and 1 `NOT_APPLICABLE` (232 total). No client compatibility row is `VERIFIED`; Prompt 22 has not started.

Prompt 21 validation: `dotnet restore InfiniteSky.sln` succeeded; the full solution build passed with 0 warnings/errors; `dotnet test InfiniteSky.sln --no-build` passed 186 tests, 0 failed, and 2 opt-in MongoDB tests skipped (Domain 50, Protocol 24, GameData 5, World 22, Server integration 62, Persistence 23 passed); the server integration project passed 62/62; `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` and `git diff --check` passed. A repository-wide scan checked 874 local Markdown links with no broken targets. No original-client or live Node differential test ran. Original Node.js files, client binaries, production data, and the existing pcap were not modified.

## Dependency warnings

- Do not widen gameplay services beyond independently source-backed calculations until required domain, game data, packet, and persistence fixtures are available.
- Do not expose a host as protocol-compatible before Stage 5.
- Do not enable corrected gameplay defaults.
- Do not port scripting/eval into production.
- Do not claim database migration readiness without dry-run and representative BSON comparison.
- Do not mark any P0 feature `VERIFIED` without linked executable evidence.

## Prompt 22 — regression and behavioral parity verification

The pre-change baseline was captured at commit `c631b4514828b9a98a6f390118e029d491d44fd7` with a clean worktree. Restore succeeded; the complete solution built with 0 warnings/errors; the suite passed 186 tests, failed 0 and skipped the 2 opt-in MongoDB Testcontainers tests (6.09 seconds). See [REGRESSION_BASELINE.md](docs/migration/REGRESSION_BASELINE.md).

The final Prompt 22 validation ran restore, full solution build, the complete suite, `dotnet format --verify-no-changes`, `git diff --check`, and a separate coverage-enabled complete suite. The final test result was 187 passed, 0 failed, 2 skipped. The new test adds a source-derived 18-byte `0x01` world-handshake request fixture; it does not add the raw `0x15` success response or a world handler. Test groups and provenance are in [REGRESSION_TEST_INVENTORY.md](docs/migration/REGRESSION_TEST_INVENTORY.md); feature-level evidence and gaps are in [BEHAVIOURAL_PARITY_COVERAGE.md](docs/migration/BEHAVIOURAL_PARITY_COVERAGE.md).

Three full-suite repeat runs before the added test had identical results and no observed flakiness. The final coverage-enabled run also passed. Coverage is reported by project/package in [COMPREHENSIVE_REGRESSION_REPORT.md](docs/migration/COMPREHENSIVE_REGRESSION_REPORT.md); percentages are not parity evidence. No Level A original-client or Level B Node differential evidence exists. Both MongoDB container tests were skipped; no live persistence result is claimed.

The matrix retains its statuses: 137 `NOT_STARTED`, 7 `IN_PROGRESS`, 48 `IMPLEMENTED`, 14 `VERIFIED`, 25 `BLOCKED`, and 1 `NOT_APPLICABLE` (232 total). The subsystem roll-up above was reconciled with the matrix. Several incomplete acceptance claims were clarified, the stale 205-feature note and incorrect listener-port notes were corrected, and the Prompt 22 issue/fix record is in [REGRESSION_ISSUES_AND_FIXES.md](docs/migration/REGRESSION_ISSUES_AND_FIXES.md).

Repeatable commands and external prerequisites are in [TEST_EXECUTION_GUIDE.md](docs/migration/TEST_EXECUTION_GUIDE.md). [VERIFICATION_READINESS.md](docs/migration/VERIFICATION_READINESS.md) records that the server is not ready for server-wide parity, original-client certification, live database compatibility or representative performance testing. Prompt 23 work has not started; explicit approval remains required.
