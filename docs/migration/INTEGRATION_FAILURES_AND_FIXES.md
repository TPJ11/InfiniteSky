# Integration Failures and Fixes

## Baseline findings

The Prompt 20 baseline restore, build, format verification, and runnable test suite had no failures. The two skipped tests require opt-in MongoDB Testcontainers execution. The baseline intentionally fails host startup when MongoDB is enabled but unreachable; this is verified expected failure behavior, not an integration defect.

## Prompt 20 execution log

| Finding | Reproduction / evidence | Diagnosis | Fix or disposition | Status |
|---|---|---|---|---|
| Initial host-login test returned legacy `IncorrectPassword` instead of success | First run of `NetworkAuthenticationHostIntegrationTests.HostRoutesSocketLoginThroughAuthenticationAndCleansUpOnShutdown` compared the 41-byte response and observed status `3`. | The proposed test password exceeded the source protocol's 14-character fixed field and was truncated by the production encoder; the server correctly rejected the mismatched credential. This was a fixture error, not a production defect. | Changed the test credential to the source-valid `Secret123`; reran the focused host test successfully. No production code was changed. | `VERIFIED` |
| World listener bind failure after login listener bind | `WorldListenerBindFailureReleasesLoginListenerDuringStartupRollback` holds the configured world port, then starts the host. | Startup fails at the second listener; `NetworkHostedService` invokes listener cleanup for the already-started login listener. | Verified the login port can be rebound after failure. No runtime fix was required. | `VERIFIED` |
| Active login session on graceful host stop | Host-level login test authenticates a real loopback connection and stops the host while the session is active. | Reverse hosted-service stop order closes network connections before subsequent world/zone shutdown; connection close reaches the sink's session cleanup. | Regression test verifies both connection and session counts return to zero. This does not exercise active player saves or Mongo. | `VERIFIED` |
| Login-to-world journey absent | Source-to-target audit and packet registry/sink inspection; world handlers do not activate players. | Required character/world packet layouts, transfer binding and data evidence are unavailable; this is missing functionality, not a reproduced wire regression. | Kept blocked and documented; no guessed packets, fallback spawn or fake success response was added. | `BLOCKED` |
| Runtime gameplay/script/command dispatch absent | DI registration and world packet registry inspection; tests contain only component slices for those areas. | The runtime services, handler contracts, game data and source-complete dependencies are not present. | Kept blocked; Prompt 20 adds no speculative runtime services. | `BLOCKED` |
| Live Mongo integration unavailable in baseline run | Full test suite reports account/character Testcontainers integrations skipped. | No opt-in Docker-backed Mongo execution was performed; synthetic BSON tests are not production compatibility evidence. | Reported as unverified. No database or production data was accessed. | `BLOCKED` |

## Runtime fixes

No source-backed protocol or gameplay implementation defect was demonstrated in the workflows exercised during Prompt 20. The only code change is a test harness that composes the existing host and login path and verifies session/connection cleanup plus partial listener-start rollback. An operational readiness risk remains: when networking is enabled, the world listener may start while world/zone readiness is degraded, but the world sink has no gameplay handlers. This is tracked as `R-052`; no listener gate or fake world response was added because its compatibility policy is not established. The original Node.js implementation and production data remain unchanged.

## Remaining failure coverage

There is no executable end-to-end path to inject failures into character selection, player spawn, movement, gameplay saves, script events, or command execution. Database loss after startup, listener failure after startup, shutdown with an active world player, and Mongo save cancellation remain unverified. Do not infer recovery guarantees from the host/login tests.
