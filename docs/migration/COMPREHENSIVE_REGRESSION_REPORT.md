# Prompt 22 Comprehensive Regression Report

## Executive result

Prompt 22 established a pre-change baseline, inventoried 30 test groups across six test projects, repeated the baseline suite, collected coverage, added one source-derived protocol fixture test, corrected evidence wording, and reran the complete solution checks. No runtime regression was reproduced and no production implementation behavior was changed.

The final test run reported **187 passed, 0 failed, 2 skipped**. The skipped tests are the opt-in MongoDB Testcontainers integrations. The server is **not verified for server-wide behavioral parity**: no original-client or Node differential test ran, Mongo live tests were skipped, and major world/gameplay systems remain absent or blocked.

## Environment and baseline

- Baseline commit: `c631b4514828b9a98a6f390118e029d491d44fd7`; worktree was clean before Prompt 22 changes.
- Environment: Windows; .NET SDK `10.0.401`; .NET 10 solution.
- Baseline commands: `dotnet restore InfiniteSky.sln`, `dotnet build InfiniteSky.sln --no-restore`, `dotnet test InfiniteSky.sln --no-build`.
- Baseline result: restore succeeded, build passed with 0 warnings/errors, 186 passed, 0 failed, 2 skipped, 188 total; test command duration 6.09 seconds.
- Full pre-change results and limitations are in [REGRESSION_BASELINE.md](./REGRESSION_BASELINE.md).

## Final validation

| Command/check | Result |
|---|---|
| `dotnet restore InfiniteSky.sln` | Succeeded; dependencies were up to date |
| `dotnet build InfiniteSky.sln --no-restore` | Passed; 0 warnings, 0 errors |
| `dotnet test InfiniteSky.sln --no-build` | 187 passed, 0 failed, 2 skipped |
| `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` | Passed |
| `git diff --check` | Passed |
| `dotnet test InfiniteSky.sln --no-build --collect:"XPlat Code Coverage"` | 187 passed, 0 failed, 2 skipped; Cobertura reports produced |
| Focused new handshake test | 1 passed, 0 failed, 0 skipped |

Final test results by project:

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `InfiniteSky.Domain.Tests` | 50 | 0 | 0 |
| `InfiniteSky.Protocol.Tests` | 25 | 0 | 0 |
| `InfiniteSky.GameData.Tests` | 5 | 0 | 0 |
| `InfiniteSky.World.Tests` | 22 | 0 | 0 |
| `InfiniteSky.Persistence.Tests` | 23 | 0 | 2 |
| `InfiniteSky.Server.IntegrationTests` | 62 | 0 | 0 |
| **Total** | **187** | **0** | **2** |

The skipped tests are the account and character repository Testcontainers tests. The opt-in was false and Docker was unavailable. The exact skip gate and test identifiers are documented in [REGRESSION_BASELINE.md](./REGRESSION_BASELINE.md).

## Test inventory and coverage

- Inventory: 30 test classes/groups in six projects; details, test level, fixture provenance and limitations are in [REGRESSION_TEST_INVENTORY.md](./REGRESSION_TEST_INVENTORY.md).
- Feature mapping: all 69 matrix rows currently marked `IMPLEMENTED`, `IN_PROGRESS` or `VERIFIED` are mapped to tests/evidence in [BEHAVIOURAL_PARITY_COVERAGE.md](./BEHAVIOURAL_PARITY_COVERAGE.md).
- The baseline suite was repeated three times before adding the handshake test. Each repeat reported 186 passed, 0 failed, 2 skipped. The baseline coverage run also had those results. No flakiness was observed in those runs; this does not prove reliability under all scheduling/load conditions.
- No property-based framework, performance benchmark project, differential runner, or original-client harness is configured.

### Coverage collector results

These are package rates from the relevant test-project Cobertura reports, not a merged whole-solution total. Shared dependencies occur in multiple reports, so percentages must not be added across projects.

| Source package | Line coverage | Branch coverage | Relevant test project |
|---|---:|---:|---|
| `InfiniteSky.Domain` | 56.32% | 94.44% | Domain |
| `InfiniteSky.Protocol` | 75.62% | 66.66% | Protocol |
| `InfiniteSky.GameData` | 95.04% | 87.50% | GameData |
| `InfiniteSky.World` | 81.81% | 69.11% | World |
| `InfiniteSky.Persistence` | 53.46% | 48.73% | Persistence |
| `InfiniteSky.Application` | 80.59% | 72.72% | Server integration |
| `InfiniteSky.Networking` | 67.71% | 56.94% | Server integration |
| `InfiniteSky.Server` | 70.62% | 61.72% | Server integration |

Coverage is execution coverage only. It does not show parity, completeness, client behavior or database compatibility. The high GameData line rate reflects the narrow implemented package and is not evidence that required `.IMG` data is present or understood.

## Differential and original-client evidence

| Evidence area | Result |
|---|---|
| Original Node.js differential execution (Level B) | Not run. Node is installed, but `node_modules`, `config.json` and the `test.js` target named by the legacy package script are absent. |
| Original-client comparison (Level A) | Not run. No original game client executable/build or client-attributable capture is available in the workspace. |
| MongoDB integration | Two Testcontainers tests skipped; no live MongoDB or production data was used. |
| Source-derived fixtures (Level C) | Limited login greeting/request/reply fixtures; a new 18-byte handshake request fixture; synthetic BSON; tracked `.NPC` source data. |
| Source-derived rule/state tests (Level D) | Authentication decisions, selected domain calculations, lifecycle, spatial boundaries and player activation. |
| Implementation-only tests (Level E) | Host composition, configuration, DI, diagnostics and loopback infrastructure behavior. |

Checked-in `storage-item`, `world-region`, `map-load-reply`, `attack-reply` and `cvec3-node-restruct` fixture files are not consumed by the current automated tests. The old pcap has unknown provenance and is not treated as original-client evidence.

## Regression findings and fixes

1. **World handshake request test gap:** added `world-handshake.hex.txt` from `packets/world/handshake.js`, documented its provenance, and added an exact decode/re-encode test. The focused test passed, and the final protocol project passed 25 tests. The raw `0x15` response and transfer flow remain unimplemented.
2. **Listener-port documentation:** corrected stale `11191`/`11192` matrix notes to configured defaults `11091`/`11092`; host tests use ephemeral ports.
3. **Matrix count drift:** corrected the stale 205-ID note to 232; confirmed 232 unique rows and the status totals below.
4. **Compression evidence wording:** corrected the matrix to say only the uncompressed two-byte envelope is tested; no compressed-state implementation or golden capture exists.
5. **Mongo criteria wording:** clarified criteria as future acceptance checks; no successful live MongoDB result is claimed.

No behavior defect was reproduced, no runtime workaround or fake response was added, and no original Node.js file changed. Full issue details are in [REGRESSION_ISSUES_AND_FIXES.md](./REGRESSION_ISSUES_AND_FIXES.md).

## Migration status

No matrix status changed during Prompt 22. The 232 rows remain:

| Status | Count |
|---|---:|
| `NOT_STARTED` | 137 |
| `IN_PROGRESS` | 7 |
| `IMPLEMENTED` | 48 |
| `VERIFIED` | 14 |
| `BLOCKED` | 25 |
| `NOT_APPLICABLE` | 1 |

The 14 `VERIFIED` entries remain limited to the exact tested foundation, network, protocol and login slices described in the matrix. None asserts full server or client parity. The subsystem counts in [MIGRATION_STATUS.md](../../MIGRATION_STATUS.md) were reconciled to the matrix.

## Subsystem verification table

| Subsystem | Tests executed | Result | Strongest evidence | Migration status | Remaining blocker |
|---|---|---|---|---|---|
| Infrastructure/configuration | Host lifecycle, configuration, DI, diagnostics, failure handling | Pass | E | Mixed: 4 verified, 3 implemented, rest not started | Full legacy startup ordering and complete failure/shutdown behavior |
| Networking/framing | Loopback accept/send/disconnect, fragmentation/coalescing, bounds | Pass | C/D/E | 3 verified, 3 implemented | Queue saturation, idle expiration and world dispatch not verified |
| Protocol | Primitive/login fixtures, structures, new handshake request fixture | Pass | C/D | 5 verified, 2 implemented, 1 in progress | Most packet families, raw world success, character/world state and compression |
| Authentication/sessions | Simulated login, duplicate races, timeout, failure and cleanup | Pass | C/D/E | 2 verified, remaining implemented | Live Mongo accounts and exact legacy transfer/duplicate rules |
| Persistence | BSON mapper/component tests; container tests skipped | 23 pass, 2 skipped | C/D/E | 5 implemented, 1 in progress | Live Mongo and representative legacy documents |
| Game data | Zone loader and tracked NPC fixture | Pass | C/D | Not started in feature matrix | `.IMG` and `.WREGION` source assets/corpora |
| Domain | Models, identifiers, value semantics and arithmetic | Pass | C/D | 10 implemented | Data-backed, packet/persistence and runtime gameplay integration |
| World engine | Runtime/readiness/clock | Pass | D/E | 3 implemented | Full simulation, ticks under original workload and network entry |
| Zones/spatial | Zone loading, spatial boundaries, concurrency and lifecycle | Pass | C/D/E | 6 implemented | Original region corpus, visibility packets, transfers and simulation |
| Entities/players | Mapper, activation, registration, rollback and cleanup boundary | Pass | C/D/E | 11 implemented, 2 blocked | Client world packets, actual network cleanup and live persistence |
| Gameplay | XP/stat arithmetic only | Pass | D | 2 in progress; other rows unstarted/blocked | Source data, packet handlers and end-to-end behavior |
| Scripting/plugins | No runtime tests | Not run/not implemented | None | Blocked/not started | Runtime scope/API and source-compatible consumers |
| Administration/social | No command/chat gameplay tests | Not run/not implemented | None | Blocked/not started | World ingress, service boundaries and permission behavior |
| End-to-end compatibility | Login socket path only | Partial pass | C/D/E | In progress; real client row blocked | Character selection → world → gameplay → persistence journey |
| Differential/original client | None | Not run | None | Blocked | Node reference prerequisites and original client build |

## Readiness and stage boundary

The regression suite is repeatable enough to serve as a foundation baseline, but not a representative MMORPG workload. Missing client/world/gameplay flows and live persistence evidence prevent meaningful full-server performance testing. Do not begin Prompt 23 or broad optimization without explicit approval. See [VERIFICATION_READINESS.md](./VERIFICATION_READINESS.md) and [TEST_EXECUTION_GUIDE.md](./TEST_EXECUTION_GUIDE.md).
