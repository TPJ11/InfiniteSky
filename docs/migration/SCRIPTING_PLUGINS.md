# Scripting and Plugins

This index was replaced by the Prompt 17 source-backed audit. The original Node.js runtime does use VMScript loaders, but its root implementation is shared-process JavaScript execution against mutable globals—not a general plugin contract. FruitSalad is a separate experimental process/RPC tree.

- [Compatibility matrix](SCRIPTING_COMPATIBILITY_MATRIX.md)
- [Engine and migration decision](SCRIPTING_ENGINE_DECISION.md)
- [Implementation status and blockers](SCRIPTING_IMPLEMENTATION.md)
- [Plugin/script lifecycle](PLUGIN_LIFECYCLE.md)
- [Root event inventory](SCRIPT_EVENT_SYSTEM.md)
- [Gameplay integration boundaries](SCRIPTING_GAMEPLAY_INTEGRATION.md)
- [Trust and security model](SCRIPTING_SECURITY_MODEL.md)

No scripting runtime, plugin API, script event bus, or legacy JavaScript compatibility is implemented in the .NET host. The feature statuses and exact blockers are recorded in the compatibility matrix and [feature parity matrix](../FEATURE_PARITY_MATRIX.md).
