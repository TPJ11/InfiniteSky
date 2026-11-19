# InfiniteSky Migration Status

## Authority and update rules

This file is the authoritative dashboard for the .NET 10 migration. Update it only from the feature matrix and linked test evidence.

- The .NET 10 solution foundation, core domain models, and binary protocol foundation are implemented where evidence supports it. Networking, database, world, and gameplay migration remains incomplete.
- A feature may be `VERIFIED` only after its test/acceptance evidence exists and is linked.
- `IMPLEMENTED` is not `VERIFIED`.
- Blockers are documented below, in the matrix notes, and in `docs/migration/MIGRATION_RISKS.md`.

## Current summary

| Metric | Count |
|---|---:|
| Total features | 207 |
| NOT_STARTED | 169 |
| IN_PROGRESS | 1 |
| IMPLEMENTED | 22 |
| VERIFIED | 14 |
| BLOCKED | 1 |
| NOT_APPLICABLE | 0 |
| Overall verified progress | 6.8% |

Known blocked research areas are not assigned `BLOCKED` status until their owner and blocker decision are formally recorded. Foundation evidence is recorded in [FOUNDATION_IMPLEMENTATION.md](docs/architecture/FOUNDATION_IMPLEMENTATION.md).

## Subsystem progress

| Subsystem | Total | Implemented | Verified | Blocked | Progress |
|---|---:|---:|---:|---:|---:|
| Infrastructure | 18 | 3 | 4 | 0 | 22.2% |
| Networking | 10 | 3 | 3 | 0 | 60% |
| Protocol | 33 | 2 | 7 | 0 | 27.3% |
| Authentication | 4 | 1 | 2 | 1 | 75% |
| Sessions | 3 | 3 | 0 | 0 | 100% |
| Persistence | 12 | 0 | 0 | 0 | 0% |
| Game data | 11 | 0 | 0 | 0 | 0% |
| Domain | 15 | 10 | 0 | 0 | 0% |
| World engine | 10 | 0 | 0 | 0 | 0% |
| Zones/spatial | 8 | 0 | 0 | 0 | 0% |
| Entities/players | 8 | 0 | 0 | 0 | 0% |
| Gameplay | 38 | 0 | 0 | 0 | 0% |
| Scripting/plugins | 8 | 0 | 0 | 0 | 0% |
| Administration | 13 | 0 | 0 | 0 | 0% |
| Integration/compatibility | 16 | 0 | 0 | 0 | 0% |
| **Total** | **207** | **22** | **14** | **1** | **6.8%** |

## Completed domain model work

- Strong identifiers for accounts, characters, entities, zones, items, skills, monsters, and NPCs.
- `Position3`, grid, direction, and rotation values preserving source numeric semantics.
- Account, character, equipment, inventory, skills, stats, guild, per-server bank, and runtime character state.
- Item, skill, monster, NPC, and experience definitions from named original record fields.
- Monster/NPC/world-item runtime entities and zone spawn/region descriptors.
- 28 domain compatibility tests covering defaults, null/sparse arrays, identifiers, enum values, numerics, definitions, and identity.

Persistence mapping, world simulation, complete packet lifecycle, and gameplay algorithms remain `NOT_STARTED` or `IN_PROGRESS` as listed in the feature matrix.

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
- Explicit production blocker: the default account repository is unavailable until persistence; test repositories do not make production authentication operational.

Login character slots, world transfer, distributed sessions, and MongoDB account lookup remain incomplete. The legacy plaintext password storage and the stricter in-process duplicate policy are documented in the authentication and session implementation reports.

## Outstanding domain gaps

- `MonstersKilled` has no typed key/usage evidence and remains deferred.
- Packet character tails, NPC/monster unknown records, and quest state are not modeled.
- Domain models are implemented but their persistence/protocol mappers are not yet implemented, so affected matrix rows remain `IMPLEMENTED`, not `VERIFIED`.
- Numeric widths at packet boundaries require later adapter validation.

## Critical outstanding features

The immediate critical path is:

1. `PRO-001..004`, `PRO-015`, `PRO-017`, `PRO-032` — framing and unresolved wire structures.
2. `DAT-001..008` — original game data and world data readers.
3. `DOM-007..008` and persistence/protocol mappings for implemented models — progression, stats, and boundary conversion.
4. `PER-003`, `PER-008`, `PER-009`, `PER-011..012` — character/BSON and safe save behavior.
5. `AUTH-003`, `PER-001..002`, `NET-008..009` — production account lookup, world-socket duplicate checks, and session transfer.
6. `GP-001..003`, `INT-001` — end-to-end character/world entry.
7. `WLD-002`, `ZON-002`, `ENT-001..002` — zone actor/spatial ownership/state.
8. `INT-006..009` — real data/client compatibility and performance evidence.

## Blocked research areas

These are not yet formal `BLOCKED` statuses but are current evidence gaps:

- compressed state envelope and canonical `.pac` capture (`PRO-032..033`);
- complete Character/world 0x17 tails (`PRO-015..017`);
- authoritative movement/collision data (`GP-008`, `ZON-008`);
- natural monster AI/spawn and player respawn semantics (`GP-012`, `GP-023..024`);
- quest protocol and data (`GP-025`);
- generic NPC interaction packet/data (`GP-026`);
- unsupported external plugins (`SCR-007`);
- representative production BSON and client data (`INT-006`, `INT-008`);
- supported scope of FruitSalad process/editor tooling (`INF-014..015`, `INT-011..012`).

## Recommended next action

The authentication/session foundation is implemented, but production account lookup remains blocked on persistence. Do not treat this stage as production-operational. The next approved stage should:

1. implement the approved persistence stage, including the production account repository and safe handling of legacy plaintext credentials;
2. acquire a representative, sanitized MongoDB export and game-data manifest;
3. freeze hex packet captures and classify the `.pac` corpus;
4. define the legacy client soak transcript before claiming complete login compatibility.

## Dependency warnings

- Do not begin gameplay services before domain, data, and packet fixtures.
- Do not expose a host as protocol-compatible before Stage 5.
- Do not enable corrected gameplay defaults.
- Do not port scripting/eval into production.
- Do not claim database migration readiness without dry-run and representative BSON comparison.
- Do not mark any P0 feature `VERIFIED` without linked executable evidence.
