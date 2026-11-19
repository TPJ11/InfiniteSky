# World Configuration

## Current setting

The original [`GameStep.js`](../../GameStep.js#L5-L30) defines a fixed 100 ms interval. The .NET host makes that observed default configurable, without changing its default:

```json
"World": {
  "TickMilliseconds": 100
}
```

The binding key is `InfiniteSky:World:TickMilliseconds`; environment-variable overrides use the existing `INFINITESKY_` prefix, for example:

```powershell
$env:INFINITESKY_InfiniteSky__World__TickMilliseconds = "100"
```

Startup validation accepts values from 1 through 10,000 milliseconds. The wide range is a host configuration bound, not a claim that the original server supports alternate cadence semantics. Use `100` for observed legacy timing. The clock reports measured elapsed time rather than assuming that each callback ran exactly on schedule.

## Guild persistence setting

When MongoDB is enabled, world initialization reads guild documents using the persistence configuration:

| Key | Default | Meaning |
|---|---|---|
| `InfiniteSky:Database:Enabled` | `false` | Enables Mongo-backed bootstrap and account persistence |
| `InfiniteSky:Database:GuildCollectionName` | `guilds` | Inferred collection name for the legacy Mongoose model |

The legacy [`db/guild.js`](../../db/guild.js) calls `mongoose.model('guilds', schema)` and defines `Name`, `Level`, `hasGuildEmblem`, and `Members`. The default collection name is inferred from that model declaration; confirm it against the deployed database before use. The setting is validated as a non-empty collection name. There is no world-specific database, world ID, or world metadata configuration.

MongoDB connection strings and database credentials must continue to be supplied through protected configuration. Do not put production credentials in `appsettings*.json`.

## Deliberate omissions

No world name/ID, capacity, availability, discovery, dynamic-world, zone list, or world-selection setting is introduced. Root `world.js` does not use a world ID or server registry; its zones come from root `config.Zones`, which belongs to the zone-management stage. The C# world is therefore a singleton with only the tick interval and existing persistence boundary configured.

The guild query failure behavior is non-fatal after the MongoDB persistence host has started: world initialization continues with `GuildDataUnavailable` readiness degradation, matching the original bootstrap's log-and-return behavior. A MongoDB connection/startup failure from the persistence host still fails overall host startup before world initialization. If persistence is disabled, guild data is unavailable rather than an invented empty-but-ready dataset.
