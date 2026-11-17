# Zones and Spatial Management Migration

## Scope and matrix coverage

`ZON-001` through `ZON-008`: zone loading, quadtree, move/safe regions, portals, entity bootstrap, area queries, and terrain boundary.

## Source files

`zone.js`, `world.js`, `QuadTree.js`, `generic/structs.js`, `data/spawninfo/*`, `data/world/*`, `packets/world/zonechange.js`, experimental `FruitSalad/Modules/navtest-revised.js`.

## Target components

`ZoneLoader`, `ZoneSpatialIndex`, `ZoneRegionIndex`, `PortalResolver`, `SpawnBootstrap`, `InterestManagement`, `SafeRegionPolicy`, and `TerrainProvider`.

## Dependencies

Requires `DAT-007..008`, `ZON-002`, `WLD-002`, and data catalog startup. Portal resolution precedes zone transfer integration.

## Implementation order

1. Zone configuration and actor construction.
2. Spawn/region data readers.
3. X/Z spatial index.
4. Client/NPC/item/monster insertion.
5. interest queries.
6. movement/safe regions.
7. portal endpoint resolver.
8. controlled monster/MOB bootstrap.
9. terrain capability boundary, initially unsupported unless evidence/data arrives.

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

- All configured loadable zones start without duplicate races.
- Area recipients match golden fixtures.
- `remove` actually removes nodes; the legacy `slice` defect may be fixed only with recorded profile expectation.
- Portal ambiguity produces explicit errors.
- Terrain is not claimed complete without active collision/map data.

## Risks

The original load path assumes fixed bounds, only uses NPC spawns, and comments terrain/MOB loading. Region pairing is inferred. Alias zones and actual client map dimensions are undocumented.
