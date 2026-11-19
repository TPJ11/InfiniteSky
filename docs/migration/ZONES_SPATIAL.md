# Zones and Spatial Management Migration

## Scope and matrix coverage

`ZON-001` through `ZON-010`: zone definitions/loading, QuadTree, regions, portal/transition boundaries, entity bootstrap, candidate queries, zone lifecycle/registry, world readiness, and terrain boundary.

## Source files

`zone.js`, `world.js`, `QuadTree.js`, `generic/structs.js`, `data/spawninfo/*`, `data/world/*`, `packets/world/zonechange.js`, experimental `FruitSalad/Modules/navtest-revised.js`.

## Implemented Prompt 14 components

The static zone catalog, optional NPC/WREGION readers, zone lifecycle/registry, X/Z spatial index, readiness integration, health, and diagnostics are implemented. Their source evidence, constraints, and tests are described in [ZONE_MANAGEMENT_IMPLEMENTATION.md](ZONE_MANAGEMENT_IMPLEMENTATION.md), [ZONE_LIFECYCLE.md](ZONE_LIFECYCLE.md), [SPATIAL_SYSTEMS.md](SPATIAL_SYSTEMS.md), and [ZONE_VISIBILITY_AND_TRANSITIONS.md](ZONE_VISIBILITY_AND_TRANSITIONS.md).

## Remaining target components

Zone actor/mailbox, region policy and portal resolution, spawn/entity bootstrap, player visibility/broadcasts, transfer coordinator, safe-region enforcement, and terrain/navigation provider remain future work.

## Dependencies

Requires `DAT-007..008`, `ZON-002`, `WLD-002`, and data catalog startup. Portal resolution precedes zone transfer integration.

## Remaining implementation order

1. Add actor/mailbox ownership when Prompt 15 introduces network-driven entity mutations.
2. Verify real `.WREGION` files and portal pairing before implementing a resolver.
3. Add NPC/player/entity insertion only with entity domain and packet integration.
4. Implement recipient selection and transfers only with authenticated sessions and source-compatible packet flows.
5. Keep terrain/navigation unsupported until validated source data is available.

## Tests

- X/Z boundary and duplicate IDs;
- region count and signed fields;
- source/destination portal pairing;
- radius boundary;
- deterministic query ordering;
- zone with no clients;
- sparse NPC/MOB data;
- add/update/remove races inside actor;
- large-object interest profiles.

## Acceptance criteria

- Configured catalog IDs load with bounded concurrency and are published only after initialization.
- Spatial boundary and concurrency tests match the documented legacy query behavior.
- Missing optional files are visible in diagnostics and malformed resources fail startup.
- Entity recipients and portal ambiguity remain unimplemented until source assets/behavior can be verified.
- Terrain is not claimed complete without active collision/map data.

## Risks

The original load path assumes fixed QuadTree bounds, loads NPC spawn definitions, and comments terrain/MOB loading. Region pairing is inferred. Alias zones and actual client map dimensions are undocumented; the tracked repository has no WREGION files.
