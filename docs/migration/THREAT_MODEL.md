# InfiniteSky .NET 10 Threat Model

## Scope and assumptions

This model covers the migrated .NET host at Prompt 24 baseline commit `4159f887517fbb34145adfa92651b6d07a24c733`, plus legacy Node.js behavior only where it is required to assess compatibility or deployment risk. It does not claim that all legacy features exist in .NET. The server is a partial migration, not a playable or production-ready replacement; world, character, gameplay, scripting, and administrative network workflows are incomplete or blocked.

Assume an unauthenticated remote actor can reach a listener only if an operator enables networking and exposes its configured bind address/port. Do not assume database, operator-shell, or filesystem access by that actor. Production firewalling, process identity, protected secret injection, and deployment topology are not verified.

See [security baseline](./SECURITY_HARDENING_BASELINE.md), [verification readiness](./VERIFICATION_READINESS.md), [runtime configuration](./RUNTIME_CONFIGURATION.md), and [networking implementation](./NETWORKING_IMPLEMENTATION.md).

## Assets

| Asset | Security / reliability interest |
|---|---|
| Account credentials and identifiers | Prevent disclosure, guessing, unauthorized login, and cross-account confusion. |
| Authenticated session and account `Level` | Preserve server ownership, invalidate on disconnect, and prevent privilege forgery. |
| Character and gameplay state in MongoDB | Preserve ownership, legacy field types, and unrelated document data. Current network workflows do not expose character/gameplay mutations. |
| Listener and host availability | Bound untrusted connection, frame, timeout, and queue resource use; fail startup/shutdown transparently. |
| Configuration and MongoDB credentials | Keep secrets out of source, logs, packets, and diagnostics; bind only intended network interfaces. |
| Operator privileges and server process | Restrict direct process/host access and future administrative execution. |
| Game data and script/plugin code | Preserve integrity and prevent untrusted code from acquiring server process capabilities. |

## Trust boundaries and entry points

| Boundary | Current implementation and trust assumptions |
|---|---|
| Game client -> server | Optional raw TCP login and world listeners. They are disabled by default and bind to loopback by default. If enabled on a reachable interface, framing and payloads are untrusted. There is no TLS or packet encryption in the legacy-compatible transport; the original login structure carries username/password fields. |
| Connection -> authenticated session | A new TCP connection is unauthenticated. The login sink handles the implemented login opcode; authentication uses a server-side account repository and creates an in-process session. The world listener does not implement client world gameplay. Connection cleanup removes the in-process session. |
| Account -> selected character | No network character-selection or character-to-session authorization workflow exists in the migrated server. The server-loaded account `Level` is distinct from client/character `IsGM` data. Ownership remains a release blocker before any character workflow is enabled. |
| Player -> GM/operator | No .NET game-command dispatcher or remote administration endpoint exists. Original Node game commands use account `Level`, while legacy CLI/REPL capabilities are trusted local-process operations; they are not migrated as remote features. |
| Script/plugin -> core server | The .NET solution has no JavaScript execution engine or runtime plugin API. The legacy Node script loader evaluates trusted server code in-process; that is not a sandbox and must not be used for untrusted scripts. |
| Server -> MongoDB | MongoDB is optional and disabled by default. When enabled, the server uses configured credentials/URI and a single driver client. The connection string is supplied through configuration, not logged by design. Database permissions, server version, production topology, and live failure recovery are unverified. |
| Configuration -> runtime | JSON defaults, environment-specific JSON, command-line options, and `INFINITESKY_` environment variables influence listeners, database, zones, and timeouts. The checked-in default disables network/database and binds to loopback. Protected deployment configuration has not been tested. |
| Administrative operator -> server | The .NET host exposes no remote management interface. Local process, filesystem, environment, and deployment privileges are external trust boundaries not established by repository tests. |

## Potential attacker capabilities and abuse scenarios

1. **Reachable unauthenticated client:** open connections, send fragmented/coalesced/malformed frames, submit login attempts, or disconnect abruptly. Existing controls include bounded receive/frame sizes, a bounded send queue, configured connection maximum, idle timeout, and authentication timeout. Repeated-authentication rate limiting and public-network abuse behavior require separate evidence.
2. **Credential observer on an untrusted network:** capture unencrypted legacy TCP login traffic. TLS/encryption cannot be added transparently without verifying original-client compatibility; use a trusted network or deployment-level tunnel where compatible and approved.
3. **Credential guesser:** exploit repeated login attempts or distinct unknown-account/incorrect-password outcomes. The legacy status mapping is retained for compatibility; it reveals account existence. The legacy database stores plaintext passwords, so database disclosure has high impact.
4. **Database/configuration compromise:** use a leaked or over-privileged MongoDB URI, exploit misconfigured network access, or trigger startup-wide account-active reset behavior. The application defaults do not enable MongoDB; database users and deployment policies must be least-privilege and isolated.
5. **Untrusted script/plugin author:** attempt code execution or resource exhaustion. No untrusted script execution exists in the .NET host. The original Node runtime executes trusted code with process privileges and has no sandbox.
6. **Forged character or GM identity:** supply account/character/GM claims in future packet handlers. Current migrated code has no network character-selection or command path; any future handler must derive authority and ownership from authenticated server state.
7. **Local operator/process compromise:** use host permissions, local console, environment, or deployment tooling. This repository does not establish OS account separation, secret store, firewall, or remote-operator policy.

## Existing controls and residual posture

- Networking and MongoDB are off by default; the default bind address is loopback.
- Networking options bound concurrent connections, receive/frame bytes, send queue capacity, idle timeout, and connection shutdown timeout.
- Protocol framing uses a fixed registry and size bounds; integration tests cover fragmentation/coalescing, over-limit input, disconnect, send serialization, and connection admission.
- Authentication uses server-loaded account data, per-connection state, a bounded operation timeout, and an atomic one-session-per-account store. Password values and request payloads are cleared from mutable byte buffers; decoded .NET strings cannot be reliably zeroed.
- Logging and Mongo persistence documentation specify that credentials, connection strings, and packet payloads are not logged. This is not equivalent to a production log review.
- Account-level data is server-derived. No .NET game command dispatcher, script runtime, or gameplay packet handler currently creates those attack surfaces.
- Two MongoDB Testcontainers tests are opt-in and were skipped at baseline. No live database, client, public network, production deployment, or original Node differential test was run.

The verified findings, compatibility differences, and unresolved risks are maintained in [SECURITY_FINDINGS.md](./SECURITY_FINDINGS.md) and [RESIDUAL_SECURITY_RISKS.md](./RESIDUAL_SECURITY_RISKS.md). This model is an implementation-scoped assessment, not a claim that the system is secure or production-ready.

## Source map

- The raw socket lifecycle is implemented by [`NetworkConnection`](../../src/InfiniteSky.Networking/NetworkConnection.cs#L48) and [`NetworkHostedService`](../../src/InfiniteSky.Server/Hosting/NetworkHostedService.cs#L77); login frame dispatch and credential handoff are in [`AuthenticationIncomingPacketSink`](../../src/InfiniteSky.Server/Authentication/AuthenticationIncomingPacketSink.cs#L56), using [`LegacyLoginRequest`](../../src/InfiniteSky.Protocol/Structures/LegacyLoginRequest.cs#L9).
- Authentication state and session creation are in [`AuthenticationService`](../../src/InfiniteSky.Application/Authentication/AuthenticationService.cs#L42); account projection/mapping and Mongo connection behavior are in [`MongoAccountAuthenticationRepository`](../../src/InfiniteSky.Persistence/MongoAccountAuthenticationRepository.cs#L43), [`LegacyAccountAuthenticationDocumentMapper`](../../src/InfiniteSky.Persistence/LegacyAccountAuthenticationDocumentMapper.cs#L37), and [`MongoPersistenceContext`](../../src/InfiniteSky.Persistence/MongoPersistenceContext.cs#L30).
- The process-local account registry is [`InMemoryAuthenticationSessionStore`](../../src/InfiniteSky.Application/Authentication/InMemoryAuthenticationSessionStore.cs#L5); startup active-marker reset is configured by [`DatabaseOptions`](../../src/InfiniteSky.Server/Configuration/DatabaseOptions.cs#L50) and invoked by [`MongoPersistenceHostedService`](../../src/InfiniteSky.Server/Hosting/MongoPersistenceHostedService.cs#L116).
- The original account password, active login comparison, command authorization, and in-process script trust boundary are documented with legacy source references in [authentication](./AUTHENTICATION_IMPLEMENTATION.md), [administrative authorization](./ADMIN_AUTHORIZATION.md), [administrative audit](./ADMIN_AUDIT_AND_SECURITY.md), and [scripting trust model](./SCRIPTING_SECURITY_MODEL.md).
