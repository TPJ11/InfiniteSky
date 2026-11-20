# .NET Runtime Configuration

This page describes the existing .NET host configuration, not the separate legacy Node.js `config.json`. It does not imply that the current .NET host is a playable or production-ready game server.

## Prerequisites and entry point

- .NET SDK `10.0.401` or a later compatible 10.0 feature band, as pinned by [`global.json`](../../global.json).
- Executable project: [`InfiniteSky.Server.csproj`](../../src/InfiniteSky.Server/InfiniteSky.Server.csproj).
- Entry point: [`Program.cs`](../../src/InfiniteSky.Server/Program.cs).
- Optional external database: MongoDB, when `InfiniteSky:Database:Enabled` is true. MongoDB must be reachable and its configured account collection/index must match the deployment.
- The host loads configured zone resources relative to its content root's `data` directory. Missing original `.IMG` and `.WREGION` files prevent full game-data/world readiness.

Build, test, format, and run from the repository root:

```powershell
dotnet restore InfiniteSky.sln
dotnet build InfiniteSky.sln --no-restore
dotnet test InfiniteSky.sln --no-restore
dotnet format InfiniteSky.sln --verify-no-changes --no-restore
dotnet run --project src\InfiniteSky.Server\InfiniteSky.Server.csproj
```

## Configuration sources

Configuration binds to the `InfiniteSky` section from `appsettings.json` plus environment-specific JSON, command-line values, and environment variables prefixed with `INFINITESKY_`. Options validate on host start. For example, `INFINITESKY_InfiniteSky__Networking__Enabled` overrides `InfiniteSky:Networking:Enabled`.

The checked-in defaults are in [`appsettings.json`](../../src/InfiniteSky.Server/appsettings.json):

| Setting | Default | Behavior |
|---|---|---|
| `InfiniteSky:Networking:Enabled` | `false` | Both TCP listeners are disabled unless explicitly enabled. |
| `InfiniteSky:Networking:BindAddress` | `127.0.0.1` | Address used by login/world listeners. |
| `InfiniteSky:Networking:LoginPort` | `11091` | Raw TCP login endpoint. |
| `InfiniteSky:Networking:WorldPort` | `11092` | Raw TCP world endpoint; no world gameplay handlers are currently wired. |
| `InfiniteSky:Database:Enabled` | `false` | Mongo persistence startup and account repository are disabled by default. |
| `InfiniteSky:Database:DatabaseName` | `ts1` | Mongo database name when enabled. |
| `InfiniteSky:Database:AccountCollectionName` | `account_mongooses` | Must be verified against target deployment. |
| `InfiniteSky:Database:CharacterCollectionName` | `character_mongooses` | Inferred legacy name; verify before use. |
| `InfiniteSky:Database:GuildCollectionName` | `guilds` | Guild collection when database bootstrap is enabled. |
| `InfiniteSky:Characters:ServerName` | `Development` | Server/shard filter for character repository operations. |
| `InfiniteSky:Zones:Load` | empty | No zones load by default; empty configuration keeps world readiness degraded. |
| `InfiniteSky:World:TickMilliseconds` | `100` | Clock cadence; there are no integrated gameplay tick consumers. |
| `InfiniteSky:Application:ShutdownTimeoutSeconds` | `30` | Generic Host shutdown deadline. |

## Loopback login development

The command below enables raw TCP listeners on loopback. It is suitable for the existing login slice only when a reachable MongoDB account repository is configured. With the default database-disabled setting, authentication cannot load real accounts. The world listener accepts TCP but does not implement the client world workflow.

```powershell
$env:INFINITESKY_InfiniteSky__Networking__Enabled = "true"
$env:INFINITESKY_InfiniteSky__Networking__BindAddress = "127.0.0.1"
$env:INFINITESKY_InfiniteSky__Networking__LoginPort = "11091"
$env:INFINITESKY_InfiniteSky__Networking__WorldPort = "11092"
dotnet run --project src\InfiniteSky.Server\InfiniteSky.Server.csproj
```

## MongoDB development configuration

Use only a disposable local database or an isolated authorized test database. The URI below has no credentials; do not commit authenticated connection strings or production records.

```powershell
$env:INFINITESKY_InfiniteSky__Database__Enabled = "true"
$env:INFINITESKY_InfiniteSky__Database__ConnectionString = "mongodb://127.0.0.1:27017/ts1"
$env:INFINITESKY_InfiniteSky__Database__DatabaseName = "ts1"
$env:INFINITESKY_InfiniteSky__Database__AccountCollectionName = "account_mongooses"
$env:INFINITESKY_InfiniteSky__Database__CharacterCollectionName = "character_mongooses"
```

With Mongo enabled the host pings at startup, verifies the account username index, and by default resets all account `active` fields. `InfiniteSky:Database:ResetActiveAccountsOnStartup` defaults to `true` for the legacy behavior; review carefully before running more than one server instance.

## Zone configuration

To attempt to load a known catalog zone, configure a positive ID:

```powershell
$env:INFINITESKY_InfiniteSky__Zones__Load__0 = "1"
dotnet run --project src\InfiniteSky.Server\InfiniteSky.Server.csproj
```

Only legacy catalog IDs are accepted. NPC and region resources are resolved under `data`; absent optional files are reflected in degraded readiness. Loading a zone does not spawn mobs, create players, or enable client world entry. The repository has NPC/MOB spawn files and `.pac` captures, but lacks the required `.IMG` catalogs and `.WREGION` corpus for complete game behavior.

## Operational limitations

- The current deployment/runtime smoke path has been documented but not run as a separate production-style process during Prompt 20.
- CI configuration is not evidence that a specific deployment topology or MongoDB instance was tested.
- Networking, database, and zone defaults intentionally do not fabricate a healthy playable server.
- Scripts, plugins, admin commands, gameplay tick handlers, character/world packet dispatch, and original-client compatibility remain unimplemented or blocked.
- See [`INTEGRATION_READINESS.md`](./INTEGRATION_READINESS.md) and [`END_TO_END_TEST_SCENARIOS.md`](./END_TO_END_TEST_SCENARIOS.md) for tested scope and blockers.
