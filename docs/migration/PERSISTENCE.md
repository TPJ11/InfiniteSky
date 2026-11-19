# Persistence Migration

## Status

Prompt 12 implements `PER-001` MongoDB connection/configuration/health infrastructure, `PER-002` account authentication document compatibility, and the account login/logout persistence slice of `PER-007`. These rows are `IMPLEMENTED`, not `VERIFIED` against a live MongoDB deployment. The container integration test is opt-in and was skipped because Docker and `mongod` are unavailable.

See [`MONGODB_PERSISTENCE_IMPLEMENTATION.md`](MONGODB_PERSISTENCE_IMPLEMENTATION.md), [`MONGODB_SCHEMA_MAPPING.md`](MONGODB_SCHEMA_MAPPING.md), and [`DATABASE_CONFIGURATION.md`](DATABASE_CONFIGURATION.md) for source evidence, BSON decisions, operations, test results, and risks.

## Implemented now

- One DI-owned MongoClient, startup validation, bounded ping/health checks, cancellation, sanitized error classification, metrics, and shutdown disposal.
- Prompt 11 account repository: exact username lookup, credential extraction with missing/null handling, active-flag update, and source-backed startup reset.
- Raw BSON reads and field-only writes so unknown account fields are not reserialized.
- Safe username-index verification/creation; no index drops or schema migrations.
- Synthetic BSON fixture plus unit/server tests. Real MongoDB compatibility is pending an isolated container run and representative sanitized legacy data.

## Deferred persistence

`PER-003` through `PER-006` and `PER-008` through `PER-012` remain incomplete: characters, counters, guilds, email, account bank/nested data, inventory/save coordination, guild loading, whole-schema validation, and bounded persistence queue.

Source includes `db.js`, `db/account.js`, `db/character.js`, `db/counter.js`, `db/guild.js`, `db/email_queue.js`, account/logout and guild loading in `world.js`, bank setup in `packets/world/world_login.js`, and direct saves throughout handlers. FruitSalad is a separate runtime with incompatible models and is not merged.

## Remaining acceptance work

- Inspect deployed collection and index names, MongoDB server/auth configuration, and a sanitized representative BSON export.
- Validate all account numeric BSON subtypes and missing/null/extra fields against that export.
- Execute the opt-in Testcontainers suite and verify deployment health/permissions without connecting to production.
- Implement character/counter/guild/email/bank/inventory persistence only in their approved roadmap stages.
- Add zero-loss comparison and safe save/drain behavior before claiming full persistence parity.

Mongo `Mixed` arrays permit irregular data. Source schemas contain duplicate JavaScript keys and runtime methods/state. There is no verified production server version or collection inventory. The startup reset updates all account documents and requires review for multi-instance deployments.
