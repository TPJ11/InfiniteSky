# Original Client Test Environment

## Result and scope

Inventory date: 2026-11-20. No usable original game client build was found in the repository or current project workspace, so this stage did not run an original client, edit a client binary, or establish a real-client test environment. The environment below records what exists and the conditions required before such a test can be safely prepared.

## Client and supporting-source inventory

| Resource | Observed state | Compatibility meaning |
|---|---|---|
| Original client executable | No `TwelveSky.exe` or other candidate game-client executable is present in the repository/worktree. Availability outside this workspace was not inspected. | No client build, version, publisher, integrity digest, or actual runtime dependencies can be identified. |
| Client build/version | Unknown. The TSX helper source contains a runtime signature search that may log a game-version string, but no client binary was available to exercise it ([`TSX_Client.cpp`](../../TSX_Client/TSX_Client/TSX_Client.cpp#L639)). | The checked-in source is not evidence of a particular supported client build. |
| TSX helper/launcher source | C++ helper, launcher, configuration, and Visual Studio project files exist under [`TSX_Client/`](../../TSX_Client/). The helper project is a Win32 dynamic library using the v110 toolset ([`TSX_Client.vcxproj`](../../TSX_Client/TSX_Client/TSX_Client.vcxproj#L20)). The launcher source expects external `TwelveSky.exe` and `TSX_Client.dll` files ([`Launcher.cpp`](../../TSX_Client/PrivateServerLauncher/Launcher.cpp#L19)). | These are private-server/research support sources, not the original game client or a tested client distribution. They were not built or run. |
| Helper configuration | [`PrivateServer.ini`](../../TSX_Client/TSX_Client/PrivateServer.ini) contains a loopback address and feature switches. | It configures the helper source; it does not establish official client settings, server address negotiation, or a supported client build. It was not used. |
| Client assets/patcher | No original client asset package or patcher distribution was identified in the tracked project resources. | Required client-side assets and patcher behavior remain unknown. No proprietary assets were downloaded or redistributed. |
| Packet capture | One tracked `FruitSalad/Data/packets/t.pcap`; metadata is recorded in [`CLIENT_PROTOCOL_CAPTURES.md`](./CLIENT_PROTOCOL_CAPTURES.md). | The capture has no documented client build, capture tool, test account, or provenance. It is not accepted as original-client evidence. |

The TSX helper source includes process-patching, packet-inspection, and encryption-related hooks. Those facilities were not built, enabled, or used for this task. No anti-cheat bypass or client-binary modification was attempted.

## Server-side test configuration

The only executed connection tests use the .NET Generic Host and loopback TCP with a deterministic test account repository. They use ephemeral ports and the production framing, login greeting, login request/reply, authentication, and session path; they do not need MongoDB or client assets. See [`INTEGRATION_TEST_HARNESS.md`](./INTEGRATION_TEST_HARNESS.md).

For a future isolated client test, use a disposable host and non-production dependencies:

- Bind both .NET TCP listeners to `127.0.0.1`; defaults are login `11091` and world `11092`.
- Use a dedicated test MongoDB instance/database and synthetic test accounts/characters only. Do not use production account records. The current character/world handlers and full character packet mapping are absent, so a database alone will not enable a complete client workflow.
- Configure any eventual test characters and game data from authorized, source-compatible fixtures. The repository lacks the required `.IMG` catalogs and `.WREGION` corpus for complete game behavior.
- Keep any capture local and access-controlled. Do not retain or share raw login packets; legacy authentication packets can contain credentials. Redact secrets before any approved derivative fixture is checked in.
- Do not expose listeners publicly. Do not disable authentication or authorization to get past a failed workflow.

No persistent client-test configuration, account, character, or database was created during this stage. No production service or data was accessed.

## Client connection settings

Source-visible values are not interchangeable with actual client requirements:

- The Node server defaults to separate TCP login/world ports `11091` and `11092` ([`config.json-dist`](../../config.json-dist#L7)).
- The legacy private-server helper configuration contains a loopback `ServerIP`, while the launcher source uses a historical client connection argument prefix ([`PrivateServer.ini`](../../TSX_Client/TSX_Client/PrivateServer.ini), [`Launcher.cpp`](../../TSX_Client/PrivateServerLauncher/Launcher.cpp#L23)). Neither source establishes the original client's official configuration format or a supported build.
- The legacy login game-start handler advertises an address and port in response to opcode `0x09` ([`handleGameStart.js`](../../packets/login/handleGameStart.js#L15)); the .NET server does not implement this transition.
- The Node configuration advertises wildcard interfaces by default; do not copy that exposure into an isolated test setup.

Hostname resolution, redirect behavior in the shipped client, timeout policy, client-side version checks, and any launcher/patcher requirements remain unverified. No client-side configuration change is currently supported or recommended.

## Original Node.js reference environment

The original Node.js process was not launched. Although Node is installed, the repository has no `node_modules`, no working `config.json`, no `test.js` despite the `package.json` test script naming it, and no MongoDB service reachable on the local default port. The package manifest depends on legacy package versions; dependencies were not installed. The checked-in `config.json-dist` is not a safe test configuration and was not copied or run.

As a result, there was no live differential test against the Node server. Source-derived comparisons use the checked-in implementation only. The exact limitations and their compatibility consequences are listed in [`CLIENT_COMPATIBILITY_ISSUES.md`](./CLIENT_COMPATIBILITY_ISSUES.md).

## Capture tooling and execution record

No `tshark` or Wireshark command was available on `PATH` during inventory. No new traffic was captured. The existing pcap was inspected only for file integrity and non-payload metadata; its packet bytes were not copied into tests or documentation.

No original client build, OS/runtime requirement, client build hash, or live client test date can be recorded. Real-client and original-Node differential results are therefore **not run**, not failed and not passed.
