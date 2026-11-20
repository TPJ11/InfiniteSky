# Operational Recovery Procedures

## Scope

These are conservative operator procedures for the currently implemented .NET host. They do not promise automated failover, lossless state recovery, or production readiness. Use only a deployment you are authorized to administer. Do not point test commands at production databases.

## Startup and configuration failures

1. Preserve the startup log and process exit code. The host returns a non-zero result for fatal startup/runtime failure; do not treat a partially started process as ready.
2. Verify required values through the approved deployment configuration channel. The server supports `appsettings*.json`, command-line configuration, and `INFINITESKY_` environment variables; never paste secret values into tickets or logs.
3. Keep `InfiniteSky:Networking:Enabled=false` until the intended bind address, ports, firewall rules, and account persistence are confirmed.
4. With MongoDB enabled, check reachability, the selected database and collections, credentials/permissions, server selection/connect timeouts, and account username index. Startup database checks occur before listeners start. A startup failure should be corrected and restarted; do not bypass validation.
5. If a listener bind fails, resolve the port/address conflict and restart. Listener startup errors propagate; the integration test covers rollback when the world listener cannot bind after login listener startup.

See [runtime configuration](./RUNTIME_CONFIGURATION.md) and [database configuration](./DATABASE_CONFIGURATION.md).

## MongoDB outage or persistence failure

- If persistence is not required for the current test, keep the database feature disabled; database operations then fail closed and the database readiness check is degraded.
- If an enabled database fails startup validation, keep the server offline until connectivity and collection/index configuration are corrected. Do not expose listeners while the account repository cannot serve real accounts.
- For an operation-time failure, retain the sanitized operation/failure-category logs and inspect database availability, authorization, timeout, and health-check state. Application writes are not automatically retried; determine whether the operation completed before manually retrying or repairing data.
- Before any write recovery, preserve a database backup using the operator's approved MongoDB procedure. Restore to an isolated copy first and validate representative account/character documents. Do not run destructive migrations or manually reset fields without confirming scope.
- `ResetActiveAccountsOnStartup=true` performs the legacy global active-flag reset for the configured account collection. Do not run multiple instances against a shared collection with this behavior enabled unless the operational effect is explicitly accepted.

No live MongoDB outage/recovery test was completed in this stage baseline; the opt-in Testcontainers tests were skipped.

## Network and authenticated-session recovery

- For failed listener startup, follow the startup procedure rather than retrying indefinitely. Listener binding and startup errors are visible to the host.
- For a client disconnect, the implemented login path removes the in-process connection/session. The persistent account `active` flag is not reset on ordinary login disconnect, matching the documented legacy behavior; do not infer that the account is safe to reactivate solely from the in-memory session count.
- A host shutdown closes login connections within configured bounds. Prompt 23 verified cleanup for login-only authenticated sessions, not active player saves or world/gameplay state.
- If public reachability is unintended, disable listeners or restrict them at the host firewall until bind/ACL settings are reviewed. The server has no TLS listener; do not send credentials over an untrusted network without an approved compatible network-layer protection.

## Script and administrative failures

The .NET host does not currently execute JavaScript scripts, expose a plugin API, dispatch GM chat commands, or expose remote administration. Do not enable the legacy Node.js REPL, shell execution, or in-process script loader for untrusted operators or content. The legacy Node script environment is trusted code with process privileges, not a sandbox.

If investigating a legacy Node deployment, separate its process and data from the .NET host, preserve logs, and use only trusted source/scripts. This repository does not establish a safe automated script rollback or privilege isolation mechanism.

## Graceful and emergency shutdown

1. Prefer the Generic Host's normal cancellation path (service manager stop or Ctrl+C in a controlled console).
2. Allow the configured host shutdown timeout and per-connection shutdown timeout to elapse; monitor the process exit and sanitized shutdown logs.
3. Confirm listeners and login sessions have drained. A forced process termination is an emergency measure and may interrupt in-flight persistence.
4. The present host does not provide complete active-player, inventory, economy, or gameplay save-on-shutdown semantics. Do not claim lossless shutdown or restart recovery.
5. After restart, check database readiness, listener state, and account active-state behavior. Validate affected records from an isolated backup before any manual repair.

## Backup, restore, and validation

- Establish backups and retention through the actual MongoDB operator/deployment process; this repository does not configure or test a backup service.
- Restrict backup access because legacy account documents contain plaintext credentials.
- Test restore on an isolated database copy, validate BSON field types and indexes, and run the opt-in integration suite only against disposable data.
- Keep original Node.js and .NET databases isolated unless a deliberate compatibility test has been approved.

## Known recovery limits

No production-style deployment, live database failure/recovery, active-player shutdown, script failure, administrative operation, or original-client recovery test was run. See [reliability results](./RELIABILITY_TEST_RESULTS.md), [verification readiness](./VERIFICATION_READINESS.md), and [residual security risks](./RESIDUAL_SECURITY_RISKS.md).
