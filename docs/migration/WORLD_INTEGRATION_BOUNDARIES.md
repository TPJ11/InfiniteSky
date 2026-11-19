# World Integration Boundaries

## Application lifecycle and diagnostics

`WorldHostedService` owns world bootstrap and the background clock. It is registered after Mongo persistence and before network listeners in [`ServerHostBuilder.cs`](../../src/InfiniteSky.Server/Hosting/ServerHostBuilder.cs). Application readiness records the world's actual readiness state; [`WorldHealthCheck`](../../src/InfiniteSky.Server/Health/WorldHealthCheck.cs) reports lifecycle state, blockers, guild/zone counts, and tick count. [`WorldMetrics`](../../src/InfiniteSky.World/WorldMetrics.cs) exposes world readiness, loaded-zone/guild gauges, tick and initialization measurements, and lifecycle failures.

Health does not treat the mere existence of a world service as healthy. Before initialization and while dependencies are unavailable, readiness is degraded; failed, stopping, and stopped states are unhealthy.

## Persistence boundary

`IGuildRepository` is the world-facing application port. `MongoGuildRepository` loads all guild documents asynchronously with cancellation and existing persistence timeouts/metrics. `MongoGuildDocumentMapper` maps only the fields supported by the legacy schema and domain model. No character, inventory, quest, or gameplay persistence is added.

The root source invokes `db.Guild.find({})`, stores returned guilds by `Name`, and logs/returns on failure ([`world.js`](../../world.js#L937-L962)). The C# bootstrap does not bind legacy JavaScript methods to guild objects; gameplay-facing guild operations remain future work.

## Networking and authentication

The world runtime does not own TCP sockets, packet parsing, session transfer, or authenticated account state. The host starts the world service before the network listener so readiness can be inspected before accepting traffic, but network packet handlers do not query or enter the world yet. Authentication success is not a player entity or world entry.

No opcodes, packet formats, or client-facing behavior are changed by this stage. World greeting/authentication packets and character/world entry remain as tracked protocol/application work.

## Zone boundary

`ZoneManager` implements `IWorldZoneReadiness` and reports the loaded configured zone count. It is hosted before `WorldHostedService`, and its health check distinguishes empty configuration, startup failure, and a complete zone set. Consequently:

- no fake zone is created;
- only configured catalog IDs are considered;
- missing legacy NPC/WREGION files remain optional, while malformed files fail startup;
- an empty zone list keeps world readiness degraded with `ZoneRuntimeUnavailable`;
- the world clock waits while the world is degraded and does not run zone simulation;
- spatial candidate indexing exists, but entity, movement, transition, and gameplay behavior does not.

Root `world.js` enumerates numeric `config.Zones` entries and loads configured zones through a bounded async queue ([`world.js`](../../world.js#L783-L806)). Prompt 14 implements the corresponding static manager and host boundary; see [ZONE_MANAGEMENT_IMPLEMENTATION.md](ZONE_MANAGEMENT_IMPLEMENTATION.md).

## Events and timers

The root world subscribes to global `step` events, runs a no-op `world.GameStep`, and forwards deltas to each loaded zone ([`world.js`](../../world.js#L33-L39), [`world.js`](../../world.js#L53-L56), [`world.js`](../../world.js#L743-L755)). The source cadence is 100 ms ([`GameStep.js`](../../GameStep.js#L5-L30)). C# provides a sequential, cancellation-aware world tick handler boundary, but does not implement zone ticks or a general game loop. No additional event bus or fire-and-forget timer is introduced.

## Future integration limits

- The singleton root world does not justify a multi-world registry or distributed coordination.
- There is no source-backed world ID to expose to clients or configuration.
- World availability/capacity semantics are not implemented.
- Authenticated sessions are not inserted into a player/entity collection.
- World shutdown is ordered before zone shutdown, but no entity/player drain or save coordination exists yet.
- Persistence-backed guild data and synthetic mapping tests do not prove compatibility with production BSON.
