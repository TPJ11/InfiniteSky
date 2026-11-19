# Entity Visibility Integration

## Implemented candidate boundary

Player registration adds a `client` entry to the existing zone spatial index at the character's saved X/Y/Z position. The source `zone.js` uses character level as the spatial node size and queries nearby clients using X/Z distance and the configured viewable-action distance. `PlayerManager.GetNearbyPlayers` reuses that zone index and returns only active player snapshots in the same zone, excluding the subject player.

The integration is intentionally a candidate lookup, not a visibility subscription or packet broadcast. It uses no second spatial index and does not make every same-zone player visible. The same zone's `ViewableActionDistance` option controls the candidate radius; it defaults to the existing zone-manager value. The index maintains its established behavior for object size and boundary pruning.

Position changes flow through `IPlayerManager.TryUpdatePosition`, which updates the zone spatial entry before changing the player runtime snapshot. A rejected out-of-bounds update leaves the prior position intact. Removing a player deletes its spatial object before manager indexes are cleared; zone shutdown and player shutdown are ordered to avoid stale registrations.

## Not implemented

- No client appearance/disappearance or spawn/despawn packets are sent.
- No original packet opcode, field layout, or recipient ordering is inferred.
- No NPC/monster/item actor is registered by this stage; legacy NPC definitions are not runtime actors.
- No persistent visibility set, movement event subscription, or cross-zone transfer notification is maintained.
- No claim of original-client visual compatibility is made.

The missing full Character wire codec, derived-stat initialization, and world spawn/despawn packet path are explicit dependencies. Candidate lookup may be used by a later packet-replication stage, but is not itself proof that the original server's full visibility rules have been reproduced.

## Tests

`ZoneManagementTests` verifies zone-local player registration, candidate filtering, self exclusion, position update behavior, zone spatial counts, and cleanup. Protocol fixtures and a live client test remain unavailable for this boundary.

## References

- [`zone.js`](../../zone.js) — original client-node size, proximity query, and broadcast path.
- [`QuadTree.js`](../../QuadTree.js) — X/Z query and per-zone node registry.
- [`ZoneRuntime.cs`](../../src/InfiniteSky.World/Zones/ZoneRuntime.cs) — running-zone spatial boundary.
- [`PlayerManager.cs`](../../src/InfiniteSky.World/Players/PlayerManager.cs) — player candidates and lifecycle cleanup.
- [`PROTOCOL_IMPLEMENTATION.md`](PROTOCOL_IMPLEMENTATION.md) — current protocol limitations.
