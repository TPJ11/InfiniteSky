# MongoDB Persistence Implementation

## Status and scope

This stage adds the MongoDB infrastructure and the production account-authentication repository required by Prompt 11. It does **not** implement character, counter, guild, email, inventory, bank, gameplay, or world persistence. The original Node.js source and any MongoDB data are unchanged.

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
| Connection failure and shutdown | [`db.js:32-45`](../../db.js) | Startup fails with a sanitized category; periodic bounded probes update health; DI disposes the singleton client on host shutdown. No application-level retry is added. |

The Node package declares `mongoose: ">=3.8.8"` ([`package.json:29`](../../package.json)) without a lockfile. The root configuration names database `ts1` but contains a legacy connection-string example; it is not copied into C# configuration or documentation ([`config.json-dist:29-35`](../../config.json-dist)). The actual MongoDB server version, authentication database, deployed collection names, index inventory, and representative account BSON were not available.

## Composition and lifecycle

- `ServerHostBuilder` projects strongly typed `DatabaseOptions` into `MongoPersistenceSettings`.
- `MongoPersistenceContext` owns one thread-safe `MongoClient` and `IMongoDatabase`. Driver read/write retries are disabled; all repository and bootstrap operations use cancellation and bounded tokens.
- `MongoPersistenceHostedService` runs after foundation initialization and before network listeners. When persistence is enabled it pings the selected database, verifies/creates the account index according to configuration, optionally resets all account `active` fields, then starts periodic health probes.
- The production `IAccountAuthenticationRepository` registration resolves to `MongoAccountAuthenticationRepository`. There is no in-memory or unavailable production fallback.
- When persistence is disabled (the default), no MongoDB client is created, the database health check reports `Degraded`, and authentication fails closed with `CannotAuthenticate`.
- Invalid enabled settings fail option validation or context construction without printing the URI. An unreachable server, authentication/authorization failure, incompatible index, missing index, malformed account, duplicate key, BSON failure, and cancellation are classified separately where applicable.

The startup reset affects **every document** in the configured account collection. It matches the original startup update but may log users out across other server processes. Operators running multiple instances must explicitly review `ResetActiveAccountsOnStartup` before deployment.

## Implemented account repository

The repository implements only the Prompt 11 contract:

- exact username lookup with case-sensitive/simple collation;
- account identifier and credential field extraction;
- missing-account return as `null`;
- malformed required fields as explicit repository failure;
- `active` update for a single numeric account `_id`;
- the source-backed startup reset operation.

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
- Unit tests cover raw BSON mapping/round-trip, BSON numeric identifiers, missing and null credentials, malformed data, error classification, disabled configuration, URI/database mismatch, and log redaction.
- Server integration tests cover DI binding, invalid configuration, disabled health reporting, and unreachable MongoDB startup/health classification.
- The Testcontainers integration test is opt-in with `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true`. It covers index creation, exact account lookup, active updates that preserve unknown fields, duplicate keys, the all-account active reset operation, missing accounts, and cancellation.
- `dotnet restore InfiniteSky.sln` and `dotnet build InfiniteSky.sln --no-restore` pass. The normal test run passes its runnable tests; the MongoDB container test is skipped because neither Docker nor `mongod` is available. No live database compatibility claim is made.

Run the isolated test where Docker is available:

```powershell
$env:INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS = "true"
dotnet test tests\InfiniteSky.Persistence.Tests\InfiniteSky.Persistence.Tests.csproj
Remove-Item Env:\INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS
```

## Deferred persistence

The original character, counter, guild, email, bank, inventory, and gameplay operations remain unmigrated. No collection is dropped, renamed, rewritten, bulk-converted, or seeded. `PER-003` through `PER-012` and full persistence parity remain incomplete.
