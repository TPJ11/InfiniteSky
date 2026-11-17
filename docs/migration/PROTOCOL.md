
# Protocol Migration

## Scope and matrix coverage

`PRO-001` through `PRO-033`: framing, primitive/fixed strings, all registered packet families, shared wire structures, compressed state, and raw `.pac` evidence.

## Source files

`CachedBuffer.js`, `PacketCollection.js`, `restruct/restruct.js`, `generic/structs.js`, `generic/packets.js`, all `packets/login/*.js`, all `packets/world/*.js`, `data/packets/*.pac`.

## Target components

- `PacketRegistry`
- binary reader/writer primitives
- login/world codecs
- `CharacterWireCodec`
- item, action, chat, party, guild, trade, commerce, event codecs
- capture/fixture tooling

## Dependencies

Protocol primitives precede all network behavior and integration. `PRO-015`, `PRO-017`, and `PRO-032` are critical path dependencies.

## Implementation order

1. Primitives, fixed strings, frame descriptors.
2. Login greeting/login/character/game-start codecs.
3. World greeting/handshake/auth/character/world-state codecs.
4. Action/entity/state codecs.
5. Item/stat/skill codecs.
6. Chat/social/party/guild/trade/duel/shop codecs.
7. Event and miscellaneous codecs.
8. Compressed-state investigation and capture classification.
9. Golden fixture suite and protocol compatibility report.

## Tests

Each packet has:

- decode hex fixture;
- encode expected bytes;
- exact size assertion;
- little-endian and reserved-byte assertion;
- truncation and overflow rejection;
- optional round trip with unknown fields preserved.

Corpus tests classify every `.pac` by parse outcome, hash, opcode candidate, and confidence. Unclassified captures prevent release claims but do not block unrelated codecs.

## Acceptance criteria

- Every active `LoginPC.Set` and `WorldPC.Set` opcode has a descriptor.
- Known server-response opcode layouts are encoded.
- Login/world/character fixtures pass byte-for-byte.
- The 9-byte receive header and asymmetric reply model are isolated and documented.
- Unknown structures are explicit and retained, never silently guessed.

## Risks

Character and world-data tails, compression, shared opcode meanings, commented packet registrations, and variable layouts remain major protocol risks. The protocol project is a compatibility gate for the complete migration.
