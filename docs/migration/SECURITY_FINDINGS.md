# Security Findings Register

## Assessment scope

This register is based on a bounded static review of security-sensitive .NET paths and the relevant legacy login source at baseline commit `4159f887517fbb34145adfa92651b6d07a24c733`. For legacy administration and scripting, it relies on the earlier source-derived [authorization](./ADMIN_AUTHORIZATION.md), [administrative audit](./ADMIN_AUDIT_AND_SECURITY.md), and [script trust](./SCRIPTING_SECURITY_MODEL.md) reports. No external or production system was contacted. The full baseline and tests are in [SECURITY_HARDENING_BASELINE.md](./SECURITY_HARDENING_BASELINE.md); implementation boundaries are in [THREAT_MODEL.md](./THREAT_MODEL.md).

Severity is conditional on the documented preconditions. `HIGH` assumes the affected listener/data path is exposed to an untrusted party; safe defaults reduce exposure but do not remove the design issue. A static review does not prove a deployed configuration is exploitable.

## Findings

| ID | Severity | Affected subsystem | Status |
|---|---|---|---|
| SEC-001 | HIGH | Login transport confidentiality | Open; compatibility/deployment mitigation required |
| SEC-002 | HIGH | Legacy password storage | Open; requires coordinated credential migration |
| SEC-003 | MEDIUM | MongoDB transport configuration | Conditional; deployed URI/TLS not inspected |
| SEC-004 | LOW | Login response information disclosure | Confirmed legacy-compatible behavior |

### SEC-001 - Login credentials travel over unencrypted TCP

- **Location:** [`NetworkConnection`](../../src/InfiniteSky.Networking/NetworkConnection.cs#L48) obtains a raw `NetworkStream` and its [`receive/write calls`](../../src/InfiniteSky.Networking/NetworkConnection.cs#L269) transfer unencrypted bytes; [`AuthenticationIncomingPacketSink`](../../src/InfiniteSky.Server/Authentication/AuthenticationIncomingPacketSink.cs#L79) decodes and passes the login request; [`LegacyLoginRequest`](../../src/InfiniteSky.Protocol/Structures/LegacyLoginRequest.cs#L9) contains fixed-width username/password fields; [`NetworkHostedService`](../../src/InfiniteSky.Server/Hosting/NetworkHostedService.cs#L77) creates the configured listeners. The default listener configuration is disabled and loopback-bound in [`appsettings.json`](../../src/InfiniteSky.Server/appsettings.json#L17).
- **Preconditions:** Networking is enabled and a listener is reachable over a network an attacker can observe.
- **Impact:** A passive observer can recover login credentials from the packet and reuse them.
- **Reproduction:** Confirmed by source path inspection. No packet capture or real credential test was performed. The loopback integration test sends synthetic source-compatible login frames but does not test confidentiality.
- **Severity/confidence:** HIGH when exposed on an untrusted network; 9/10 confidence in the transport behavior.
- **Compatibility:** The original Node listener also uses raw TCP and the source login packet carries these fields ([`login.js`](../../login.js#L21), [`handleLogin.js`](../../packets/login/handleLogin.js#L48)). Adding TLS directly to the listener may break existing clients and is not certified.
- **Remediation/status:** No wire-level code change was made. Keep the listener disabled or restricted to trusted networks; use an approved VPN/tunnel or a separately versioned client/server transport change. Do not send real credentials over an untrusted network.
- **Residual risk:** High for any deployment that exposes the legacy protocol without compatible network-layer protection.

### SEC-002 - Legacy password values are stored in plaintext

- **Location:** The original account model stores `Password` as a Mongo string ([`account.js`](../../db/account.js#L11)); account creation persists the supplied password ([`createaccount.js`](../../cli/createaccount.js#L13)); the active legacy login handler compares stored and supplied strings directly ([`handleLogin.js`](../../packets/login/handleLogin.js#L321)). The .NET Mongo repository still projects the compatibility field ([`MongoAccountAuthenticationRepository`](../../src/InfiniteSky.Persistence/MongoAccountAuthenticationRepository.cs#L43)); the mapper converts it to UTF-8 bytes ([`LegacyAccountAuthenticationDocumentMapper`](../../src/InfiniteSky.Persistence/LegacyAccountAuthenticationDocumentMapper.cs#L37)); the .NET verifier uses a fixed-time byte comparison ([`LegacyPlaintextCredentialVerifier`](../../src/InfiniteSky.Application/Authentication/LegacyPlaintextCredentialVerifier.cs#L11)).
- **Preconditions:** An attacker or unauthorized operator obtains read access to the legacy account collection or its backups.
- **Impact:** Stored account passwords are exposed and may be reused against other services. Fixed-time comparison in .NET does not protect the at-rest representation.
- **Reproduction:** Confirmed by source and mapping inspection. No production database was accessed; the two opt-in MongoDB integration tests were skipped.
- **Severity/confidence:** HIGH impact if the account data is disclosed; 10/10 confidence in the legacy storage behavior.
- **Compatibility:** Replacing the field with a hash without coordinated changes would break legacy Node authentication and existing account compatibility.
- **Remediation/status:** Open. Requires an approved migration that updates all active authenticators, handles legacy records safely, and defines password reset/rehash behavior. No schema or stored credential format was changed in this stage.
- **Residual risk:** Treat account collections, exports, and backups as credential-bearing secrets; restrict database/backup access and plan coordinated credential migration before public production.

### SEC-003 - Remote MongoDB TLS is not required by application validation

- **Location:** [`InfiniteSkyOptionsValidator`](../../src/InfiniteSky.Server/Configuration/InfiniteSkyOptionsValidator.cs#L118) accepts MongoDB connection-string schemes; [`MongoPersistenceContext`](../../src/InfiniteSky.Persistence/MongoPersistenceContext.cs#L30) builds the client settings from the configured URI. The checked-in default disables MongoDB and has an empty URI ([`appsettings.json`](../../src/InfiniteSky.Server/appsettings.json#L29)).
- **Preconditions:** MongoDB is enabled with a remote `mongodb://` URI that does not request TLS, and the database path is observable.
- **Impact:** Database credentials and traffic may be exposed in transit.
- **Reproduction:** Confirmed that code does not require TLS; no deployment URI or remote database was inspected, so exposure is conditional and not established for a real deployment.
- **Severity/confidence:** MEDIUM conditional risk; 8/10 confidence in the configuration behavior.
- **Compatibility:** This does not alter the game-client protocol. Requiring TLS could break a deployment that relies on a trusted non-TLS database network.
- **Remediation/status:** Open pending deployment topology and certificate requirements. Require TLS and validate certificates for remote MongoDB deployments, or keep database traffic on a protected trusted network. Verify the actual configured URI and behavior before enabling production persistence.
- **Residual risk:** The application will accept a non-TLS remote URI; no live Mongo/TLS behavior was tested.

### SEC-004 - Login status distinguishes unknown accounts from incorrect passwords

- **Location:** Legacy handler emits separate outcomes ([`handleLogin.js`](../../packets/login/handleLogin.js#L289)); the .NET sink maps separate outcomes ([`AuthenticationIncomingPacketSink`](../../src/InfiniteSky.Server/Authentication/AuthenticationIncomingPacketSink.cs#L188)); status values are defined in [`LegacyLoginStatus`](../../src/InfiniteSky.Protocol/Structures/LegacyLoginStatus.cs#L8).
- **Preconditions:** An unauthenticated actor can submit login requests to a reachable listener.
- **Impact:** The response can be used to enumerate valid account names, which can inform targeted guessing.
- **Reproduction:** Confirmed by source mapping and existing login tests. No account data or external service was queried.
- **Severity/confidence:** LOW; 10/10 confidence in the distinct response behavior.
- **Compatibility:** This preserves the source protocol's status mapping. Replacing it with a uniform response would be a client-visible behavior change.
- **Remediation/status:** No change made; retain only with explicit compatibility acceptance and pair with transport isolation and future abuse controls.
- **Residual risk:** Account existence is disclosed to a reachable login client.

## Reliability/architecture residual (not a confirmed vulnerability)

### REL-001 - Multi-instance account/session state is not coordinated

- **Location:** The in-memory store indexes sessions inside one process ([`InMemoryAuthenticationSessionStore`](../../src/InfiniteSky.Application/Authentication/InMemoryAuthenticationSessionStore.cs#L5)); `ResetActiveAccountsOnStartup` defaults true ([`DatabaseOptions.cs`](../../src/InfiniteSky.Server/Configuration/DatabaseOptions.cs#L50)); startup invokes a global update ([`MongoPersistenceHostedService.cs`](../../src/InfiniteSky.Server/Hosting/MongoPersistenceHostedService.cs#L116), [`MongoAccountAuthenticationRepository.cs`](../../src/InfiniteSky.Persistence/MongoAccountAuthenticationRepository.cs#L96)).
- **Preconditions:** Multiple server instances share one account database.
- **Impact:** One instance can clear other instances' persisted active markers at startup, and independent process-local registries do not prevent the same account from opening sessions on different instances.
- **Reproduction:** Confirmed by source control flow; no multi-instance or live MongoDB test was run.
- **Severity/confidence:** Reliability/ownership risk; 9/10 confidence in source behavior. It is not a vulnerability for the documented single-process model.
- **Compatibility:** Startup reset behavior mirrors legacy global active-flag behavior; changing it affects stale-session handling.
- **Remediation/status:** Keep deployment single-process unless a distributed ownership design and stale-active policy are approved and tested. Open for any multi-instance deployment.
- **Test coverage:** Existing unit tests cover same-process duplicate login; none cover shared-database multi-instance behavior.
- **Residual risk:** Shared-database multi-instance operation is unsupported/unverified and must not be inferred safe.

## Findings not raised as confirmed issues

No .NET GM command dispatcher, remote administration interface, JavaScript runtime, or plugin loader is implemented. These features are blocked/absent, not verified secure. Legacy low-level command permissions and in-process script execution are documented as Node.js compatibility risks in [ADMIN_AUTHORIZATION.md](./ADMIN_AUTHORIZATION.md), [ADMIN_AUDIT_AND_SECURITY.md](./ADMIN_AUDIT_AND_SECURITY.md), and [SCRIPTING_SECURITY_MODEL.md](./SCRIPTING_SECURITY_MODEL.md).

The reviewed login path has no per-account/IP rate limiter and leaves the connection open after an incorrect-password reply. Repeated guessing is a **potential** risk if the listener is reachable, but no successful attack or workload impact was demonstrated. It is not recorded as a confirmed vulnerability; an attempt-rate/lockout policy needs legitimate-client and NAT-sharing requirements before implementation.

No other confirmed vulnerability was established in the bounded review. Unreviewed domain/gameplay flows and all untested deployment/database scenarios remain outside that conclusion.
