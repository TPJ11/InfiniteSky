# Entities and Players Migration

## Scope and matrix coverage

`ENT-001` through `ENT-008`: player, state projection, HP/Chi, monster, NPC, item, entity IDs, and target resolution.

## Source files

`world.js`, `generic/CharacterState.js`, `generic/Monster.js`, `generic/Npc.js`, `generic/Item.js`, `generic/AI.js`, `zone.js`, action packet handlers.

## Target components

`PlayerActor`, `MonsterActor`, `NpcActor`, `WorldItemActor`, `PlayerStateProjector`, `VitalitySystem`, `EntityIdAllocator`, `TargetResolver`.

## Dependencies

Depends on domain models, zone actor/index, packet state codecs, and stat calculator.

## Implementation order

1. Entity ID allocator.
2. Player actor and connection binding.
3. State projection.
4. World item ownership/expiry.
5. NPC actor.
6. Monster actor.
7. vitality/death lifecycle.
8. target resolution and disconnection cleanup.

## Tests

- add/remove/reconnect ownership;
- exact state packet fields;
- HP/Chi cap/regeneration;
- target type/zone/range;
- item protection and expiry;
- NPC broadcast cadence;
- monster death and clear/reset;
- entity ID reuse/generation behavior.

## Acceptance criteria

- Each entity has one zone owner and a stable client-visible ID mapping.
- Disconnect and transfer remove/transfer state without stale ownership.
- Packet projection includes all known fields and reserved placeholders.
- Runtime HP/Chi, position, and target state cannot mutate persistence by aliases.

## Risks

Legacy code has inconsistent plural arrays, `onDelete` method names, target fields, and monsters using undefined expected fields. ID lifetime/reuse must be validated against client behavior.
