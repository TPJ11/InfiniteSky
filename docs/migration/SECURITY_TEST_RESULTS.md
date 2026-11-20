# Security Test Results

## Baseline

At commit `4159f887517fbb34145adfa92651b6d07a24c733`, `dotnet restore InfiniteSky.sln`, `dotnet build InfiniteSky.sln --no-restore`, and `dotnet test InfiniteSky.sln --no-build` completed successfully. The suite reported 188 passed, 0 failed, and 2 skipped opt-in MongoDB Testcontainers tests. Per-project totals and dependency audit commands/results are in [SECURITY_HARDENING_BASELINE.md](./SECURITY_HARDENING_BASELINE.md).

The following existing tests ran in that full suite:

| Area | Evidence exercised |
|---|---|
| Framing | Incomplete header/negative size rejection; partial and coalesced frames; receive-buffer overflow; configured frame-size rejection |
| Connection lifecycle | Admission at the configured test limit; remote disconnect cleanup exactly once; ordered network processing; listener shutdown |
| Authentication/session | Concurrent duplicate-account login; repeated request serialization; disconnect cancellation; timeout rollback; invalid fields; missing password; repository/session-store failures; cleanup |
| Ownership | Player activation requires an owned character; foreign character and session-expiry rejection; duplicate activation handling |
| Persistence mapping/configuration | Malformed identity and credential type rejection; connection-string/database-name mismatch rejection without exposing the URI |
| Startup/host lifecycle | Invalid configuration; listener bind failure and startup rollback; hosted-service cancellation; host shutdown cleanup |

These are deterministic unit/loopback tests, not an external penetration test. The tests do not establish original-client behavior, safe public-network exposure, or production database guarantees.

## Dependency checks

`dotnet list InfiniteSky.sln package --vulnerable --include-transitive` reported no vulnerable packages from the configured NuGet source at baseline. This is source- and date-bounded. The legacy Node.js/FruitSalad manifests have no lockfiles, so no reproducible npm audit ran; this must not be interpreted as a clean audit of those dependency graphs.

A bounded heuristic scan of tracked text files found no private-key block or credential-bearing MongoDB URI. The only directly classified URI was an unauthenticated loopback development value; other credential-like matches were documentation/vendor text, tests, and the in-memory performance harness. The scan is not an exhaustive secret-detection guarantee.

## Not run / not established

- MongoDB integration and failure recovery: 2 opt-in tests skipped; no live database was contacted.
- Original Node.js differential or original game client: unavailable.
- Public-network/TLS, credential sniffing, or third-party attack tests: not run.
- Brute-force/rate-limit, slow-client, malformed-packet flood, queue-saturation, and resource exhaustion tests: not run.
- Character ownership via client packet, GM authorization, gameplay abuse, economy duplication: no corresponding client-reachable .NET handlers exist.
- Script sandbox escape: no .NET script runtime exists; the legacy runtime is trusted in-process code and is not a sandbox.
- Production secret-store, firewall, filesystem-permission, service-identity, backup/restore, and deployment tests: not run.

For the scoped source review and disposition of findings, see [SECURITY_FINDINGS.md](./SECURITY_FINDINGS.md), [THREAT_MODEL.md](./THREAT_MODEL.md), and [RESIDUAL_SECURITY_RISKS.md](./RESIDUAL_SECURITY_RISKS.md).
