# Scripting Engine and Migration Decision

## Decision

Use **native C# migration (strategy A)** for server-owned behavior. Do not embed a JavaScript engine, spawn a Node compatibility worker, or create a general plugin framework in this stage. No scripts or plugins are claimed migrated, and active VMScript-backed source modules remain explicitly blocked until their service dependencies and observable behavior can be migrated and tested.

This is the smallest source-supported target: the root server's VMScript is not a stand-alone script API. It executes code against mutable process globals, Node modules and live server objects. Recreating that surface safely would first require a broad host/API port that the approved architecture and current gameplay/network/persistence stages have not completed. A C# plugin contract now would invent capabilities absent from the source.

This decision does **not** disable or modify the original Node.js server. It also does not claim that the .NET host can execute legacy scripts. The original source remains the behavioral reference; scripts that cannot yet be migrated remain `BLOCKED`, not silently skipped or treated as complete.

## Source requirements that constrain the choice

- Root startup creates VMScript loaders for `generic/`, `db/`, `cli/`, packet directories, the built-in `login.js`/`zone.js`/`world.js` modules, configured `config.plugins` filenames and, after `world_started`, `commands/`.
- Root scripts use `vm.runInThisContext`, global state, filesystem watchers and custom dependency declarations. `main.js`, `world.js`, `db.js`, packet handlers and command handlers directly access live server objects.
- Root plugin names are filenames, with no plugin ID/version/capability manifest. `config.json-dist` has an empty `plugins` array, and the repository has no root `plugins/` directory.
- `FruitSalad` is a distinct child-process/RPC implementation using BSON-framed calls. It does not provide the root runtime's shared object identity or execution contract.
- `InfiniteSky.Scripting.csproj` references `InfiniteSky.Application` but contains no implementation source, scripting package dependency or scripting tests.

See [SCRIPTING_COMPATIBILITY_MATRIX.md](SCRIPTING_COMPATIBILITY_MATRIX.md) and [SCRIPTING_IMPLEMENTATION.md](SCRIPTING_IMPLEMENTATION.md) for the module inventory and current blockers.

## Strategy comparison

| Strategy | Behavioral compatibility | Node.js APIs / modules | Async, globals and state | Security and resource model | .NET 10 and operations | Decision |
|---|---|---|---|---|---|---|
| Native C# migration | High when an individual source behavior is understood and ported with source-derived tests; does not execute arbitrary legacy JS. | Reimplemented only through explicit C# service contracts; no automatic `require`, filesystem, timers, `Buffer`, or native modules. | Uses existing DI/application/world ownership; behavior must be migrated deliberately rather than relying on JS coercion, closures or shared globals. | Strongest opportunity for narrow capabilities and normal host cancellation; no dynamic code execution. | Fits the existing .NET 10 projects, tests, logging and Generic Host without native JS runtime deployment. | **Selected** for root server behavior; each unported script remains a blocker. |
| Embedded managed JavaScript engine (for example, Jint) | JavaScript language compatibility depends on engine/version; not equivalent to root Node's full runtime or VMScript global wiring. | Node `require`, filesystem, process, timers, sockets, Mongo and native modules would need explicit host adapters. | Promises/callbacks and object identity require designed bridges; host objects would expose C# state and semantics, not Node objects. | Embedding is not a sandbox. CPU/memory/host-object exposure need explicit limits and testing. | Adds a runtime dependency and custom adapter/deployment work; no current package or compatibility tests exist. | Not selected: does not solve the observed Node.js dependency surface. |
| Embedded V8 (for example, ClearScript) | Strong language execution potential, but still not the Node runtime or the root process's existing live object graph. | V8 does not automatically provide Node `require`, filesystem, timers, `Buffer`, native modules or package resolution. | Requires host bridges and lifecycle integration; reentrancy/thread-affinity and async semantics must be tested. | Native engine presence does not imply isolation; process-level resource constraints remain necessary. | Adds native V8 binaries, architecture/runtime packaging and servicing obligations. | Not selected: higher deployment burden without the required Node compatibility. |
| Separate Node.js worker | Runs the actual Node engine and APIs if the correct Node/runtime/packages are deployed. | Broadest Node support, but only inside the worker. | A process boundary replaces root shared globals/closures/live object references with serialization and RPC; callback timing, ordering and mutation semantics change. | Process isolation can bound capabilities only with OS policy; a message protocol is still required. | Requires Node distribution, version/dependency management, worker supervision and cross-process diagnostics. | Not selected for root VMScript; FruitSalad's BSON RPC is evidence of a separate design, not proof of root compatibility. |
| Hybrid C# host plus JavaScript runtime | Can retain selected scripts only after deciding a stable interop boundary; does not transparently preserve the current shared VM. | Requires choosing and implementing the host APIs/scripts allowed to use. | Two state models and event loops introduce ordering, lifetime and transaction risks. | Capabilities can be narrowed at the bridge, but a general service locator or ambient host object would be unsafe. | Highest complexity and duplicated operational/testing surface. | Deferred unless an identified source script cannot reasonably be migrated to C# and has a testable boundary. |
| Data-driven replacement | Exact only when the original is declarative data rather than executable logic. | None required for declarative data; not applicable to JS handlers. | Does not preserve callbacks, side effects or mutable state unless separately modeled. | Small surface if schemas are constrained. | Low runtime burden, but requires source evidence that the file is declarative. | Use only per data set after proving the source behavior is data-only. |

## JavaScript and Node semantics

No engine was added or tested against representative legacy scripts in this stage. In particular, no compatibility claim is made for CommonJS module caching/resolution, `require()` relative paths, `Buffer`, timers, process APIs, callbacks, promises, `this`, closures, `undefined`/`null`, truthiness, number coercion, serialization, or VMScript's prototype/transfer reload behavior.

The root VMScript implementation uses a shared `vm.runInThisContext` execution path and explicitly maintains global script bookkeeping. The alternative FruitSalad loader injects `global.require`, parses `.json` and `.js`, and uses separate process globals. These are different source runtimes; combining their behavior would be speculative.

## Implementation boundary

1. Port a source-verified behavior into its owning existing C# project/service.
2. Keep packet, persistence, world, gameplay and command boundaries typed and explicit.
3. Use a structured data representation only when the original behavior is demonstrably declarative.
4. If a future script is proven to require live JavaScript, write a separate decision for that script and its exact host API, lifecycle, trust level, resource bounds and tests before selecting an engine.
5. Do not add hot reload, arbitrary evaluation, a plugin marketplace, or a generic package/dependency resolver without source-backed requirements and explicit approval.

## Known blockers

- No .NET script API or script consumers exist in the Scripting project.
- C# equivalents for many original globals (packet registries, game data, account/character database models, world/player objects and GM command API) are incomplete.
- Root command scripts are Prompt 18 scope; gameplay and packet modules remain incomplete under Prompt 16.
- No source-backed external plugin set exists to validate plugin compatibility.
- Root zone callbacks have no connected root script loader; the alternate Nangin script does not register its schedule.

Accordingly, no runtime code, scripting package, test fixture, configuration option, or new extension API was added. This prevents a false compatibility claim and leaves every unsupported behavior visible in the matrix.

## References consulted

- [Node.js `vm` documentation](https://nodejs.org/api/vm.html): `vm` contexts and `runInThisContext`; the documentation explicitly warns that `node:vm` is not a security mechanism.
- [Jint project](https://github.com/sebastienros/jint): a JavaScript interpreter for .NET; it does not by itself supply the server's Node.js modules or root VMScript host contract.
- [ClearScript project](https://github.com/ClearFoundry/ClearScript): .NET scripting integration with V8; its V8 engine does not by itself supply Node.js `require`, process APIs, or this server's globals.

No performance benchmark, runtime prototype, or representative-script compatibility test was performed.
