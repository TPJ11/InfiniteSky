# Protocol Implementation

## Project layout

`InfiniteSky.Protocol` is the binary-compatibility boundary. It has no project references to networking, gameplay, MongoDB, hosting, or Domain runtime.

Main namespaces:

- `InfiniteSky.Protocol.Binary`: reader, writer, fixed string, float compatibility
- `InfiniteSky.Protocol.Frames`: receive header/framing
- `InfiniteSky.Protocol.Packets`: opcode descriptors and registry
- `InfiniteSky.Protocol.Structures`: known fixed packet/shared structures

## Reader API

`LegacyBinaryReader`:

- wraps `ReadOnlyMemory<byte>`;
- tracks `Offset`, `Length`, `Remaining`;
- exposes `RemainingSpan`;
- validates bounds before every read;
- throws `LegacyProtocolReadException` with offset/requested/remaining;
- supports unsigned/signed 8/16/32 and standard float32;
- supports fixed byte sequences and fixed strings;
- has explicit `Reset` and `Skip`.

## Writer API

`LegacyBinaryWriter`:

- uses `ArrayBufferWriter<byte>`;
- exposes written span/memory and exact offset;
- writes little-endian signed/unsigned 8/16/32 and float32;
- copies fixed byte sequences;
- writes fixed strings with NUL padding and low-byte character compatibility;
- throws `LegacyProtocolWriteException` for invalid bottle/declared field shapes.

It does not silently truncate fixed strings. The fixed-string API reports the number of source bytes that fit and always explicitly writes the fixed width. Node fixed-string behavior truncates source values; C# does the same.

## Float compatibility

Normal float APIs use `BinaryPrimitives.WriteSingleLittleEndian` and `ReadFloat32`.

`LegacyRestructFloat` provides an explicit scalar bug method:

- `WriteScalarBugged` increments the uint32 bit pattern once;
- `ReadScalar` reads the resulting bit pattern through normal IEEE-754 interpretation.

This is not used in ordinary float APIs unless a structure is known to be a scalar `restruct.float32l` packet field.

## Structure conventions

Each fixed structure:

- is a record with wire field values;
- exposes `WireSize`;
- defines `Encode(LegacyBinaryWriter)`;
- defines static `Decode(LegacyBinaryReader)`.

Unknown fields are not discarded:

- `Equipment.Unknown`;
- `StorageItem.Unknown`;
- `WREGION.Unknown1..3`;
- attack reply A-D and unknown16.

Bottle capacity remains raw bytes because source field meanings are unknown.

## Frame and registry

`LegacyReceiveFrame`:

- header size 9;
- opcode offset 8;
- validates complete frame length;
- does not interpret header bytes 0-7.

`LegacyPacketRegistry`:

- explicit byte-opcode map;
- duplicate rejects;
- fixed and header-only descriptor kinds;
- unknown opcode lookup is observable by caller.

`LegacyCorePacketRegistry` seeds only fixed layouts established by source.

## Error handling

All reads validate remaining bytes before access. Reader exceptions include offset and requested/remaining lengths.

Writer structures validate:

- negative lengths at buffer API;
- fixed bottle capacity exactly eight bytes.

No arbitrary packet allocation limits are added. Future networking can add configurable maxima.

## Fixtures

Fixtures are under `tests/fixtures/protocol` and contain only hexadecimal byte sequences.

| Fixture | Origin |
|---|---|
| `login-greeting.hex.txt` | original Node `restruct` for `LoginServerListInfoPacket` |
| `login-request.hex.txt` | original Node `restruct` fixed Username/Password/pad |
| `cvec3-node-restruct.hex.txt` | original top-level scalar `float32l` pack bug |

Storage, WREGION, attack, and map fixtures are source-derived constructed layouts, not captured live packets. Their README labels them explicitly.

The C# protocol tests include exact login and Node float fixtures, primitive boundary tests, fixed strings, truncation, padding, bounds, receive framing, opcode registry, shared structures, and envelope behavior.

## Implemented coverage

Implemented foundation features:

- bounded little-endian reader/writer;
- fixed strings;
- normal float32 and legacy scalar bug support;
- receive frame parser;
- opcode registry/core fixed descriptors;
- source-derived login greeting structure, emitted on login connection-open;
- shared equipment, storage, gift, quick-use, spawn, region, attack, map, pin, login, handshake, and envelope structures.

Not implemented:

- 9-byte header field meanings;
- complete Character struct;
- WorldAuth and WorldState packets;
- compression algorithm;
- complete client-facing login/world packet dispatch (only the login greeting and login request/reply path are connected);
- every packet in the packet catalogue.
