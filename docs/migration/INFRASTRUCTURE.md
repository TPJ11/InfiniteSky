# Infrastructure Migration

## Scope and matrix coverage

`INF-001` through `INF-016`: hosting, startup ordering, configuration, shutdown, cadence, diagnostics, web status, launch scripts, and the experimental FruitSalad supervisor.

## Source files

`main.js`, `GameStep.js`, `util.js`, `db.js`, `webserver.js`, `package.json`, `config.json-dist`, `FruitSalad/main.js`, `FruitSalad/Modules/rpc.js`, `FruitSalad/Processes/process.js`, `FruitSalad/Config/processes.json`, batch launchers.

## Target components

- `InfiniteSky.Server/ServerHost`
- `ServerOptions` and validation
- `StartupCoordinator`
- `ShutdownCoordinator`
- listener hosted services
- logging/metrics/tracing configuration
- optional operational HTTP host

## Dependencies

Requires matrix `DAT-001`, `PER-001`, `NET-001`, and architecture approval. No infrastructure completion is valid before startup and shutdown save semantics exist.

## Implementation order

1. Create host and strongly typed options.
2. Add version/build metadata, health/liveness/readiness.
3. Load diagnostics configuration.
4. Validate and open persistence/data inputs.
5. Start network listeners.
6. Start world clocks/zones.
7. Add graceful stop ordering and persistence drain.
8. Add optional web status and deployment scripts.
9. Inventory FruitSalad supervisor functionality and either reproduce or record a deviation.

## Tests

- options validation, including placeholders and invalid ports/masks;
- startup failure at each dependency gate;
- readiness transition;
- monotonic 100 ms cadence using fake time;
- cancellation and final-save ordering;
- status endpoint data/redaction;
- process exit and listener close;
- log/metric naming and credential redaction.

## Acceptance criteria

- One executable starts as the approved modular monolith.
- Required MongoDB/game-data failures prevent world readiness.
- Both default listeners are isolated hosted services.
- Normal shutdown stops ingress, drains saves, closes listeners, and disconnects MongoDB.
- Safe hot reload is limited to approved settings.
- No global process state substitutes for DI/lifecycle.

## Risks

The original event order relies on global initialization and can differ from the intended dependency graph. FruitSalad and the top-level server are distinct runtimes. Aggressive shutdown, reload, or listener changes may expose legacy client assumptions. See `MIGRATION_RISKS.md`.
