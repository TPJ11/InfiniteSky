# Networking Migration

## Scope and matrix coverage

`NET-001` through `NET-010`: TCP lifecycle, framing, output ordering, backpressure, phases, address translation, session transfer, duplicate login, and keepalive.

## Source files

`login.js`, `world.js`, `CachedBuffer.js`, `PacketCollection.js`, `util.js`, `packets/login/handleGameStart.js`, `packets/world/handshake.js`, `packets/world/zonechange.js`.

## Target components

- `InfiniteSky.Networking/TcpListenerService`
- `ConnectionReader` and `ConnectionWriter`
- `GameConnection`
- `ConnectionStateMachine`
- `AdvertisedEndpointProvider`
- `InfiniteSky.Application/SessionTransferStore`
- `OnlineSessionRegistry`

## Dependencies

Depends on `PRO-001..004`, `PRO-010`, `PRO-014`, `PER-002`, `PER-007`, and `INF-002`.

## Implementation order

1. Define connection lifecycle and phase transitions.
2. Implement bounded pipeline receive parser with fixture descriptors.
3. Implement single-writer outbound ordering.
4. Add timeouts/backpressure and metrics.
5. Implement remote endpoint normalization and advertised address selection.
6. Implement in-memory login/world transfer store with timeout.
7. Add duplicate-account coordination.
8. Connect handlers without moving gameplay into networking.

## Tests

- one byte per TCP read;
- coalesced and fragmented frames;
- multiple frames per read;
- maximum size and incomplete frame;
- simultaneous replies preserving order;
- slow-client queue saturation;
- connection abort during read/write;
- login/world phase enforcement;
- transfer single consumption, expiry, and account mismatch;
- loopback/NAT/external advertised addresses.

## Acceptance criteria

- Zero direct socket access from domain/world components.
- Every complete packet has one typed handler invocation.
- Unknown/invalid input follows an explicit disconnect/recover policy.
- Transfer and duplicate-login races are deterministic.
- Login and world ports preserve legacy client behavior.

## Risks

The original receive header's first eight bytes are ignored, so their real client meaning remains unknown. Server writes are not symmetrically framed. Network hardening must be profile-gated when it changes disconnect or transfer behavior.
