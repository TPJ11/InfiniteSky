# InfiniteSky Migration Status

## Authority and update rules

This file is the authoritative dashboard for the .NET 10 migration. Update it only from the feature matrix and linked test evidence.

- The .NET 10 solution foundation is implemented. No C# engine, protocol, networking, database, or gameplay migration has been performed.
- A feature may be `VERIFIED` only after its test/acceptance evidence exists and is linked.
- `IMPLEMENTED` is not `VERIFIED`.
- Blockers are documented below, in the matrix notes, and in `docs/migration/MIGRATION_RISKS.md`.

## Current summary

| Metric | Count |
|---|---:|
| Total features | 200 |
| NOT_STARTED | 185 |
| IN_PROGRESS | 0 |
| IMPLEMENTED | 13 |
| VERIFIED | 2 |
| BLOCKED | 0 |
| NOT_APPLICABLE | 0 |
| Overall verified progress | 1% |

Known blocked research areas are not assigned `BLOCKED` status until their owner and blocker decision are formally recorded. Foundation evidence is recorded in [FOUNDATION_IMPLEMENTATION.md](docs/architecture/FOUNDATION_IMPLEMENTATION.md).

## Subsystem progress

| Subsystem | Total | Implemented | Verified | Blocked | Progress |
|---|---:|---:|---:|---:|---:|
| Infrastructure | 18 | 3 | 2 | 0 | 11.1% |
| Networking | 10 | 0 | 0 | 0 | 0% |
| Protocol | 33 | 0 | 0 | 0 | 0% |
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
| **Total** | **200** | **13** | **2** | **0** | **1%** |

## Completed domain model work

- Strong identifiers for accounts, characters, entities, zones, items, skills, monsters, and NPCs.
- `Position3`, grid, direction, and rotation values preserving source numeric semantics.
- Account, character, equipment, inventory, skills, stats, guild, per-server bank, and runtime character state.
- Item, skill, monster, NPC, and experience definitions from named original record fields.
- Monster/NPC/world-item runtime entities and zone spawn/region descriptors.
- 28 domain compatibility tests covering defaults, null/sparse arrays, identifiers, enum values, numerics, definitions, and identity.

Protocol, persistence mapping, world simulation, and gameplay algorithms remain `NOT_STARTED`.

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
5. `NET-001..009` — transport, session phase, and transfer.
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

Prompt 08 core domain model work is complete and awaits explicit approval for Prompt 09. The next approved work should:

1. freeze hex packet captures and classify the `.pac` corpus;
2. acquire a representative, sanitized MongoDB export and game-data manifest;
3. define the legacy client soak transcript;
4. begin Stage 2 fixture-oriented protocol and data readers.

## Dependency warnings

- Do not begin gameplay services before domain, data, and packet fixtures.
- Do not expose a host as protocol-compatible before Stage 5.
- Do not enable corrected gameplay defaults.
- Do not port scripting/eval into production.
- Do not claim database migration readiness without dry-run and representative BSON comparison.
- Do not mark any P0 feature `VERIFIED` without linked executable evidence.
