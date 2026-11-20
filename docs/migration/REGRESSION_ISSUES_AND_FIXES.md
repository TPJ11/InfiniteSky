# Prompt 22 Regression Issues and Fixes

## Findings

| ID | Finding | Evidence | Resolution | Result |
|---|---|---|---|---|
| `R22-001` | Implemented world-handshake request structure had no direct exact-byte test. | `LegacyWorldHandshake` existed, and the original `packets/world/handshake.js` specifies a 13-byte name, unsigned byte slot and little-endian 32-bit map ID; no existing test called the codec. | Added `world-handshake.hex.txt`, provenance in the protocol fixture README, and `FrameAndStructureTests.WorldHandshakeMatchesOriginalNodeLayout`. | Focused test passed. The test covers only the request structure; raw `0x15` and transfer behavior remain absent. |
| `R22-002` | Feature matrix listed listener smoke ports as `11191`/`11192`, inconsistent with the configured defaults. | `NetworkingOptions`, `appsettings.json` and migration runtime documentation show `11091`/`11092`; host tests use ephemeral ports. | Corrected the matrix notes to 11091/11092 and clarified tests use ephemeral ports. | Documentation corrected; no runtime behavior changed. |
| `R22-003` | Feature-count coverage note said 205 IDs although the matrix contains 232 unique rows. | Recounted all feature rows and unique IDs; all status totals match 232. | Corrected the coverage note to 232. | Documentation corrected; no feature statuses changed. |
| `R22-004` | Protocol matrix wording could be read as claiming compression/decompression golden tests had passed. | The only test covers the uncompressed two-byte envelope; no compressed capture or codec exists. | Rewrote the criterion and note to require canonical source/capture evidence and explicitly mark compression unverified. | No compression behavior was invented or added. |
| `R22-005` | Several Mongo matrix criteria read like completed live integration evidence despite the opt-in repository tests being skipped. | `MongoIntegrationFactAttribute` gates both Testcontainers tests; Docker was unavailable and the opt-in was false. | Rephrased affected verification criteria as future acceptance checks and retained the live-test-pending notes. | No live MongoDB/production-data claim is made. |

## Runtime regressions

No runtime regression was reproduced in the exercised .NET 10 paths. No production C# behavior was changed in Prompt 22. The only test addition locks down an already implemented, source-defined request structure. No original Node.js source file was modified.

## Unresolved failures and risk areas

- No full login-to-world client workflow is implemented, so that workflow cannot be regression-tested.
- MongoDB account/character Testcontainers integrations remain skipped; mapper tests use synthetic documents.
- No original client or live Node differential reference was run.
- Queue saturation, idle-timeout expiration, post-start database failure/recovery, shutdown during active gameplay persistence, and full world-player cleanup lack direct test evidence.
- `WorldClockTests` uses system time and short delays. Three baseline repeat runs and subsequent coverage execution had no failures, but that is limited evidence and not a proof of determinism.
- Protocol fixture files not referenced by active test methods are not counted as coverage.
