# Gameplay Migration

## Scope and matrix coverage

`GP-001` through `GP-038`: creation, progression, stats, movement, combat, skills, buffs, inventory/equipment, drops, spawn/respawn, quests, NPC interactions, parties, social systems, commerce/events, and mail.

## Source files

`generic/new_character_config.js`, `generic/statcalculation.js`, `generic/battle_functions.js`, `generic/AI.js`, `world.js`, `zone.js`, all gameplay packet handlers, `commands/*.js`.

## Target components

Domain calculators plus World services for movement, combat, skills, equipment, inventory, loot, spawn, death, interaction, social, event, and commerce.

## Dependencies

Depends on domain models, game data, zone/entity actors, protocol codecs, persistence queue, and compatibility profiles. `GP-001`/`GP-003` are login critical path; combat, inventory, and transfer are P0 acceptance categories.

## Implementation order

1. Character creation/selection/entry.
2. Progression and stat calculation.
3. Basic movement/state projection.
4. Inventory/equipment/item use.
5. drops and deterministic loot.
6. combat and experience.
7. skills/buffs.
8. monster spawn/respawn.
9. player death/respawn.
10. party/chat/trade/duel/guild/friends.
11. NPC commerce/interaction.
12. event and random-item systems.
13. quests only after protocol/data evidence.

## Tests

- golden stat/EXP vectors;
- deterministic random combat/loot;
- slot/capacity/currency conservation;
- atomic trade/inventory operations;
- skill cooldown/cost/expiry;
- spawn and death fake-time;
- social disconnect/transfer;
- event state and reward idempotency;
- full packet-to-persistence gameplay transcripts.

## Acceptance criteria

### Character/progression

- Three clan templates match starting data.
- Three-slot character list/create/delete works.
- Level cap 145 and point bands match fixtures.

### Combat/world

- Both compatibility profiles produce documented, reproducible outputs.
- Every state mutation is actor-owned.
- Death/spawn/drop flows cannot produce negative HP/quantity.

### Inventory/economy

- No item or silver duplication/loss across pickup/equip/trade/storage/bank/reconnect.
- Capacity and stack limits are tested.

### Skills/social/events

- Active packet families work; unimplemented families remain explicitly stubbed/blocked.
- Parties/guilds/trades clean up on disconnect and transfer.

## Risks

Combat registrations overlap, monster damage and XP logic contains defects, unknown monster drop fields drive behavior, and player/monster respawn is incomplete. Quests and generic NPC interaction have insufficient evidence. These cannot be called parity-complete without client/capture evidence.
