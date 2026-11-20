# Quests and NPC Interactions

## Quest evidence

Root `packets/world/quest.js` currently contains only a captured 0x45 packet comment. It does not implement a root quest handler. `FruitSalad/Processes/Zone/Packets/quest.js` is a distinct, partial process implementation:

- 0x45 may load the first clan quest or a next quest and update/save `QuestPrevious`, `QuestCurrent`, `QuestPart`, `QuestCounter`, and `QuestOther`; TODOs note missing start-NPC proximity and mandatory/optional quest handling.
- 0x47 can create a quest monster for a limited type-5 path, but proximity, duplicate spawn and despawn rules are TODOs.
- 0x49 explicitly tells the user completion is not implemented, resets quest fields, and loops over reward types; reward types 1-3 are unimplemented and quest-condition/end-NPC checks are TODOs.

The code comments themselves say packet/field meanings are uncertain. This alternate path is not a basis for claiming the root client behavior.

## NPC interaction evidence

`generic/Info_Npc.js` loads NPC info and includes an `Items` array whose shop semantics are marked TODO. `generic/Npc.js` defines NPC state serialization but has placeholder HP and no AI. Root `trader.js`, `openshop.js`, and item-action branches provide fragmented commerce/interactions; the generic interaction packet and dialogue rules are not established.

## .NET status

No quest, quest state, dialogue, NPC interaction, objective, reward or persistence logic is implemented. Quest features remain `NOT_STARTED` and are blocked on source-backed root protocol/data evidence. NPC dialogue/interaction remains `NOT_STARTED` due unclear packet semantics. Do not copy the FruitSalad process implementation into the root protocol pipeline or fabricate quest prerequisites/rewards.
