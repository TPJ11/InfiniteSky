# Script Event System

## Root events

The root application uses Node's standard `EventEmitter` as `main.events`. It does not define a separate script event bus with typed payloads, priorities or cancellation. The active event flow found in source is:

| Event | Publisher | Listener / effect | Status |
|---|---|---|---|
| `config_loaded` | `util.loadConfig` | `main.js` listener notes runtime config values can change but listeners/ports are not rebuilt. | Active application event |
| `Shutdown` | `main.shutdown` | Announces shutdown before registered shutdown methods run. | Active application event |
| `db_connected` | Mongoose `connected` callback in `db.js` | Indicates database connection; the same callback starts the `db` VMScript loader. | Active application event |
| `db_accounts_schema_loaded` | `db/account.js` | `main.js` registers `clearLoggedInAccounts`, whose body only logs that it is deprecated. | Active, minimal listener |
| `db_character_schema_loaded` | `db/character.js` | A former login listener is commented out. | Active emit; no active scripting listener found |
| `world_started` | root `world.js` after listener, zone and guild bootstrap initiation | `main.js` starts the GM command script loader. World registers step listeners. | Active application event |
| `ready` | root `world.js` after configured zone load queue drains | `main.js` starts the 100 ms `GameStep`. | Active application event |
| `step` | root `main.js` callback from `GameStep` | World dispatches `GameStep(delta)` and its step method. | Active runtime event; not a script scheduler |
| `gameinfo_loaded` | Several calls in `GameInfoLoader.js` are comments | A former login dependency listener is also commented out. | No active emit found |
| `unload_zone` | `world.js` call is commented | No active subscriber found. | Inactive |

The `main.events` listeners use ordinary Node EventEmitter calls: `emit` invokes registered listeners synchronously in registration order, and `once` is the available one-shot form. There is no source-backed priority, cancellation token, async handler contract, or plugin error isolation policy beyond the behavior of individual listeners. An exception thrown by a listener propagates through the `emit` call unless a particular caller catches it.

## Zone hooks

Root `Zone` initializes `zone_script` to `{}` and conditionally invokes `onClientJoin` and `onClientLeave`. The checked-in `zones/1.js` exports a class containing join/leave and other empty hooks, but the root zone loader does not require or attach that file, and no root assignment to `zone_script` was found. Therefore those file methods are not established active root gameplay.

`packets/world/nangi_track.js` references a zone-specific status slot. This does not install a script listener or schedule a race. The separate FruitSalad NanginTrack script contains a commented schedule registration and a startup warning that it is not implemented.

## .NET status

No script-compatible event dispatcher was added. Host lifecycle, world lifecycle, and gameplay event semantics remain owned by their current C# projects; this stage does not translate root event names into new public hooks. The event feature remains `NOT_STARTED` until a migrated consumer and its required payload/order are source-verified.

Source references: `main.js`, `util.js`, `db.js`, `db/account.js`, `db/character.js`, `world.js`, `GameStep.js`, `zone.js`, `zones/1.js`, and `FruitSalad/Processes/Zone/Scripts/NanginTrack/NanginTrack.js`.

Event semantics reference: [Node.js Events documentation](https://nodejs.org/api/events.html).
