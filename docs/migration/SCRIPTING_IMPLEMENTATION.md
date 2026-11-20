# Scripting Implementation Status

## Result

Prompt 17 performed a source audit and chose native C# migration for server-owned behavior. It did **not** add a JavaScript engine, script discovery/execution service, plugin loader, event bus, script context or timer scheduler. The existing `InfiniteSky.Scripting` project contains only its project file and an `InfiniteSky.Application` reference; no C# scripting implementation or tests are present.

No executable implementation can be added without inventing a host API or changing the root shared-VM contract. The source-backed blockers and strategy are recorded in [SCRIPTING_ENGINE_DECISION.md](SCRIPTING_ENGINE_DECISION.md). This is an explicit blocked outcome, not a claim that scripting is complete.

## Root runtime modules

The root `main.js` creates shared VMScript loaders for:

- `generic/` during bootstrap;
- `db/` after Mongoose connects;
- `cli/` when the CLI is constructed;
- `packets/login/` and `packets/world/` through `PacketCollection`;
- the built-in `login.js`, `zone.js`, and `world.js`, then any filenames in `config.plugins`, after game-info loading;
- `commands/` after `world_started`.

VMScript reads matching files and watches directories/files. Its dependency mechanism calls module bodies after global dependency names become available. Source modules expect mutable globals and live sockets/database/world/game-data objects. This is a root Node application architecture, not a standalone plugin interface.

The distributed `config.json-dist` sets `plugins` to an empty array. There is no tracked root `plugins/` directory. README references to Dropbox/GitHub/Google Drive integrations do not correspond to checked-in plugin implementations.

## Alternate runtime

`FruitSalad/main.js` runs a separate child-process supervisor. Its process list includes Login, World, SpawnLogger, SpawnEdit and ItemEditor. Children expose API functions over a custom length-prefixed BSON stream. `FruitSalad/VMScript.js` is a separate singleton watcher/loader; it explicitly sets `global.require` and supports `.js` and `.json` file handling. This is experimental/alternate evidence and is not the root script contract.

The alternate Zone process can watch zone-specific script directories. Its only checked-in map script, NanginTrack, logs that it is not implemented and leaves the schedule registration commented. Its function bodies therefore do not establish active race behavior.

## Lifecycle, event, context and timer support

| Capability | .NET status | Evidence / limitation |
|---|---|---|
| Discovery and loading | BLOCKED | The root loader enumerates and evaluates JS against shared globals; the .NET host has no source-compatible runtime or script service contract. |
| Reload/unload | BLOCKED | File watchers re-run code but do not provide reliable teardown of registered callbacks, global state or timers. |
| Plugin registration/dependencies | BLOCKED | Root has filename configuration and `vms.depends` global-name dependencies, not plugin metadata/version/capability declarations. |
| Event hooks | NOT_STARTED | Root `main.events` events are application lifecycle events; the .NET application uses its own host lifecycle and has no script consumer. |
| Script context | BLOCKED | Legacy modules reach global services and mutate live application objects; there is no bounded source-compatible context. |
| Timers | NOT_STARTED | Root `GameStep` is world runtime infrastructure, not a script scheduler. No active script schedule definition was found. |
| Script persistence | BLOCKED | No script-owned persistent state schema was found; the root primarily relies on process-global mutable state and optional transfer callbacks. |
| Error isolation | BLOCKED | Root load/dependency errors are logged and often do not stop the loader; no rollback, forced cancellation or extension isolation contract exists. |
| Security boundary | BLOCKED | Root VM execution is trusted in-process code, not a sandbox. The empty `sandbox.js` is not used as a sandbox. |

See [SCRIPT_EVENT_SYSTEM.md](SCRIPT_EVENT_SYSTEM.md), [PLUGIN_LIFECYCLE.md](PLUGIN_LIFECYCLE.md) and [SCRIPTING_SECURITY_MODEL.md](SCRIPTING_SECURITY_MODEL.md).

## Gameplay and administration boundaries

Packet handlers and commands are loaded as JavaScript modules, but this does not establish a separate gameplay script API. Prompt 16 found packet/gameplay features with individual partial, dormant, stubbed and absent states. Prompt 17 did not migrate any such handler, change packet registration, or resolve those gameplay blockers.

GM command scripts are explicitly deferred to Prompt 18. No command handler, arbitrary CLI evaluation, shell execution, packet script, map script or event handler is reported as migrated.

## Configuration and deployment

The .NET host has no script-directory, plugin-directory, enabled-script, runtime-selection or hot-reload option. No production path, package, native runtime or secret was added. The original Node.js server remains the only runtime for its original VMScript modules; the .NET host does not execute them.

## Validation and status

This stage changes documentation and feature tracking only. There are no new scripting tests because no execution behavior was implemented. Validation completed: `dotnet restore InfiniteSky.sln` passed; `dotnet build InfiniteSky.sln --no-restore` passed with 0 warnings/errors; `dotnet test InfiniteSky.sln --no-build --no-restore` passed with 178 tests and 2 opt-in MongoDB tests skipped; `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` and `git diff --check` passed. No original Node.js file is modified.
