# Administration

## .NET host

The current .NET 10 host does not implement a GM chat command dispatcher, operator command console, remote administration interface, or complete moderation workflow. Do not use legacy Node.js commands against the .NET server and do not infer support from the account-level value stored in an authenticated session.

The legacy source uses the server-loaded account `Level` for command checks. That field is available internally, but the .NET world listener has no chat packet dispatcher and no command handlers, response routing, active-character command context, or audit service. Character `IsGM` is not interchangeable with account `Level`.

## Legacy behavior reference

The source-derived command names, aliases, syntax, legacy thresholds, effects, and stubs are in [ADMIN_COMMAND_COMPATIBILITY_MATRIX.md](migration/ADMIN_COMMAND_COMPATIBILITY_MATRIX.md). Authorization caveats are in [ADMIN_AUTHORIZATION.md](migration/ADMIN_AUTHORIZATION.md); moderation and audit findings are in [MODERATION_IMPLEMENTATION.md](migration/MODERATION_IMPLEMENTATION.md) and [ADMIN_AUDIT_AND_SECURITY.md](migration/ADMIN_AUDIT_AND_SECURITY.md).

Root `kick` and `shutdown` paths include stubs; some low-threshold legacy commands mutate state or send raw packets. These are not recommended privileges or approved operational commands. The old local CLI supports JavaScript evaluation and shell execution and is not included in the .NET candidate.

No administration acceptance test passed because no .NET administration execution path exists. Administration remains a release blocker if the migration must preserve those features.
