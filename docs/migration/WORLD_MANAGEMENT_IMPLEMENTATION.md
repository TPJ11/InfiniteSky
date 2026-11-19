# World Management Implementation

## Scope and compatibility findings

Prompt 13 implements a process-local lifecycle for the single root world runtime. It does not add a world identifier, world selection, multiple instances, dynamic creation, clustering, or a world registry. The original root implementation constructs one global `world` object in [`world.js`](../../world.js#L15-L46); `world.clients`, `world.clientID`, and `world.socket_transfers` are runtime collections, not a registry of world instances.

The root world starts its TCP listener, calls `loadAllZones()` and starts the asynchronous `prepareGuilds()` query, emits `world_started`, and subscribes the world tick callbacks ([`world.js`](../../world.js#L15-L40)). Zone readiness is signaled separately when the configured load queue drains ([`world.js`](../../world.js#L783-L806)). The guild query is not awaited by that startup path; on query error it logs and returns, otherwise it populates a name-keyed `world.guilds` object ([`world.js`](../../world.js#L937-L962)).

The root implementation has a hard-coded 100 ms interval in [`GameStep.js`](../../GameStep.js#L5-L30). `world.GameStep` is currently a no-op, and `world.step` forwards elapsed time to each loaded zone ([`world.js`](../../world.js#L53-L56), [`world.js`](../../world.js#L743-L755)). Zone loading itself remains outside this stage. The separate FruitSalad process model is not used as the root world's behavioral definition.

## Implemented components

| Responsibility | Implementation | Evidence / boundary |
|---|---|---|
| World process state | `WorldRuntime` and immutable `WorldRuntimeSnapshot` | Lock-protected single runtime; no shared player or zone dictionaries |
| Lifecycle | `WorldHostedService` | Generic Host service initializes before networking and shuts down before persistence |
| Configuration | `WorldConfiguration` from `InfiniteSky:World:TickMilliseconds` | Defaults to the observed 100 ms cadence; validates 1–10,000 ms |
| Guild bootstrap | `IGuildRepository`, `MongoGuildRepository`, `MongoGuildDocumentMapper` | Reads legacy guild documents when MongoDB is enabled; names are looked up ordinally |
| Zone boundary | `IWorldZoneReadiness` | No provider is registered yet, so the runtime accurately reports zone runtime unavailable |
| Tick boundary | `WorldClock`, `IWorldTickHandler` | Uses `PeriodicTimer` and measured elapsed time; does not implement zone ticks or simulation |
| Health and metrics | `WorldHealthCheck`, `WorldMetrics` | Reports lifecycle/readiness, guild/zone counts, ticks, and initialization/failure measurements |

No world registration/lookup service is added: the observed root implementation owns one singleton world and has no world-ID lookup contract. Guild lookup is a data lookup (`FindGuildByName`), not world discovery.

## Initialization and dependency order

The Generic Host starts persistence before `WorldHostedService`, and world before `NetworkHostedService`, using the registrations in [`ServerHostBuilder.cs`](../../src/InfiniteSky.Server/Hosting/ServerHostBuilder.cs). This guarantees that the persistence host's initial checks have run before the world bootstrap and that listeners do not start before world initialization completes.

1. Transition the runtime from `Created` to `Initializing`.
2. If MongoDB persistence is enabled and its hosted-service startup checks have succeeded, asynchronously read the configured guild collection. A guild-read failure is logged and represented as a readiness blocker, matching the original behavior of logging a failed guild query without aborting world startup. A MongoDB connection/startup failure still fails the Generic Host before the world service starts.
3. Validate and snapshot zone-readiness-provider results. With no registered provider, no zones are fabricated and zone readiness remains unavailable.
4. Publish the immutable configuration/guild/readiness snapshot and transition through `Initialized`, `Starting`, and `Running`.
5. Start the hosted background operation. The world clock remains idle until `WorldRuntimeSnapshot.IsReady` is true; no zone provider currently makes it ready.
6. During host shutdown, stop the world operation before MongoDB persistence is stopped.

Initialization errors fail the host startup and mark the runtime failed. Disabled persistence and a guild-read failure after persistence startup are degraded conditions, not false success. Failure of MongoDB's own hosted-service startup checks prevents the world service from starting. Cancellation is propagated; the host controls the overall shutdown timeout.

## Persistence mapping

The original schema is declared in [`db/guild.js`](../../db/guild.js): model name `guilds`, `Name`, `Level` defaulting to `0`, `hasGuildEmblem`, and mixed `Members`. The .NET repository reads only that world-bootstrap data and maps the known member name/role fields into existing domain types. It preserves null member slots and rejects malformed types/roles. It performs no writes or schema changes.

The C# default collection name `guilds` is inferred from the Mongoose model name, not confirmed against a deployed database. It is configurable as `InfiniteSky:Database:GuildCollectionName`. Live BSON compatibility is not verified; mapper tests use synthetic documents.

## Concurrency and ownership

`WorldRuntime` serializes state transitions and snapshot replacement with a private lock. Guild data is copied into an ordinal-keyed dictionary before it becomes visible. Tick callbacks execute sequentially and are awaited; the clock uses cancellation-aware `PeriodicTimer` waits. No database or network operation is performed while holding the runtime lock.

This stage has no world registry, world-level mutable player collection, zone actor, or gameplay state. Zone readiness is a read-only boundary, not a placeholder zone implementation.

## Verification

- `InfiniteSky.World.Tests`: lifecycle and clock tests cover valid transitions, failure paths, readiness blockers, ordered ticks, and cancellation.
- `InfiniteSky.Persistence.Tests`: synthetic BSON mapping tests cover defaults, null member slots, roles, and malformed fields. The opt-in MongoDB Testcontainers test was skipped because `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true` was not set.
- `InfiniteSky.Server.IntegrationTests`: host startup/shutdown, DI composition, configuration validation, health/readiness, and unavailable MongoDB behavior.
- `dotnet restore InfiniteSky.sln` and `dotnet build InfiniteSky.sln --no-restore` succeeded. The full solution run passed 125 tests with 1 opt-in MongoDB test skipped; `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` passed. `InfiniteSky.GameData.Tests` contains no discoverable tests.

This is unit and host-integration evidence, not a full client/world integration test. The complete verification evidence and current status are listed in [`MIGRATION_STATUS.md`](../../MIGRATION_STATUS.md).

## Known gaps

- No world ID or metadata is established by the root Node startup code.
- The root's `world_started` event occurs before zone loading and guild loading complete; the separate `ready` event follows the zone queue. The C# host currently exposes a single degraded world snapshot because the zone subsystem is not implemented.
- No production zone readiness provider or tick handler exists. No world tick reaches zones, and no gameplay simulation is implemented.
- The root world TCP protocol handler, session transfer, character entry, world clients, and world state packets remain separate migration work.
- The original guild query is not awaited; C# awaits the query when persistence is enabled, but degrades on failure to preserve continued startup.
- The physical legacy guild collection and representative production BSON remain unverified.

Prompt 14 zone management and spatial systems have not been started.
