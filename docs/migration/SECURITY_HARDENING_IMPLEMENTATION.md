# Prompt 24 Security Hardening Decisions

## Scope and outcome

The source review examined security-sensitive .NET networking, login, session, persistence, startup/shutdown, script, and administrative boundaries, plus the relevant legacy login source. Legacy Node.js administrative and script behavior is cited from the earlier source-derived [authorization](./ADMIN_AUTHORIZATION.md), [administrative audit](./ADMIN_AUDIT_AND_SECURITY.md), and [script trust](./SCRIPTING_SECURITY_MODEL.md) reports. This was a bounded static review, not a line-by-line audit of every domain/gameplay model, not an external penetration test, and not a production deployment test.

No runtime or dependency code change was made in this stage. The confirmed high-impact issues depend on legacy wire/storage behavior and cannot be safely removed without client/account migration evidence. The conditional MongoDB TLS risk has no deployed connection string or topology to validate. No unsupported game feature, schema, permission, packet, or client behavior was invented. The findings and required deployment decisions are explicit in [SECURITY_FINDINGS.md](./SECURITY_FINDINGS.md).

## Finding and observation disposition

| Finding | Hardening decision | Compatibility / evidence |
|---|---|---|
| SEC-001 - plaintext game login transport | Keep listeners disabled and loopback-bound by default. Require a trusted network or approved tunnel for current clients; do not claim TLS. | The legacy client wire protocol is raw TCP. No client or TLS-capable gateway was available to validate an in-band protocol change. |
| SEC-002 - plaintext legacy password storage | No field/hash conversion was made. Restrict access to MongoDB, exports, and backups; plan a coordinated migration across every active authenticator. | Changing stored format without changing the original Node server would break legacy login. No representative/live account database was available. |
| SEC-003 - remote MongoDB TLS not required | Require operators to use TLS and certificate validation for remote databases, or a separately protected database network; verify the actual URI before enabling persistence. | No deployed URI or MongoDB service was available. Enforcing a new default without deployment compatibility evidence was deferred. |
| SEC-004 - distinct unknown-account/password replies | Preserve the source status mapping and document account enumeration. | Response codes are part of the observed legacy protocol; no original-client test was available to justify changing them. |
| OBS-001 - repeated online login attempts | Do not add arbitrary per-IP/account limits or lockout behavior. Keep the listener isolated pending an approved policy and legitimate-client workload evidence. | The server has a connection ceiling and per-operation timeout but no attempt-rate policy. No brute-force test was run and no successful attack was demonstrated. |
| REL-001 - process-local session store/global active reset | Treat the deployed model as single-process until a separately reviewed distributed ownership design exists. For multiple instances, disable the global startup reset only after stale-active semantics are explicitly managed. | Multi-instance deployment and live database behavior are not tested; no database lock/schema or cross-process guarantee was invented. |

## Existing controls retained

The migration already includes bounded receive/frame sizes and send queue, configurable connection and idle limits, per-authentication timeout, fixed-time comparison of compatibility credential bytes, buffer clearing where mutable, an atomic in-process account session store, typed MongoDB filters/ownership predicates in existing repositories, and structured sanitized persistence diagnostics. The baseline suite exercises the named unit/loopback paths; it does not establish their correctness under malicious public traffic or production MongoDB.

No GM command dispatcher, remote administration interface, JavaScript runtime, or plugin loader was added. Those attack surfaces remain unimplemented/blocked, not verified secure. Legacy Node command and script risks remain documented and are not silently carried into the .NET host.

## Dependency decisions

At baseline, NuGet reported no known vulnerable packages from `nuget.org`. Updates were available (including a MongoDB.Driver patch and test/transitive packages); the deprecated-package audit identified xUnit 2 as legacy and an older transitive logging package in some test graphs. No package was upgraded without a vulnerability advisory and compatibility test requirement. Legacy npm dependencies were not updated and lack lockfiles, preventing a reproducible dependency audit; the Node.js sources remain the behavioral reference.

## Verification and remaining work

The pre-change restore, complete solution build, and test suite passed: 188 passed, 0 failed, 2 opt-in MongoDB tests skipped; build had 0 warnings/errors. Existing tests cover framing boundaries, auth/session failure transitions, selected ownership checks, startup rollback, and login shutdown cleanup. Exact scope and unrun cases are in [SECURITY_TEST_RESULTS.md](./SECURITY_TEST_RESULTS.md) and [RELIABILITY_TEST_RESULTS.md](./RELIABILITY_TEST_RESULTS.md).

No runtime hardening change was made, so no Prompt 23 benchmark rerun was needed. Original-client, live MongoDB/TLS, production-network, account migration, rate limiting, full gameplay authorization, and active-player shutdown remain unverified. The system is not ready for final release preparation.
