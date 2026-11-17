# Core Domain Model Mapping

## Scope

This mapping covers the pure `InfiniteSky.Domain` models added for the C# migration. It excludes packet codecs, BSON documents, repositories, world actors, AI, combat, inventory mutation, and networking.

The top-level Node.js tree is authoritative. FruitSalad is used only as corroborating evidence and is explicitly called out when it conflicts.

## Identifier mappings

| C# type | Source | Source value | C# representation | Decision |
|---|---|---|---|---|
| `AccountId` | `db/account.js` | Mongoose `_id: Number`, counter sequence | `record struct` over non-negative `long` | Counter/protocol use integer IDs, but the current Mongo schema does not impose 32-bit bounds |
| `CharacterId` | `db/character.js`, `db/counter.js` | Mongoose `_id: Number`, `characterid` counter | non-negative `long` | Same rationale as account ID |
| `EntityId` | `CharacterState`, `Monster.js`, `Npc.js`, `Item.js`, QuadTree | unsigned node/UniqueID and `0xFFFFFFFF` sentinel in packet paths | `uint` | Runtime packet/node identifiers are unsigned; absence uses `null` in C# |
| `ItemDefinitionId` | `Info_Item.js`, `generic/structs.js` | `int32lu ID` | `uint` | Information file and packet structures are unsigned 32-bit |
| `SkillDefinitionId` | `Info_Skill.js`, `structs.js` | `ID int32lu` | `uint` | Information and quick-skill structures are unsigned 32-bit |
| `MonsterDefinitionId` | `Info_Monster.js` | `ID int32lu` | `uint` | Source information record is unsigned |
| `NpcDefinitionId` | `Info_Npc.js` | `ID int32lu` | `uint` | Source information record is unsigned |
| `ZoneId` | config zones, `WREGION`, `zonechange.js` | signed region ZoneID and unsigned packet request; `0` has town-return meaning | non-negative `int` | Preserves `0` and signed region compatibility without merging with definition IDs |

Different identifier types are not assignment-compatible.

## Shared values

| C# type | Source | Mapping and compatibility decision |
|---|---|---|
| `Position3` | `generic/CVec3.js`, `generic/structs.js` | `double X/Y/Z` in the domain because JavaScript state uses Number; binary adapters later round/validate to float32 |
| `DirectionDegrees` | `CharacterState.js`, spawn/action structs | `double Value`; no range normalization because source accepts numeric state and emits degrees/floats |
| `GridPosition` | `StorageItem`, inventory actions | unsigned `Column` and `Row`; no constructor bounds because malformed/sparse persisted data must remain representable |
| `Rotation2` | `Item.StatePacket Rotation[2]` | two `uint` values |

## Account

Sources: `db/account.js`, `packets/world/world_login.js`.

| C# member | Source field | Rule |
|---|---|---|
| `Id` | `_id` | required identifier |
| `Username` | `Username` | required, exact-case string; no source normalization |
| `Email` | `Email` | nullable because no schema default is defined |
| `Active` | `active` | `int`, default 0; preserved instead of collapsing arbitrary numeric values to bool |
| `Level` | `Level` | `int`, default 0 |
| `LastIp` | `LastIP` | nullable |
| `UsesPin` | `UsePin` | bool, default false |
| `Pin` | `Pin` | string, source default `"0000"` |
| `LastLogin` | `LastLogin` | nullable `DateTimeOffset`; `Date.now` is a persistence factory concern |
| `ActionPoints` | `AP` | integral number, default 0 |
| `CharacterIndividuals` | `CharacterIndividuals` | nullable per-server dictionary; original default is null |
| `AccountServerData.Bank` | world login initialization | ordered list; absent/undefined source slots become null |
| `BankSilver` | world login initialization | integral number, default 0 |

`Password` is intentionally excluded from the pure domain model. It is security/persistence data and will receive a separate credential boundary.

## Character aggregate

Sources: `db/character.js`, `generic/CharacterState.js`, `generic/new_character_config.js`.

| C# member/group | Source fields | Mapping |
|---|---|---|
| identity | `_id`, `AccountID`, `Name`, `ServerName` | strongly typed IDs; required name; nullable server |
| social | `GuildName`, `Title`, `IsGM` | null-capable guild/title; integer GM value preserved |
| appearance | `Clan`, `Gender`, `Hair`, `Face` | enum values 0-2/0-1 and integer style IDs |
| progression | `Level`, `Experience`, `Honor`, `SkillPoints`, `StatPoints`, `StatBonus`, `OtherIngame`, `PlayTime`, `CP` | `Level=1` source default; other numeric defaults 0 |
| attributes | `StatStrength`, `StatChi`, `StatDexterity`, `StatVitality` | source default 0 |
| economy | `Silver`, `StorageSilver`, `BankSilver`, `StorageUse`, `SilverBig` | integral values |
| vitals | `Health`, `Chi` | nullable numeric values because schema fields have no defaults |
| saved location | `MapID`, `RealX/Y/Z` | independent nullable fields; `Position3` is exposed only when all coordinates exist |
| equipment | `Ring`, `Cape`, `Armor`, `Glove`, `Amulet`, `Boot`, `CalbashBottle`, `Weapon`, `Pet` | nullable; source schema defaults equipment to null |
| collections | Inventory, QuickUseItems, SkillList, Skills, SkillBar, Storage, Friends | mutable ordered lists; JavaScript undefined/empty slots map to C# null where source handlers treat them as empty |
| bonuses | `StrBonus`, `DexBonus`, `LuckBuff`, `StrengthBuff`, `ExperienceBuff`, auto pill, elemental/faction/damage/daily fields | grouped in `CharacterBonuses` without algorithms |
| metadata | `LastUpdated`, `Deaths` | nullable timestamp and non-negative intended count; parser does not clamp |

Character and Account are classes so two instances representing the same IDs are not automatically value-equal. This preserves runtime object identity.

## Equipment and inventory

Sources: `db/character.js`, `generic/structs.js`, `generic/CharacterState.js`.

| C# type | Original representation |
|---|---|
| `EquipmentItem` | `ID`, `Enchant`, `Combine`, optional bottle `Capacity` |
| `PetEquipment` | `ID`, `Activity`, `Growth` |
| `InventoryItem` | Mixed object with `ID`, optional Column/Row, Amount, Enchant/Combine, Activity/Growth |
| `QuickUseItem` | `ID`, `Amount` |
| `SkillEntry` | `ID`, `Level` |
| `EquipmentSlot` | item-action slot order 0-8 |

The model deliberately permits null/missing optional fields because normal items, pets, bottles, and legacy Mixed records use different shapes.

## Static definitions

### Item definition

Source: `generic/Info_Item.js`.

Fields preserved include ID, Name, rarity, item type, display ID, level, clan, prices, capacity, requirements, base attributes, damage/defense, elemental damage/resistance, hit/dodge/deadly, three skill bonuses, use value type/value, refinement, final-hit and Chi modifiers, descriptions, and optional pet statistics.

Top-level values are authoritative:

- item type 1-24;
- rarity 0-4.

FruitSalad's alternate rarity table uses different values and is not applied to this model.

### Skill definition

Source: `generic/Info_Skill.js`.

Header fields preserve ID, name, category, sprite, clan/weapon, descriptions, Chi usage, points/max level, and start/end modifiers.

`SkillModifiers` uses `double` because the source loader divides every raw 32-bit modifier by 100 before runtime use.

### Monster definition

Source: `generic/Info_Monster.js`.

Known fields preserve ID/name, model/click data, level/health, radii, speeds, attack/defense/success/block/critical, elemental attack, improve stones, and pet fields. The very large `UnknownNNN` tail is intentionally excluded rather than assigned guessed semantics.

### NPC definition

Source: `generic/Info_Npc.js`.

Preserved: ID, Name, PageCount, Chat1-Chat5. Unknown fields are excluded pending packet/data evidence.

### Experience definition

Source: `generic/Info_Exp.js`.

Preserved: level/range, skill point, clan damage/defense/hit/dodge, elemental damage, clan HP and Chi.

## Runtime entity and state mappings

Sources: `generic/CharacterState.js`, `generic/Monster.js`, `generic/Npc.js`, `generic/Item.js`.

| C# member | Original behavior |
|---|---|
| `EntityState.UniqueId` | Node/QuadTree ID |
| `EntityState.LocalId` / `OtherId` | source's runtime ID and definition-facing IDs |
| life/stance/skill/frame | copied from source state; no simulation added |
| Location/LocationTo | `CVec3` values |
| Direction/TargetDirection/FacingDirection | numeric degrees/floats |
| TargetObjectIndex | nullable; source `-1` becomes null |
| Health | original runtime numeric state |

`MonsterEntity`, `NpcEntity`, and `WorldItemEntity` use reference identity and retain the source defaults:

- monster maximum/current health from definition;
- NPC health 1;
- item amount/life/JustSpawned 1 and empty owner.

## Zones and world records

Sources: `generic/structs.js`, `zone.js`, `config.json-dist`.

| C# type | Source mapping |
|---|---|
| `SpawnDefinition` | `UniqueID u32`, `ID u32`, CVec3 float32, `Direction float32`; source record is 24 bytes |
| `WorldRegion` | `Unknown1`, ZoneID, Unknown2, Unknown3, X/Y/Z, Radius as signed int32 |
| `ZoneDescriptor` | configured zone ID, Name, Load, DisplayName |

## Equality semantics

- Identifiers and value records use structural equality.
- Static definitions use record structural equality.
- Account, Character, Guild, runtime monsters/NPCs/items use reference identity.
- Ordered lists preserve original array ordering and null holes.

## Known unresolved questions

- Account/character IDs are JavaScript/Mongo numbers; C# `long` is chosen for domain/persistence compatibility, while protocol adapters must still validate 32-bit fields.
- The source `MonstersKilled` hash has no type/key usage; it is deferred.
- Password hashing/authentication is excluded from the core domain.
- NPC/monster unknown record tails are not guessed.
- Quest fields occur only in wire structs and are not added to the persistent character model.
- Runtime sentinels, some fixed string lengths, and wire float32 rounding belong to protocol adapters.
- `CharacterName`, fixed-width encoding, and case folding are deferred to protocol/persistence mappings; the domain retains exact strings.

## Test fixture provenance

The Guanyin start template, PIN default, collection sizes, enum values, entity spawn defaults, skill modifier scaling, pet maxima, and signed/unsigned source declarations are derived from original files. Tests that use values such as `12.5` or negative monster values are explicitly structural boundary tests; they verify C# representation and are not presented as game-data or balance fixtures.
