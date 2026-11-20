# Development

## Requirements and workflow

Use the SDK pinned by `global.json` (`10.0.401`), central package versions in `Directory.Packages.props`, and checked-in `packages.lock.json` files:

```powershell
dotnet restore InfiniteSky.sln --locked-mode
dotnet build InfiniteSky.sln --no-restore
dotnet test InfiniteSky.sln --no-restore
dotnet format InfiniteSky.sln --verify-no-changes --no-restore
```

CI is configured to run restore/build/test/format on Windows and Ubuntu. This does not certify deployment support on every platform.

## Solution structure

- `InfiniteSky.Domain`: domain records, IDs, value objects, and narrow source-derived arithmetic.
- `InfiniteSky.Application`: authentication/session and player/application use-case boundaries.
- `InfiniteSky.Protocol`: bounded binary readers/writers, frame descriptors, and selected legacy structures.
- `InfiniteSky.Networking`: TCP transport, framing, send queue and connection lifecycle.
- `InfiniteSky.Persistence`: Mongo client/repositories, raw BSON adapters, health and error classification.
- `InfiniteSky.GameData`: selected source-backed legacy zone resource readers.
- `InfiniteSky.World`: world/zone lifecycle, spatial candidates and internal player registry.
- `InfiniteSky.Scripting`: project boundary only; no runtime.
- `InfiniteSky.Server`: `Program` entry point, Generic Host, options, DI and hosted-service composition.
- `tools/InfiniteSky.PerformanceHarness`: real-loopback login/auth test harness, not a gameplay simulator.
- `tests/`: six test projects for domain, game data, persistence, protocol, server integration and world.

`Program.Main` delegates to `ServerApplication`, which builds `ServerHostBuilder`. Hosted-service registration is ordered to satisfy persistence → zones → world → players → networking → readiness startup dependencies; shutdown is reversed. Services use explicit singleton lifetimes for the current host.

## Protocol and networking

The transport and binary compatibility rules are in [PROTOCOL.md](PROTOCOL.md) and [protocol specification](protocol/PROTOCOL_SPECIFICATION.md). Network receives are bounded and pass through the frame registry. The production packet sink handles the login authentication path only; world gameplay dispatch is absent. Do not add packet fields or response fallbacks without source/capture evidence.

## Persistence and data

MongoDB repositories use targeted raw-BSON projections/updates. Collection names and deployed BSON assumptions require confirmation. Do not replace whole documents or drop indexes. See [DATABASE.md](DATABASE.md) and [MONGODB_SCHEMA_MAPPING.md](migration/MONGODB_SCHEMA_MAPPING.md).

Zone resources are loaded from the host content root's `data` directory; current data inventory and limitations are in [FINAL_GAP_AUDIT.md](migration/FINAL_GAP_AUDIT.md). Do not add generated gameplay catalogs as substitutes for missing authorized legacy assets.

## World, gameplay, scripts and commands

World/zone/player components provide internal lifecycle boundaries, not complete simulation or client flow. The isolated XP/stat calculations are not connected to packet, data or persistence systems. The C# Scripting project contains no compatible JavaScript runtime; administration/commands are not wired. These limitations are tracked in the parity matrix and [FINAL_FEATURE_RECONCILIATION.md](migration/FINAL_FEATURE_RECONCILIATION.md).

## Tests and changes

Unit/component tests do not establish original-client parity. Mongo integration tests require Docker and explicit opt-in. The performance harness measures login only. Preserve test fixture provenance, run full validation for changes, update the matching feature evidence, and never mark a feature `VERIFIED` based solely on a successful build.

Changes should remain within project boundaries and retain the original Node.js sources as the behavioral oracle. No generated release artifacts, databases, secrets, or unauthorized game/client assets belong in source commits.
