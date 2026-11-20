# Prompt 24 Security and Reliability Baseline

## Snapshot

| Item | Baseline |
|---|---|
| Commit | `4159f887517fbb34145adfa92651b6d07a24c733` |
| Branch/worktree | `master`, clean before validation |
| Captured | 2026-11-20 |
| OS | Windows 11 Pro x64, build `10.0.26300` |
| CPU/memory | Intel Core i9-12900K, 24 logical processors; 31.75 GiB visible memory |
| .NET SDK | `10.0.401` |
| Installed target runtime | .NET / ASP.NET Core `10.0.12` |
| Target framework | `net10.0` |

The installed host is a development workstation, not a production deployment. No production service, account, database, or external game client was contacted.

## Baseline commands and results

Executed from the repository root before Prompt 24 changes:

| Command | Result |
|---|---|
| `git rev-parse HEAD` and `git status --short --branch` | Commit above; clean worktree |
| `dotnet --version` | `10.0.401` |
| `dotnet --list-runtimes` | Includes `Microsoft.NETCore.App 10.0.12` and `Microsoft.AspNetCore.App 10.0.12` |
| `dotnet restore InfiniteSky.sln --verbosity minimal` | Succeeded; all projects up to date |
| `dotnet build InfiniteSky.sln --no-restore` | Succeeded; 0 warnings, 0 errors |
| `dotnet test InfiniteSky.sln --no-build` | 188 passed, 0 failed, 2 skipped |

Test totals: Domain 50, Protocol 25, GameData 5, World 22, Persistence 23 passed/2 skipped, and Server Integration 63. The skipped tests are the opt-in MongoDB Testcontainers integration cases; this baseline does not verify a live MongoDB deployment.

The pre-change Prompt 23 login/session benchmark and its limitations are recorded in [PERFORMANCE_BENCHMARK_RESULTS.md](./PERFORMANCE_BENCHMARK_RESULTS.md). It exercised up to 100 loopback login sessions, not gameplay or public network exposure. No original-client, Node differential, live MongoDB, production deployment, or full-world test was run.

## Dependency audit at baseline

The solution uses central package versions in [`Directory.Packages.props`](../../Directory.Packages.props). The solution targets .NET 10; the repository pins SDK `10.0.401` in [`global.json`](../../global.json). Production code has a direct `MongoDB.Driver` package; `Testcontainers.MongoDb`, xUnit, the test SDK, and coverage tooling are test dependencies. No embedded JavaScript engine or script execution runtime is present.

| Dependency class | Baseline inventory |
|---|---|
| Runtime | `net10.0`; .NET/ASP.NET Core 10.0.12 installed on the test host |
| Direct production NuGet | `MongoDB.Driver` 3.14.0 in the persistence project |
| Transitive runtime packages observed | MongoDB.Bson 3.14.0, DnsClient 1.6.1, SharpCompress 0.48.1, Snappier 1.3.1, and ZstdSharp.Port 0.7.3 through MongoDB dependencies |
| Test-only direct packages | Microsoft.NET.Test.Sdk 17.14.1, xUnit 2.9.3, xunit.runner.visualstudio 3.1.4, coverlet.collector 6.0.4, and Testcontainers.MongoDb 4.17.0 |
| Other test transitive packages | Testcontainers Docker client/network packages; an older `Microsoft.Extensions.Logging.Abstractions` 2.0.0 appears in some test/harness dependency graphs |
| Legacy Node/FruitSalad | Multiple old `package.json` manifests with broad/unpinned ranges; no npm/yarn/pnpm lockfile is checked in |
| NuGet lockfile | No `packages.lock.json` was found; direct package versions are centrally pinned, but transitive resolution is not committed as a lockfile |

Commands run against `https://api.nuget.org/v3/index.json`:

| Command | Result |
|---|---|
| `dotnet list InfiniteSky.sln package --vulnerable --include-transitive` | No vulnerable packages reported for solution projects by the configured source at baseline |
| `dotnet list InfiniteSky.sln package --deprecated --include-transitive` | No deprecated packages reported in production projects; test projects report xUnit 2 as legacy and an old transitive `Microsoft.Extensions.Logging.Abstractions` 2.0.0 in integration/test harness graphs |
| `dotnet list InfiniteSky.sln package --outdated --include-transitive` | Updates are available, including MongoDB.Driver 3.14.0 to 3.14.1 and several transitive/test packages; no packages were upgraded during baseline capture |

This is a point-in-time NuGet advisory check against the stated feed, not a supply-chain certification. The legacy Node.js and FruitSalad package manifests have no checked-in npm lockfile, so a reproducible npm dependency graph and corresponding audit result were not available. Those Node.js sources remain the behavioral reference and were not altered.

A bounded regex scan of tracked text files found no private-key block or credential-bearing MongoDB URI. The credential-like assignment matches were reviewed without printing values: the README MongoDB URI is loopback-only and has no user-info; other matches were documentation/vendor text, test fixtures, and the performance harness's fixed password for generated in-memory accounts. This is not a comprehensive secret scanner and does not prove that every possible secret format is absent.

## Checked-in runtime and deployment assumptions

The defaults in [`appsettings.json`](../../src/InfiniteSky.Server/appsettings.json) disable networking and MongoDB. If networking is enabled without an override, the listeners bind to `127.0.0.1`; login/world TCP ports default to 11091/11092. The configured connection maximum is 100, receive/frame bounds are 10,240 bytes, send queue capacity is 64, and idle timeout is 120 seconds. Database connection string is empty by default. Development configuration raises log verbosity to Debug; production configuration sets `Microsoft` logs to Warning.

Runtime configuration may be supplied through environment-specific JSON, command-line values, or `INFINITESKY_`-prefixed environment variables as described in [RUNTIME_CONFIGURATION.md](./RUNTIME_CONFIGURATION.md). This baseline does not establish protected production secret injection, filesystem permissions, process identity, firewall rules, TLS support, MongoDB server-version compatibility, backup/restore procedures, or an installed service/container deployment. The existing operational document explicitly says the production-style smoke path has not been run.

The host can start listeners and the login/authentication slice, but the world listener does not implement the client world workflow. Zones are empty by default, full world readiness is not established, and character, gameplay, scripting, and administrative packet workflows remain absent or blocked. See [VERIFICATION_READINESS.md](./VERIFICATION_READINESS.md) and [SCALABILITY_LIMITS.md](./SCALABILITY_LIMITS.md).

## Baseline security boundary

This report records the starting environment and commands only. It is not a security certification or a finding that the server is safe for public deployment. The threat model, source-reviewed findings, fixes, and residual risks are recorded separately in [THREAT_MODEL.md](./THREAT_MODEL.md), [SECURITY_FINDINGS.md](./SECURITY_FINDINGS.md), and [RESIDUAL_SECURITY_RISKS.md](./RESIDUAL_SECURITY_RISKS.md).
