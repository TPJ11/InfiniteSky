# Prompt 22 Regression Baseline

## Snapshot

| Field | Value |
|---|---|
| Baseline commit | `c631b4514828b9a98a6f390118e029d491d44fd7` |
| Worktree before baseline | Clean |
| Operating system | Windows |
| .NET SDK | `10.0.401` |
| Solution target | .NET 10 |
| Original Node.js files changed before baseline | No |

The baseline was captured before any Prompt 22 changes. The test duration is the duration reported by the complete `dotnet test` command, not the sum of per-project durations.

## Commands and results

| Command | Result |
|---|---|
| `dotnet restore InfiniteSky.sln` | Succeeded |
| `dotnet build InfiniteSky.sln --no-restore` | Passed; 0 warnings, 0 errors |
| `dotnet test InfiniteSky.sln --no-build` | 186 passed, 0 failed, 2 skipped; 188 total; 6.09 seconds |

Baseline tests by project:

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `InfiniteSky.Domain.Tests` | 50 | 0 | 0 |
| `InfiniteSky.Protocol.Tests` | 24 | 0 | 0 |
| `InfiniteSky.GameData.Tests` | 5 | 0 | 0 |
| `InfiniteSky.World.Tests` | 22 | 0 | 0 |
| `InfiniteSky.Server.IntegrationTests` | 62 | 0 | 0 |
| `InfiniteSky.Persistence.Tests` | 23 | 0 | 2 |
| **Total** | **186** | **0** | **2** |

## Skipped tests

The two skipped tests are:

- `MongoAccountAuthenticationRepositoryIntegrationTests.MongoRepositoryReadsAndUpdatesLegacyAccountDocuments`
- `MongoCharacterRepositoryIntegrationTests.RepositoryEnforcesOwnershipAndUpdatesOnlyLegacyWorldLocationFields`

`MongoIntegrationFactAttribute` skips them unless `INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true`. Docker was unavailable in the recorded environment and the opt-in variable was not enabled, so neither Testcontainers test started. These are **not passed tests** and do not establish live MongoDB or production BSON compatibility.

## Baseline limitations and existing blockers

- The baseline is one complete suite execution. It does not by itself establish test reliability.
- The suite uses synthetic repositories and BSON values for many tests. The socket login harness substitutes the account repository.
- No original client was available, and no client test was run.
- No differential execution against the Node.js server was run. The workspace has Node.js `v24.18.0`, but lacks `node_modules`, `config.json`, and the `test.js` target named by the legacy package test script. No dependency installation or legacy server launch was attempted.
- Docker and the MongoDB CLI were unavailable; the opt-in MongoDB tests were not run.
- Code coverage, repeatability, and flaky-test assessments were not part of this pre-change baseline.
- Character selection, world entry, gameplay, scripting, administration, full character persistence, and client-visible end-to-end workflows remain unverified or blocked as recorded in [CLIENT_COMPATIBILITY_READINESS.md](./CLIENT_COMPATIBILITY_READINESS.md), [INTEGRATION_TEST_HARNESS.md](./INTEGRATION_TEST_HARNESS.md), and the [feature parity matrix](../FEATURE_PARITY_MATRIX.md).
