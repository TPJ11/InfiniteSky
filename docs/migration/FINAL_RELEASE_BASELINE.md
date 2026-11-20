# Final Release Baseline

## Snapshot

| Item | Evidence |
|---|---|
| Baseline commit | `4df1184d5a0dabdd4696f3bde15b2c6785769153` |
| Branch/worktree | `master`, clean before Prompt 25 changes |
| OS | Windows 11 Pro x64, build `10.0.26300` |
| .NET SDK | `10.0.401` (`global.json` pinned; `latestFeature` roll-forward) |
| Installed target runtime | `Microsoft.NETCore.App 10.0.12`, `Microsoft.AspNetCore.App 10.0.12` |
| Target framework | `net10.0` |
| Deployment evidence | Development workstation only; no service, container, cloud, or production topology validated |
| Candidate target selected | Framework-dependent `win-x64` only; no Linux/macOS release target is claimed |

This is the baseline before Prompt 25 documentation, lockfile, and packaging changes. The original Node.js implementation remains the behavioral reference. No deployment database, account, client, or public server was contacted.

## Baseline commands and results

Executed from the repository root before Prompt 25 changes:

| Command | Result |
|---|---|
| `git rev-parse HEAD` | `4df1184d5a0dabdd4696f3bde15b2c6785769153` |
| `git status --short --branch` | `## master...origin/master` |
| `dotnet --info` | SDK `10.0.401`; host/runtime `10.0.12`; Windows x64 |
| `dotnet restore InfiniteSky.sln` | Succeeded; projects were up to date |
| `dotnet build InfiniteSky.sln --no-restore` | Succeeded; 0 warnings, 0 errors |
| `dotnet test InfiniteSky.sln --no-build --no-restore` | 188 passed, 0 failed, 2 skipped |

Test totals: Domain 50, Protocol 25, GameData 5, World 22, Persistence 23 passed/2 skipped, and Server Integration 63. The two skipped tests are the opt-in MongoDB Testcontainers integrations; they were not run against live or production data. Docker availability and the opt-in setting were not prerequisites to the baseline test command.

## Runtime/deployment configuration

The checked-in defaults in `src/InfiniteSky.Server/appsettings.json` disable raw TCP listeners and MongoDB. Listener defaults are loopback `127.0.0.1`, TCP ports `11091`/`11092`; database configuration has an empty URI; no zones are selected. Thus a default process is a development foundation, not a playable or operationally ready game server. No HTTP health endpoint is hosted. See [RUNTIME_CONFIGURATION.md](RUNTIME_CONFIGURATION.md).

Required game data is not bundled. The repository has NPC/MOB spawn files and `.pac` files but no `.IMG` catalogs or `.WREGION` corpus. An original client build, representative production BSON, and a production collection/index inventory were unavailable. Mongo collection names are inferred and must be confirmed before any deployment.

## Existing artifacts at baseline

`artifacts/publish/linux-x64` existed before Prompt 25, with a filesystem timestamp of 2026-11-17. Its source revision, publish command, hashes, runtime smoke result, and provenance were not established. It is ignored by Git and is not accepted as a release artifact or evidence of Linux deployment support. Prompt 25 creates a separate, provenance-recorded Windows candidate; the old directory is not overwritten or included.

## Limitations

This baseline does not certify original-client compatibility, complete protocol parity, full persistence compatibility, gameplay, scripts, administration, public-network operation, TLS, MongoDB recovery, multi-client gameplay, or production capacity. The latest historical security review records cleartext login transport and plaintext legacy password storage as unresolved high-severity risks. See [SECURITY_FINDINGS.md](SECURITY_FINDINGS.md), [PERFORMANCE_BENCHMARK_RESULTS.md](PERFORMANCE_BENCHMARK_RESULTS.md), and [CLIENT_COMPATIBILITY_READINESS.md](CLIENT_COMPATIBILITY_READINESS.md).

## Prompt 25 final validation

The following checks ran after the Prompt 25 documentation and lockfile changes, on Windows 11 x64 with SDK `10.0.401` and runtime `10.0.12`:

| Command/check | Result |
|---|---|
| `dotnet restore InfiniteSky.sln --locked-mode` | Succeeded for all 16 solution projects |
| `dotnet build InfiniteSky.sln --no-restore --configuration Release` | Succeeded; 0 warnings, 0 errors |
| `dotnet test InfiniteSky.sln --no-build --no-restore --configuration Release` | 188 passed, 0 failed, 2 skipped |
| `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` | Succeeded; no formatting changes required |
| Clean-checkout `dotnet publish src\InfiniteSky.Server\InfiniteSky.Server.csproj --configuration Release --runtime win-x64 --self-contained false --no-restore --output <publish-dir>` | Succeeded; framework-dependent Windows x64 output |
| `dotnet list InfiniteSky.sln package --vulnerable --include-transitive` | No vulnerable packages reported by the configured nuget.org source |
| Changed-Markdown local-link check | 207 local links checked across 25 changed Markdown files; 0 broken |
| `git diff --check` | Succeeded |

The skipped tests are the two opt-in MongoDB Testcontainers integrations. Docker is not installed/available in this environment, so no live MongoDB integration, containerized startup, or production BSON validation is claimed. The `dotnet list package --vulnerable` command used nuget.org metadata and is not a complete security or supply-chain audit.

The Prompt 23 loopback login benchmark was rerun at 1, 10, 50, and 100 simulated clients, with three repetitions per profile and five seconds of idle authenticated sessions. All 483 measured authentication attempts succeeded, cleanup returned connection/session counts to zero after each repetition, and no send/receive failures were reported. The run did not exercise gameplay, original clients, MongoDB, or production capacity; per-profile measurements are recorded in [PERFORMANCE_BENCHMARK_RESULTS.md](PERFORMANCE_BENCHMARK_RESULTS.md).

The first clean Windows worktree exposed the repository's pre-existing C# line-ending mismatch: global `core.autocrlf=true` checked out CRLF while `.editorconfig` requires LF. `.gitattributes` now pins C# files to LF, and the final clean-checkout format verification passed. The first RID publish also exposed that lockfiles did not include `win-x64`; `Directory.Build.props` now declares that single RID and the lockfiles were regenerated without package-version changes. Final locked restore, Release build/test/format, and `win-x64` publish all passed from commit `82047a23ca7e22813262924af13cc95cd905b23d`.

The clean published output was packaged and extracted independently. The extracted server ran for five seconds with networking and MongoDB disabled and no zones selected; logs reported world readiness as degraded. This verifies package startup only, not healthy world readiness, authentication, original-client behavior, gameplay, persistence, or graceful shutdown from the archive. Archive size and SHA-256 are in [RELEASE_MANIFEST.md](RELEASE_MANIFEST.md).
