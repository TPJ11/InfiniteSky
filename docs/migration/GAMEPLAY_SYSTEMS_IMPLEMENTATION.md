# Gameplay Systems Implementation and Source Audit

## Runtime distinction

The root `main.js` loads `login.js`, `zone.js`, and `world.js`, and `world.js` loads root `packets/world`. This is the primary Node.js reference. `FruitSalad/main.js` launches a separate set of RPC workers from `FruitSalad/Config/processes.json`; it contains another `Processes/Zone/Packets` implementation. Those files are recorded as alternate evidence, not merged into primary behavior.

The .NET solution entering Prompt 16 contains character/item/skill/monster/NPC data models, the protocol framing foundation (including a 56-byte attack reply structure), world/zone/spatial lifecycle, and Prompt 15 player activation. It has no gameplay packet handlers, full character/world state codec, game-information image catalog, gameplay repositories, or gameplay simulation service.

## Source-backed feature findings

The detailed per-feature dependency, complexity, planned-order, testing, and migration-status register is [GAMEPLAY_IMPLEMENTATION_PLAN.md](./GAMEPLAY_IMPLEMENTATION_PLAN.md). Current findings:

| Area | Original implementation | .NET 10 result this stage |
|---|---|---|
| Character creation/selection/world entry | Root login/world handlers are present but rely on legacy data, large codecs and old persistence behavior. | No client workflow added; Prompt 15 internal activation is not protocol integration. |
| Progression | `world.js` has an active `giveEXP` method; it reads `infos.Exp`, increments level/points, caps at 145, refills vitals on level-up, sends updates and saves. The monster attack caller found in `action.js` is in a dormant handler body; the method may also be invoked through commands. `generic/Info_Exp.js` declares a 27-field external record, including two unnamed values. | `LegacyExperienceAwarder` implements only the isolated arithmetic and level/point update. No catalog, stat recalculation, vitals refill, packet, save, or network integration. `GP-004` remains `IN_PROGRESS`. |
| Stats/equipment | `generic/statcalculation.js` has an active calculator and a deprecated duplicate; its level update delegates to `updateAll`, skill update is empty, and several item/skill behavior comments remain. `statpoints.js` handles keys 0-7. | `LegacyStatPointAllocator` implements only key-to-attribute/point arithmetic. No derived calculations, stat packet handler, reset path, persistence, or 0x84 response. `GP-005` remains `IN_PROGRESS`. |
| Movement | `newAction.js` accepts client coordinates, copies them into character state, and broadcasts state; heartbeat/action paths have a TODO for server-side movement comparison. | No movement packet codec/handler; Prompt 15 exposes only internal spatial position updates. |
| Combat | `generic/battle_functions.js` and `packets/world/action.js` contain experimental PvP/PvM handler bodies, but the root `action.js` 0x03-0x05 packet registrations are commented out. Active `newAction.js` checks an attack target/distance and broadcasts state; it does not apply damage. The dormant PvM body double-subtracts HP and immediately heals on kill. | No combat logic migrated. Root active damage dispatch is not established; the reply structure alone does not provide attack handling. |
| Skills/buffs | Root handlers accept use/state packets and interpolate some modifiers into hard-coded buff slots; 0x7A handler has no expiration work. | No skill behavior migrated. Definitions/data assets are not available to establish effect parity. |
| Inventory/equipment/items | Root `itemactions.js` contains many action handlers, but has TODO validation and incomplete operations; `Info_Item.js` contains some recovery cases. Action 0x0C nevertheless sets HP/Chi to max and sends a failed reply. | Domain data shapes exist; gameplay operations and persistence remain unmigrated. |
| NPCs/monsters | NPC definitions and runtime packet structures exist. Root NPC spawn loading is active; `.MOB` loading in `zone.js` is commented. Monster AI defaults to a log-and-stop callback; monster death/drop behavior is incomplete. | Prompt 15 currently supports player entities only; no NPC/monster gameplay actors. |
| Quests | Root `packets/world/quest.js` contains a packet capture comment only. FruitSalad has a partial separate implementation with explicit missing validation/rewards. | No quest code. Quest protocol/data remain blocked. |
| Social/economy | Party, duel, guild, friends, chat, shops, trade and event handlers vary from partial to stub; root trade settlement is commented. | No gameplay service migration. |
| Timers/simulation | `GameStep.js` emits a nominal 100 ms callback; world comments state server-side simulation is TODO. Some entity/status timers are local packet/interval code. | No gameplay tick handlers or entity simulation added. |

## Work implemented

`src/InfiniteSky.Domain/Progression/LegacyExperienceAward.cs` provides an immutable calculation over `CharacterProgression` and supplied `ExperienceDefinition` records. It preserves positive-award handling, level 145 cap, threshold arithmetic, skill-point additions from the next-level record, the source stat-point bands, and the source's increment-before-missing-record behavior. `LegacyStatPointAllocator` maps source keys 0-7, overrides the client amount with one/five points, and checks available points. Tests use synthetic values; no original `005_00001.IMG` data was present for golden-value comparison.

No client-visible gameplay flow is operational from the .NET host as a result of this change. Neither calculation is called by a handler, repository, or host registration.

## Validation evidence

- `dotnet restore InfiniteSky.sln`: passed.
- `dotnet build InfiniteSky.sln --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test InfiniteSky.sln --no-build --no-restore`: 178 passed, 0 failed, 2 opt-in MongoDB integration tests skipped.
- `dotnet format InfiniteSky.sln --verify-no-changes --no-restore`: passed.
- New gameplay tests are isolated Domain unit tests over synthetic values. No gameplay packet integration, source game-data comparison, real MongoDB gameplay integration, or original-client test was available or claimed.

## Limitations

- The root `data` tree contains `.MOB`/`.NPC` spawn files, `.pac` packet data and translation CSVs, but not the `005_00001.IMG` experience information used by the original runtime or the item/skill/monster images required for calculations.
- Root combat handler registrations contain legacy/experimental branches and are not a single coherent ruleset.
- Missing scripts, assets and packet meanings are documented, not replaced with guessed behavior.
- Nothing in this stage implements Prompt 17 scripting/plugins or Prompt 18 administration.

See [GAMEPLAY_PROTOCOL_COMPATIBILITY.md](./GAMEPLAY_PROTOCOL_COMPATIBILITY.md) before treating any gameplay behavior as client compatible.
