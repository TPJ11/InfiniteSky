# Deployment

## Status and scope

Only a local framework-dependent `win-x64` candidate is prepared in Prompt 25. This document does not establish a supported production topology. The server is **NOT_READY** for public deployment: client-facing world/gameplay handling is incomplete and high-severity credential risks remain. Do not expose it to untrusted networks.

## Prerequisites

- Windows x64 for the candidate built and smoke-tested in this stage.
- .NET 10 runtime (`Microsoft.NETCore.App`); SDK `10.0.401` is required to build from source.
- MongoDB only if persistence is enabled. No production MongoDB version minimum, connection topology, or deployed schema/index inventory has been verified.
- Required authorized server-side game data supplied separately when zones are configured. The candidate contains no game data or client assets.

The GitHub workflow is configured for Windows and Ubuntu build/test jobs, but that is not evidence that Linux deployment or a Linux release artifact was validated. Only `win-x64` is included in the Prompt 25 manifest. Self-contained publishing, Linux/ARM RIDs, trimming, and Native AOT are not supported or claimed here.

## Install a local candidate

Extract the candidate archive to a clean directory, preserve its contents and SHA-256 from [RELEASE_MANIFEST.md](migration/RELEASE_MANIFEST.md), and run from that directory:

```powershell
.\InfiniteSky.Server.exe
```

The package is framework-dependent; install a supported .NET 10 runtime separately. To run the entry assembly directly:

```powershell
dotnet .\InfiniteSky.Server.dll
```

The candidate defaults disable networking and MongoDB and select no zones. This safe default can start the foundation but does not provide a playable server or healthy world. The server currently has no HTTP readiness endpoint.

## Configuration

Configuration sources are `appsettings.json`, environment-specific JSON, command-line configuration, and `INFINITESKY_`-prefixed environment variables. Examples and every implemented option are listed in [RUNTIME_CONFIGURATION.md](migration/RUNTIME_CONFIGURATION.md). Treat the packaged `appsettings.json` as a safe template; do not put production secrets in JSON or source control.

To configure an isolated MongoDB test, inject the URI through the approved protected environment mechanism. Verify database name, collection names, index state, TLS, credentials, and the startup-wide active-account reset before enabling persistence. `ResetActiveAccountsOnStartup` defaults true and can affect all documents in the configured account collection.

If configuring a zone, provide required data under the host content root's `data` directory. Missing optional NPC/WREGION files produce degraded zone diagnostics; `.IMG` catalogs and the `.WREGION` corpus needed for full world behavior are not present in the repository. No data migration or seeding is performed by deployment.

## Network and database exposure

The implemented game listeners are raw TCP, login port `11091` and world port `11092`, disabled by default and loopback-bound when enabled without an address override. The world listener does not implement a gameplay flow. Do not open these ports to the Internet or use real credentials on an untrusted path. There is no built-in TLS protocol, web management service, or public health endpoint.

Back up existing MongoDB data before enabling the host. This code does not perform an automatic backup or reverse database writes during binary rollback. Follow [DATABASE.md](DATABASE.md) and [OPERATIONS.md](OPERATIONS.md).

## Upgrade and rollback

No schema migration is part of this candidate. Keep a copy of the prior binaries and configuration, take an operator-managed database backup, and test restore against an isolated copy. If startup fails, disable listeners, preserve logs and configuration, restore prior binaries/configuration, and validate database state before any manual repair. These procedures have not been rehearsed against production.

No container definition, Windows Service installer, Linux service unit, cloud deployment, automatic restart policy, or automated rollback is included.
