# Binary Protocol Specification

## Evidence

Primary Node.js sources:

- receive framing: [`CachedBuffer.js`](../../CachedBuffer.js)
- packet opcode registry: [`PacketCollection.js`](../../PacketCollection.js)
- binary primitives and fixed strings: [`restruct/restruct.js`](../../restruct/restruct.js)
- shared structures: [`generic/structs.js`](../../generic/structs.js)
- login structures: [`packets/login/handleLogin.js`](../../packets/login/handleLogin.js), [`handleGameStart.js`](../../packets/login/handleGameStart.js)
- world structures: [`packets/world/world_login.js`](../../packets/world/world_login.js), [`zonechange.js`](../../packets/world/zonechange.js), [`action.js`](../../packets/world/action.js)
- uncompressed envelope: [`generic/packets.js`](../../generic/packets.js)

The C# implementation follows these sources. It does not infer layout for files that are only commented or captured.

## Transport framing

The original receive parser uses a 9-byte header:

| Offset | Meaning |
|---:|---|
| 0-7 | opaque/reserved by the parser |
| 8 | one-byte opcode |

Header bytes 0-7 are retained by `LegacyReceiveFrame` but are not interpreted.

After the header, packet-specific payload bytes follow. Payload length is either:

- a fixed `Size` supplied to `PacketCollection.Set`;
- `Restruct.size`;
- absent for header-only packets.

The parser waits for a complete frame, and leaves incomplete trailing bytes for the next TCP read.

Server replies are direct packets: ordinary replies are not prepended with the 9-byte receive header. Reply structures generally begin at byte 0 with their reply packet ID.

## Endianness

The `restruct` implementation uses:

| Primitive | Size | Encoding |
|---|---:|---|
| `int8lu` | 1 | unsigned byte |
| `int8ls` | 1 | signed modulo 256 |
| `int16lu` | 2 | unsigned little-endian |
| `int16ls` | 2 | signed little-endian |
| `int32lu` | 4 | unsigned little-endian |
| `int32ls` | 4 | signed little-endian |
| `float32l` | 4 | 32-bit IEEE-754 little-endian |

No 64-bit common primitive is established by the main `.restruct` packet source. JavaScript/Mongo numbers do not create a wire 64-bit integer rule.

## Strings

Fixed-width strings are implemented by `restruct.string(name, width)`:

- JavaScript `charCodeAt(i)` is passed into `pack8(val & 0xff)`;
- C# truncates to the fixed byte width and uses low 8 bits of characters;
- bytes after the first `0x00` are ignored while decoding;
- remaining bytes are zero-padded while encoding;
- no source-level UTF-8 conversion is performed;
- array-string support repeats fixed fields.

The C# implementation reads fixed strings as Latin-1 for a reversible 1-byte/1-character compatibility representation. It does not claim general UTF-8 support.

## Floating-point compatibility

Normal float fields use IEEE-754 single little-endian.

The top-level `restruct/restruct.js` scalar `float32l.pack` implementation contains a confirmed compatibility defect:

```text
uint32 bits = float32 bits
write uint32(bits + 1)
```

An original Node fixture for `{-630.25,50.5,-815.75}` in `CVec3` is:

```text
01 90 1d c4 01 00 4a 42 01 f0 4b c4
```

Decoding through normal IEEE-754 interpretation yields approximately:

```text
-630.25006103515625
50.500003814697266
-815.75006103515625
```

The C# protocol layer exposes:

- `WriteFloat32` / `ReadFloat32` for normal IEEE-754;
- `LegacyRestructFloat.WriteScalarBugged` and `ReadScalar` for explicit scalar restruct compatibility.

`LegacyVector3` uses scalar bugged encoding; `LegacyStandardVector3` uses normal encoding for data/packet fixtures that are known to contain standard floats.

Array float packing in `restruct.js` has different and incomplete behavior. It is not included as a generic compatibility API until a provenance-backed fixture establishes exact semantics.

## Shared structures

### Equipment

Size 12:

| Offset | Size | Type | Field |
|---:|---:|---|---|
| 0 | 4 | uint32 LE | ID |
| 4 | 4 | uint32 LE | Capacity |
| 8 | 1 | uint8 | Enchant |
| 9 | 1 | uint8 | Combine |
| 10 | 2 | uint16 LE | Unknown |

Source: `generic/structs.js`, `Equipt`.

### StorageItem

Size 20:

| Offset | Size | Type |
|---:|---:|---|
| 0 | 4 | uint32 LE ID |
| 4 | 4 | uint32 LE Column |
| 8 | 4 | uint32 LE Row |
| 12 | 4 | uint32 LE Amount |
| 16 | 1 | uint8 Enchant |
| 17 | 1 | uint8 Combine |
| 18 | 2 | uint16 LE Unknown |

### SmallStorageItem

Size 12:

| Offset | Size | Type |
|---:|---:|---|
| 0 | 4 | uint32 LE ID |
| 4 | 4 | uint32 LE Amount |
| 8 | 1 | uint8 Enchant |
| 9 | 1 | uint8 Combine |
| 10 | 2 | uint16 LE Unknown |

### Pet variants

- pet storage item: ID u32, Column u32, Row u32, Activity u32, Growth u32 = 20 bytes;
- small pet storage item: ID u32, Activity u32, Growth u32 = 12 bytes;
- equipped pet: ID u32, Unknown u16, Activity u16, Growth u32 = 12 bytes.

The source contains multiple spellings: `Pet`, `PetEquip`, `StorageItemPet`, `SmallStorageItemPet`. The C# names retain semantics rather than type aliases.

### Bottle

Bottle layout is ID u32 followed by eight raw capacity bytes. The source does not name or type the eight bytes. C# preserves them as `ReadOnlyMemory<byte>` without guessing fields.

### GiftItem

ID u32, Amount u32 = 8 bytes.

### QuickUseItem

ID u32, Amount u32 = 8 bytes.

### QuickUseSkill

ID u32, Level u32 = 8 bytes.

### SpawnInfo

Size 24:

| Offset | Size | Type |
|---:|---:|---|
| 0 | 4 | uint32 LE UniqueID |
| 4 | 4 | uint32 LE ID |
| 8 | 12 | LegacyStandardVector3 or source struct |
| 20 | 4 | float32 LE Direction |

`SpawnInfo.Location` came through a nested struct in one source. The C# implementation uses normal standard float encoding unless explicitly documented otherwise.

### WREGION

Size 32. Eight signed little-endian int32 fields:

```text
Unknown1, ZoneID, Unknown2, Unknown3, X, Y, Z, Radius
```

### AttackPacketReply

Size 56:

| Offset | Type |
|---:|---|
| 0 | 11 uint32 values |
| 44 | 4 uint16 values |
| 52 | int32 DamageHP |

The source uses status/unknown names that are partly uncertain. C# retains the known layout and names A-D for unresolved fields.

## Payload structures

### Login greeting

Server packet `0x00`, payload 21 bytes:

```text
PacketID u8
Entry0.Unknown u8
Entry0.PlayerCount u32 LE
Entry1-3: four 5-byte entries
```

Source: `packets/login/onConnected.js`.

### Login request

Payload 91 bytes:

```text
Username[14]
Password[14]
Padding[63]
```

The receive header is separate. Opcode appears at receive-header offset 8.

### World handshake

Payload 18 bytes:

```text
Name[13]
Slot u8
MapID u32 LE
```

### Zone change request

Payload 16 bytes:

```text
ZoneID u32 LE
PortalID u32 LE
Unknown1 u32 LE
Unknown2 u32 LE
```

### Map-load reply

Payload/byte layout 22 bytes:

```text
PacketID u8
Status u8
IP[16]
Port u32 LE
```

Earlier shorthand in migration docs that described 21 bytes was incorrect; fixed IP length is 15+NUL =16.

## Uncompressed state envelope

The original `makeCompressedPacket` uncompressed branch writes:

```text
PacketID u8
IsCompressed u8
Packet payload bytes
```

Total envelope size is 2. If `useCompressedPackets=true`, compression code is commented and no complete format is established. C# only implements the uncompressed envelope.

## Packet registry

The C# `LegacyCorePacketRegistry` contains only fixed layouts proven sufficient by source:

Login:

- `0x03` 91-byte login
- `0x04` 8-byte keepalive
- `0x06` 170-byte character create
- `0x07` 21-byte character delete
- `0x09` 13-byte game start
- `0x0A` 9-byte failure
- `0x12` 18-byte pin change
- `0x13` 13-byte pin attempt

World:

- `0x01` 18-byte handshake
- `0x08` 16-byte zone change
- `0x14` 68-byte item action
- `0x18` 20-byte apply buff
- `0x19` 24-byte used skill/item
- `0x7A` 62-byte buff confirmation

The full opcode corpus remains in packet-catalogue migration work. Variable/unknown layouts are deliberately absent.

## Unresolved items

- receive-header bytes 0-7;
- complete WorldAuth 0x02 layout and size;
- full Character struct tail;
- World state 0x17 layout;
- zlib/compression header and compressed body;
- array float packing bug semantics;
- party/trade/guild variable fields;
- 0x43 observed 33-byte but unregistered change-scroll packet;
- 0x45 quest packet captured with no structure.
