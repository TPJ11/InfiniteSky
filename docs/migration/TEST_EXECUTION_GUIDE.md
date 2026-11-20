# Regression Test Execution Guide

## Requirements

- .NET 10 SDK.
- Run from the repository root.
- The default suite needs no production credentials, client assets, or MongoDB service.
- The loopback socket tests bind ephemeral local ports.
- The two MongoDB integration tests need Docker and the explicit opt-in environment variable described below.

## Restore, build and run

```powershell
dotnet restore InfiniteSky.sln
dotnet build InfiniteSky.sln --no-restore
dotnet test InfiniteSky.sln --no-build
```

The repository test wrapper is:

```powershell
.\scripts\test.ps1
```

It runs `dotnet test InfiniteSky.sln --no-restore --logger "console;verbosity=minimal"`.

## Run a test project or focused group

```powershell
dotnet test tests\InfiniteSky.Domain.Tests\InfiniteSky.Domain.Tests.csproj --no-restore
dotnet test tests\InfiniteSky.Protocol.Tests\InfiniteSky.Protocol.Tests.csproj --no-restore
dotnet test tests\InfiniteSky.GameData.Tests\InfiniteSky.GameData.Tests.csproj --no-restore
dotnet test tests\InfiniteSky.World.Tests\InfiniteSky.World.Tests.csproj --no-restore
dotnet test tests\InfiniteSky.Persistence.Tests\InfiniteSky.Persistence.Tests.csproj --no-restore
dotnet test tests\InfiniteSky.Server.IntegrationTests\InfiniteSky.Server.IntegrationTests.csproj --no-restore
dotnet test tests\InfiniteSky.Protocol.Tests\InfiniteSky.Protocol.Tests.csproj --no-restore --filter FullyQualifiedName~WorldHandshakeMatchesOriginalNodeLayout
dotnet test tests\InfiniteSky.Server.IntegrationTests\InfiniteSky.Server.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~NetworkAuthenticationHostIntegrationTests
```

`--no-restore` assumes dependencies have already been restored. Omit it for a clean environment or run restore first.

## MongoDB compatibility tests

The repository's two Testcontainers cases are opt-in. Use only a disposable local Docker environment; the tests create uniquely named databases and synthetic records.

```powershell
$env:INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS = "true"
dotnet test tests\InfiniteSky.Persistence.Tests\InfiniteSky.Persistence.Tests.csproj --no-restore
Remove-Item Env:\INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS
```

If Docker is not available, keep the opt-in unset and report these tests as skipped. Do not point the suite at production MongoDB or production data.

## Coverage and formatting

Each test project references `coverlet.collector`. Collect Cobertura reports with:

```powershell
dotnet test InfiniteSky.sln --no-build --collect:"XPlat Code Coverage"
```

Reports are written under each test project's `TestResults` directory. Coverage percentages describe executed lines/branches in the instrumented assemblies; they are not behavioral parity evidence and must not be added across projects because dependencies may be instrumented in more than one project.

Formatting/analyzer check used by CI:

```powershell
dotnet format InfiniteSky.sln --verify-no-changes --no-restore
```

The GitHub workflow builds/tests Release on Windows and Ubuntu and runs the same formatting verification.

## Unavailable test commands

- No differential runner exists for the Node.js server. The legacy package script points to `node test.js`, but `test.js`, `node_modules`, and a working `config.json` are absent. Do not run `npm test` as a parity check.
- No original-client test harness/build is available.
- No full character-to-world/gameplay end-to-end harness exists.
- No performance/benchmark project or property-based test runner is configured.
