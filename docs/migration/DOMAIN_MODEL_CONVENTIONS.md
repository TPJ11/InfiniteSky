# Domain Model Conventions

## Project boundary

Core models live only in `InfiniteSky.Domain`. They must not reference:

- sockets, HTTP, Pipelines, or transport;
- MongoDB/BSON/ORM annotations;
- packet/restruct/binary layouts;
- Generic Host/DI;
- configuration or logging;
- world-zone/game-loop services.

Definitions and runtime state may be consumed by those later projects, but the dependency direction remains inward.

## Naming

- Namespaces group source concepts: `Accounts`, `Characters`, `Items`, `Skills`, `Guilds`, `Entities`, `Definitions`, `Zones`, `Values`, `Identifiers`.
- C# names are descriptive and correct spellings while mapping documentation records legacy spellings such as `CalbashBottle`, `ShawdowResistance`, `Rareness`, and `LocationNew`.
- No abbreviations are used when a source concept has a clear name.

## Identifiers

Every externally meaningful ID is a distinct readonly record struct. `long`, `int`, and `uint` are selected from source counter/signedness evidence.

Rules:

- account and character IDs are never interchangeable;
- definition IDs are never interchangeable;
- runtime `EntityId` is separate from persistent character IDs;
- nullable ID represents absent state instead of a `-1` or all-ones sentinel;
- IDs do not gain database or packet behavior.

## Value objects

Use records or readonly record structs where:

- values have structural equality;
- source objects contain no mutable identity;
- no lifecycle or subsystem owner exists.

`Position3`, `GridPosition`, `Rotation2`, `DirectionDegrees`, spawn/region records, equipment items, definitions, and progression snapshots follow this rule.

## Entity identity

Use classes and reference identity for Account, Character, Guild, runtime monster/NPC/item entities, and mutable equipment/collections. Equal IDs or data do not imply the same live object.

## Nullability and absent values

- Explicit schema default `null` maps to a nullable C# reference.
- Optional schema fields map to nullable value/reference types.
- Array undefined/null holes map to nullable list elements because original code treats them as empty in relevant branches.
- A compound value is nullable as a whole when all source components are needed; `SavedCharacterLocation.Position` is null if any X/Y/Z component is absent.
- Runtime `-1` targets become nullable IDs rather than being stored as impossible negative entity IDs.

Do not use `dynamic`, `object`, or a general property bag to hide missing type evidence.

## Validation

Domain constructors validate only invariants demonstrated by source:

- persistent/runtime counters used as IDs are non-negative;
- runtime/wire node IDs use `uint`;
- entity-required names/definitions are not null.

The domain does not add:

- protocol fixed-string length;
- inventory collision/stack rules;
- character level/stat caps;
- clan/weapon restrictions;
- packet rate limits;
- Mongo collection/index rules.

Those remain gameplay/persistence/protocol responsibilities.

## Numeric conventions

- JavaScript `Number` runtime coordinates and derived statistics use `double`.
- Source `uint32` information/record values use `uint`.
- Source `int32` region/combat/stat fields use `int` when the record type is named.
- Mongoose currency/count fields use `long` where handlers assume integral values but schema does not impose a 32-bit range.
- Skill modifier runtime values use `double` because `Info_Skill.js` divides raw 32-bit values by 100.

Never cast from a broad numeric type by truncation at the domain boundary without an explicit compatibility decision and test.

## Constants and enums

Enums preserve source numeric values:

- Clan 0-2;
- Gender 0-1;
- ItemType 1-24;
- ItemRarity 0-4;
- EquipmentSlot 0-8;
- SkillCategory 1-4;
- GuildMemberRole 0-2.

Unknown numeric records remain representable until a parser validates them. No renumbering or string-enum substitution is permitted.

No reusable bit-flag enumeration was evidenced in the migrated structures. Numeric values such as faction/visibility state remain ordinary fields until a later source trace identifies bit semantics.

## Collections

- Preserve original array order.
- Preserve null/sparse entries.
- Use `IList<T?>` for mutable aggregate slots.
- Do not silently compact arrays.
- Do not enforce protocol lengths in the domain constructor; expose exact source counts as `LegacyCollectionSizes`.

## Definitions versus runtime state

`ItemDefinition`, `SkillDefinition`, `MonsterDefinition`, `NpcDefinition`, and `ExperienceDefinition` are static source data. `MonsterEntity`, `NpcEntity`, `WorldItemEntity`, and `CharacterRuntimeState` are mutable runtime projections. Do not store packet timers, network handles, or AI methods in definitions.

## Future mappings

Persistence and protocol adapters must:

- own BSON attributes/documents and packet attributes/offsets;
- convert sentinel values and fixed numeric widths explicitly;
- validate bounds at the external boundary;
- retain unknown bytes/fields separately;
- never mutate core definitions to make serialization convenient.
