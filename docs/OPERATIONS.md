# Operations

## Current operational boundary

The host provides a Generic Host lifecycle and console logging. It does not host an HTTP health endpoint, expose a management API, provide a GM command console, or support a complete player/gameplay loop. The process reaching `Running` means the composed foundation services started; it does not mean MongoDB, zones, or a playable world are healthy.

## Start, stop, restart

From the candidate directory:

```powershell
.\InfiniteSky.Server.exe
```

Use Ctrl+C for normal console cancellation. For managed hosting, request normal process/service cancellation only after the deployment wrapper is independently configured and tested. The Generic Host shutdown timeout defaults to 30 seconds; connection shutdown also has a configured timeout. Host tests cover selected cancellation and login-session cleanup, not lossless world/player persistence.

Restart by stopping the process normally, preserving logs, applying reviewed configuration, and starting the same candidate again. No built-in service manager, restart controller, or automatic rollback exists.

## Readiness and monitoring

Health checks are registered for foundation liveness/readiness, MongoDB, world, and zones, but the server does not expose them over HTTP. No command currently queries them. Do not use an open TCP port, a `foundation started` log, or process existence as evidence that authentication/world readiness is healthy.

Logs use the configured .NET logging providers (console in the current host); no file destination or rotation policy is configured. Record stdout/stderr with the deployment's approved collector without capturing credentials or entire account/character documents.

Metrics are implemented as .NET instruments for selected authentication, networking, persistence, world, zone and player component counters/gauges. No Prometheus/OpenTelemetry exporter or external metrics endpoint is shipped. Operators cannot inspect active players through a supported command; the current listener path is not a playable world.

## Troubleshooting

| Symptom | Supported response |
|---|---|
| Startup option validation fails | Preserve the non-secret error, compare configuration with `src/InfiniteSky.Server/appsettings.json` and [RUNTIME_CONFIGURATION.md](migration/RUNTIME_CONFIGURATION.md), correct the value, restart. |
| MongoDB startup fails | Keep listeners disabled; verify protected URI injection, database, collections, index, TLS and credentials. Do not bypass validation. |
| Listener bind fails | Keep the process offline, check address/port ownership and configuration, then retry after resolving the conflict. Host integration tests cover rollback on a listener bind failure. |
| World/zones report degraded | Inspect configured zone IDs and externally supplied data. Empty zone selection and missing optional files are explicitly reported; do not invent placeholder zones. |
| Login fails | Confirm isolated MongoDB is enabled and reachable, collection/index assumptions are verified, and use only a test account. Legacy login responses differ for unknown account/password. |
| Resource pressure or malformed traffic | Disable external reachability and preserve bounded logs/metrics. Flood, slow-client and queue-saturation policies have not been validated; no general DoS guarantee is made. |
| Script or GM command issue | The .NET host has no compatible script/plugin runtime or command dispatcher. Do not copy/run legacy eval, shell, or command scripts as a workaround. |

## Persistence recovery and backups

The host does not provide backup, restore, automatic failover, or full-character save recovery. Back up MongoDB using the deployment's approved tools and restore to an isolated copy first. Account documents contain plaintext legacy credentials; restrict backup access. See [DATABASE.md](DATABASE.md) and [OPERATIONAL_RECOVERY.md](migration/OPERATIONAL_RECOVERY.md).

## Shutdown and emergency stop

Prefer the host's normal cancellation path. If an emergency termination is required, understand that in-flight database operations may be interrupted. The current active-player services are internal and not wired to the client world workflow; no lossless player-state shutdown is claimed. Verify database state before manually replaying any operation.
