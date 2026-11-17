# Scripting and Plugins Migration

## Scope and matrix coverage

`SCR-001` through `SCR-008`: VMScript, packet script registration, FruitSalad RPC/processes, plugins, admin command modules, map scripts, and safe capabilities.

## Source files

`vmscript/**`, `PacketCollection.js`, `sandbox.js`, `main.js`, `GMCommand.js`, `FruitSalad/VMScript.js`, `FruitSalad/Modules/rpc.js`, `FruitSalad/Processes/process.js`, `FruitSalad/Processes/Zone/Scripts/**`, plugins documented/referenced by README.

## Target components

`InfiniteSky.Scripting/ScriptHost`, `PacketExtensionHost`, `PluginLoader`, `ExtensionChannel`, `CommandModuleLoader`, and `ZoneScriptBridge`.

## Dependencies

Depends on isolated process/capability design, application command registry, and stable command/event DTOs. It is optional for core host startup.

## Implementation order

1. Inventory script APIs and side effects.
2. Port bundled/core handlers to C#.
3. Define typed event/command capability contracts.
4. Implement isolated script host and timeouts.
5. Add controlled command and zone script modules.
6. Port/reject external process/RPC capabilities.
7. Define plugin installation, reload, and audit.

## Tests

- script load/unload/reload;
- dependency order;
- command duplicate;
- cancellation and CPU/time limit;
- no file/network/process/global escape;
- core continuity after script failure;
- event DTO validation;
- plugin version/capability rejection.

## Acceptance criteria

- Core gameplay/login does not require scripting.
- No script can directly access sockets, globals, BSON documents, filesystem, or OS processes.
- Failure disables only the extension and is visible.
- Reload never mutates active core objects.
- Unsupported plugins receive a documented deviation or `NOT_APPLICABLE` decision.

## Risks

Legacy `vm.runInThisContext`, CLI eval, OS command execution, and shared globals are unsafe and non-isolatable. JavaScript scripts may require Node APIs, hot global prototypes, or FruitSalad-only packages. A parity script runtime is not the same as core feature parity.
