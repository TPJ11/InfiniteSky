# Scripting and Gameplay Integration

## Root server

Root VMScript loads packet handler files, shared `generic/` modules and GM command files. Those modules directly implement or call protocol, gameplay, world, database and player behavior; they are not independent content scripts with a stable NPC/quest/item/skill API.

| Source family | Actual role | Prompt 17 result |
|---|---|---|
| `packets/login/*.js` | Login packet handler modules registered by `PacketCollection`. | No handler port, protocol change or login behavior claimed. |
| `packets/world/*.js` | World packet modules for gameplay/social operations and server events. Their completeness varies; active registration is distinct from commented or stub bodies. | No packet or gameplay integration claimed. Continue tracking each feature under `PRO-*` and `GP-*`. |
| `generic/*.js` | Shared game data, packet structures, entities, stat and helper modules. | No generic JavaScript execution or duplicate gameplay rule layer added. |
| `commands/*.js` | Operator/GM command modules. | Explicitly deferred to Prompt 18. |
| `zones/1.js` | CommonJS module that defines a map-script class and empty/example callbacks. | No root loader or attachment found; not active gameplay. |
| `FruitSalad/Processes/Zone/Scripts/NanginTrack/NanginTrack.js` | Alternate-process race prototype with commented scheduler registration and incomplete payout. | Original stub only; no race, timer or reward implemented. |

No source-backed root script set was found for quests, NPC dialogue, item-specific scripts, skill effect plugins, or monster AI plugins. Existing packet/data modules and the Prompt 16 audit remain authoritative; filenames or comments alone do not enable new gameplay behavior.

## Dependencies on Prompt 16

The gameplay gaps identified in Prompt 16 remain gaps. No missing Prompt 16 behavior became available simply because the source uses JavaScript files. The .NET progression arithmetic remains isolated, and gameplay, packet registration, data assets, persistence and world/entity wiring remain unchanged.

## Compatibility rule

For each future native migration, preserve the original module's actual trigger, validations, side effects, packet ordering and persistence effects. If behavior is only present in a dormant handler, commented scheduler, or unreferenced file, record that status instead of registering it as a new C# hook.
