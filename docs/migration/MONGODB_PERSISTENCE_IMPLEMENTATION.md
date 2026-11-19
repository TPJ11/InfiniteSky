# MongoDB Persistence Implementation

## Status and scope

Prompt 12 added MongoDB infrastructure and the production account-authentication repository. Prompt 13 adds a narrow, read-only guild bootstrap for the single world runtime. Prompt 15 adds a raw-BSON character read mapper/repository and targeted saved-world-location update. Counter allocation, full character round-trip, guild writes, email, inventory, bank, gameplay, and broader world-state persistence remain unimplemented. The original Node.js source and any MongoDB data are unchanged.

The official `MongoDB.Driver` package is pinned centrally at `3.14.0`. No production MongoDB server version was found in the repository or available for inspection, so this implementation does not claim a production minimum. The opt-in container test currently targets MongoDB `7.0`.

## Original behavior traced

| Behavior | Legacy source | C# behavior |
|---|---|---|
| Connect once from startup configuration | [`main.js:142`](../../main.js), [`db.js:9-23`](../../db.js) | `MongoPersistenceContext` owns one DI-managed `MongoClient`; no client is created when disabled. |
| Account lookup by exact `Username` | [`db/account.js:71-86`](../../db/account.js), [`packets/login/handleLogin.js:279-324`](../../packets/login/handleLogin.js) | `MongoAccountAuthenticationRepository` uses a simple-collation equality filter and projects `_id`, `Username`, and `Password`. |
| Credential comparison | [`packets/login/handleLogin.js:322`](../../packets/login/handleLogin.js) | Existing application service compares the legacy password text; the repository transfers only transient UTF-8 bytes and the service clears them. Missing or null `Password` is a failed credential, not an empty password. |
| Mark account active | [`packets/login/handleLogin.js:325-329`](../../packets/login/handleLogin.js) | `$set` only `active` to BSON `Int32(1)`. It does not replace or reserialize the account document. |
| Reset account active flags at startup | [`db.js:19-23`](../../db.js), [`db/account.js:135-151`](../../db/account.js) | Configurable startup `UpdateMany` sets only `active` to BSON `Int32(0)`; enabled by default to match the observed Node startup behavior. It is awaited before listener startup. |
| Reset on legacy zone-transfer logout | [`db/account.js:30-48`](../../db/account.js) | Prompt 11 continues to call `SetActiveAsync(false)` only for the existing rollback/logout paths; ordinary login disconnect does not clear the persistent field. |
| Account username uniqueness | [`db/account.js:11-27`](../../db/account.js) | Startup verifies the unique ascending `Username` index. It creates the `Username_1` index only when configured and missing; it never drops or modifies an index. |
| Load guilds for the world bootstrap | [`db/guild.js:6-14`](../../db/guild.js), [`world.js:937-962`](../../world.js) | `MongoGuildRepository` asynchronously reads and maps the configured guild collection. Query failures degrade world readiness; guild writes and gameplay-facing methods remain future work. |
| Load account-owned characters | [`packets/login/handleLogin.js`](../../packets/login/handleLogin.js), [`db/character.js`](../../db/character.js) | `MongoCharacterRepository` filters by `AccountID` and `ServerName`, maps at most three raw BSON documents, and preserves Mongo's natural result order. |
| Load selected character by owner | [`packets/login/handleGameStart.js`](../../packets/login/handleGameStart.js) | `GetOwnedAsync` filters by `_id`, `AccountID`, and `ServerName`; foreign and missing characters both return `null`. |
| Save location on disconnect | [`world.js:540-568`](../../world.js), [`db/character.js`](../../db/character.js) | `$set` only `MapID` and `RealX/Y/Z`; coordinates truncate toward zero. It does not update HP/Chi or replace the document. |
| Connection failure and shutdown | [`db.js:32-45`](../../db.js) | Startup fails with a sanitized category; periodic bounded probes update health; DI disposes the singleton client on host shutdown. No application-level retry is added. |

The Node package declares `mongoose: ">=3.8.8"` ([`package.json:29`](../../package.json)) without a lockfile. The root configuration names database `ts1` but contains a legacy connection-string example; it is not copied into C# configuration or documentation ([`config.json-dist:29-35`](../../config.json-dist)). The actual MongoDB server version, authentication database, deployed collection names, index inventory, and representative account BSON were not available.

## Composition and lifecycle

- `ServerHostBuilder` projects strongly typed `DatabaseOptions` into `MongoPersistenceSettings`, including account, character, and guild collection names.
- `MongoPersistenceContext` owns one thread-safe `MongoClient` and `IMongoDatabase`. Driver read/write retries are disabled; all repository and bootstrap operations use cancellation and bounded tokens.
- `MongoPersistenceHostedService` runs after foundation initialization and before network listeners. When persistence is enabled it pings the selected database, verifies/creates the account index according to configuration, optionally resets all account `active` fields, then starts periodic health probes.
- The production `IAccountAuthenticationRepository` registration resolves to `MongoAccountAuthenticationRepository`. There is no in-memory or unavailable production fallback.
- The production `IGuildRepository` registration resolves to `MongoGuildRepository`. The world host reads guilds after persistence-host startup and before network listeners. If the guild query fails after persistence startup, the world reports degraded readiness; if persistence startup itself fails, the host does not proceed to world startup.
- The production `ICharacterRepository` registration resolves to `MongoCharacterRepository`. Character reads remain unavailable when MongoDB is disabled; the host does not use a fake or in-memory persistence fallback.
- When persistence is disabled (the default), no MongoDB client is created, the database health check reports `Degraded`, and authentication fails closed with `CannotAuthenticate`.
- Invalid enabled settings fail option validation or context construction without printing the URI. An unreachable server, authentication/authorization failure, incompatible index, missing index, malformed account, duplicate key, BSON failure, and cancellation are classified separately where applicable.

The startup reset affects **every document** in the configured account collection. It matches the original startup update but may log users out across other server processes. Operators running multiple instances must explicitly review `ResetActiveAccountsOnStartup` before deployment.

## Implemented account repository

The account repository implements only the Prompt 11 contract:

- exact username lookup with case-sensitive/simple collation;
- account identifier and credential field extraction;
- missing-account return as `null`;
- malformed required fields as explicit repository failure;
- `active` update for a single numeric account `_id`;
- the source-backed startup reset operation.

The separate guild repository implements only the Prompt 13 read path. It reads raw BSON and maps legacy guild bootstrap fields; it does not save guild state or implement guild packet/gameplay operations. The character repository implements only the Prompt 15 selection/activation boundary and location save, not full character document round-tripping or gameplay writes. See [`WORLD_MANAGEMENT_IMPLEMENTATION.md`](WORLD_MANAGEMENT_IMPLEMENTATION.md) and [`CHARACTER_PERSISTENCE.md`](CHARACTER_PERSISTENCE.md).

It reads raw `BsonDocument` values and does not register global conventions or BSON class maps. Unknown fields and nested legacy structures are therefore not deserialized into C# classes or rewritten. Account IDs accept BSON `Int32`, `Int64`, and integral finite `Double` values; unsupported, fractional, negative, or out-of-range values are treated as malformed. The active flag is written as `Int32`, matching the schema's numeric field and integral Node updates; confirm this choice against a representative database export before claiming complete BSON parity.

The login repository does not reproduce the original active/live-world-socket check or the `Level > 80` stale-active exception. Those are outside the Prompt 11 repository contract; the existing C# in-process session policy remains stricter. See [`AUTHENTICATION_IMPLEMENTATION.md`](AUTHENTICATION_IMPLEMENTATION.md).

## Index management

The account username index is expected to be ascending, unique, non-sparse, non-partial, and simple-collation, as implied by the Mongoose schema declaration. New indexes use the stable name `Username_1`. A same-key index with incompatible uniqueness, sparsity, partial filter, or collation fails startup. Existing indexes are never dropped or altered. When index creation encounters duplicate account names, startup fails and reports an index/duplicate-key category; operators must inspect the data and resolve it manually.

`EnsureIndexesOnStartup` controls creation. When false, the required index is verified but not created. The MongoDB-generated `_id_` index is relied on and is not modified.

## Health and observability

The `mongodb-persistence` health check reports disabled, not checked, connected, authentication failure, unavailable, or operational failure without exposing connection details. It is registered with the readiness tag; no HTTP health endpoint is currently hosted by the server.

The `InfiniteSky.Persistence` meter records operation counts, durations, failures, and a health-status gauge. Metric dimensions are limited to operation and failure category; usernames, account IDs, document values, and URIs are not tags. Persistence logs include only the operation name and a safe failure category.

## Retry and consistency policy

The source contains no explicit application retry policy. The C# driver has automatic retry reads and writes disabled, and repository operations are not blindly retried. Authentication's `active` changes are single-document `$set` operations; the startup reset is the legacy bulk `$set`. No transactions are introduced. The periodic ping is a read-only health probe, not a retry of an application write.

## Tests and verification

- BSON fixture: [`legacy-account-document.json`](../../tests/InfiniteSky.Persistence.Tests/Fixtures/legacy-account-document.json) is a synthetic fixture based on the source schema, not a database export.
- Character BSON fixture: [`legacy-character-document.json`](../../tests/InfiniteSky.Persistence.Tests/Fixtures/legacy-character-document.json) is synthetic and covers only fields represented by the current domain model.
- Unit tests cover raw BSON mapping, numeric identifiers, missing/null fields, sparse arrays, malformed data, error classification, disabled configuration, URI/database mismatch, and log redaction.
- Server integration tests cover DI binding, invalid configuration, disabled health reporting, and unreachable MongoDB startup/health classification.
- Testcontainers integration tests are opt-in with `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true`. They cover account index/lookup/update/reset behavior, synthetic guild loading, character ownership, targeted location saves, and unknown-field preservation.
- Prompt 15 validation: restore succeeded, the complete solution built with zero warnings/errors, all 156 runnable tests passed, and two MongoDB integration tests were skipped. Docker is not installed in the current environment, so no live MongoDB compatibility claim is made. Full-solution format verification passed.

Run the isolated test where Docker is available:

```powershell
$env:INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS = "true"
dotnet test tests\InfiniteSky.Persistence.Tests\InfiniteSky.Persistence.Tests.csproj
Remove-Item Env:\INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS
```

## Deferred persistence

Character loading and targeted location saves do not establish full `PER-003` compatibility: unknown Mixed fields are read but not represented, there is no production BSON fixture, and no whole-document round trip is supported. Counter, guild writes, email, bank, inventory, and gameplay operations remain unmigrated. No collection is dropped, renamed, rewritten, bulk-converted, or seeded. `PER-004` through `PER-012` and full persistence parity remain incomplete.
