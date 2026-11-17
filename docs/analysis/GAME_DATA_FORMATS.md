# InfiniteSky Game Data Formats

## Data categories

The server consumes:

1. original-game binary `.IMG` information records;
2. binary `.NPC`, `.MOB`, and `.WREGION` world/spawn records;
3. CSV translation overlays;
4. JSON configuration and character templates;
5. MongoDB documents;
6. experimental raw `.pac` packet captures;
7. zip/compressed world formats only mentioned/commented in structures.

## Common binary rule

`GameInfoLoader` reads:

```text
uint32 RecordCount
RecordCount × fixed-size record
```

All numeric primitive values are little-endian. Every `.IMG` uses a `restruct` declaration. The loader:

- validates at least four bytes;
- ensures enough bytes for count × struct size;
- asynchronously unpacks up to 10 records at once;
- only stores records with a nonzero `ID`;
- optionally overlays CSV translation fields;
- emits `load` after every registered information loader completes.

Source: [`GameInfoLoader.js`](../../GameInfoLoader.js).

The spawn/zone reader follows the same first-four-bytes record count convention but unpacks directly into arrays. Source: [`zone.js`](../../zone.js), [`generic/structs.js`](../../generic/structs.js).

## `.IMG` information files

### Experience: `005_00001.IMG`

Record fields:

- Level, EXPStart, EXPEnd;
- Unknown1, Unknown2, SkillPoint;
- clan damage/defense/hit/dodge;
- light/shadow/dark elemental damage;
- clan HP and Chi.

The source assigns `ID = Level`.

Format owner: [`generic/Info_Exp.js`](../../generic/Info_Exp.js).

### Items: `005_00002.IMG`

Record prefix:

- ID uint32;
- Name[28];
- Rareness;
- ItemType;
- display item;
- `Level` and `LevelRequirement`;
- clan restriction;
- purchase and sale prices;
- capacity;
- base attributes: Strength, Dexterity, Vitality, Chi, Luck;
- damage, defense;
- Light/Shadow/Dark damage and resistance;
- hit/dodge/deadly;
- three skill bonus IDs and amounts;
- ValueType and Value1;
- refinement, experience and Chi modifiers;
- descriptions [25], [25], [26].

Many intermediate fields are named `_N` and not reverse engineered.

Runtime item methods classify item types 1-24, determine inventory equipment slot, slot footprint (one or four inventory cells), stackability, clan eligibility, use effect, and rarity display. Pet records receive hard-coded pet stat tables by specific item IDs.

Format owner: [`generic/Info_Item.js`](../../generic/Info_Item.js).

### Skills: `005_00003.IMG`

Record prefix:

- ID;
- Name[28];
- Category;
- SpriteStartID;
- Clan and Weapon requirements;
- three descriptions.

The file has a large unknown middle. Known late fields include ChiUsage, PointsToLearn, MaxSkillLevel, then two `SkillData` modifier structs.

Each skill modifier struct includes:

- ChiCost, defensive skill, Chi recovery;
- acupressure/stun values;
- AirWalk/EnergyBall, damage/range;
- light/shadow/dark damage;
- hit, duration, damage/defense;
- elemental resistances;
- hit, dodge, movement, attack speed;
- luck/deadly return-damage;
- regeneration.

Percent-like values are divided by 100 during loading.

Format owner: [`generic/Info_Skill.js`](../../generic/Info_Skill.js).

### Monsters: `005_00004.IMG`

Known fields:

- ID, Name[24], models and click dimensions;
- Level at record offset 88;
- Health at offset 96;
- walk/run/death speed;
- attack/defense/success/block;
- elemental attack/defense/critical;
- LightATK, ShadowATK, DarkATK;
- inferred money/drop groups;
- improve stone chance/ID pairs;
- pet drop chance and IDs.

Most of offsets 180-880 remain `UnknownNNN`. The active drop code interprets many paired fields as chance/ID combinations without a fully validated schema.

Format owner: [`generic/Info_Monster.js`](../../generic/Info_Monster.js).

### NPCs: `005_00006.IMG`

Known:

- ID;
- Name[28];
- 17 unknown u32 fields;
- PageCount;
- Chat1-Chat5, each 51 bytes;
- a long u32 tail, all unnamed.

Format owner: [`generic/Info_Npc.js`](../../generic/Info_Npc.js).

## Translation CSV overlays

Loaded from `data/translation`:

- `Items.csv`: ID, Name, Description1-3
- `Skills.csv`: ID, Name, Description1-3
- `Monsters.csv`: ID, Name
- `Npcs.csv`: ID, Name, Chat1-5

`fast-csv` reads comma-delimited quoted fields. Existing binary records are enriched by matching ID; missing records may be created from CSV IDs and passed through the record transform.

Source: [`GameInfoLoader.js`](../../GameInfoLoader.js).

No active `Exp.csv` overlay is used; the branch for it is commented.

## Spawn formats

### `.NPC` and `.MOB`

Path:

```text
data/spawninfo/NNN.NPC
data/spawninfo/NNN.MOB
```

Both use:

```text
uint32 count
count × SpawnInfo
```

`SpawnInfo`:

| Offset | Size | Type | Field |
|---:|---:|---|---|
| 0 | 4 | uint32 LE | UniqueID |
| 4 | 4 | uint32 LE | ID |
| 8 | 12 | 3 × float32 LE | Location |
| 20 | 4 | float32 LE | Direction |

Total: 24 bytes.

Source: [`generic/structs.js:477-481`](../../generic/structs.js).

Only NPC loading is active. `.MOB` loading is commented in `Zone.Load`.

### `.WREGION`

Path:

```text
data/world/ZNNN_ZONEMOVEREGION.WREGION
data/world/ZNNN_ZONESAFEREGION.WREGION
```

Format:

```text
uint32 count
count × WREGION
```

`WREGION` has eight signed little-endian int32 fields:

1. Unknown1
2. ZoneID
3. Unknown2
4. Unknown3
5. X
6. Y
7. Z
8. Radius

Total: 32 bytes.

Source: [`generic/structs.js:483-491`](../../generic/structs.js).

## Character and inventory MongoDB schema

### Account

Account fields include numeric `_id`, unique Username, Password, Email, active state, Level, LastIP, UsePin/Pin, LastLogin, server-specific CharacterIndividuals, and AP. Password and PIN are stored as strings without hashing in this implementation.

Source: [`db/account.js`](../../db/account.js).

### Character

Major persisted groups:

- identity: `_id`, AccountID, ServerName, Name, GuildName, clan/title;
- progression: level, experience, stat/skill points, honor, play time;
- attributes: Strength, Chi, Dexterity, Vitality;
- equipment: ring, cape, armor, glove, amulet, boot, calabash bottle, weapon, pet;
- economy: Silver, StorageSilver, BankSilver, AP;
- arrays: Inventory, QuickUseItems, SkillList, Skills, SkillBar, Storage, Friends;
- runtime save state: MapID and RealX/Y/Z, Health and Chi;
- combat/event counters and modifiers.

Inventory, storage, skills, and bank data use `Mixed` or arrays of Mixed, so schema permits inconsistent records.

Source: [`db/character.js`](../../db/character.js).

### Bank

Bank inventory and silver are not top-level character data. The world login handler initializes:

```text
account.CharacterIndividuals[serverName].Bank       (56 slots)
account.CharacterIndividuals[serverName].BankSilver
```

and exposes save helpers on the character socket. [`packets/world/world_login.js`](../../packets/world/world_login.js).

### Guild

Guild schema:

- unique indexed Name;
- Level;
- hasGuildEmblem;
- Members as `Mixed[]`.

Source: [`db/guild.js`](../../db/guild.js).

### Counters and mail

Counters use `_id` plus atomic incremented `seq`, supporting numeric IDs. Email queue is a separate Mongoose model for queued mail. [`db/counter.js`](../../db/counter.js), [`db/email_queue.js`](../../db/email_queue.js).

## JSON formats

### New character templates

`generic/new_character_config.js` assigns `infos.NewCharacter[ClanName]` values, copied directly to the new Mongoose character. It is runtime JavaScript, not external JSON. Source: [`generic/new_character_config.js`](../../generic/new_character_config.js).

### Configuration

`config.json-dist` configures:

- message of the day/server name;
- ports/interfaces;
- plugins;
- MongoDB connection;
- clan names and SingleClanOnly;
- admin IP/password;
- NAT translations;
- external IP;
- view distance/drop multiplier;
- zone IDs and load flags.

The active loader parses this JSON synchronously and watches it with `fs.watchFile`. Source: [`main.js`](../../main.js), [`util.js`](../../util.js).

## Packet structures as game data

Runtime state is serialized through shared `generic/structs.js`:

- CVec3: X/Y/Z float32
- `Equipt`: ID, Capacity, Enchant, Combine, Unknown
- `StorageItem`: 20 bytes
- `SmallStorageItem`: 12 bytes
- pet variants
- full `Character` structure.

See [`generic/structs.js`](../../generic/structs.js).

## Spatial/configuration data

Zone world data is configured through zone IDs rather than an active terrain loader. `Zone.Load` creates a fixed 100,000×100,000 QuadTree with default depth 6 and limit 10. Terrain loading is commented. Consequently, terrain height, navigation mesh, collision map, and path connectivity are not active inputs to top-level gameplay.

Source: [`zone.js`](../../zone.js), [`QuadTree.js`](../../QuadTree.js).

## Data correctness issues

- Item stats use a misspelled `ShawdowResistance` consistently in places.
- Monster runtime expects fields (`Damage`, `Defense`) that differ from loaded fields (`AttackPower`, `DefensePower`).
- Character schema and wire `Character` struct have overlapping but non-identical names.
- Arrays can contain holes (`null`, undefined, Mongoose array elements), with different serializers handling them differently.
- Item bank/storage use 12-byte forms while normal inventory uses 20 bytes.
- `calbash`/`calabash` spelling differs.
- Translation overlays can fill source records with incomplete/unknown schema values.
- Monster drop and NPC tail meanings are largely unresolved.
- Many `.pac` files are captures/test fixtures, not production format definitions.
