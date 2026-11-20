# Production Hardening Checklist

This checklist is evidence-based and intentionally distinguishes reviewed, partially tested, blocked, and untested areas. A passing unit test or static source review alone does not establish production readiness.

| Area | Status | Evidence / remaining work |
|---|---|---|
| Dependency vulnerability review | PARTIAL | NuGet audit found no vulnerable packages from nuget.org; npm dependency graphs lack lockfiles and were not audited. |
| Network exposure | PARTIAL | Defaults disable listeners and bind to loopback; public bind/firewall/TLS deployment was not tested. |
| Packet validation | PARTIAL | Framing bounds, truncation, fragmentation/coalescing, and connection limits have tests; flood/resource-exhaustion behavior is untested. |
| Authentication | PARTIAL | Legacy login path and session tests pass; plaintext legacy credential storage and cleartext-compatible transport remain risks. |
| Session and character ownership | PARTIAL | Account session store and component ownership checks are tested; character-selection/world-entry network flow is absent. |
| Administrative authorization | BLOCKED | No .NET command dispatcher or remote admin interface; legacy privilege inventory is documented but no handlers were tested. |
| Gameplay validation and duplication | BLOCKED | Client-reachable gameplay/economy handlers are absent; no claim about item/currency duplication resistance. |
| MongoDB safety and recovery | PARTIAL | Configuration/mappers and two opt-in integration tests exist; tests were skipped and no live deployment was tested. |
| Script trust boundary | PARTIAL | No .NET script runtime; legacy in-process scripts are trusted code and not sandboxed. |
| Configuration and secrets | PARTIAL | Safe defaults and environment overrides are documented; production secret injection and full secret scanning were not verified. |
| Logging and information disclosure | PARTIAL | Existing docs/tests specify redaction; a production log review and complete exception-path audit were not verified. |
| Startup failure handling | PARTIAL | Invalid configuration and listener-start rollback tests pass; database/zone/script production failures were not exercised. |
| Shutdown reliability | PARTIAL | Login connection/session cleanup is tested; active-player saves and lossless shutdown are not implemented/verified. |
| Concurrency | PARTIAL | Selected login/session/world component races have tests; end-to-end gameplay and persistence concurrency are untested. |
| Abuse resistance | NOT_STARTED | No controlled malformed-packet flood, brute-force, slow-client, queue-saturation, or connection-rate test. |
| Performance after hardening | NOT_APPLICABLE | No runtime hardening changes have been benchmarked in this stage baseline; see Prompt 23 login-only measurements. |
| Original-client compatibility | BLOCKED | No original client was available; protocol-sensitive security changes cannot be certified against it. |
| Deployment, backups, recovery | NOT_STARTED | No production-style service/container, firewall, least-privilege process, backup, or restore rehearsal. |
| Residual risks documented | IN_PROGRESS | See [RESIDUAL_SECURITY_RISKS.md](./RESIDUAL_SECURITY_RISKS.md); do not use this checklist as a release approval. |

The server is **not ready for final release preparation**. See [security findings](./SECURITY_FINDINGS.md), [security test results](./SECURITY_TEST_RESULTS.md), and [operational recovery](./OPERATIONAL_RECOVERY.md).
