# Movement and Combat

## Movement source behavior

The root `packets/world/newAction.js` registers 0x03, 0x04 and 0x05 over the shared `WorldPC.ActionPacket` structure:

- 0x03 updates persisted-in-memory `RealX/Y/Z`; its server-side comparison is TODO.
- 0x04 copies frame, stance, skill, facing/direction and location into character state and rebroadcasts a state packet to area clients. It updates the saved character coordinates.
- 0x05 also copies `Location`, `LocationTo`, `LocationNew`, and target IDs, then uses action/skill cases and area replication. Server-side movement comparison is still TODO.

The source therefore trusts client-reported positions. It does not establish a movement speed limit, collision check, authoritative simulation, or anti-cheat threshold. The root `world.js` also contains a TODO for server-side simulation. `FruitSalad/Processes/Zone/Packets/action.js` is another implementation with additional TODOs for movement simulation and focus updates; it is not the root path.

## Combat source status

The root `packets/world/action.js` contains an experimental combat handler, but its 0x03-0x05 `WorldPC.Set` registrations at the end of the file are commented out. The active `packets/world/newAction.js` 0x05 handler resolves the target node and computes/logs distance for skills 5-7; it does not apply damage. Therefore the following combat calculations describe dormant code, not an established active root-client behavior.

`generic/battle_functions.js` defines:

```text
Hurt = (((Math.random() * 0.15) - 0.05) * attackerDamage)
       + (attackerDamage - defenderDefense)
```

Its `calculate` helper floors the resulting damage into `TotalDamage` and `DamageHP`, sets `Deadly` to 1, and copies elemental values. The dormant `packets/world/action.js` body calls it in the client-versus-client branch, subtracts `DamageHP` from current HP without a visible lower clamp, and constructs a compressed 0x2C broadcast. No active root dispatch to that body was found.

The dormant monster branch in the same handler uses a different roll: `floor(random * (1.5 * Damage - Damage) + Damage)`, subtracts `monster.info.DefensePower`, clamps the result at zero, awards XP once, and records the attacker. It then subtracts the same damage a second time from monster HP. When HP drops below zero it clamps and immediately restores full health; item drops are commented out. It does not call `Monster.onDeath` as a working death/respawn flow.

These are not a complete or balanced combat system. `generic/battle_functions.js` is not used by the dormant PvM branch, hit/dodge/critical/elemental resolution is not established by the available source, and other registrations overlap. No corresponding C# combat rules or handlers are implemented.

## Migration boundary and tests

Prompt 15 `PlayerManager.TryUpdatePosition` updates spatial membership, but is not a network movement service. There is no source-complete C# action request codec or controlled-client transcript in this stage. Consequently there are no compatibility claims for client movement, broadcasts, attack eligibility, damage, HP changes or combat packets. GP-007..011 remain `NOT_STARTED`.

Do not add server-authoritative validation or alter the legacy damage defects by default. Any correction requires an explicitly distinct profile and source/client evidence.
