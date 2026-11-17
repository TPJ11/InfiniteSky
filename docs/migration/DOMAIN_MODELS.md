# Domain Model Migration

## Scope and matrix coverage

`DOM-001` through `DOM-015`: account, character, positions, inventory, equipment, guild, progression, stats, skills, buffs, combat, entity definitions, and ephemeral social sessions.

## Source files

`db/account.js`, `db/character.js`, `generic/CharacterState.js`, `generic/CVec3.js`, `generic/statcalculation.js`, `generic/battle_functions.js`, `generic/Item.js`, `generic/Monster.js`, `generic/Npc.js`, `generic/Info_*.js`, party/trade/duel scripts.

## Target components

All persistent and runtime-independent models in `InfiniteSky.Domain`: identities, value objects, aggregates, calculators, results, and pure state transitions.

## Dependencies

Depends on game-data schema definition. Domain precedes persistence mappers and world actor implementation.

## Implementation order

1. IDs, vectors, amounts, enum/value types.
2. Account/Character persistence models.
3. Equipment/inventory/skills.
4. Guild/social models.
5. Definitions for items/skills/monsters/NPCs.
6. Progression and stat calculator.
7. Buff/combat/loot results.
8. Legacy compatibility and corrected rule strategies.

## Tests

Pure tests must cover:

- clan start templates and bounds;
- progression at every threshold/point band;
- item/enchant/combine/pet stat vectors;
- all inventory action invariants;
- skill max/duplicate rules;
- buff overlap/expiration;
- seeded combat and damage bounds;
- model equality and immutability.

## Acceptance criteria

- No domain reference to Socket, TCP, BSON, hosting, or protocol buffers.
- Runtime actor state is not serialized as persistent character data.
- Every numeric mapping states signedness and wire precision.
- Legacy and corrected rules have explicit deterministic results.

## Risks

Level/EXP overflow, pet growth, item bonuses, and unknown monster fields may be numerically sensitive. Avoid decimal/float substitutions that differ from original 32-bit float and JavaScript arithmetic without golden vectors.
