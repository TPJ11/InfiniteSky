# Release Acceptance Criteria

These criteria define evidence required for an owner decision. They do not grant publication, deployment, or migration-acceptance permission. The current assessment is recorded separately in [RELEASE_ACCEPTANCE_REPORT.md](RELEASE_ACCEPTANCE_REPORT.md).

| ID | Area | Acceptance evidence required | Mandatory blocker when unmet? |
|---|---|---|---|
| AC-01 | Build | Clean locked restore and full Release build on each declared target OS/RID, with no errors or unreviewed warnings | Yes |
| AC-02 | Automated tests | All applicable tests pass; skipped external-service tests are identified and backed by equivalent isolated integration evidence before accepting that subsystem | Yes for release-critical behavior |
| AC-03 | Protocol | Golden source/client fixtures and verified request/response behavior for every feature claimed as supported | Yes for any claimed client workflow |
| AC-04 | Authentication | Original-compatible credentials and login/session behavior verified, secrets excluded from logs, deployment transport risk explicitly accepted or mitigated | Yes; current high risks block public release |
| AC-05 | Character selection | Ownership-checked selection, complete response codec, slot/order behavior, and persistence fixtures verified | Yes for playable release |
| AC-06 | World entry | Login transfer, world handshake/auth, initialization ordering, disconnect and multi-client cleanup verified against an authorized original client or equivalent source transcript | Yes for playable release |
| AC-07 | Gameplay | Every advertised gameplay feature has source-derived rules, data, packet handling, persistence, and deterministic/integration coverage | Yes for advertised functionality |
| AC-08 | Persistence | Representative legacy BSON, live isolated Mongo integration, unknown-field preservation, write-failure behavior, backup/restore rehearsal | Yes for account/character release |
| AC-09 | Scripting | Supported scripts/API and trust boundary explicitly chosen; compatibility and lifecycle tested, or scripts explicitly excluded with owner approval | Yes if legacy scripts are required |
| AC-10 | Administration | Supported operator/GM actions and authorization verified end-to-end, or explicitly excluded by owner approval | Yes if admin functionality is required |
| AC-11 | Original client | Exact authorized client build/version, configuration, login/selection/world-entry and supported gameplay tested; no simulated-only claim | Yes for original-client compatibility claim |
| AC-12 | Multi-client | Concurrent clients exercise shared state and cleanup in an implemented gameplay path | Yes for multiplayer release claim |
| AC-13 | Performance | Prompt 23 workloads reviewed; capacity claims bounded to measured workloads; representative gameplay load measured before any production capacity claim | Yes for capacity claims |
| AC-14 | Security/reliability | High/critical findings remediated or formally accepted by the release owner with mitigations; startup, shutdown, failure and abuse controls tested | Yes for public deployment |
| AC-15 | Deployment | Clean published artifact starts with supported runtime/configuration/data; actual health/readiness and external dependencies verified | Yes |
| AC-16 | Documentation/operations | Deployment, operations, recovery, troubleshooting, limitations and handover instructions match tested behavior | Yes |
| AC-17 | Dependencies | Locked package graph, vulnerability review, runtime requirements, license/attribution review, and unresolved advisories documented | Yes |
| AC-18 | Data/assets | Required server-side data inventory and provenance documented; all packaged assets authorized; no client assets or secrets included | Yes |
| AC-19 | Reproducibility | Restore/build/publish reproduced from a clean checkout; artifact contents and hashes independently verified | Yes for release candidate integrity |
| AC-20 | Approval gate | Explicit owner approval for candidate acceptance, publication and deployment recorded separately | Yes; never inferred from this report |

There are no performance SLAs or player-count targets in this document. Prompt 23's login-only 100-session result is not a gameplay capacity criterion. Original stubs, experimental behavior, unknown protocol fields, and absent source assets are not silently reclassified as supported features.
