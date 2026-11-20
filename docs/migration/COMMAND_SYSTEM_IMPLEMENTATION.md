# Command System Implementation Status

## Result

Prompt 18 completed a source audit and recorded compatibility requirements, but added no executable command runtime or administrative command handlers. The command system is **BLOCKED**, not implemented: the .NET world listener has no chat packet handling, the current incoming packet sink handles login authentication only, and the session's account `Level` is not yet consumed by any command authorization path.

An unconnected parser/registry would not preserve observable command behavior, would not authorize real world clients, and could not send compatible responses. The authenticated session now retains account `Level`, but no speculative command framework or no-op handlers were added.

## Original root architecture

The root `main.js` creates `GMCommands` from `GMCommand.js`; after `world_started`, it creates a VMScript over the direct `commands/` directory. Each script registers a `Command` with a name, numeric account-level threshold, and callback. Alias registration creates another `Command`; duplicate-name registration removes the earlier entry and appends the replacement. VMScript watches and reloads files, but there is no matching unregister/teardown contract.

Normal chat `0x13`, faction chat `0x3E`, and whisper `0x09` each intercept messages beginning with `/`, strip exactly one leading slash, and invoke the same registry. The command name is lowercased and separated at the first literal space. The remainder is passed untouched. Unknown commands and insufficient privilege produce fixed system messages; callback errors are logged and can expose `exception.toString()` to level-80+ accounts.

The local Node REPL is a separate interface. It supports slash-prefixed CLI commands but also arbitrary JavaScript evaluation and `#` shell execution. These are not game-chat commands and are not suitable as implicit .NET host features.

## Current .NET dependencies

| Required boundary | Current state | Evidence |
|---|---|---|
| World chat ingress | `LegacyCorePacketRegistry.CreateWorld()` has no chat registrations for `0x13`, `0x3E`, or `0x09`. | [LegacyCorePacketRegistry.cs](../../src/InfiniteSky.Protocol/Packets/LegacyCorePacketRegistry.cs) |
| Packet dispatch | `NetworkHostedService` installs `AuthenticationIncomingPacketSink`; it handles only login-endpoint opcode `0x03`. Other world frames are not dispatched as gameplay. | [NetworkHostedService.cs](../../src/InfiniteSky.Server/Hosting/NetworkHostedService.cs), [AuthenticationIncomingPacketSink.cs](../../src/InfiniteSky.Server/Authentication/AuthenticationIncomingPacketSink.cs) |
| Authenticated privilege | The account auth projection reads `Level`; `AuthenticatedSession.AccountLevel` carries that server-derived value. It is not sourced from the client or character `IsGM`. | [AuthenticatedSession.cs](../../src/InfiniteSky.Application/Authentication/AuthenticatedSession.cs), [MongoAccountAuthenticationRepository.cs](../../src/InfiniteSky.Persistence/MongoAccountAuthenticationRepository.cs), [LegacyAccountAuthenticationDocumentMapper.cs](../../src/InfiniteSky.Persistence/LegacyAccountAuthenticationDocumentMapper.cs) |
| Correct privilege source | Legacy commands authorize `client.account.Level`; `Character.IsGM`/wire `isGM` is a different field and cannot be substituted. The value is available only through the authenticated session; `PlayerRuntime` does not duplicate it. | [GMCommand.js](../../GMCommand.js), [character.js](../../db/character.js), [MongoCharacterDocumentMapper.cs](../../src/InfiniteSky.Persistence/MongoCharacterDocumentMapper.cs), [AuthenticatedSession.cs](../../src/InfiniteSky.Application/Authentication/AuthenticatedSession.cs) |
| Command execution context | The .NET `PlayerRuntime` owns a character, session/connection IDs, zone, entity ID, and position. It has no command dispatch or response API and does not duplicate the privilege value carried by the authenticated session. | [PlayerRuntime.cs](../../src/InfiniteSky.World/Players/PlayerRuntime.cs), [AuthenticatedSession.cs](../../src/InfiniteSky.Application/Authentication/AuthenticatedSession.cs) |
| World response protocol | No source-compatible world chat command response writer or chat packet handler is registered. | [LegacyCorePacketRegistry.cs](../../src/InfiniteSky.Protocol/Packets/LegacyCorePacketRegistry.cs), [NetworkHostedService.cs](../../src/InfiniteSky.Server/Hosting/NetworkHostedService.cs) |
| Command effects | Teleport, spawn, combat, inventory, account maintenance, and mail handlers depend on legacy objects and services that are not yet present as equivalent application boundaries. | [ADMIN_COMMAND_COMPATIBILITY_MATRIX.md](./ADMIN_COMMAND_COMPATIBILITY_MATRIX.md) |
| Script-defined registration | Prompt 17 did not add a compatible script runtime or command-registration boundary. | [SCRIPTING_IMPLEMENTATION.md](./SCRIPTING_IMPLEMENTATION.md) |

## Target design gate

The source supports a small command registry, not a marketplace or general-purpose plugin framework. Once the dependencies above exist, the minimum target should be:

1. A typed parser that preserves the root command-name casing, first-space split, untouched argument remainder, and exact slash/chat fallback rules.
2. A registry keyed by the source command name/alias, with explicit duplicate behavior and registration ownership.
3. Authorization using a server-derived account privilege carried by the authenticated session; never a packet or character-supplied GM flag.
4. A command context limited to authenticated actor, active player/character, invocation channel, parsed arguments, cancellation, and source-required services.
5. Typed results translated into the verified legacy system-message or packet output.
6. Structured logs for command ID, actor/target IDs, authorization/execution result, and correlation ID, without raw secrets or unbounded arguments.
7. Specific handlers only after their world, gameplay, persistence, and packet dependencies are implemented and testable.

No static catalog of no-op commands should be registered to imitate legacy discoverability. The source compatibility inventory is [ADMIN_COMMAND_COMPATIBILITY_MATRIX.md](./ADMIN_COMMAND_COMPATIBILITY_MATRIX.md); all migrated behavior remains unverified until real dispatch, authorization, outputs, and effects are tested.

## Validation and status

No command parser or handler tests are claimed because no command execution code was added. The full solution validation and documentation-only checks for Prompt 18 are reported in [MIGRATION_STATUS.md](../../MIGRATION_STATUS.md).
