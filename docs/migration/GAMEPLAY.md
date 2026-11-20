# Gameplay Migration

Gameplay coverage is tracked feature-by-feature in [GAMEPLAY_IMPLEMENTATION_PLAN.md](./GAMEPLAY_IMPLEMENTATION_PLAN.md) and [FEATURE_PARITY_MATRIX.md](../FEATURE_PARITY_MATRIX.md). The original root Node.js runtime is authoritative; `FruitSalad` is a separate alternate process tree and is not silently combined with root behavior.

Prompt 16 currently implements only an isolated, source-derived XP arithmetic slice. It is not connected to game data, player/world handlers, packet output, stat recalculation or persistence. No gameplay feature is verified.

See:

- [Gameplay source audit and implementation status](./GAMEPLAY_SYSTEMS_IMPLEMENTATION.md)
- [Movement and combat](./MOVEMENT_AND_COMBAT.md)
- [Skills and progression](./SKILLS_AND_PROGRESSION.md)
- [Items, inventory and equipment](./ITEMS_INVENTORY_EQUIPMENT.md)
- [NPCs, monsters and loot](./NPCS_MONSTERS_AND_LOOT.md)
- [Quests and interactions](./QUESTS_AND_INTERACTIONS.md)
- [Trading and economy](./TRADING_AND_ECONOMY.md)
- [Gameplay protocol compatibility](./GAMEPLAY_PROTOCOL_COMPATIBILITY.md)

The highest risks are absent `005_*.IMG` data, missing gameplay request codecs, conflicting/experimental root combat paths, incomplete item operations, disabled natural monster spawn loading, empty respawn/AI behavior, and incomplete quest/trade flows. None may be filled with inferred mechanics.
