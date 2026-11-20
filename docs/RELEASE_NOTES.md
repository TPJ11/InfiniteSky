# Local .NET 10 Release Candidate Notes

## Candidate

Candidate identifier: `infinite-sky-dotnet10-prompt25-rc1`. This is a local, unpublished, unaccepted review package. No semantic version or release date is assigned. See [RELEASE_MANIFEST.md](migration/RELEASE_MANIFEST.md) for source revision, target, archive, and hash.

The measured local archive is `artifacts/release-candidate/infinite-sky-dotnet10-prompt25-rc1-win-x64.zip` (3,066,194 bytes; SHA-256 `DB7BD8A9BC1A843D897773714D9D0439F16AD5BE6474EC16DDD1CA2EF063F67D`). It was published from clean checkout revision `82047a23ca7e22813262924af13cc95cd905b23d` and smoke-started after extraction with safe defaults; world readiness was degraded because no zones were configured.

## Migration overview

The staged migration provides a .NET Generic Host foundation, validated configuration, bounded TCP transport, selected legacy binary structures, a source-derived login path, account/guild/character persistence slices, world/zone/spatial infrastructure, internal player activation boundaries, and isolated XP/stat arithmetic.

## Verified scope

The current solution restores/builds and its Release test run reports 188 passing tests, 0 failures, and 2 skipped opt-in MongoDB container tests. A Prompt 25 rerun completed the login/authentication workload at 1/10/50/100 loopback clients with all measured logins succeeding and cleanup to zero; it did not establish game capacity. Exact results are in [PERFORMANCE_BENCHMARK_RESULTS.md](migration/PERFORMANCE_BENCHMARK_RESULTS.md). Verified feature criteria are maintained in [FEATURE_PARITY_MATRIX.md](FEATURE_PARITY_MATRIX.md) and [FINAL_FEATURE_RECONCILIATION.md](migration/FINAL_FEATURE_RECONCILIATION.md).

No original-client compatibility, full behavioral parity, live MongoDB compatibility, or production performance is claimed. No public release has occurred.

## Known issues and release status

The candidate is **NOT_READY**. The login protocol is raw TCP and the legacy database password representation is plaintext; both are documented high-severity risks. Character/world entry and most gameplay, scripts, and administrative commands are absent or blocked. Required `.IMG`/`.WREGION` assets and representative production BSON are unavailable. Original-client and live-database tests did not run.

The package is framework-dependent Windows x64 only, with networking and MongoDB disabled by default. It excludes credentials, databases, original client files, and game assets. See [KNOWN_ISSUES.md](KNOWN_ISSUES.md), [DEPLOYMENT.md](DEPLOYMENT.md), and [RELEASE_ACCEPTANCE_REPORT.md](migration/RELEASE_ACCEPTANCE_REPORT.md).

## Upgrade and approval

No database migration or destructive schema change is included. Back up and validate isolated data before any deployment experiment. Do not publish, push a tag, deploy, or treat this candidate as accepted without explicit owner approval.

## Relevant migration commits

- `0ad5f0a` — initial .NET 10 solution foundation.
- `45ffea3` — security and residual-risk review documentation.
- `4df1184` — pre-finalization repository head and concise security findings summary.
- `e4dd1ce` — final documentation and initial locked package graphs.
- `8911a58` — consistent C# LF checkout for reproducible format verification.
- `82047a2` — locked `win-x64` target and candidate payload source revision.
