# Client Packet Fixture Inventory

## Provenance rules

The repository currently has source-derived protocol fixtures, not a verified original-client packet corpus. Existing hex files are retained as source/constructed fixtures and are not represented as client captures. The only `.pcap` has unknown provenance and may contain sensitive payloads; its bytes were not imported, redacted, or copied into tests.

| Fixture | Source/provenance | Coverage | Client build / live capture |
|---|---|---|---|
| [`login-greeting.hex.txt`](../../tests/fixtures/protocol/login-greeting.hex.txt) | Encoded from `packets/login/onConnected.js`; 21-byte server greeting. | Exact bytes for packet `0x00`; used by `LoginGreetingMatchesOriginal21ByteNodeFixture`. | None; source-derived, not client-captured. |
| [`login-request.hex.txt`](../../tests/fixtures/protocol/login-request.hex.txt) | Constructed from the Node login request `Restruct` definition. | Fixed-width username/password and pad in the 91-byte payload. | None; synthetic non-production fixture, not client-captured. |
| [`login-success-reply.hex.txt`](../../tests/fixtures/protocol/login-success-reply.hex.txt) | Constructed from the Node login reply writer. | 41-byte successful response layout. | None; source-derived, not client-captured. |
| [`login-account-not-found-reply.hex.txt`](../../tests/fixtures/protocol/login-account-not-found-reply.hex.txt) | Constructed from the Node login reply writer. | 41-byte account-not-found response. | None; source-derived, not client-captured. |
| [`login-success-nonascii-reply.hex.txt`](../../tests/fixtures/protocol/login-success-nonascii-reply.hex.txt) | Constructed from the Node login reply writer. | Fixed-width username byte behavior. | None; source-derived, not client-captured. |
| [`cvec3-node-restruct.hex.txt`](../../tests/fixtures/protocol/cvec3-node-restruct.hex.txt) | Constructed from the original top-level `restruct` scalar-float behavior. | Legacy floating-point serialization edge case. | None; source-derived, not client-captured. |
| [`storage-item.hex.txt`](../../tests/fixtures/protocol/storage-item.hex.txt) | Constructed from `generic/structs.js`. | Shared storage item layout. | None; constructed, not client-captured. |
| [`world-region.hex.txt`](../../tests/fixtures/protocol/world-region.hex.txt) | Constructed from `generic/structs.js`. | WREGION structure layout. | None; constructed, not client-captured. |
| [`attack-reply.hex.txt`](../../tests/fixtures/protocol/attack-reply.hex.txt) | Constructed from `packets/world/action.js`. | Attack reply fields. | None; constructed, not client-captured. |
| [`map-load-reply.hex.txt`](../../tests/fixtures/protocol/map-load-reply.hex.txt) | Constructed from the legacy game-start response definition. | Advertised address/port reply layout. | None; source-derived, not client-captured. |

The canonical manifest is [`tests/fixtures/protocol/README.md`](../../tests/fixtures/protocol/README.md). It distinguishes direct source-derived values from constructed layouts. No fixtures were added from `FruitSalad/Data/packets/t.pcap`.

## Prompt 21 fixture additions

No new binary fixture was necessary for the greeting change: the existing 21-byte source-derived greeting fixture already encodes the exact Node.js packet. The host-level integration test now asserts that this packet arrives immediately after a login TCP connection is accepted, before the test sends the login request.

The test uses the production `LegacyLoginGreeting` encoder and a loopback socket. It is protocol/simulated-client evidence only; it is not an original-client capture or full login-to-world compatibility test.

## Required metadata for future client fixtures

Each approved captured fixture must include:

- stable fixture ID and sanitized hex bytes;
- source client build and SHA-256 (kept in an authorized internal record if distribution is restricted);
- capture origin and date;
- direction, endpoint role, opcode, framing, and payload length;
- client action and expected connection/session transition;
- matching original Node response, if actually captured;
- redaction declaration and proof the resulting fixture contains no credentials/tokens;
- test name and exact comparison method.

Unknown bytes remain unknown. Do not synthesize missing requests or server success responses.
