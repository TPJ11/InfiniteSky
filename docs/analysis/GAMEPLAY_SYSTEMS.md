# InfiniteSky Gameplay Systems

## Assessment

InfiniteSky contains a broad but incomplete MMORPG gameplay implementation. Persistent characters, item/equipment records, zones, movement/state replication, stat calculation, and many packet-triggered inventory actions exist. The authoritative simulation is unusually weak: clients supply positions and actions, monster AI is not actively assigned, and several death, spawn, quest, and NPC interaction paths are placeholders.

The analysis below treats executable code as implemented behavior. Comments, disabled registrations, and zero-sized packet handlers are identified as incomplete or evidentiary only.

## Character creation and progression

### Creation

Character creation is handled by login packet `0x06`. The handler requires an authenticated login socket, a slot below 3, an empty slot, a unique name, a clan below 3, valid hair/face, and a clan-appropriate weapon. It rejects invalid state by returning a create status; status meanings are documented in [`packets/login/handleManageCharacter.js`](../../packets/login/handleManageCharacter.js).

Each new character is created from the `db.Character` Mongoose model, receives an incrementing numeric `_id` from counter `characterid`, and copies the selected clan template from `infos.NewCharacter`.

The three templates provide:

| Clan | Home map | Starting XYZ | Starting equipment/skills |
|---|---:|---|---|
| Guanyin | 1 | -630, 50, -815 | armor 8, glove 9, boot 10; skills 1/2 |
| Fujin | 6 | -225, 412, 1575 | armor 14, glove 15, boot 16; skills 20/21 |
| Jinong | 11 | -743, -388, 2719 | armor 20, glove 21, boot 22; skills 39/40 |

All templates begin at level 1, zero experience, 50,000 silver, one point in each base stat, four basic inventory items, quick-use pills, and two skill entries. Sources: [`generic/new_character_config.js`](../../generic/new_character_config.js), [`packets/login/handleManageCharacter.js`](../../packets/login/handleManageCharacter.js), [`db/counter.js`](../../db/counter.js).

### Persistent character model

The Mongoose character model stores identity, guild, GM flag, clan, appearance, level, experience, stat and skill points, equipment, inventory/storage/skill arrays, map coordinates, HP/Chi, combat modifiers, pet-related data, and progression counters. Multiple properties such as `Name`, `PlayTime`, and `Honor` are duplicated in the schema. Array-backed inventory and equipment subrecords are mostly `Mixed`, so MongoDB does not enforce their wire layout.

Source: [`db/character.js`](../../db/character.js).

### Experience and level up

`socket.giveEXP(value)` adds positive experience:

1. Looks up `infos.Exp[currentLevel]`.
2. Adds experience, capped at the end of level 145.
3. Advances through levels while experience exceeds each level's `EXPEnd`.
4. Adds the target level's `SkillPoint`.
5. Adds stat points: normally 5, zero for levels 100-112, and 30 above 112.
6. Updates level, sends a `0x2F` status update, restores current HP/Chi, recalculates stats, broadcasts a `0x2E` level-up packet, and saves.

The function has unusual cumulative adjustment: it increments Experience while processing overflow, rather than subtracting the prior level threshold. Combat calls this function with damage dealt, so normal monster damage currently awards the same quantity as damage. Source: [`world.js`](../../world.js), [`generic/Info_Exp.js`](../../generic/Info_Exp.js).

Stats can also be altered by the item-action/stat-point handlers and GM commands.

## Attributes and derived statistics

The active stat subsystem is `generic/statcalculation.js`. It produces runtime-only `character.infos` values:

- Damage, Defense, HitRate, Dodge, Luck, DeadlyRate
- MaxHP, MaxChi
- Light, Shadow, and Dark resistance
- clan elemental damage
- per-equipment contributions and pet contributions

Clan multipliers are:

| Clan | HP/stat Vit | Chi/stat Chi | Hit/Dex | Dodge/Dex | weapon strength multipliers |
|---|---:|---:|---:|---:|---|
| Guanyin | 15.31 | 20 | 5.14 | 2.57 | 1.43, 3.86, 4.30, 4.08 |
| Fujin | 14.29 | 17.14 | 4.57 | 2.29 | 1.36, 3.17, 2.71, 2.94 |
| Jinong | 16.33 | 22.29 | 5.71 | 2.86 | 2.59, 5.39, 5.61, 5.17 |

Level/base values come from the matching fields in `infos.Exp`. Equipment modifies:

- weapons: damage, hit rate, elemental damage, and strength multiplier by item type;
- armor: vitality, defense, dodge, and resistances;
- boots/gloves/cape/amulet/ring: slot-specific combat attributes;
- pets: HP, defense, damage, dodge, hit rate, elemental bonuses, and selected skills.

Enchant is usually interpreted as `Enchant * 3` percent. Combine adds fixed per-item-level ticks. Pet bonuses scale by `Growth / MaxGrowth`, with hard-coded effects by pet ID.

`updateAll()` recalculates every equipment contribution and derived value. It can save character HP/Chi when equipment changes lower maxima. Skill and item-wide bonus calculation is largely empty or incomplete.

Source: [`generic/statcalculation.js:35-760`](../../generic/statcalculation.js).

## Movement and positioning

The current character position exists in two forms:

- persistent `character.RealX/RealY/RealZ`;
- runtime `character.state.Location/LocationTo/LocationNew`.

World action packets `0x03`, `0x04`, and `0x05` carry stance, skill, frame, locations, directions, target IDs, and health fields. The active `newAction.js` handlers copy client values directly into persistent/runtime state. They do not:

- simulate speed server-side;
- validate portal or terrain collision;
- check the distance moved;
- check action-rate timing;
- verify the supplied target state consistently.

The `0x05` handler calculates 2D distance to a targeted node and logs it, but normal attack damage is not resolved there. The older `action.js` attack implementation contains damage and target behavior, while its registration is commented at the end.

Sources: [`packets/world/newAction.js`](../../packets/world/newAction.js), [`packets/world/action.js`](../../packets/world/action.js), [`generic/CharacterState.js`](../../generic/CharacterState.js).

Characters are inserted into a zone QuadTree keyed in X/Z. State updates are sent to clients within `config.viewable_action_distance`, default 500. [`zone.js`](../../zone.js), [`QuadTree.js`](../../QuadTree.js).

## Combat and damage

### General damage helper

`generic.Battle.damage` returns:

```text
(((random * 0.15) - 0.05) * attackerDamage) + attackerDamage - defense
```

`generic.Battle.calculate` creates an attack packet with IDs, attack index, status `1`, total damage floored, deadly `1`, attacker elemental values, and `DamageHP`. It does not implement hit chance, dodge, resistance, critical probability, blocking, or attack speed. [`generic/battle_functions.js`](../../generic/battle_functions.js).

### Player versus player

The legacy `action.js` implementation locates the selected QuadTree node, calls `generic.Battle.calculate`, deducts HP from the defender, and broadcasts an attack reply. HP reaching zero is not consistently routed into respawn/death state. The registration for that action path is commented, while `newAction.js` is active. [`packets/world/action.js:320-620`](../../packets/world/action.js).

### Player versus monster

The legacy implementation:

1. Randomizes player damage from 100%-150%.
2. Subtracts `monster.info.DefensePower`.
3. Subtracts the result from monster HP twice in one branch.
4. Awards the damage as experience.
5. If HP falls below zero, sets it to zero then resets it immediately to full health.
6. Emits drops from monster data.

No active monster AI attacks players, and the active new action handlers do not apply this damage. Therefore monster combat is prototype/legacy behavior rather than a complete active combat loop.

### Monster data

Runtime monsters use `AttackPower`, `DefensePower`, `WalkSpeed`, `RunSpeed`, level, health, and elemental data from `infos.Monster`. Their runtime `infos` maps `Damage` from `info.Damage` and `Defense` from `info.Defense`, although the loaded monster schema names `AttackPower` and `DefensePower`; this is inconsistent.

Sources: [`generic/Monster.js`](../../generic/Monster.js), [`generic/Info_Monster.js`](../../generic/Info_Monster.js), [`packets/world/action.js`](../../packets/world/action.js).

## Skills and buffs

### Learning and bar placement

Item-action operations `0x1B`, `0x1C`, `0x1D`, and `0x1F` map skill-to-bar, remove-from-bar, skill-up, and learn-skill behavior. Learning/upgrade checks and persistence occur in [`packets/world/itemactions.js`](../../packets/world/itemactions.js). Separate [`packets/world/learnskill.js`](../../packets/world/learnskill.js) contains no active implementation.

Characters store:

- `SkillList`
- `Skills`
- `SkillBar`
- `SkillPoints`

### Skill use

Active action opcode `0x05` recognizes several skill animation IDs by setting `onSkillStateUpdate`. Opcode `0x19` captures SkillID and SkillLevel in the next state update. Opcode `0x18` applies support/buff effects using skill modifier start/end interpolation.

The active buff implementation validates the skill exists in `SkillList`, calculates duration and cost, but does not deduct the computed Chi cost. It writes into state arrays:

- `Buffs[11]`, `[5]`, `[3]`, `[13]`, `[1]`, `[0]`, `[9]`
- `Buffs2[4]`, `[5]`, `[6]`, `[0]`, `[1]`
- `BuffHS`

It hard-codes amounts for several slots (`22`, `200`). Opcode `0x7A` accepts a fixed expiration packet but does not clear state.

Skill records contain 34 modifier fields, including Chi cost, damage, range, duration, resistances, speed, luck, reflection, regeneration, and cast effects. The original values are divided by 100 on load.

Sources: [`generic/Info_Skill.js`](../../generic/Info_Skill.js), [`packets/world/skillUse.js`](../../packets/world/skillUse.js), [`packets/world/newAction.js`](../../packets/world/newAction.js).

## Monsters and AI

The AI framework provides:

- attacker collection with accumulated damage;
- named AI objects with trigger interval;
- `AIObject.step(delta)` dispatch.

No concrete production AI behavior is added by the top-level source, and `Zone.step` only updates the QuadTree. Monster updates are 4-second state broadcasts, not AI ticks. `Monster_Prototype.onDelete` and death behavior contain inconsistent method/property names; `respawn` is not implemented on monsters.

AI is therefore a framework, not working gameplay AI.

Sources: [`generic/AI.js`](../../generic/AI.js), [`generic/Monster.js`](../../generic/Monster.js), [`zone.js`](../../zone.js).

## NPC interactions

NPC definitions load name, PageCount, and five chat strings, followed by a very large unknown tail. NPC runtime objects expose position/state and a `0x19` state packet.

Zone load parses `.NPC` files and creates NPCs. NPCs are broadcast every four seconds. There is no active generic interaction dispatcher in the top-level packet tree: item buying/selling uses NPC IDs/items through item-action packets, and NPC-specific guild/zone interactions are scattered.

NPC combat and quest dialogue behavior are absent. Direct NPC attacks immediately return in the legacy combat code.

Sources: [`generic/Info_Npc.js`](../../generic/Info_Npc.js), [`generic/Npc.js`](../../generic/Npc.js), [`zone.js`](../../zone.js), [`packets/world/itemactions.js`](../../packets/world/itemactions.js).

## Inventory and equipment

The active inventory packet is opcode `0x14`, containing an action type and item/index/position fields. `ItemActions` implements action codes:

| Action | Behavior/status |
|---:|---|
| `0x00` | Pick up a world item; ownership check; silver direct; stack checks |
| `0x01` | Drop silver/items |
| `0x02` | Move to quick-use bar |
| `0x03` | Equip; checks level, clan, item type, empty slot, recalculates stats |
| `0x04` | Move stored item |
| `0x05` | Inventory to private storage |
| `0x06` | Inventory to Gate Master storage/bank |
| `0x08/0x09` | Coins/gold conversion |
| `0x0A` | Discard item stub |
| `0x0B` | Move from pill bar |
| `0x0C` | Use item |
| `0x0E` | Unequip |
| `0x0F/0x10` | Retrieve private/Gate Master storage |
| `0x11` / `0x07` | Buy/sell item to NPC |
| `0x14` | Move item in inventory |
| `0x15/0x16/0x18/0x19` | storage/bank silver |
| `0x1B-0x1F` | skill-bar/learn/upgrade |
| `0x20-0x2A` | mostly stubs |

Inventory uses Mongoose Mixed objects with column/row and optional amount/enchant/combine. Collision checks exist on the character model, but are not applied consistently in all actions. Many paths emit an information message but still permit partial or unchecked transitions.

Item use applies only recognized `ItemInfo.ValueType` effects:

- point HP
- percent HP
- point Chi
- percent Chi
- percent HP/Chi

Sources: [`packets/world/itemactions.js`](../../packets/world/itemactions.js), [`generic/Info_Item.js`](../../generic/Info_Item.js), [`db/character.js`](../../db/character.js).

## World item drops

`Zone.createItem/addItem` creates a QuadTree entity from item ID or spawn data. Each item:

- is broadcast on spawn and every 4 seconds;
- expires after 180,000 ms;
- has an owner-protection name cleared after 5,000 ms;
- is removed by changing Life/ItemID and broadcasting a final state.

The legacy monster-death code rolls against `config.ItemDropRate` and a 100,000 chance base. It can produce:

- improve stones;
- silver;
- several pill/item pairs;
- pet drops;
- boss items;
- quest-like items;
- crafting materials.

Many drop fields remain named `UnknownNNN`, with logic based on inferred offsets. There are duplicated conditions and questionable magic values (`Unknown56 + 75000`, `75000 + Unknown56 * rate`).

Sources: [`zone.js`](../../zone.js), [`generic/Item.js`](../../generic/Item.js), [`packets/world/action.js:492-1320`](../../packets/world/action.js).

## Quests

There is no active quest subsystem. Character serialization has `QuestID`, `QuestStart`, and `QuestComplete`, but the corresponding gameplay handler file contains only a captured unrecognized packet comment for opcode `0x45`. No parser, quest state machine, objective tracking, reward handler, or registration is implemented.

Sources: [`generic/structs.js`](../../generic/structs.js), [`packets/world/quest.js`](../../packets/world/quest.js).

## Maps, zones, and transfers

Configured maps come from `config.Zones`, including named and unnamed zones. Each loaded zone:

- creates a fixed QuadTree spanning X/Y -50,000 to 50,000;
- maintains clients, NPCs, monsters, items, move regions, safe regions;
- loads NPC spawn data;
- loads movement and safe-region files;
- does not load terrain in active code;
- does not load monster spawns in active code.

Map movement transfer request opcode `0x08` looks up the target zone and portal records, saves destination location, queues the transfer socket, and directs the client to the world endpoint.

Sources: [`config.json-dist`](../../config.json-dist), [`zone.js`](../../zone.js), [`world.js`](../../world.js), [`packets/world/zonechange.js`](../../packets/world/zonechange.js).

## Spawning and respawning

Active zone loading reads:

- `data/spawninfo/<zone>.NPC`;
- `data/world/Z<zone>_ZONEMOVEREGION.WREGION`;
- `data/world/Z<zone>_ZONESAFEREGION.WREGION`.

Each begins with a uint32 record count and fixed structs.

Monster `.MOB` loading is entirely commented. Runtime monster spawning is available through admin commands, but natural world monster spawns do not occur.

Character respawn is an empty method. Death state is set for character/monster code, but respawn to home, corpse timing, experience/item loss, and death timers are not implemented. Special map recovery in game start moves known map 119 characters to clan towns.

Sources: [`zone.js`](../../zone.js), [`generic/CharacterState.js`](../../generic/CharacterState.js), [`db/character.js`](../../db/character.js), [`packets/login/handleGameStart.js`](../../packets/login/handleGameStart.js).

## Game events and social gameplay

Implemented or partially implemented:

- party invites, join, leave, disband, roster broadcasts;
- guild creation/membership/invites with MongoDB persistence;
- trade invitation/state and player-shop flows;
- duel request/response flow;
- chat, faction chat, whisper, friends;
- Nangi track status;
- gambler and lucky ticket;
- item enchant/combine/trader/AP shop variants.

Many handlers are prototypes or contain partial validation and unknown fields. Party chat is explicitly unimplemented. Event scheduling, raids, bosses, timed map events, and global world-state simulation are absent.

Sources: [`packets/world/party.js`](../../packets/world/party.js), [`packets/world/guild.js`](../../packets/world/guild.js), [`packets/world/trade.js`](../../packets/world/trade.js), [`packets/world/duel.js`](../../packets/world/duel.js), [`packets/world/nangi_track.js`](../../packets/world/nangi_track.js).

## Administrative commands

Command scripts are hot-loaded through `GMCommands.Start()` and executed from slash-prefixed chat. Authorization compares `client.account.Level` with the command registration level. The dispatcher catches errors and exposes them only to level 80+ accounts.

Command families include:

- information: commands/help, online, where, maps, expinfo, iteminfo, monsterinfo, npcinfo, skillinfo, showstats;
- account and persistence: makeaccount, importaccounts, unlockaccount, save, shutdown;
- movement: goto, teleport/tp, bring, home, portalgo;
- spawn management: spawnitem, spawnmonster, spawnnpc, clearitems, remove spawned NPC/monster, killmobs;
- combat/state: heal, die, revive, give XP, reset/statreset/skillreset, set, setskills;
- inventory/equipment: inventory, clearinventory, refill;
- social/admin: announce/broadcast, kick, hide/unhide, mail, report, pay functions;
- debugging/test: debug, test, state, world, node.

Not every command registration is production-grade. Some test files register duplicate names (`stats`, `level`) and commands such as `run`/test expose VM/object state. `game_gmcommands.js` lists many common MMORPG commands only in comments, except `return`.

Sources: [`GMCommand.js`](../../GMCommand.js), [`commands/`](../../commands/).

## Principal broken and incomplete gameplay

- No active natural monster spawning; `.MOB` load is commented.
- No working monster AI or AI step integration.
- No authoritative movement/collision/speed checks.
- Active action handlers no longer apply legacy combat.
- Monster damage is subtracted twice in the legacy kill branch.
- Monster HP is restored immediately on death, not respawned via timer.
- Character respawn is empty.
- No quest state machine.
- No general NPC interaction registry.
- No reliable hit/dodge/crit/elemental damage in active combat.
- Buff expiration and Chi consumption are not implemented.
- Many item actions return "not implemented".
- Zone socket removal uses `slice` rather than assigning the result, so it does not remove the client.
- Monster references mix plural/singular arrays (`Monster`/`Monsters`, `NPCNextID`/monster counters).
- Item drop fields are mostly offset guesses and duplicate checks.
- Skills `updateSkills()` is empty.
- Database state arrays do not always have deterministic length or type.

## Migration risks

- Separating client-authoritative movement from authoritative combat changes behavior and packet expectations.
- Reconstructing fields from unknown monster/NPC struct tails may produce incorrect rules.
- Replacing immediate monster heal with real respawn changes drop and PvE pacing.
- Normalizing Mixed inventory arrays will require data migration and tolerant reads.
- Fixing duplicate Monster HP subtraction is correctness work but changes balance.
- Replacing JavaScript global script state affects hot reload and command/game-event plugins.
- Legacy GM commands may expose powerful operations to low-level accounts; permission levels must be audited before migration.
