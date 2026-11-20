# Migration Handover

## Current status

The .NET 10 solution is a partial migration, not a playable or production-ready replacement. Release readiness is **NOT_READY**. The original Node.js implementation remains in the repository as the behavioral reference. No release publication, production deployment, or migration acceptance has occurred.

## Repository map

| Path | Responsibility |
|---|---|
| `src/InfiniteSky.Domain` | Domain identifiers, character/item/entity models, value types, isolated progression calculations |
| `src/InfiniteSky.Application` | Authentication/session, character access/activation boundaries, application health and diagnostics |
| `src/InfiniteSky.Protocol` | Legacy binary readers/writers, framing descriptors, selected packet structures |
| `src/InfiniteSky.Networking` | TCP listeners/connections, bounded framing, send queues and transport metrics |
| `src/InfiniteSky.Persistence` | MongoDB account/guild/character persistence slices and raw-BSON mapping |
| `src/InfiniteSky.GameData` | Selected legacy zone-resource parsing/catalog |
| `src/InfiniteSky.World` | World/zone lifecycle, spatial index, internal player registry/lifecycle |
| `src/InfiniteSky.Scripting` | Project boundary only; no compatible script runtime |
| `src/InfiniteSky.Server` | Entry point, DI, configuration, hosted-service order, readiness/health checks |
| `tests/` | Six test projects; MongoDB Testcontainers cases are opt-in |
| `tools/InfiniteSky.PerformanceHarness` | Bounded login/auth load harness; not gameplay load |
| `docs/migration/` | Stage evidence, feature matrix links, blockers and operational limits |

The original Node.js root files, `FruitSalad/`, `packets/`, `db/`, `generic/`, `data/`, and `vmscript/` remain source evidence. Do not modify or delete them as part of release acceptance.

## Build and verification

Use the SDK pinned by `global.json`:

```powershell
dotnet restore InfiniteSky.sln --locked-mode
dotnet build InfiniteSky.sln --no-restore --configuration Release
dotnet test InfiniteSky.sln --no-restore --configuration Release
dotnet format InfiniteSky.sln --verify-no-changes --no-restore
```

MongoDB integration tests require `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true` and Docker; only use disposable isolated test data. Current baseline skipped two such cases. See [TESTING.md](../TESTING.md).

## Runtime configuration and boundaries

The host reads `InfiniteSky` settings from JSON, command-line values, and `INFINITESKY_` environment variables. Networking and MongoDB are disabled by default; configured zones are empty. If Mongo is enabled, verify database name, collection names, unique username index, TLS and credentials; the startup active-flag reset is global and defaults on. Never expose raw TCP login to an untrusted network.

No client-facing character selection/world gameplay handlers, GM commands, script runtime, or health HTTP endpoint are present. Internal player activation and gameplay arithmetic tests do not establish a playable server.

## Evidence and known issues

- [FINAL_RELEASE_BASELINE.md](FINAL_RELEASE_BASELINE.md): commit/environment and baseline validation.
- [FINAL_FEATURE_RECONCILIATION.md](FINAL_FEATURE_RECONCILIATION.md): 232 feature IDs and subsystem status.
- [RELEASE_ACCEPTANCE_REPORT.md](RELEASE_ACCEPTANCE_REPORT.md): criteria and blockers.
- [RELEASE_MANIFEST.md](RELEASE_MANIFEST.md): local candidate provenance and hashes.
- [KNOWN_ISSUES.md](../KNOWN_ISSUES.md): release-blocking and operational issues.
- [RESIDUAL_SECURITY_RISKS.md](RESIDUAL_SECURITY_RISKS.md) and [SCALABILITY_LIMITS.md](SCALABILITY_LIMITS.md): security and tested workload limits.

## Maintenance and release gates

1. Preserve the source authority and retain unknown packet/BSON fields.
2. Implement only behavior with source evidence, exact data/packet fixtures, and ownership boundaries.
3. Do not mark a feature `VERIFIED` from compilation, static review, or component tests that do not exercise its stated contract.
4. Re-run locked restore, tests, security/package audits, original-client scenarios, and relevant benchmarks after changes.
5. Keep candidate artifacts local until an explicit owner decision. Do not push tags, publish, deploy, or delete the Node.js implementation.

The intended next work is resolving client/protocol/data/persistence prerequisites, then completing and verifying one supported user journey. No future stage is authorized by this handover.
