# Administrative Authorization

## Legacy authorization source

The root account model declares numeric `Level` with a default of 0. `GMCommand.Execute` checks `client.account.Level < command.Level` before callback invocation and returns `account.Level too low for this command` on denial. Each command embeds its own minimum numeric threshold; the source does not define named roles, inheritance, or a centralized role table. `commands/commands.js` uses the same account field to filter the help listing.

The character model separately stores `IsGM`, and the character wire struct contains a 32-bit `isGM` field. The command dispatcher does not use either field. These fields are not evidence of equivalent authority and must not replace the account-level check.

The root login model loads an account document containing `Level`, but the current .NET authentication projection intentionally only selects `_id`, `Username`, and `Password`. `AuthenticatedSession` therefore has no privilege value, and `PlayerRuntime` has no verified account authorization state. C# `Account.Level` exists as a domain property but is not populated by the authentication path.

Sources: [account.js](../../db/account.js#L12-L20), [character.js](../../db/character.js#L48-L63), [structs.js](../../generic/structs.js#L142-L149), [GMCommand.js](../../GMCommand.js#L48-L73), [commands.js](../../commands/commands.js#L8-L33), [MongoAccountAuthenticationRepository.cs](../../src/InfiniteSky.Persistence/MongoAccountAuthenticationRepository.cs#L32-L62), [AuthenticatedSession.cs](../../src/InfiniteSky.Application/Authentication/AuthenticatedSession.cs), [Account.cs](../../src/InfiniteSky.Domain/Accounts/Account.cs).

## Compatibility and security findings

- The root checker is server-side and compares a stored account property; no client-supplied `Level` parameter is read by the dispatcher.
- The dispatcher does not perform its own authentication/null-account check before reading `client.account.Level`; it relies on the packet/socket lifecycle to supply an account-bearing client.
- Most important: several active root commands require level 0 and still mutate gameplay state or send arbitrary packet data. Examples include `/xp`, `/inventory clean|fill`, `/state`, `/clearskills`, `/send`, `/upgrade`, `/hit`, and `/refill`. This is source behavior, not an endorsement or a .NET permission policy.
- `commands/makeaccount.js` compares the requested new level to the actor level but does not validate the requested value as an integer within the CLI’s 0–100 bounds. The local CLI `createaccount` does validate that range.
- At level 80+, callback exceptions are returned to the caller as `ex.toString()`, potentially exposing implementation details.
- `systemAdminIP`/`systemAdminPassword` support in `db/account.js` is commented; no active remote administrative authentication based on those fields was found.
- No moderation privilege hierarchy or per-command policy beyond numeric minimum levels was discovered.

## .NET status

There is no .NET command authorization service or world chat command execution path. Prompt 18 did not infer a role system or map `IsGM` to account `Level`. Every game command remains `BLOCKED` in [ADMIN_COMMAND_COMPATIBILITY_MATRIX.md](./ADMIN_COMMAND_COMPATIBILITY_MATRIX.md).

Before any handler is enabled, the server needs:

1. A source-compatible, server-derived account-level value in the authenticated session boundary, loaded from the account document with BSON compatibility tests.
2. World connection activation that proves the login session owns the active character/connection.
3. Authorization on every dispatch before command-specific lookup or mutation.
4. Tests for unauthenticated, stale-session, missing-level, boundary-level, and forged-client-field cases.
5. A separately documented policy for source commands that expose high-impact mutations at low levels. Do not silently change the legacy threshold; gate any security correction and list the compatibility difference.
