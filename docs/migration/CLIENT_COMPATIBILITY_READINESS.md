# Original Client Compatibility Readiness

## Decision

**BLOCKED — not ready for original-client testing.** No original game client executable/build was found in the repository or current project workspace. Character list/selection, login-to-world handoff, world authentication, zone entry, movement, visibility, and gameplay are not wired to the .NET listeners. Prompt 21 performed source-derived compatibility work and simulated loopback verification only; it does not claim original-client compatibility. Prompt 22 has not started.

## Readiness gates

| Gate | State | Evidence / remaining work |
|---|---|---|
| Authorized original client build and provenance | `BLOCKED` | No executable/build/version or client assets in repository/worktree. Client availability outside workspace is not assessed. |
| Isolated test accounts/data | `BLOCKED` | Login host tests use an in-memory repository. No isolated MongoDB or sanitized representative character BSON is available. |
| Login connection/greeting | `IMPLEMENTED` | Source-backed 21-byte greeting is emitted on login connection-open and asserted on a loopback socket against the existing source-derived fixture. No client observed it. |
| Login authentication | `IMPLEMENTED` | Simulated login success/failure/duplicate/reconnect tests pass with a test repository. No live Mongo or original client. |
| Character selection | `BLOCKED` | No complete `Character` wire codec, character-list response, or selection handler. |
| World handoff and authentication | `BLOCKED` | No login transfer handler/store or world handshake/authentication dispatch. |
| Required game data | `BLOCKED` | `.IMG` catalogs and `.WREGION` corpus are absent; no fallback map/zone/spawn is allowed. |
| Packet capture corpus | `BLOCKED` | One old pcap has unknown client provenance and potentially sensitive payloads; it is not a golden client fixture. No new capture tool was available. |
| Original Node differential reference | `BLOCKED` | Missing `node_modules`, isolated config, test script, and reachable MongoDB; original process not run. |
| Multi-client gameplay | `BLOCKED` | No original client instances or client-reachable world gameplay path. |

## What was validated

- The exact Node-derived login greeting is now sent as the first application packet on a login TCP connection.
- The host/socket tests verify greeting-before-request ordering and the existing authentication/session behavior using simulated clients.
- The full existing test suite and formatting checks are run as part of this stage; see the execution record in [`CLIENT_COMPATIBILITY_TEST_RESULTS.md`](./CLIENT_COMPATIBILITY_TEST_RESULTS.md) and [`MIGRATION_STATUS.md`](../../MIGRATION_STATUS.md).
- No original Node.js source, client binary, production data, or live account was changed or used.

## Preconditions for a real-client attempt

1. Provide an authorized client build with version/provenance and a lawful test basis; do not redistribute it.
2. Provide required assets and sanitized test account/character data in isolated storage.
3. Complete source-backed character list/selection, transfer, world handshake, and initialization dispatch.
4. Establish which client-side configuration/redirect behavior is supported without modifying the client binary or weakening authentication.
5. Resolve test-only capture tooling and redact credentials/tokens before creating any derived fixture.
6. Keep the server loopback-only and verify startup/readiness against actual required data.
7. Record client build, server commit, exact action, packet sequence, state, logs, and result. Only then can that specific workflow be considered for `VERIFIED`.

## Scope boundary

The presence of the `TSX_Client` helper/launcher sources and a pcap does not make an original client available. This stage did not build/run those tools, patch a client, bypass an anti-cheat mechanism, or infer unsupported encryption/handshake behavior. The stage ends here; do not begin Prompt 22 without explicit approval.
