# Player and Entity Implementation

## Scope and pre-implementation findings

The root Node.js server is the behavioral reference. `FruitSalad/` contains a separate process-based runtime and is not merged into this implementation.

The current .NET 10 solution already has `Character`, `CharacterRuntimeState`, `EntityId`, `CharacterId`, `NpcEntity`, `MonsterEntity`, and `WorldItemEntity` domain types. Prompt 14 supplies the configured-zone manager and a per-zone spatial index. Before this stage, however, there was no character repository, player registry/runtime lifecycle, character-selection use case, or player-to-session binding.

The following dependencies remain incomplete and limit client-compatible world entry:

- The login server's `0x05` character-list reply embeds the large `structs.Character` layout. The corresponding C# codec and fixtures are absent. The structure contains reserved and unknown regions, so this stage does not invent or emit a partial replacement.
- `handleGameStart.js` recalculates derived stats before setting current and maximum HP/Chi. The source stat calculator and required game-data catalog are not yet available as a complete .NET dependency.
- No production character BSON export or live MongoDB instance is available. The character collection name follows Mongoose's inferred pluralization of the source model name and remains configurable.
- The legacy `0x01` world handshake carries a username, slot, and map ID, then locates an in-memory transfer by username. It does not validate the supplied slot/map against the selected character; map validation is commented out. The migration will require validated internal ownership/session data and will not treat these client fields as authorization.
- Existing networking recognizes the source packet framing but the current incoming sink does not implement character-list, game-start, or world-handshake behavior. Player management added here is an application boundary, not a claim of end-to-end client login/world-entry compatibility.

## Source behavior

### Login and character selection

`packets/login/handleLogin.js` queries characters by `AccountID` and `ServerName` without an explicit sort, sends up to three slot replies (`0x05`), then empty replies for remaining slots. A successful account login reply precedes those character records. `packets/login/handleGameStart.js` accepts a slot in a 13-byte `0x09` request, selects only an existing slot, uses the selected character's saved `MapID`, and returns a `0x0A` endpoint reply.

The game-start path builds transient state from the character's persisted `RealX/Y/Z`, current HP and Chi, and a derived-stat calculator. It attempts to return special-map 119 characters to clan towns, but that branch reads lowercase `character.clan` while the schema declares `Clan`; no corrected fallback is inferred here.

### Session handoff and player lifecycle

`packets/world/handshake.js` decodes the existing 18-byte `0x01` packet (`Name[13]`, slot byte, map ID uint32), finds the login-side transfer entry by username, transfers the selected account/character objects to the world socket, resolves the target zone, saves the new map/location, and replies with byte `0x15`. It comments out a map mismatch check and does not use a client-supplied session key.

`zone.js` adds a player socket to the zone's client list and QuadTree as type `client`; the generated per-zone node ID is copied to `CharacterState.UniqueID`. Player candidates use horizontal X/Z proximity and character level as the node size. The state builder reads saved coordinates and randomizes facing to an integer degree in `[0,359]`; maximum HP/Chi depend on the missing stat calculator. `Zone.removeSocket` removes the spatial node, but its `Clients.slice(...)` result is not assigned, so the legacy client list can retain a removed socket.

On disconnect, `world.js` copies current HP/Chi and position into the character. During transfer it saves `ToLocation` and `ToMapID`; otherwise it saves current location and map. Coordinates are passed through `parseInt` before persistence. It calls `save`, removes zone membership, and removes the world client. No periodic character save is established by these paths.

### Persistence

`db/character.js` defines `character_mongoose` with numeric `_id` and `AccountID`, `ServerName`, identity/appearance/progression fields, optional saved map/coordinates/vitals, nested equipment, and `Mixed` inventory/skill/storage arrays. `db/counter.js` supplies numeric IDs through the `characterid` counter. The schema contains duplicate declarations for `Name`, `PlayTime`, and `Honor`; Mongo's deployed BSON subtypes and the physical character collection must be checked before production use.

This stage uses account-scoped reads and field-only world-location updates. It does not replace a character document, so unmodeled gameplay fields remain untouched. Character creation/deletion, the full `0x05` codec, inventory mutation, derived-stat calculation, and gameplay saves remain out of scope.

## Implemented boundary

The C# player lifecycle is deliberately limited to authenticated character loading, unique runtime registration, source-zone resolution, saved-position tracking in the Prompt 14 spatial index, nearby-player candidate queries, and idempotent despawn/disconnect snapshots. Runtime entity IDs remain zone-local `EntityId` values; persistent character IDs and account IDs remain separate `long`-backed domain identifiers.

`MongoCharacterDocumentMapper` maps the known character domain fields and preserves sparse/null array slots. `MongoCharacterRepository` filters by persistent ID, account ownership, and configured `ServerName`; the login list retains the root handler's three-character limit and no explicit sort. Its save method performs a targeted `$set` of map and coordinates only.

Character selection and activation require an existing authenticated login connection and an account-owned character in the configured server. The lifecycle does not select a character automatically, invent a spawn point, emit a spawn packet, or calculate gameplay stats. If a saved zone is unavailable or the saved position is incomplete, activation fails explicitly and publishes no player.

Disconnect and graceful shutdown persistence update only `MapID` and truncated `RealX/Y/Z`, matching the source's observed coordinate save shape. It leaves HP/Chi and every other document field unchanged because this stage does not own mutable gameplay vitals. A failed save is surfaced after the player is removed from runtime indexes; retry/transaction policy is not source-defined. Host stop ordering removes/saves player entities before world and zone shutdown.

## Compatibility and migration risks

- The actual `0x05` packet and world-state packet compatibility remains blocked on the full Character codec, unknown-byte retention, derived stats, and source fixtures.
- Player activation is an application boundary and is not wired into the incoming packet sink; real network disconnects do not yet invoke the player cleanup API.
- The source world handshake's username-only transfer lookup is not considered a safe session binding. The C# use case requires an authenticated login connection and an explicit world connection association; no new client authentication packet is introduced.
- The Mongo collection `character_mongooses` is inferred from `character_mongoose`; operators must configure/verify the actual deployed collection. Numeric BSON widths and missing/null legacy field distributions are unverified without a production export.
- Character state references runtime `Contribution` and `GuildTag` properties that are not declared in the persisted schema; this stage does not invent mappings for them.
- NPC loading is active in legacy `zone.js`, while monster MOB loading is commented out. The C# stage has no complete NPC-definition catalog, so it does not create NPC actors from spawn records. Monster AI, items, movement simulation, combat, and other gameplay systems remain deferred.
- Legacy source has duplicate disconnect callbacks and incomplete removal paths. C# manager operations are synchronized and idempotent, but real network disconnect wiring awaits the missing player packet flow.

## Verification

This document will be updated with concrete test/build evidence before the Prompt 15 commit. Synthetic BSON tests are not production data compatibility tests; no live MongoDB result or original-client result will be claimed without running those environments.

## Source references

- [`handleLogin.js`](../../packets/login/handleLogin.js#L189) — account character query, three-slot listing, and login response.
- [`handleGameStart.js`](../../packets/login/handleGameStart.js#L21) — slot selection, saved-map use, derived-stat initialization, and transfer queue.
- [`handshake.js`](../../packets/world/handshake.js#L7) — username-keyed transfer handoff and `0x15` acknowledgment.
- [`CharacterState.js`](../../generic/CharacterState.js#L43) — persistent-to-runtime projection, saved position, random facing, current/max vitals.
- [`character.js`](../../db/character.js#L48) — legacy character schema and mixed arrays.
- [`counter.js`](../../db/counter.js#L5) — persistent character ID counter mechanism.
- [`world.js`](../../world.js#L679) — pending transfer queue; [`world.js`](../../world.js#L540) — disconnect position save/removal flow.
- [`zone.js`](../../zone.js#L67) — player registration/spatial ID; [`zone.js`](../../zone.js#L131) — player removal; [`zone.js`](../../zone.js#L155) — distance-based broadcast.
- [`QuadTree.js`](../../QuadTree.js#L137) — per-zone runtime node-ID counter and spatial index.
- [`structs.js`](../../generic/structs.js#L145) — large legacy Character wire structure.
- [`FruitSalad/`](../../FruitSalad/) — alternate runtime, intentionally not used as root-server behavior.
