# Zone Visibility and Transitions

## Visibility and area-of-interest evidence

The source has two distinct area-broadcast paths:

1. `Zone.sendToAllAreaLocation` queries the QuadTree with `CVec3` X/Z coordinates and `type: 'client'`, optionally excludes a supplied socket, and skips unauthenticated results ([`zone.js`](../../zone.js#L167-L177)).
2. `Zone.sendToAllArea` iterates the zone client list and uses `CVec3.getDistance`, a full 3D calculation; it checks authentication, live socket handle, and optional self-exclusion ([`zone.js`](../../zone.js#L155-L165), [`generic/CVec3.js`](../../generic/CVec3.js#L58-L78)).

These are not interchangeable. `config.viewable_action_distance` defaults to 500 and is passed by several action/update broadcasts ([`config.json-dist`](../../config.json-dist#L56), [`zone.js`](../../zone.js#L79-L80)). NPC and other zone nodes are indexed with type labels; active broadcasts select clients, while the spatial index also contains non-client candidates.

The C# `QueryActionCandidates` exposes source-distance X/Z candidates and `ISpatialIndex.Query` supports type filtering. It does not filter authenticated sessions, select sockets, send packets, implement entity visibility, or reconcile the two legacy broadcast algorithms. Those responsibilities require later player/entity and networking integration. No visibility packet is added.

## Portal records and incomplete rules

Move and safe regions are arrays of 32-byte WREGION records, each with eight signed int32 fields. The original `Zone.getPortal` chooses the first even-index move region with a matching destination ID; `getPortalEndPoint` chooses the first odd-index region matching an ID ([`generic/structs.js`](../../generic/structs.js#L483-L492), [`zone.js`](../../zone.js#L188-L208)).

The active zone-change handler has a separate path. It scans the current zone's regions for the requested ID, selects index 0 for a hardcoded special-zone list, scans destination regions backward for the current zone ID, and falls back to the destination's first region. Portal-position/radius checks, transfer authorization, and map-server readiness are comments/TODOs ([`zonechange.js`](../../packets/world/zonechange.js#L20-L89), [`zonechange.js`](../../packets/world/zonechange.js#L107-L190)). The tracked repository contains no `.WREGION` files, so real portal pairs cannot be fixture-verified.

The handler maps request ID 0 to a clan town (clan 0 → zone 1, clan 1 → zone 6, clan 2 → zone 11). On its success branch it sets the destination coordinates and map ID, saves the character, marks transfer flags, and queues the socket ([`zonechange.js`](../../packets/world/zonechange.js#L44-L57), [`zonechange.js`](../../packets/world/zonechange.js#L191-L207)). This is not a complete or validated transition protocol.

## Zone-change wire boundary

The original world packet `0x08` has four little-endian uint32 fields: `ZoneID`, `PortalID`, `Unknown1`, and `Unknown2` (16 payload bytes). The handler requires an authenticated socket. It writes a map-load response with packet ID `0x0A`, one-byte status, fixed-width IP string (`packets.IPAddressLength + 1`), and uint32 port ([`zonechange.js`](../../packets/world/zonechange.js#L5-L20), [`zonechange.js`](../../packets/world/zonechange.js#L215-L241)).

No transition handler, portal resolver, transfer queue, player movement, zone persistence, or client packet emission is implemented in Prompt 14. The existing protocol structures remain the compatibility boundary; this zone stage does not redefine or emit them.

## Implemented boundary and deferred work

Implemented: per-zone proximity index, type-filtered candidate lookup, configurable source action distance, WREGION field parsing, and host/zone readiness.

Deferred: portal pairing semantics, special-zone authorization, source/destination membership transactions, saved-position integration, reconnect/transfer queue behavior, player spawn/despawn, exact network recipient selection, and zone-entry/exit packets. These remain explicit dependencies of the player/entity and world-transfer stages; no coordinate, radius, or packet behavior has been invented to fill the missing data.
