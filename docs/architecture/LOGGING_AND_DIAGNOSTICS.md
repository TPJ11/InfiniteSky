# Logging and Diagnostics

## Logging

The solution uses `Microsoft.Extensions.Logging` and the console provider supplied by Generic Host. Categories and levels are configured in appsettings.

Lifecycle logs use source-generated methods:

```text
InfiniteSky foundation started
InfiniteSky foundation stopped
InfiniteSky foundation startup failed
```

Startup logs include application name, environment, compatibility profile, and explicit network/database state. Secrets, connection strings, passwords, packet payloads, and account identifiers are not included.

Exceptions are logged at critical/failure boundaries with their exception object. Build-time or process-level failures are written to standard error by `ServerApplication`, which returns exit code 1.

## Vendor-independent sources

`InfiniteSkyDiagnostics` owns:

- `ActivitySource`: `<ServiceName>.Activities`
- `Meter`: `<ServiceName>.Metrics`

The foundation emits real lifecycle activities:

- `application.lifecycle.start`
- `application.lifecycle.stop`

It deliberately creates no counters, histograms, or gauges for packet, database, zone, entity, or gameplay systems that do not exist.

## Naming conventions

Future instrumentation:

| Area | Activity/meter prefix |
|---|---|
| networking | `network` |
| protocol | `protocol` |
| persistence | `database` |
| world | `world` |
| game loop | `world.tick` |
| zones | `zone` |
| entities | `entity` |
| gameplay | `gameplay` |

Names should be stable, lower-case dotted strings. Use tags only for bounded categorical values such as zone ID, subsystem, opcode, and outcome. Never tag with username, password, session secret, or raw payload.

## Export

No telemetry exporter or vendor SDK is installed. The standard `ActivitySource` and `Meter` can later be subscribed by OpenTelemetry, EventCounters, or an approved exporter without changing domain/application code.

## Log flushing

Generic Host disposes registered logging providers during host disposal, and the process waits for normal shutdown within the configured host timeout. No detached logger or buffered global queue is used.
