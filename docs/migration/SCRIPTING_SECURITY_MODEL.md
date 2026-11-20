# Scripting Security and Trust Model

## Original trust boundary

The root server treats its scripts as trusted server-owned code. VMScript evaluates matching `.js` files in the server process using `vm.runInThisContext`, after making loader/module state global. This is not a separate VM context or a security sandbox. Scripts receive access to mutable globals and can call the same server services, sockets, database models, filesystem and process APIs available to the Node process.

Node's [`vm` documentation](https://nodejs.org/api/vm.html) explicitly states that the module is not a security mechanism and should not be used to run untrusted code.

`sandbox.js` exports an empty object, but the `WorldPC` construction passes it as a third argument to `PacketCollection`; that constructor has two declared parameters and does not pass the value to VMScript. It therefore supplies no sandbox to packet scripts.

The local CLI directly evaluates JavaScript and dispatches `#` prefixed input to `child_process.exec`. This is a trusted operator/developer interface, not an extension security boundary. The root plugin filename list has no signature, publisher, allow-list, capability or resource metadata.

FruitSalad also executes trusted code in each child process. The process split is a partial OS boundary, not a script sandbox: its RPC APIs expose functions and serialized arguments, and no OS-level isolation policy was found in the repository.

## .NET trust decision

The .NET host will not execute arbitrary JavaScript or load third-party extensions in this stage. No claim is made that an embedded runtime, `CancellationToken`, separate project or process automatically sandboxes scripts or forcibly stops a synchronous infinite loop.

No unrestricted dynamic execution, shell escape, plugin API, filesystem grant or database service locator has been added. This is consistent with the source inventory and avoids exposing unfinished C# gameplay, persistence or network objects.

## Risks retained for future work

- Legacy script changes can mutate live process state and cause client-visible changes without restart.
- The root VMScript reload/unload path has no complete rollback, subscription or timer cleanup contract.
- A syntax/load error can be logged without preventing the loader's completion counter from advancing.
- In-process scripts have the server process's privileges and can retain players, sockets, timers or database objects.
- Directory watchers and source paths are not portable as written; several paths concatenate a literal backslash.
- Porting arbitrary `eval` or a plugin loader would expand attack surface and is not justified by the empty configured plugin list.

If a future requirement calls for untrusted extensions, specify and test OS/process isolation, CPU/memory limits, allowed host capabilities, logging/redaction, update trust, shutdown and cancellation before choosing a runtime. Do not describe a language engine or API wrapper as a sandbox without evidence.
