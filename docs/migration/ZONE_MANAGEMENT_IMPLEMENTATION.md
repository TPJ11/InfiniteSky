# Zone Management and Spatial Infrastructure

## Status and scope

Prompt 14 implements the configured-zone loading boundary, immutable legacy resource loading, zone lifecycle and registry, world readiness integration, and source-compatible spatial candidate queries. The feature matrix records these components as `IMPLEMENTED`, not `VERIFIED`; no original-client or live-world behavior is claimed.

The implementation remains in the approved `InfiniteSky.World` and `InfiniteSky.GameData` projects. It does not add a separate Zones project, socket handling, entity runtime, player spawning, movement, collision, combat, or gameplay.

## Original behavior inspected

- The root world enumerates numeric `config.Zones` entries and calls `world.loadZone` for entries with `Load=true`. The async queue defaults to concurrency 4; duplicate loading is a TODO, alias zones are a TODO, and `world.unloadZone` invokes a `Zone.unload` method that is not implemented ([`world.js`](../../world.js#L783-L829)).
- `config.json-dist` contains 62 configured IDs: `1-21, 34, 37-40, 43, 46, 49-50, 55-56, 59, 71-75, 78, 81-82, 89-91, 94, 101, 104-105, 110, 112, 115-116, 119-120, 124-126, 138, 140-143`. Names follow `Z###`. The only configured display names are ID 1 `Gungsong Fortress`, 6 `Padsong Fortress`, 11 `Hanma Palace`, and 119 `Nangin Track` ([`config.json-dist`](../../config.json-dist#L56-L115)).
- Zone loading creates a QuadTree at X/Z origin `(-50000,-50000)`, size `100000`, depth 6, limit 10. It reads `.NPC`, move-region `.WREGION`, and safe-region `.WREGION` files. Missing files are tolerated; the legacy callback marks the zone loaded after the series ([`zone.js`](../../zone.js#L533-L676)).
- NPC and WREGION files are count-prefixed: NPC records are 24 bytes (`UniqueID`, `ID`, three float32 coordinates, float32 direction); WREGION records are eight signed int32 values, 32 bytes total ([`generic/structs.js`](../../generic/structs.js#L477-L489)). NPC rows with `ID == 0` are skipped before legacy NPC creation.
- MOB loading and terrain loading are commented out in `Zone.Load`; no C# MOB population or terrain/navigation behavior is introduced. Safe-region records are retained, but the original load path only logs unknown fields and does not establish a general safe-zone policy.
- The repository tracks 52 `.NPC` and 56 `.MOB` spawn files. No `.WREGION` file is present under `data`; `data/world` is ignored. A real tracked fixture, `data/spawninfo/001.NPC`, is exercised by the GameData tests. There are no real region-file fixtures to validate against.

## Implemented components

| Responsibility | Implementation | Evidence / boundary |
|---|---|---|
| Zone IDs and metadata | [`LegacyZoneCatalog`](../../src/InfiniteSky.GameData/Zones/LegacyZoneCatalog.cs#L11) | Exact 62-ID source catalog; unknown IDs fail instead of receiving synthesized metadata. |
| Resource parsing | [`LegacyZoneResourceLoader`](../../src/InfiniteSky.GameData/Zones/LegacyZoneResourceLoader.cs#L12) | Async, cancellable reads; reuses `LegacyBinaryReader`, `LegacySpawnInfo`, and `LegacyWorldRegion`; distinguishes absent from empty files. |
| Zone lifecycle | [`ZoneRuntime`](../../src/InfiniteSky.World/Zones/ZoneRuntime.cs#L14) | Created → Initializing → Initialized → Starting → Running → Stopping → Stopped, with Failed on initialization failure. |
| Zone manager and registry | [`ZoneManager`](../../src/InfiniteSky.World/Zones/ZoneManager.cs#L11), [`ZoneRegistry`](../../src/InfiniteSky.World/Zones/ZoneRegistry.cs#L8) | Bounded parallel load, registration only after successful initialization/start, duplicate IDs rejected, stable numeric enumeration, reverse-order stop. |
| Spatial index | [`ZoneSpatialIndex`](../../src/InfiniteSky.World/Spatial/ZoneSpatialIndex.cs#L12) | Zone-local lock-protected range QuadTree, generated node IDs, update/remove/query, snapshots, type filtering, disposal. |
| World coordination | [`ZoneManagementHostedService`](../../src/InfiniteSky.Server/Hosting/ZoneManagementHostedService.cs#L16) | Starts before `WorldHostedService`, supplies `IWorldZoneReadiness`, stops after the world because Generic Host shutdown is reverse registration order. |
| Health and metrics | [`ZoneHealthCheck`](../../src/InfiniteSky.Server/Health/ZoneHealthCheck.cs#L10), [`ZoneMetrics`](../../src/InfiniteSky.World/Zones/ZoneMetrics.cs#L7) | Lifecycle/readiness, missing optional-resource counts, registration/object counters, failures, load and query duration histograms. |

`GameData` now references `Protocol` only to reuse the existing bounds-checked binary reader and exact legacy record structures. The dependency remains one-way (`Protocol` → `Domain`; `GameData` → `Domain` and `Protocol`) and does not create a cycle.

## Configuration and loading

`InfiniteSky:Zones:Load` selects IDs from the original catalog. The tracked configuration intentionally defaults to an empty list; no zone is fabricated or started by default. `AsyncLoadLimit` defaults to the legacy queue limit of 4, `QuadTreeDepth` to 6, `QuadTreeLimit` to 10, and `ViewableActionDistance` to 500. These settings are startup-only and validated.

For example, load catalog zone 1 from PowerShell:

```powershell
$env:INFINITESKY_InfiniteSky__Zones__Load__0 = "1"
dotnet run --project src\InfiniteSky.Server\InfiniteSky.Server.csproj
```

Resources resolve beneath the host content root:

```text
data\spawninfo\001.NPC
data\world\Z001_ZONEMOVEREGION.WREGION
data\world\Z001_ZONESAFEREGION.WREGION
```

The `.NPC` and `.WREGION` files are optional, matching the original missing-file behavior. If present but truncated or malformed, initialization fails explicitly and previously registered zones are cleaned up. Parsed NPC records remain definitions only: Prompt 15 entity construction and insertion are not implemented. MOB files are not read.

## World readiness and lifecycle

The zone manager is registered as the `IWorldZoneReadiness` provider before world startup. It reports ready only when the configured list is nonempty and every selected zone is running and registered. With the default empty list, the manager starts without error but is not ready; world readiness remains degraded. This preserves the previous no-fake-zone behavior. `ZoneHealthCheck` separately reports `Degraded` when any optional NPC or region resource file is missing, while preserving the original loader's rule that file absence alone does not prevent zone initialization.

On startup, each selected zone resolves a known descriptor, loads resources, creates its spatial index, enters Running, then is registered. Parallelism is bounded by the legacy load-limit setting. Any failure cancels the startup operation, unpublishes loaded zones, disposes their indexes, records failure, and propagates the error. Shutdown first removes registry entries and then disposes zones in reverse order. No database or network I/O occurs while holding registry or spatial locks.

## Compatibility, incomplete behavior, and risks

- The source supports numeric configured zones only as data-backed entries; this implementation rejects IDs outside the 62-entry source catalog. ID 0 in a zone-change request is a request sentinel, not a loaded zone.
- No dynamic instances, capacity rules, map boundaries, coordinate transform, world/zone transfer, portal authorization, player membership, or actual visibility packets are added.
- Region structs are parsed as signed values and preserved raw. Portal interpretation and safe-region enforcement remain unimplemented because the original active rules are incomplete and region assets are absent.
- NPC definitions are parsed but no NPCs are instantiated. Monster `.MOB` loading remains disabled as in `zone.js`.
- The legacy `world.unloadZone` path is incomplete. C# shutdown safely clears its own registry and spatial resources, but cannot drain players, entities, timers, persistence, or network sessions that do not exist yet.
- The original zone `step` only updates the QuadTree; this stage does not add a simulation clock or zone tick actor. Future actor/mailbox ownership remains a later integration concern.

## Tests and verification

- `LegacyZoneResourceLoaderTests` verifies count-prefixed records, signed WREGION fields, missing-resource handling, malformed/truncated input, the exact catalog, and the tracked original `001.NPC` file.
- `ZoneManagementTests` verifies radius/size semantics, X/Z projection, strict child-pruning boundary behavior, update/remove/type filtering, concurrent access, duplicate IDs, readiness, failure cleanup, and shutdown.
- `FoundationHostTests.ConfiguredZonesStartBeforeWorldAndSupplyRealReadiness` verifies hosted-service ordering, readiness propagation, health, and shutdown with a controlled loader.
- The test fixtures for WREGION are generated from the existing 32-byte structure because the repository contains no real WREGION assets. They do not prove compatibility with deployed map resources.

The full solution build, full test suite, format verification, and original-Node-file diff check are recorded in the Prompt 14 completion report and migration status.
