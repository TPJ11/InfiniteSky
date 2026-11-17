# Persistence Migration

## Scope and matrix coverage

`PER-001` through `PER-012`: Mongo lifecycle, accounts, characters, counters, guilds, email, online state, banks, save coordination, guild loading, and compatibility validation.

## Source files

`db.js`, `db/account.js`, `db/character.js`, `db/counter.js`, `db/guild.js`, `db/email_queue.js`, account/logout code in `world.js`, bank setup in `packets/world/world_login.js`, direct saves throughout commands and handlers.

## Target components

- `MongoContext`
- BSON documents and mappers
- account/character/guild/counter/email repositories
- `CharacterIndividualMapper`
- `CharacterSaveCoordinator`
- `PersistenceQueue`
- `InfiniteSky.DataCompare`

## Dependencies

Depends on `DOM-001..006`; critical migration depends on `PER-011` before production and `PER-012` before zone loops.

## Implementation order

1. Connect/disconnect/error policy.
2. Account and counter documents.
3. Character document tolerant mapper.
4. Inventory/bank/storage mapping.
5. Guild/email mapping.
6. Atomic account/online/session rules.
7. Save queue and dirty snapshots.
8. Shadow comparison/dry-run migration tool.
9. Startup/shutdown drain integration.

## Tests

- BSON round trip on representative fixtures;
- missing/extra fields;
- sparse and null arrays;
- duplicate account/name handling;
- concurrent counters;
- account active flag races;
- 56-slot bank and per-server data;
- write failure/retry;
- graceful drain;
- zero-loss field comparison report.

## Acceptance criteria

- An existing character/account can be read and written without losing selected known fields.
- No zone actor performs MongoDB I/O.
- Critical inventory/economy operations have explicit save acknowledgement.
- Startup detects incompatible required fields.
- Counter and duplicate-login tests are concurrency-safe.
- Password/PIN behavior and any later hashing migration are explicitly governed.

## Risks

Mongo `Mixed` arrays permit irregular data. Original schemas duplicate fields and attach runtime methods/state. Collection/casing/version assumptions are unknown and must be validated against a real representative database.
