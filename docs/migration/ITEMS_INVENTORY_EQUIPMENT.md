# Items, Inventory, and Equipment

## Original behavior and completeness

The legacy character schema in `db/character.js` stores `Inventory`, `QuickUseItems`, `SkillList`, `Skills`, `SkillBar`, `Storage`, and equipment as Mixed/nested values, with silver and bank balances separately. `generic/structs.js` defines protocol storage structures and fixed array regions. These are source shapes, not transactional gameplay guarantees.

The root `packets/world/itemactions.js` registers many 0x14 action types. Some branches are active:

- pickup validates world object existence, owner name (with an explicit TODO in the owner check context), stackability and a stack limit of 99; silver pickup updates character silver and removes the world item.
- equip checks inventory bounds, item definition, level, clan, and packet item type, then moves the item to its equipment property and recalculates equipment info.
- storage, inventory and quick-use operations include slot, amount and stacking logic, but there are TODO checks for bounds/collisions and action reply consistency.
- item action 0x0C currently sets current HP and Chi to their maximum, sends 0x2F, and then sends a failed item-action reply. It does not consume an item in that shown path.

`generic/Info_Item.js` has recovery code for point and percentage HP/Chi values (ValueTypes 1-5) and an optional script hook. That method does not prove that the root 0x0C path invokes it. `FruitSalad/Processes/Zone/Packets/inventoryAction.js` is an alternate implementation and contains its own rollback/security TODOs.

## .NET status

Prompt 08 domain types represent inventory, equipment and item definitions; Prompt 15 raw-BSON mapping preserves supported loaded fields and location writes avoid replacing unknown fields. No item-action packet service, equipment effects, consumable service, item-world pickup/drop, bank/storage transaction or gameplay persistence operation is implemented.

GP-017..021 and GP-033..034 remain `NOT_STARTED`. Before implementation, collect original item images and wire fixtures, clarify the 0x0C discrepancy against a client trace, and define account/character write ownership. Every operation must test item and silver conservation on failure and concurrency; no whole-document replacement is acceptable where it could erase unknown legacy fields.
