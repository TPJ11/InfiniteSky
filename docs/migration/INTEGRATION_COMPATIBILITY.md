# Integration and Compatibility Migration

## Scope and matrix coverage

`INT-001` through `INT-015`: end-to-end login/world, entity visibility, inventory, combat, transfers, database migration, packet corpus, client soak, performance, restart, editors/tooling, and deployment/runbooks.

## Source files

The complete repository, plus `README.md`, `docs/`, `FruitSalad/Processes/ItemEditor/**`, `SpawnEdit/**`, `SpawnLogger/**`, `TSX_Client/**`, `.pac` captures, batch/Grunt/package tooling.

## Target components

Test projects, `InfiniteSky.DataCompare`, protocol dump tooling, spawn/item tooling where approved, deployment assets, and operations documentation.

## Dependencies

Integration depends on completed subsystem vertical slices. It must not wait until all features exist to begin: login-to-world, inventory, and one zone are early compatibility gates.

## Implementation order

1. Fixture runner and protocol transcript.
2. DB mapper comparison.
3. Login/character/world/zone smoke integration.
4. movement/visibility transcript.
5. inventory/economy and reconnect.
6. combat/skill/drop transcript.
7. transfer and shutdown/restart.
8. client soak and performance.
9. tooling/editor evaluation.
10. deployment and operator validation.

## Tests

- real loopback sockets;
- optional MongoDB integration;
- generated original-data records;
- packet and BSON goldens;
- controlled client transcript;
- deterministic profiles;
- load/soak;
- memory/allocation and GC;
- packet/tick/save latency;
- migration dry-run.

## Acceptance criteria

### Packet compatibility

- 100% of registered active opcodes have codec fixtures.
- 100% of P0 login/world/inventory/action opcodes match byte-for-byte captures.
- Fragmentation/coalescing transcripts pass.

### Database compatibility

- All selected known fields, slots, bank, guild, and counter semantics round-trip.
- Compatibility validator reports unknown/extra source fields and no unexplained loss.

### Gameplay/world

- Login, create/select, enter, move, see, inventory, combat profile, zone transfer, and reconnect scenarios pass.
- Zone tick and network output remain ordered and deterministic.

### Operations

- Clean build/deploy/start/health/migrate/stop is documented and exercised.
- Readiness only becomes healthy after dependencies.
- Performance budgets are recorded, not merely observed.

## Risks

Without a representative client, packet captures, production BSON documents, and original data corpus, final parity cannot be proven. Client soak and byte/database comparison are release gates.

## Repository coverage rule

Before a subsystem is accepted as migrated, run a source-directory-to-matrix audit. Every relevant source file must be:

1. covered by one or more feature IDs;
2. listed in an approved deviation;
3. classified as third-party/research/vendor; or
4. classified `NOT_APPLICABLE` only with an approved reason.
