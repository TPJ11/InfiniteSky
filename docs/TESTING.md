# Testing

## Standard commands

Run from the repository root with the pinned .NET SDK:

```powershell
dotnet restore InfiniteSky.sln --locked-mode
dotnet build InfiniteSky.sln --no-restore
dotnet test InfiniteSky.sln --no-restore
dotnet format InfiniteSky.sln --verify-no-changes --no-restore
```

The six test projects cover Domain, GameData, Persistence, Protocol, Server integration and World. The current baseline passed 188 tests with 0 failures; two opt-in MongoDB integration tests were skipped. Always report those skips.

## External and specialized tests

### MongoDB

The persistence Testcontainers tests require Docker and explicit opt-in:

```powershell
$env:INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS = "true"
dotnet test tests\InfiniteSky.Persistence.Tests\InfiniteSky.Persistence.Tests.csproj
Remove-Item Env:\INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS
```

Use only a disposable local container; never point test configuration at production.

### Login load harness

`tools/InfiniteSky.PerformanceHarness` creates real loopback sockets and an in-memory test account repository. It covers login/authentication sessions up to the configured test limit of 100; it is not a world/gameplay performance benchmark. See [PERFORMANCE_TEST_PLAN.md](migration/PERFORMANCE_TEST_PLAN.md).

### Client/differential and gameplay tests

No original game client executable/build or live Node differential environment was available. Character selection, world entry, most gameplay, scripting, administration, full persistence workflows, and active-player network journeys have no runnable end-to-end implementation. Do not replace missing tests with invented packets, fake rules, or mock workflows and call them compatibility tests.

## Results and evidence

Test counts and skip reasons are recorded in [COMPREHENSIVE_REGRESSION_REPORT.md](migration/COMPREHENSIVE_REGRESSION_REPORT.md), [SECURITY_TEST_RESULTS.md](migration/SECURITY_TEST_RESULTS.md), [RELIABILITY_TEST_RESULTS.md](migration/RELIABILITY_TEST_RESULTS.md), and the current [release baseline](migration/FINAL_RELEASE_BASELINE.md). Coverage measures code execution only, not behavioral parity.
