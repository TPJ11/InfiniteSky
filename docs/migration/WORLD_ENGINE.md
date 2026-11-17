# World Engine Migration

## Scope and matrix coverage

`WLD-001` through `WLD-010`: world clock, zone actors, registries, output, transfer, regen, snapshots, bootstrap, compatibility profiles, and lifecycle.

## Source files

`GameStep.js`, `world.js`, `zone.js`, `QuadTree.js`, `packets/world/world_login.js`, disconnect handlers, and world-state scripts.

## Target components

`InfiniteSky.World/WorldRuntime`, `ZoneActor`, `ZoneCommand`, `WorldClock`, `OnlineWorldRegistry`, `ZoneOutputCollector`, `ZoneTransferCoordinator`, and snapshot producers.

## Dependencies

Requires world-domain values, packet state codecs, zone loader/index, persistence queue, and session transfer.

## Implementation order

1. World clock with fake `TimeProvider`.
2. Zone command/mailbox actor.
3. Online and zone routing registry.
4. Output collector to connection writers.
5. Snapshot/save queue.
6. Transfer coordinator.
7. Regen and timer management.
8. Guild/world bootstrap.
9. compatibility profiles.
10. unload/shutdown.

## Tests

- mailbox FIFO and cancellation;
- equal-tick ordering;
- no cross-zone locks/deadlocks;
- backpressure at queue bounds;
- output recipient order;
- transfer timeout/failure;
- save snapshot after disconnect;
- timer and regeneration fake-time behavior;
- actor failure isolation.

## Acceptance criteria

- Exactly one mutable owner exists for each entity.
- A zone actor never awaits network/database I/O while mutating state.
- Tick p95 and mailbox depth are measured.
- Profiles are explicit in logs and tests.
- Shutdown produces no lost acknowledged save operations.

## Risks

Preserving packet order across actors, transfer atomicity, legacy 100 ms cadence, and timer semantics may affect client behavior. Global world operations must not bypass actor mailboxes.
