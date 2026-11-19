# MongoDB Database Configuration

## Defaults and enabling

MongoDB persistence is disabled by default. When disabled, the host creates no MongoDB client, reports the database check as `Degraded`, and account/character persistence fails closed; world guild data is also unavailable. To enable MongoDB account access, character loading, targeted location saves, and guild bootstrap, configure a MongoDB URI and verify the database and physical collections first.

| Setting | Default | Validation / behavior |
|---|---|---|
| `Enabled` | `false` | No connection is attempted unless true. |
| `Provider` | `MongoDB` | Must be MongoDB when enabled. |
| `ConnectionString` | empty | Required MongoDB URI (`mongodb://` or `mongodb+srv://`); never logged. |
| `DatabaseName` | `ts1` | Required when enabled; must match a database path in the URI, if present. |
| `AccountCollectionName` | `account_mongooses` | Inferred from Mongoose model `account_mongoose`; verify against the deployed database. |
| `CharacterCollectionName` | `character_mongooses` | Inferred from Mongoose model `character_mongoose`; verify against the deployed database. |
| `GuildCollectionName` | `guilds` | Inferred from Mongoose model `guilds`; used for the read-only world bootstrap; verify against the deployed database. |
| `EnsureIndexesOnStartup` | `true` | Create the unique ascending username index if missing; never drop/alter an existing index. Set false to verify only. |
| `ResetActiveAccountsOnStartup` | `true` | Runs the legacy global `active=0` reset before listeners start. Review for multi-instance deployments. |
| `ServerSelectionTimeoutSeconds` | `5` | Valid range 1–120. |
| `ConnectTimeoutSeconds` | `5` | Valid range 1–120. |
| `OperationTimeoutSeconds` | `10` | Valid range 1–300; each repository operation is also cancellable by its caller. |
| `HealthCheckIntervalSeconds` | `30` | Valid range 1–3600. |
| `HealthCheckTimeoutSeconds` | `3` | Valid range 1–120. |
| `MinConnectionPoolSize` | `0` | `0 <= Min <= Max <= 10000`. |
| `MaxConnectionPoolSize` | `100` | `1 <= Max <= 10000`. |

The defaults are defined in [`DatabaseOptions.cs`](../../src/InfiniteSky.Server/Configuration/DatabaseOptions.cs) and mirrored in the safe, disabled [`appsettings.json`](../../src/InfiniteSky.Server/appsettings.json).

## Environment configuration

The server reads environment variables using the `INFINITESKY_` prefix and the standard `__` section separator. For a local unauthenticated development database:

```powershell
$env:INFINITESKY_InfiniteSky__Database__Enabled = "true"
$env:INFINITESKY_InfiniteSky__Database__ConnectionString = "mongodb://127.0.0.1:27017/ts1"
$env:INFINITESKY_InfiniteSky__Database__DatabaseName = "ts1"
$env:INFINITESKY_InfiniteSky__Database__AccountCollectionName = "account_mongooses"
$env:INFINITESKY_InfiniteSky__Database__CharacterCollectionName = "character_mongooses"
$env:INFINITESKY_InfiniteSky__Database__GuildCollectionName = "guilds"
$env:INFINITESKY_InfiniteSky__Characters__ServerName = "Development"
dotnet run --project src\InfiniteSky.Server\InfiniteSky.Server.csproj
```

This example contains no credentials and assumes only a local development server. For authenticated deployments, supply the complete URI from a secret manager or protected environment configuration, including a properly escaped username/password and `authSource` when required. Never place production credentials in `appsettings*.json`, source control, command history, logs, or health output.

The account, character, and guild collection defaults follow legacy model declarations and Mongoose naming, but the repository contains no deployed collection inventory. Confirm all three before enabling the database. `InfiniteSky:Characters:ServerName` mirrors the root `config.server_name` default. `config.json-dist` lists a different `collections` array that source search did not find in use; it is not proof of actual collection names. The original legacy connection string and sample user are intentionally not repeated here.

## Startup and operations

The host validates enabled options before startup. It then creates one long-lived MongoClient, pings the selected database, verifies or creates the account username index, and—when enabled—resets all account `active` values to zero. The world service then reads guild documents before network service startup. Startup errors distinguish configuration, authentication/authorization, unavailability, index, BSON, and repository failures using sanitized messages.

The username index is ascending and unique, with the new index name `Username_1`. If existing data prevents unique index creation or an incompatible index already exists, startup stops. Inspect and remediate duplicate or incompatible data manually; this service never drops or changes indexes.

`ResetActiveAccountsOnStartup=true` intentionally matches the original `db.Account.logoutAll()` behavior, which affects every document in the configured collection. It may interfere with another running server process. Set it to false only after deciding how stale active flags will be managed.

Driver automatic retry reads/writes are disabled. The implementation uses bounded operations and does not retry application writes. Periodic pings are read-only and update health state.

## Health and logs

The health check is registered as `mongodb-persistence` with readiness/database tags. A disabled database is `Degraded`, an unchecked or failed database is `Unhealthy`, and only a successful bounded probe reports `Healthy`. No HTTP health endpoint is currently exposed by the server; a host/monitor can query the registered `HealthCheckService`.

Structured logs contain operation and failure category only. Metrics use low-cardinality operation/failure tags and do not include usernames, identifiers, connection strings, credentials, or account documents.

## Tests

Unit and server tests do not require a live MongoDB instance. The isolated Testcontainers integration test uses an ephemeral MongoDB `7.0` container and runs only when `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true` is set and Docker is available. No integration test connects to the configured production database.

The latest solution test run skipped the container test because `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true` was not set. No production MongoDB version minimum or live compatibility result is claimed.
