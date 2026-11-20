# Client Compatibility Issues and Fixes

## Findings

| ID | Finding | Source evidence / reproduction | Disposition | Status |
|---|---|---|---|---|
| `CCI-001` | .NET login listener omitted the Node login greeting on socket acceptance. | Node calls `LoginPC.onConnected` from login connection setup, and that function writes the 21-byte `0x00` server-list packet ([`login.js`](../../login.js), [`onConnected.js`](../../packets/login/onConnected.js)). The .NET connection path previously started reading without an application open callback. | Added an application sink connection-open hook. Login endpoint encodes `LegacyLoginGreeting(0, 1000)` before starting its receive loop. Host/socket tests assert the exact 21 bytes from the existing Node-derived fixture before sending a request. No world greeting was added. | `IMPLEMENTED` |
| `CCI-002` | Successful .NET login does not send the original three character-slot packets. | Node success path invokes `sendCharacterInfo()` and sends slots 0-2 after the 41-byte login reply ([`handleLogin.js`](../../packets/login/handleLogin.js#L31)). | Remains unimplemented. No placeholder slots or guessed `Character` structure were sent. | `BLOCKED` |
| `CCI-003` | Original-client build and version cannot be established. | No game executable exists in the repository/worktree. The support DLL can search a loaded process for a version string, but no target binary was present. | No client run, client-specific configuration, or compatibility claim. Request an authorized client build and its provenance before testing. | `BLOCKED` |
| `CCI-004` | The tracked pcap is not attributable to a client build or scenario. | `t.pcap` has 18 IPv4/TCP frames on a flow using server port `11092`, but no capture provenance/client build is documented. | Metadata only was inspected. Payload bytes were not copied or published; the artifact is excluded from golden client fixtures. | `BLOCKED` |
| `CCI-005` | Login-to-world packet path is unavailable in .NET. | Original code has login game-start `0x09` → handoff `0x0A`, then world handshake/auth `0x01`/`0x02`; the .NET sink handles only login `0x03`. | Remains blocked pending verified transfer semantics, complete wire structures, game data, and client-reachable world/player lifecycle. | `BLOCKED` |
| `CCI-006` | Character/world assets and representative persistence samples are absent. | The repository lacks `.IMG` catalogs and `.WREGION` corpus; Mongo integration tests require opt-in infrastructure, and no isolated Mongo service is present. | No character/world success packets or fallback spawn/data were fabricated. | `BLOCKED` |
| `CCI-007` | Client encryption/compression requirements are not established. | Node `makeCompressedPacket` defaults to an uncompressed two-byte envelope, while its compressed branch is explicitly incomplete. TSX helper comments/patch hooks are not a specification. | No encryption, compression, checksum, negotiation, or version behavior was invented. Revisit only with authorized client traffic and source-backed reference behavior. | `BLOCKED` |
| `CCI-008` | Live Node differential testing could not be prepared safely. | No `node_modules`, `config.json`, `test.js`, or local MongoDB listener was present. `config.json-dist` is not isolated test configuration. | The original server was not launched and no dependencies or test database were installed/provisioned. | `BLOCKED` |

## Fix details: login greeting

The greeting implementation is deliberately limited to the source-established login socket event. It sends the same 21-byte packet represented by [`login-greeting.hex.txt`](../../tests/fixtures/protocol/login-greeting.hex.txt), with unknown byte `0` and player count `1000`. The connection-open callback is awaited before the receive loop so a client cannot race the greeting with a request and observe reversed output order.

The change does **not** make character selection or world entry possible. Existing login authentication still uses a test repository in the host integration scenario, and success is not followed by fabricated character data.

## Unresolved compatibility evidence

- No original client build was executed.
- No original Node server was executed; source inspection is not live differential evidence.
- The old pcap does not establish login or client version behavior.
- The login/world address handoff, full character codec, world authentication fields, client-side encryption/version policy, and world initialization packet order remain incomplete or ambiguous.
- The current world listener may accept TCP connections while world readiness is degraded; it has no gameplay handlers. See risk `R-052` in [`MIGRATION_RISKS.md`](./MIGRATION_RISKS.md).
