# MongoDB Schema Mapping

## Evidence and limits

The root Node runtime uses Mongoose with an unpinned minimum `>=3.8.8` ([`package.json:29`](../../package.json)); no package lockfile, production server version, collection inventory, index dump, or sanitized database export is present. `config.json-dist` identifies database `ts1` but also lists `account`, `character`, and `counters` under a `collections` property ([`config.json-dist:29-35`](../../config.json-dist)). A repository search found no use of that property. It is therefore not treated as authoritative physical collection naming.

The root model declarations use `account_mongoose`, `character_mongoose`, `counters`, `guilds`, and `emailqueue` ([`db/account.js:26-27`](../../db/account.js), [`db/character.js:391-392`](../../db/character.js), [`db/counter.js:16-17`](../../db/counter.js), [`db/guild.js:13-14`](../../db/guild.js), [`db/email_queue.js:14-15`](../../db/email_queue.js)). Mongoose normally pluralizes model names to derive physical collections, which implies `account_mongooses`, `character_mongooses`, `counters`, `guilds`, and `emailqueues`; the deployed names have **not** been verified. `AccountCollectionName` is configurable, and operators must inspect the target database before enabling writes.

## Supported account document

`db/account.js:11-25` defines the root account schema:

| Field | Legacy declaration / meaning | C# handling |
|---|---|---|
| `_id` | `Number`, unique and indexed | Accepts BSON `Int32`, `Int64`, or integral finite `Double`; surfaced as the domain `AccountId`. |
| `Username` | `String`, unique and indexed | Exact case-sensitive equality lookup; required to be a BSON string. |
| `Password` | `String`, plaintext | Projected only for authentication; missing/null means no valid credential. Returned as temporary UTF-8 bytes and cleared by the application. No hashing or conversion is performed. |
| `active` | `Number`, default `0` | Updated only with `$set`; C# writes BSON `Int32(0|1)`. |
| `Level` | `Number`, default `0` | Not loaded by the current authentication repository. |
| `Email` | `String` | Not loaded or modified. |
| `LastIP` | `String` | Not loaded or modified. |
| `UsePin` | `Boolean`, default `false` | Not loaded or modified. |
| `Pin` | `String`, default `"0000"` | Not loaded or modified. |
| `LastLogin` | `Date`, default `Date.now` | No C# write or conversion; raw BSON remains untouched. |
| `CharacterIndividuals` | object-like field, default `null` | Not loaded or modified; missing and explicit null remain distinct in raw storage. |
| `AP` | `Number`, default `0` | Not loaded or modified. |

The Node schema uses JavaScript/Mongoose types; the original server's serialized numeric BSON subtype was not captured. The C# fixture deliberately demonstrates an integral `Int32` account `_id` and active value, plus a BSON date and explicit null. It is synthetic and must not be presented as a production sample. Existing account documents are read as raw `BsonDocument`; no naming convention, default injection, class map, or whole-document save is applied.

The repository projects `_id`, `Username`, and `Password`, unlike Mongoose's full-document `findOne`. On login success, the C# update changes only `active`, unlike the legacy hydrated-document `.save()`, reducing the risk of losing unknown fields. The startup reset uses an empty filter and `$set active: 0`, corresponding to `logoutAll` ([`db/account.js:135-151`](../../db/account.js)); it is configurable because it affects the entire account collection.

## Original root database operations

| Operation | Source evidence | Migration state |
|---|---|---|
| Connect, connection events, shutdown close | [`db.js:9-45`](../../db.js); invoked by [`main.js:142`](../../main.js) | Implemented by the C# singleton context and hosted lifecycle. |
| Login account lookup and active update | [`packets/login/handleLogin.js:279-329`](../../packets/login/handleLogin.js) | Implemented through the account repository, with the noted session-policy differences. |
| Account zone-transfer logout and startup reset | [`db/account.js:30-48`](../../db/account.js), [`db/account.js:135-151`](../../db/account.js) | Startup reset implemented; existing authentication rollback paths can set one account inactive. |
| Character slot listing and reads | [`packets/login/handleLogin.js:195-218`](../../packets/login/handleLogin.js) | Not implemented. |
| Character lookup/create/delete | [`packets/login/handleManageCharacter.js:107-121`](../../packets/login/handleManageCharacter.js), [`packets/login/handleManageCharacter.js:220-265`](../../packets/login/handleManageCharacter.js) | Not implemented. |
| Atomic ID counter | [`db/counter.js:5-20`](../../db/counter.js) | Not implemented. Schema `_id` is a string; `seq` is a number; `findByIdAndUpdate` uses `$inc`, `upsert`, `new`, and a `seq` projection. Error behavior calls back with `null`. |
| Character save and nested state | [`db/character.js:50-110`](../../db/character.js), repeated `.save()` calls in [`world.js`](../../world.js) and packet handlers | Not implemented. Schema includes numeric IDs/stats, nullable `GuildName`, equipment subdocuments, and `Mixed` inventory/skill/storage arrays. Duplicate JavaScript object keys mean later keys overwrite earlier declarations. |
| Guild load and writes | [`db/guild.js:6-14`](../../db/guild.js), [`world.js:939-949`](../../world.js), guild packet handlers | Read-only world bootstrap implemented through `MongoGuildRepository`; guild writes and packet workflows remain unimplemented. Fields include case-sensitive `Name`, numeric `Level`, boolean emblem flag, and `Members: [Mixed]`. |
| Email queue schema | [`db/email_queue.js:6-15`](../../db/email_queue.js) | Not implemented; no active query/insert consumer was found. Fields include indexed `Subject`/`To`, `Body`, `Added` date default, and optional `Sent` date. |

Declared root indexes are Mongoose schema flags, not a deploy-time index inventory: account `_id` and `Username` are unique/indexed; character `_id` and `Name` are unique/indexed with indexed `GuildName`; guild `Name` is unique/indexed; email `Subject` and `To` are indexed. Runtime index names and any drift require inspection of the deployed database.

## Separate FruitSalad runtime

FruitSalad is not the root Node server and has a distinct connection module. Its package uses Mongoose `^4.0.6` plus `mongoose-cache` with a 50-entry, one-hour cache ([`FruitSalad/package.json:23-24`](../../FruitSalad/package.json), [`FruitSalad/Modules/db.js:5-30`](../../FruitSalad/Modules/db.js)).

Its account model is named `accounts` and adds `Logged` and `Status` instead of root `active` ([`FruitSalad/Database/account.js:13-34`](../../FruitSalad/Database/account.js)). Its character model is named `characters`, and its guild uses lowercase `name` and model name `guild` ([`FruitSalad/Database/character.js:51-64`](../../FruitSalad/Database/character.js), [`FruitSalad/Database/guild.js:3-13`](../../FruitSalad/Database/guild.js)). It also defines item, monster, NPC, quest, skill, experience, spawn-group, spawn-log, and spawn-point models under `FruitSalad/Database/`. These are alternate-runtime/game-data structures and are not mapped by this account persistence stage. Do not merge their field names or documents into root account models.

## BSON and query compatibility decisions

- String field names and casing remain exactly as declared (`Username`, `Password`, `LastIP`, `CharacterIndividuals`).
- Query collation is explicitly `simple`, preserving the source's default case-sensitive equality rather than inheriting a differently collated deployment collection.
- Numeric account IDs are tolerated across common BSON integer and integral-double encodings. String IDs, fractional doubles, negative IDs, and nonnumeric types fail explicitly.
- Password type must be BSON string when present; `null` and missing are represented as no credential. Unexpected types fail closed rather than being coerced.
- The account repository does not hydrate Mongoose defaults into missing fields. A synthetic fixture checks BSON `Int32`, date, null, and an unknown nested field.
- The world guild bootstrap maps only the legacy `Name`, `Level`, `hasGuildEmblem`, and member `Name`/`LeaderFlag` data needed by its current domain snapshot. Missing `Level` and emblem use schema defaults; null member slots are preserved; malformed member structures fail explicitly. Its `guilds` collection name is configurable but inferred, not verified against a deployment.
- Updates use `$set` on one field only. There are no account replacements, schema migrations, collection drops, bulk conversions, or transactions.
- `active` writes use `Int32`; exact legacy serialized numeric width remains unverified without a representative dump and the deployed Node BSON version.
- The connection string must identify the configured database if it includes a path. Configure `authSource` explicitly in the URI when the deployment's authentication database differs from MongoDB's default.

## Compatibility risks still open

1. Verify the physical account and guild collection names and existing index definitions before enabling persistence.
2. Compare account `_id` and `active` BSON numeric subtypes against a sanitized export.
3. Confirm deployed MongoDB server version, authentication mechanism, and least-privilege permissions for `ping`, `listIndexes`, `createIndexes`, account lookup, and update.
4. Decide whether global startup `active` reset is appropriate for a multi-instance deployment.
5. Test against representative missing/null/extra account fields and malformed legacy documents.
6. Preserve root and FruitSalad as separate data contracts until an explicit migration plan exists.
