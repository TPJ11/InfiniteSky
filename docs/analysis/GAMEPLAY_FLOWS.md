# InfiniteSky Gameplay Flows

## Character creation to world entry

```text
Login packet 0x03
  -> account lookup/password check
  -> character list packets 0x05

Login packet 0x06 (optional create)
  -> validate slot/name/clan/appearance/weapon
  -> clone clan NewCharacter template
  -> allocate characterid counter
  -> Mongoose save
  -> create reply packet 0x07

Login packet 0x09
  -> select persisted character
  -> CharacterState created from RealX/Y/Z and stats
  -> guild membership repaired
  -> socket entered world transfer queue
  -> reply 0x0A with world endpoint

World packet 0x01
  -> find transfer queue by account username
  -> copy account/character/state
  -> select Zone and set MapID
  -> save destination position

World packet 0x02
  -> initialize storage/bank
  -> pack 0x16 full character
  -> send 0x17 world data
  -> add socket to zone and QuadTree
  -> broadcast character state and begin periodic HP/Chi regeneration
```

Sources: [`handleLogin.js`](../../packets/login/handleLogin.js), [`handleManageCharacter.js`](../../packets/login/handleManageCharacter.js), [`handleGameStart.js`](../../packets/login/handleGameStart.js), [`handshake.js`](../../packets/world/handshake.js), [`world_login.js`](../../packets/world/world_login.js).

## Experience and level flow

```text
legacy combat damage or /xp command
  -> socket.giveEXP(value)
  -> lookup infos.Exp[current level]
  -> add experience
  -> while above EXPEnd:
       load next level
       add SkillPoint
       add 5/0/30 StatPoints by level band
       update level
  -> send 0x2F state
  -> restore HP/Chi to Max
  -> recalculate stats
  -> broadcast 0x2E LevelUp
  -> save character
```

Sources: [`world.js`](../../world.js), [`commands/giveexp.js`](../../commands/giveexp.js).

## Stat recalculation flow

```text
character.infos.updateAll()
  -> update Weapon
  -> update Armor
  -> update Boot
  -> update Glove
  -> update Cape
  -> update Amulet
  -> update Ring
  -> update Pet
  -> update Vitality/Chi/Dexterity/Strength
  -> derive Defense/HitRate/Dodge/Deadly/Luck/Resists/Elemental
  -> updateSkills() [empty]
```

Equipment calculations load static item values from `infos.Item`, multiply enchant/combine/growth according to slot rules, and add current level's clan base values from `infos.Exp`.

Source: [`statcalculation.js`](../../generic/statcalculation.js).

## Movement flow

```text
Client action opcode 0x03
  -> copy client coordinates into character.RealX/Y/Z
  -> no movement response

Client action opcode 0x04
  -> copy frame/stance/skill/direction/location
  -> broadcast state to nearby clients excluding self

Client action opcode 0x05
  -> copy state and target IDs
  -> for normal attack skills 5/6/7:
       resolve target QuadTree node
       calculate X/Y distance and message
       do not apply active damage
  -> broadcast compressed character state packet
```

There is no active server-side path integration or speed/collision rejection. Source: [`newAction.js`](../../packets/world/newAction.js).

## Combat flow (implemented but legacy/inactive)

```text
client attack in legacy action handler
  -> find action.nodeID in Zone.QuadTree
  -> determine target type

client target:
  -> Generic.Battle.calculate
  -> subtract HP
  -> area attack reply 0x2C

monster target:
  -> randomize 100%-150% character damage
  -> subtract monster defense
  -> subtract monster HP
  -> give damage amount as EXP
  -> subtract monster HP again
  -> if < 0:
       mark killed
       immediately restore full HP
       roll item drops
  -> send 0x2C
```

The registrations that would dispatch this legacy handler are commented; current `newAction.js` does not perform the damage branch. Source: [`action.js`](../../packets/world/action.js).

## Skill/buff flow

```text
C -> S 0x05 skill animation ID
S: selected animation marks character.state.onSkillStateUpdate

C -> S 0x19 SkillID/Level
S -> area state with skill data

C -> S 0x18 SkillID/Level/Slot
  -> verify SkillID in character.SkillList
  -> load infos.Skill
  -> interpolate duration/amount from ModifiersStart to End
  -> write Buffs/Buffs2/BuffHS state
  -> reset skill state
  -> broadcast state

C -> S 0x7A expiration/confirmation
  -> accepted, no active cleanup
```

The calculated Chi cost is logged but not deducted. Source: [`skillUse.js`](../../packets/world/skillUse.js).

## Inventory/equipment flow

```text
C -> S 0x14 ItemActionPacket
  -> action number dispatch

pickup 0x00:
  -> find world item in QuadTree
  -> verify owner protection
  -> silver: add directly and remove item
  -> non-silver: check index, ID, stackability, <=99
  -> update Inventory and save
  -> reply 0x2B

equip 0x03:
  -> lookup Inventory index
  -> lookup infos.Item
  -> validate level/clan/type
  -> ensure equipment slot empty
  -> copy item to equipped slot
  -> character.infos.updateEquipment
  -> remove inventory item
  -> save and reply 0x2B

use 0x0C:
  -> lookup item/use value
  -> modify HP/Chi points or percentage
  -> consume/save as handler permits
```

Source: [`itemactions.js`](../../packets/world/itemactions.js), [`Info_Item.js`](../../generic/Info_Item.js).

## Drop and pickup flow

```text
legacy monster HP <= 0
  -> for each inferred drop pair:
       random(0,100000) <= chance × ItemDropRate
       create Item at monster location
       set owner name
       Zone.addItem

Zone.addItem
  -> add item node to QuadTree
  -> initial 0x1B state broadcast
  -> every 4 seconds rebroadcast
  -> owner-only timer 5 seconds
  -> remove timer 180 seconds

Client picks up with action 0x00
  -> add inventory/silver
  -> remove item
  -> final item packet with Life 0/ItemID 0
```

Sources: [`action.js`](../../packets/world/action.js), [`Item.js`](../../generic/Item.js), [`zone.js`](../../zone.js).

## Zone loading and transfer flow

```text
world.loadAllZones()
  -> async queue, configured Zone IDs
  -> Zone.Init
  -> create QuadTree
  -> load .NPC records
  -> load MoveRegion .WREGION
  -> load SafeRegion .WREGION
  -> Loaded=true

Client 0x08 zone request
  -> validate target exists
  -> locate source/destination portal
  -> write target coordinates to state
  -> queue socket for transfer
  -> reply 0x0A endpoint
  -> client reconnects
```

Sources: [`world.js`](../../world.js), [`zone.js`](../../zone.js), [`zonechange.js`](../../packets/world/zonechange.js).

## NPC and quest flows

### NPC appearance

```text
.NPC load
  -> SpawnInfo ID/location/direction
  -> infos.Npc lookup
  -> new Npc object
  -> QuadTree node
  -> 4-second 0x19 broadcast
```

### NPC interaction

NPC purchasing/selling paths exist inside inventory actions and merchant/trader handlers, but there is no generic "interact/open dialogue" packet implementation.

### Quest

```text
0x45 capture exists
  -> no active registration
  -> no parse/state/reward logic
```

Quest progression is therefore not executable in the current top-level implementation. Source: [`quest.js`](../../packets/world/quest.js), [`generic/structs.js`](../../generic/structs.js).

## Death, disconnect, and respawn

```text
Character disconnect:
  -> persist current HP/Chi
  -> persist current/target XYZ depending on transfer
  -> save
  -> remove from zone/client list
  -> account active=0 unless zone transfer

Character death:
  -> CharacterState.respawn() is empty
  -> Character.Kill sets HP=0, Skill=12, Frame=0
  -> no server timer/state machine

Monster death:
  -> legacy branch resets monster HP to full immediately
  -> drops may be created
  -> no actual death/respawn scheduler
```

Sources: [`world.js`](../../world.js), [`CharacterState.js`](../../generic/CharacterState.js), [`db/character.js`](../../db/character.js), [`action.js`](../../packets/world/action.js).

## Administrative command flow

```text
client chat message begins "/"
  -> GMCommands.Execute(command text, socket)
  -> split command/arguments
  -> lookup dynamically hot-loaded command
  -> compare account.Level with command.Level
  -> invoke command function
  -> return information packet or mutate state
```

This path can invoke state-changing commands, data queries, spawns, kicks, saves, and server shutdown depending on the account level. Source: [`GMCommand.js`](../../GMCommand.js), [`commands/`](../../commands/).
