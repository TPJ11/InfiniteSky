# NPCs, Monsters, and Loot

## Original runtime

`generic/Npc.js` and `generic/Monster.js` define state packet layouts and constructors from loaded info records. NPC HP is initialized to 1 with a source comment that maximum HP is unknown. `generic/Monster.js` initializes HP from its definition and carries an attacker collection.

Root `zone.js` actively loads `.NPC` spawn files and has NPC create/add routines. The `.MOB` loader block is commented out, although `.MOB` files exist under `data/spawninfo`. Manual/admin monster creation and add routines exist, but this is not natural spawn/respawn parity. Monster `onDeath` has no reward implementation. Its attacker collection can add damage and sort descending, but the AI framework's default callback only logs that AI is unimplemented and returns null.

Root `packets/world/action.js` comments out item drop construction. Its active monster attack path double-subtracts damage and then heals the monster immediately on a kill threshold. Monster definition fields in `generic/Info_Monster.js` include many unnamed drop-related offsets; item/monster `.IMG` files required by `GameInfoLoader` are not present in this repository's `data` tree.

## .NET boundary

The Domain project contains `NpcDefinition`, `MonsterDefinition`, and entity data models; GameData currently provides zone resource loading, not complete NPC/monster info catalogs or live NPC/monster behavior. Prompt 15's player registry does not register NPCs or monsters.

No AI, combat, loot, drop ownership, death, respawn or natural monster spawning is implemented. GP-012, GP-022..024, and GP-026 remain `NOT_STARTED`. Do not create default aggression, drops or respawn locations from names or data shape alone.
