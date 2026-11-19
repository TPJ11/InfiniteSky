# Entities and Players Migration

## Scope and matrix coverage

`ENT-001..008` remain the future network actor, state replication, vitality, non-player lifecycle, and target-resolution scope. Prompt 15 adds the internal player and entity infrastructure rows `ENT-009..021`; see the [feature parity matrix](../FEATURE_PARITY_MATRIX.md) for individual status/evidence.

## Source behavior

The root `world.js`, `generic/CharacterState.js`, `zone.js`, and `QuadTree.js` show account/character objects attached to world clients, zone-local QuadTree IDs stored separately as `UniqueID`, saved position projection, client proximity queries, and disconnect persistence/removal. Character persistence is defined in `db/character.js`; login/selection paths are in `packets/login/handleLogin.js` and `handleGameStart.js`, with the handoff in `packets/world/handshake.js`.

The root source supports player/client, NPC, monster, and item-like object categories. NPC definition loading is active but no source-backed NPC runtime has been migrated. MOB loading is commented out and no monster spawn simulation is started.

## Prompt 15 implementation

- `ICharacterRepository` and `MongoCharacterRepository` support account/server-scoped listing, owned-character lookup, and targeted saved map/location updates.
- `MongoCharacterDocumentMapper` maps supported legacy fields to the established domain `Character`, including sparse/null array entries, optional values, equipment, progression, attributes, and bonuses.
- `PlayerActivationService` requires a live authenticated login session, validates account ownership, and checks for a complete persisted zone/position before activation.
- `PlayerManager` enforces unique character/session/world-connection registration and composite zone/runtime-entity lookup. Runtime `EntityId` values are the zone spatial index's `uint`; persistent IDs remain `long`-backed domain values.
- Zone-locked spatial registration prevents publishing into a stopped zone and rolls back failed registration. Position updates use the existing spatial index, nearby-player queries return candidates only, and removal is idempotent.
- `PlayerManagementHostedService` saves supported location snapshots and removes players before world/zone shutdown.

## Explicitly outside this stage

No character creation/deletion, slot-packet codec, derived stats, initial HP/Chi maxima, NPC/monster runtime actor, movement simulation, combat, gameplay scripting, world transfer, spawn/despawn packet, or client-visible state replication is implemented. No original-client compatibility claim is made. Real network disconnects cannot invoke the new application cleanup API until a player/world packet lifecycle is wired.

## Verification and risks

Focused tests cover mapper behavior, ownership, session expiration, active player registry races, spawn rollback, spatial updates/candidates, disconnect saves, host cleanup order, and DI. The MongoDB Testcontainers test is optional and is only evidence when run with Docker and the explicit environment gate. The fixture is synthetic; the inferred `character_mongooses` collection, production BSON subtypes, and client protocol remain unverified.

See [PLAYER_ENTITY_IMPLEMENTATION.md](PLAYER_ENTITY_IMPLEMENTATION.md), [ENTITY_LIFECYCLE.md](ENTITY_LIFECYCLE.md), [CHARACTER_PERSISTENCE.md](CHARACTER_PERSISTENCE.md), [PLAYER_SESSION_AND_WORLD_ENTRY.md](PLAYER_SESSION_AND_WORLD_ENTRY.md), and [ENTITY_VISIBILITY_INTEGRATION.md](ENTITY_VISIBILITY_INTEGRATION.md).
