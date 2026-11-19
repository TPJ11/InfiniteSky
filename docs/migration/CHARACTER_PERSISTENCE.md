# Character Persistence

## Legacy schema and mapping

The root Mongoose model in `db/character.js` is named `character_mongoose`; the C# default `character_mongooses` is inferred using Mongoose collection pluralization and must be verified against a deployment. It is configurable as `InfiniteSky:Database:CharacterCollectionName`.

`MongoCharacterDocumentMapper` maps the currently modeled source fields without changing the source `BsonDocument`:

| BSON fields | Domain target |
|---|---|
| `_id`, `AccountID`, `ServerName`, `Name` | Persistent `CharacterId`, `AccountId`, server/name identity |
| `Clan`, `Gender`, `Hair`, `Face`, `GuildName`, `Title`, `IsGM` | Appearance and identity metadata |
| `Level`, `Experience`, `OtherIngame`, `StatBonus`, `Honor`, `SkillPoints`, `StatPoints`, `PlayTime`, `CP` | Progression |
| `StatStrength`, `StatChi`, `StatDexterity`, `StatVitality` | Base attributes |
| `StorageUse`, `Silver`, `StorageSilver`, `BankSilver`, `SilverBig` | Economy |
| `Health`, `Chi` | Optional persisted vitals |
| `Ring`, `Cape`, `Armor`, `Glove`, `Amulet`, `Boot`, `CalbashBottle`, `Weapon`, `Pet` | Equipment |
| `Inventory`, `Storage`, `QuickUseItems`, `SkillList`, `Skills`, `SkillBar`, `Friends` | Character arrays |
| `MapID`, `RealX`, `RealY`, `RealZ` | Optional saved zone and position |
| `StrBonus`, `DexBonus`, buffs, defenses, damage, daily fields | Supported character bonuses |
| `Deaths`, `LastUpdated` | Persistent metadata |

The mapper recognizes the schema's `Ammount` spelling for quick-use items and the `CalbashBottle` spelling for the bottle equipment slot. Inventory/skill arrays retain order and null elements where their domain collection supports them. Numeric IDs accept BSON Int32, Int64, or integral Double and reject fractional, negative, or out-of-range values. Optional map/coordinate/vital fields remain optional.

`MonstersKilled` and any other unmodeled/Mixed legacy data are not interpreted. The repository does not serialize the mapped aggregate back over the source document, so unmodeled fields are preserved.

## Repository operations

`ICharacterRepository` exposes:

1. `GetForAccountAsync`: filter by `AccountID` and `ServerName`, return at most three documents in Mongo's natural result order to match the root login slot limit; no sort is added.
2. `GetOwnedAsync`: filter by character `_id`, account owner, and server name. Missing or foreign-owned characters both return `null`.
3. `SaveWorldLocationAsync`: filter by the same identity/ownership tuple and `$set` only `MapID`, `RealX`, `RealY`, and `RealZ`.

The world-location update truncates each coordinate toward zero, matching the source disconnect path's `parseInt` behavior, and writes BSON Double values corresponding to the legacy JavaScript Number model. It does not replace the full character document and does not change `Health`, `Chi`, inventory, or other gameplay-owned values.

Persistence failures are categorized and logged without character data or connection strings. A missing update target raises `CharacterNotFound`; cancellation is propagated.

## Save policy and compatibility limits

The observed root implementation saves character state during disconnect; the current stage saves only the current world location on explicit disconnect and graceful server shutdown. No periodic saves, transfer saves, or runtime HP/Chi updates are introduced. A save failure after disconnect leaves the player removed from runtime indexes and is surfaced; retry coordination or durable queuing is not source-defined and remains future work.

The checked-in fixture is synthetic. No production BSON export or live MongoDB result is available in this environment. Integration coverage exists behind the repository's existing opt-in MongoDB test gate; skipped tests are not claimed as live compatibility evidence.

## Verification

- `MongoCharacterDocumentMapperTests` verifies supported fields, legacy spellings, sparse/null array slots, optional values, malformed numeric rejection, and no mapper mutation of raw BSON.
- `MongoCharacterRepositoryIntegrationTests` seeds an isolated MongoDB container, checks account/server ownership, confirms coordinate truncation, and verifies unmodeled fields and vitals remain unchanged. It runs only with `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true` and Docker available.

## References

- [`character.js`](../../db/character.js) — source schema, Mongoose model name, field names, Mixed arrays, and defaults.
- [`counter.js`](../../db/counter.js) — character counter ID mechanism.
- [`world.js`](../../world.js) — observed disconnect save fields and coordinate conversion.
- [`MongoCharacterDocumentMapper.cs`](../../src/InfiniteSky.Persistence/MongoCharacterDocumentMapper.cs) — field-only legacy read mapping.
- [`MongoCharacterRepository.cs`](../../src/InfiniteSky.Persistence/MongoCharacterRepository.cs) — owned reads and targeted updates.
- [`MongoPersistenceContext.cs`](../../src/InfiniteSky.Persistence/MongoPersistenceContext.cs) — configured raw BSON collection access.
