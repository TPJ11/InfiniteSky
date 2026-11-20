# Scripting and Plugins

## .NET runtime

The .NET server does not load or execute original Node.js VMScript modules and does not provide a JavaScript engine, generic plugin loader, hot reload, script API, event bus for plugins, or script sandbox. The `InfiniteSky.Scripting` project is a project boundary only. No script execution tests are claimed.

## Legacy reference and trust model

The root Node.js runtime executes trusted code in a shared process/global context and exposes live server services; it is not a security sandbox. FruitSalad is a separate child-process/RPC runtime and is not behaviorally interchangeable. Source loaders, hooks, events, timers, configured module filenames, and stub/disabled modules are inventoried in [SCRIPTING_COMPATIBILITY_MATRIX.md](migration/SCRIPTING_COMPATIBILITY_MATRIX.md).

Do not copy legacy JavaScript eval, shell execution, ambient globals, or plugin paths into the .NET candidate. Individual source-verified behavior must be migrated into owned C# services and tested against its actual contract. No unsupported Node API compatibility is claimed.

Required references: [SCRIPTING_ENGINE_DECISION.md](migration/SCRIPTING_ENGINE_DECISION.md), [PLUGIN_LIFECYCLE.md](migration/PLUGIN_LIFECYCLE.md), [SCRIPT_EVENT_SYSTEM.md](migration/SCRIPT_EVENT_SYSTEM.md), and [SCRIPTING_SECURITY_MODEL.md](migration/SCRIPTING_SECURITY_MODEL.md).
