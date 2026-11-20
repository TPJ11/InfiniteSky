# Client Compatibility Test Results

## Execution record

| Field | Value |
|---|---|
| Execution date | 2026-11-20 |
| .NET server commit before Prompt 21 changes | `b283640` (`feat: add host-level integration baseline and tests`) |
| Original client build | None available / unknown |
| Original Node.js reference | Not run |
| Environment | Windows workspace; loopback sockets; deterministic test account repository |
| MongoDB / production data | Not used; no local MongoDB service was reachable |
| Capture | No new capture; the old repository pcap was inspected for metadata only |

## Compatibility evidence

| Scenario | Client build | Test method | Expected result | Actual result | Status | Evidence / remaining blocker |
|---|---|---|---|---|---|---|
| Login greeting bytes | None | Source fixture test and real loopback host test | On login connect, receive exact 21-byte `0x00` greeting before the login request. | Exact Node-derived bytes are emitted and asserted by the tests. | `IMPLEMENTED` | Protocol/simulated-socket evidence only; original-client response not observed. |
| Login authentication response | None | Existing host/socket integration tests with test repository | Source-layout response follows a login request and session is associated with connection. | Success, wrong-password, duplicate, reconnect, disconnect, and host shutdown behavior pass in simulated tests. | `IMPLEMENTED` | Does not use MongoDB, real client, or character-list response. |
| Client character selection | None | Not run | Receive three source-compatible slots, select/load an owned character. | No client-facing character list or selection path. | `BLOCKED` | Full character wire mapping, handler, BSON evidence and client are absent. |
| World connection/entry | None | Not run | Handoff and world packets match source ordering and bytes. | No world transfer/authentication dispatch. | `BLOCKED` | No handler, representative data or client transcript. |
| Movement, visibility, gameplay, chat, scripting, admin | None | Not run | Supported interactions produce exact client-visible state and packet results. | No connected world gameplay path. | `BLOCKED` | See [`ORIGINAL_CLIENT_COMPATIBILITY_MATRIX.md`](./ORIGINAL_CLIENT_COMPATIBILITY_MATRIX.md). |
| Original Node differential exchange | None | Not run | Same authorized client action produces source-matching bytes/state on both servers. | Original server was not launched. | `BLOCKED` | Missing dependencies, isolated `config.json`, test script and MongoDB. |
| Multi-client original-client flow | None | Not run | Two clients enter a zone, observe and cleanly disconnect. | Not exercised. | `BLOCKED` | Client builds and world flow unavailable. |

## Commands executed

- `dotnet test tests\InfiniteSky.Server.IntegrationTests\InfiniteSky.Server.IntegrationTests.csproj --no-restore` — passed 62/62 after the greeting change.
- `dotnet restore InfiniteSky.sln` — succeeded; dependencies were already current.
- `dotnet build InfiniteSky.sln --no-restore` — succeeded with 0 warnings and 0 errors.
- `dotnet test InfiniteSky.sln --no-build` — 186 passed, 0 failed, 2 opt-in MongoDB tests skipped (Domain 50, Protocol 24, GameData 5, World 22, Server integration 62, Persistence 23 passed).
- `dotnet format InfiniteSky.sln --verify-no-changes --no-restore` — passed.
- `git diff --check` — passed.
- Repository Markdown scan — 874 local file links checked, 0 broken.

No original-client test is marked `VERIFIED`. A simulated socket test is not a substitute for one.
