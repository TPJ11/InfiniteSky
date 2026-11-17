# Dependency Injection

## Composition conventions

`Program` only calls `ServerApplication.RunAsync`. `ServerHostBuilder` owns Generic Host setup, and `AddInfiniteSkyInfrastructure` is the current infrastructure registration extension.

Future projects should expose one registration extension only when that project has multiple related registrations or owns framework adapters. Extensions must describe their lifetime and optionality.

## Registered foundation services

| Service | Lifetime | Purpose |
|---|---|---|
| `TimeProvider` | Singleton | Deterministic time boundary |
| `ApplicationLifecycleCoordinator` | Singleton | Lifecycle state, readiness, transitions |
| `IApplicationReadiness` | Singleton | Same coordinator instance |
| `InfiniteSkyDiagnostics` | Singleton/disposable | Process `ActivitySource` and `Meter` |
| `FoundationHostedService` | Singleton/hosted | Initialisation/last-stop lifecycle hook |
| `ApplicationReadinessHostedService` | Singleton/hosted | Ready/first-stop lifecycle hook |
| `FoundationLivenessHealthCheck` | Health check service | Process lifecycle liveness |
| `FoundationReadinessHealthCheck` | Health check service | Foundation readiness |
| `HealthCheckPublisherHostedService` | Hosted framework service | Registered by health checks; no public endpoint |

## Rules

1. Domain and protocol projects do not reference the DI container.
2. Infrastructure wiring occurs in the composition root.
3. No service locator or static global service provider.
4. No singleton consumes a scoped/transient service that outlives a request; `ValidateScopes` and `ValidateOnBuild` are always enabled.
5. Factories are used for options-based construction, not to hide dependency creation.
6. `IHostedService` ordering is explicit because lifecycle readiness depends on it.
7. Cancellation tokens flow from hosted/zone/network operations; do not create fire-and-forget tasks in a registration.
8. Use `TryAdd*` only when an extension is intentionally replaceable by tests/plugins; the server lifecycle hooks are not duplicateable.

## Testing DI

Tests verify:

- configured host registration builds;
- lifecycle/readiness references share the coordinator singleton;
- diagnostics source is singleton across scopes;
- lifecycle hosted services resolve in the expected bounded set;
- host options consume validated configuration.

Architecture tests and future project registration tests should continue to enforce the approved dependency graph.
