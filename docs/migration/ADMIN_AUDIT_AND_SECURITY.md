# Administrative Audit and Security Status

## Legacy controls

- Game commands use account `Level` thresholds in the server-side `GMCommand.Execute` path, but the active inventory includes high-impact level-0 commands. The source thresholds are compatibility evidence, not a safe default policy.
- `Character.IsGM` is not the field used by the command dispatcher.
- Console operations are process-local but include JavaScript and shell evaluation. No additional confirmation, allowlist, command audit, or sandbox boundary is implemented in those paths.
- `send` can write a file-backed `.pac` payload directly to a client and has a one-byte hex fallback.
- `sendmail` sends account/character identifiers and the user-provided body through the configured email transport.
- `makeaccount` and `importaccounts` perform direct database writes. `importaccounts` reads a local JSON file and assigns imported accounts a fixed level 60.
- Command failures are written through `dumpError`; level-80+ callers receive the exception string. Raw commands/arguments are not recorded through a structured audit sink.
- No durable command, moderation, or authorization audit record was identified.

The complete source-derived behavior is recorded in [ADMIN_COMMAND_COMPATIBILITY_MATRIX.md](./ADMIN_COMMAND_COMPATIBILITY_MATRIX.md) and authorization findings in [ADMIN_AUTHORIZATION.md](./ADMIN_AUTHORIZATION.md).

## Deliberate Prompt 18 restrictions

No command was made executable in .NET because the world packet path, authoritative account-level session state, and command-specific application services are absent. This prevents accidental exposure of commands through a listener that cannot yet enforce source permissions.

The following legacy capabilities are not carried forward implicitly:

- arbitrary JS evaluation through `eval` or a REPL;
- shell execution via `#`;
- raw `.pac` file injection;
- unbounded operator or game-chat input;
- exception text sent to clients;
- direct mutation of persistence documents from command handlers.

These are intentional restrictions, not claims of feature parity. The legacy Node files remain unchanged.

## Future minimum safeguards

Before enabling administrative behavior:

1. Derive actor privilege from persisted account data and bind it to a current authenticated session; never accept client-supplied privilege values.
2. Require an authenticated active player for game-chat commands and an explicit local operator boundary for console commands.
3. Apply per-command authorization before target lookup or side effects.
4. Use bounded parsing and typed argument validation that preserves legacy behavior where required.
5. Route mutations through validated gameplay, world, and persistence services.
6. Log command ID, actor/target identifiers, timestamp, invocation channel, authorization/execution outcome, and correlation ID without credentials, secrets, raw unsafe payloads, or sensitive account fields.
7. Make audit durability claims only after a defined store and failure policy are implemented; no audit database schema is introduced in this stage.
8. Use cancellation and graceful host lifecycle operations; do not start fire-and-forget moderation or shutdown tasks.

Any permission-threshold correction is a separately documented compatibility difference. Prompt 18 has not changed the original numeric thresholds or Node.js behavior.
