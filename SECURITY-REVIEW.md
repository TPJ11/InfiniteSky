# InfiniteSky Security Review Summary

Review baseline: `4159f887517fbb34145adfa92651b6d07a24c733`

Review commit: `45ffea3`
Scope: bounded static review of security-sensitive .NET paths and relevant legacy login behavior; no production systems were contacted.

| # | Severity | File | Lines | Vulnerability | Confidence |
|---|----------|------|-------|---------------|------------|
| 1 | 🟠 HIGH | [db/account.js](db/account.js#L11) | 11 | SEC-002: Legacy account passwords are stored as plaintext and remain read for compatibility by the .NET repository. | 10/10 |
| 2 | 🟠 HIGH | [NetworkConnection.cs](src/InfiniteSky.Networking/NetworkConnection.cs#L269) | 269 | SEC-001: Login traffic uses raw TCP without encryption when exposed to an untrusted network. | 9/10 |
| 3 | 🟡 MEDIUM | [InfiniteSkyOptionsValidator.cs](src/InfiniteSky.Server/Configuration/InfiniteSkyOptionsValidator.cs#L118) | 118 | SEC-003: Remote MongoDB URIs are accepted without requiring TLS; no deployed URI was inspected. | 8/10 |
| 4 | ⚪ LOW | [AuthenticationIncomingPacketSink.cs](src/InfiniteSky.Server/Authentication/AuthenticationIncomingPacketSink.cs#L188) | 188 | SEC-004: Distinct unknown-account and incorrect-password statuses permit account enumeration. | 10/10 |

These are not claims of a successful attack or of production exposure. The server defaults disable networking and MongoDB and bind listeners to loopback. The server is **not ready for public deployment or final release preparation**. No compatibility-sensitive runtime changes were made because the legacy wire/storage behavior and deployment configuration cannot be safely changed without client and account migration evidence.

The login path has no per-account/IP rate limiter; repeated guessing is a potential risk, not a confirmed vulnerability. Multi-instance session uniqueness and global active-marker reset are documented reliability residuals. .NET administrative, script, character/world, and gameplay network handlers remain absent or blocked, not verified secure.

See the detailed [security findings register](docs/migration/SECURITY_FINDINGS.md), [threat model](docs/migration/THREAT_MODEL.md), [residual risks](docs/migration/RESIDUAL_SECURITY_RISKS.md), and [hardening checklist](docs/migration/PRODUCTION_HARDENING_CHECKLIST.md). Baseline restore/build/tests passed; 188 tests passed and 2 opt-in MongoDB tests were skipped. NuGet reported no vulnerable packages from nuget.org. Original Node.js source files were unchanged.
