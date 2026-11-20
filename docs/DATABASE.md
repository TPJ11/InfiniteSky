# Database

## MongoDB scope

The server uses the official MongoDB C# driver through the DI-managed persistence context. Database access is disabled by default. Configuration examples, timeouts, health behavior, collection-name assumptions, and the global startup active-flag reset are documented in [DATABASE_CONFIGURATION.md](migration/DATABASE_CONFIGURATION.md).

Implemented persistence is limited to:

- Account lookup by exact username, authentication fields, targeted `active` updates, and optional startup-wide reset.
- Read-only guild loading for world bootstrap.
- Account/server-scoped character listing/ownership reads and targeted saved map/location updates.
- Username-index verification/creation when configured; existing indexes are not dropped or rewritten.

It does not provide full character-document round-trip, counter allocation, guild writes, email/player mail, inventory/bank/equipment/quest persistence, gameplay mutation persistence, multi-instance session coordination, or transactional economy operations.

## Legacy schema and compatibility

Original account, character, guild, counter and email definitions are under `db/`; alternate FruitSalad schemas differ and are not merged. C# uses selected raw-BSON mappings. Synthetic fixtures exercise only known field subsets and do not prove compatibility with a deployed database. Collection names such as `account_mongooses` and `character_mongooses` are inferred from Mongoose naming and must be confirmed. BSON numeric subtypes and unknown field preservation require representative sanitized exports.

Two opt-in MongoDB Testcontainers tests are skipped in the current baseline. No live database, production account, or real BSON dump was accessed. The integration environment must be disposable and isolated.

## Security, backup and restoration

Legacy passwords are stored as plaintext. Restrict database and backup access. Remote MongoDB TLS is not forced by app validation; verify TLS/certificates or use a protected database network before enabling remote persistence. `ResetActiveAccountsOnStartup=true` updates all active flags in the configured account collection and is unsafe to assume harmless for shared/multi-instance use.

The application does not run schema migrations, backups, restore, failover, or destructive conversions. Back up through the actual MongoDB operator process and test restore on an isolated copy. Do not modify production records based solely on synthetic fixtures.

See [MONGODB_PERSISTENCE_IMPLEMENTATION.md](migration/MONGODB_PERSISTENCE_IMPLEMENTATION.md), [MONGODB_SCHEMA_MAPPING.md](migration/MONGODB_SCHEMA_MAPPING.md), [CHARACTER_PERSISTENCE.md](migration/CHARACTER_PERSISTENCE.md), and [OPERATIONAL_RECOVERY.md](migration/OPERATIONAL_RECOVERY.md).
