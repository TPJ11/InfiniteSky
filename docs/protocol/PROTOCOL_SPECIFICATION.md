# InfiniteSky Protocol Specification

## Scope

This document describes the reverse-engineered wire protocol visible in the tracked Node.js implementation. It distinguishes implemented behavior from comments and experimental code. The repository is for Twelve Sky-style emulation and contains incomplete protocol knowledge.

Primary sources:

- [`CachedBuffer.js`](../../CachedBuffer.js): TCP reassembly and opcode dispatch.
- [`PacketCollection.js`](../../PacketCollection.js): packet registration and collection.
- [`restruct/restruct.js`](../../restruct/restruct.js): binary primitives and fixed strings.
- [`generic/structs.js`](../../generic/structs.js): shared character and item structures.
- [`packets/login/`](../../packets/login/): login-server handlers.
- [`packets/world/`](../../packets/world/): world-server handlers.

## Transport and framing

### Servers

The login and world implementations are separate TCP servers:

- Login listener: `config.ports.login`, default `11091`.
- World listener: `config.ports.world`, default `11092`.

The values and server startup are defined in [`config.json-dist`](../../config.json-dist), [`login.js`](../../login.js), and [`world.js`](../../world.js).

There is no TLS, application-level handshake nonce, HTTP upgrade, or framing encryption in these implementations.

### Receive framing

For an accepted socket, `CachedBuffer` maintains a 10,240-byte receive buffer and uses:

| Property | Value |
|---|---:|
| Header size | 9 bytes |
| Opcode offset | byte 8 |
| Default maximum buffer | 10,240 bytes |

The parser appends TCP data, waits for at least 9 bytes, reads one unsigned byte at `position + 8`, advances over the 9-byte header, and dispatches by that opcode.

Packet definitions determine payload size:

- `Restruct`: require `Restruct.size` bytes, then unpack.
- `Size`: require exactly `Size` bytes.
- No size: invoke the handler immediately after the 9-byte header.

Incomplete trailing data is retained for the next `data` event. An unrecognized opcode is logged and the buffer is discarded. Source: [`CachedBuffer.js:31-104`](../../CachedBuffer.js)

The header's bytes 0-7 are not interpreted by the receive implementation. They must not be assumed to contain an offset, length, sequence, or encryption marker.

### Reply framing

Server replies are normally an explicit Node.js `Buffer` or string. They do not generally prepend the 9-byte receive header. The first byte is frequently a reply opcode, for example:

- `0x03`: login result
- `0x05`: character slot
- `0x07`: character create result
- `0x08`: character delete result
- `0x0A`: server/world transfer
- `0x16`: character/world information
- `0x17`: world/faction information

Source: [`handleLogin.js`](../../packets/login/handleLogin.js) and [`world_login.js`](../../packets/world/world_login.js).

### Compressed packets

The repository contains comments and data files for compression and alternate packet formats, but the standard `CachedBuffer` parser does not contain a general compression flag or decompression stage. `makeCompressedPacket` and raw `.pac` files must be treated as implementation evidence, not established for every opcode. Source: [`generic/packets.js`](../../generic/packets.js), [`data/packets/`](../../data/packets/)

## Binary encoding

The `restruct` implementation uses:

| Type | Size | Endianness |
|---|---:|---|
| `int8lu` / `int8ls` | 1 | unsigned/signed |
| `int16lu` / `int16ls` | 2 | little-endian |
| `int32lu` / `int32ls` | 4 | little-endian unsigned/signed |
| `float32l` | 4 | IEEE-754 little-endian |

All common protocol integers are explicitly little-endian. Source: [`restruct/restruct.js:942-963`](../../restruct/restruct.js).

## String encoding

Fixed strings use `restruct.string(name, length)`:

- The implementation has no real UTF-8 conversion; `encodeUTF8` and `decodeUTF8` are currently no-ops.
- A string is truncated to the fixed byte/character length.
- Remaining bytes are zero-padded.
- Reading stops at the first zero and excludes it from the resulting string.
- Array strings can use `string(name, itemLength, itemCount)`.

Source: [`restruct/restruct.js:1385-1464`](../../restruct/restruct.js).

## Shared structures

### CVec3

12 bytes:

| Offset | Size | Type | Field |
|---:|---:|---|---|
| 0 | 4 | float32 LE | X |
| 4 | 4 | float32 LE | Y |
| 8 | 4 | float32 LE | Z |

Source: [`generic/structs.js:13-16`](../../generic/structs.js).

### Equipt

12 bytes in `Equipt` according to actual source? The struct emits ID(4), Capacity(4), Enchant(1), Combine(1), Unknown(2) = 12 bytes.

| Offset | Size | Type | Field |
|---:|---:|---|---|
| 0 | 4 | uint32 LE | ID |
| 4 | 4 | uint32 LE | Capacity |
| 8 | 1 | uint8 | Enchant |
| 9 | 1 | uint8 | Combine |
| 10 | 2 | uint16 LE | Unknown |

Source: [`generic/structs.js:18-23`](../../generic/structs.js).

### StorageItem

20 bytes:

| Offset | Size | Type | Field |
|---:|---:|---|---|
| 0 | 4 | uint32 LE | ID |
| 4 | 4 | uint32 LE | Column |
| 8 | 4 | uint32 LE | Row |
| 12 | 4 | uint32 LE | Amount |
| 16 | 1 | uint8 | Enchant |
| 17 | 1 | uint8 | Combine |
| 18 | 2 | uint16 LE | Unknown |

Source: [`generic/structs.js:64-71`](../../generic/structs.js).

`SmallStorageItem` is 12 bytes: ID(4), Amount(4), Enchant(1), Combine(1), Unknown(2).

Pet inventory variants use ID(4), then activity/growth fields instead of normal column/row fields. Item type `6` uses a 20-byte `cbFix` layout. Item type `22` uses pet storage layouts. Source: [`generic/structs.js:41-82,493-625`](../../generic/structs.js).

### Character

The `Character` struct begins with a known field prefix and continues into a large fixed tail containing many unnamed fields. Important known offsets:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 4 | isGM |
| 4 | 4 | Unused1 |
| 8 | 4 | PlayTime |
| 12 | 13 | Name |
| 25 | 119 | StuffLikeLocation |
| 144 | 4 | Clan |
| 148 | 4 | Gender |
| 152 | 4 | Hair |
| 156 | 4 | Face |
| 160 | 4 | Level |
| 164 | 4 | Experience |
| 168 | 4 | OtherIngame |
| 172 | 4 | SkillPoints |
| 176 | 4 | Honor |
| 180 | 4 | StatPoints |
| 184 | 4 | StatVitality |
| 188 | 4 | StatStrength |
| 192 | 4 | StatChi |
| 196 | 4 | StatDexterity |
| 200 | 4 | GameStuff1 |
| 204 | 4 | GameStuff2 |
| 208 | 12 | Amulet |
| 220 | 12 | Cape |
| 232 | 12 | Armor |
| 244 | 12 | Glove |
| 256 | 12 | Ring |
| 268 | 12 | Boot |
| 280 | 12 | CalbashBottle |
| 292 | 12 | Weapon |
| 304 | 12 | Pet |
| 316 | 4 | storageUse |
| 320 | 4 | Silver |

Inventory begins at offset 350 and uses 64 fixed `StorageItem` records. Storage begins at 1666 and has 28 12-byte records. Bank begins at 3350 and has 56 12-byte records. The source also names a long tail through approximately offset 4376. Remaining meanings are unknown.

Source: [`generic/structs.js:145-477`](../../generic/structs.js).

## Login authentication

### Server greeting

On a login connection, the server sends packet `0x00`. The payload is an opcode plus four 5-byte server entries:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 1 | PacketID |
| 1 | 1 | entry 0 unknown |
| 2 | 4 | entry 0 player count |
| 6 | 5 | entry 1 |
| 11 | 5 | entry 2 |
| 16 | 5 | entry 3 |

The implementation sets entry 0 to 1000 and zeroes the remaining entries. Source: [`packets/login/onConnected.js:5-26`](../../packets/login/onConnected.js).

### Login request

Packet is opcode `0x03`, payload size 91 bytes:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 14 | fixed Username |
| 14 | 14 | fixed Password |
| 28 | 63 | padding |

The receive framing includes the 9-byte header and locates opcode at header byte 8. Source: [`packets/login/handleLogin.js:48-50,187-188`](../../packets/login/handleLogin.js).

Authentication is plaintext:

1. Find account by exact `Username`.
2. Reject database error, missing account, or wrong password.
3. Check active state against a world socket.
4. Set `socket.authenticated = true`.
5. Set account `active = 1`.

There is no nonce, password hash in the wire, secure transport, or signed login token.

The login reply is 41 bytes: opcode `0x03`, status, 12-byte padding, username[12], 10-byte padding, `"****"`[4], and final padding. Source: [`handleLogin.js:238-255`](../../packets/login/handleLogin.js).

Login status values are:

| Value | Meaning |
|---:|---|
| 0 | Success |
| 1 | Cannot authenticate |
| 2 | Account not found |
| 3 | Incorrect password |
| 4 | Suspended |
| 5 | Already logged in |
| 6 | Need to pay |
| 7 | Wrong version |
| 8 | Maintenance |
| 9 | Server full |
| 10 | Unknown error |

Source: [`handleLogin.js:5-18`](../../packets/login/handleLogin.js).

After success, 0-3 character packets are sent. Each has opcode `0x05`, slot byte, exists byte, fixed `Character` struct, and one unknown byte. Source: [`handleLogin.js:31-36,208-230`](../../packets/login/handleLogin.js).

## Character login

### Character create

Opcode `0x06` is a character-create request:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 1 | Slot |
| 1 | 4 | Clan u32 LE |
| 5 | 13 | Name |
| 18 | 4 | Gender u32 LE |
| 22 | 4 | Hair u32 LE |
| 26 | 4 | Face u32 LE |
| 30 | 84 | ignore |
| 114 | 4 | WeaponID u32 LE |
| 118 | 52 | ignore |

The implementation rejects existing slots, duplicate names, invalid clan, invalid hair/face, and invalid weapon. It allocates `_id` using MongoDB counter `characterid`, initializes stats, and replies opcode `0x07`.

Create status values include 0 success, 1 generic failure, 2 name in use, 3 clan restriction, 4 invalid hair/face, and 5 invalid weapon.

Source: [`handleManageCharacter.js:26-234`](../../packets/login/handleManageCharacter.js).

### Character delete

Opcode `0x07` uses slot u8, clan u32 LE, and Namepart[4]. Reply opcode `0x08` is ID/status u8. Source: [`handleManageCharacter.js:238-273`](../../packets/login/handleManageCharacter.js).

### Enter world

Opcode `0x09` requests a world:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 4 | MapID u32 LE |
| 4 | 1 | slot |
| 5 | 8 | UserID fixed string |

The server puts the socket into `world.socket_transfers`, resolves NAT/LAN address, and replies opcode `0x0A`:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 1 | packet ID |
| 1 | 1 | status |
| 2 | 16 | IP fixed string |
| 18 | 4 | port u32 LE |

Source: [`handleGameStart.js:15-135`](../../packets/login/handleGameStart.js).

## World session authentication

### Handshake

After connecting to the returned world address, opcode `0x01` payload is 18 bytes:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 13 | Name |
| 13 | 1 | Slot |
| 14 | 4 | MapID u32 LE |

The server finds the login socket in `world.socket_transfers` by Name, copies account/character state, chooses or validates zone, and writes a raw `\x15` success. It does not validate a cryptographic session key. Source: [`handshake.js:5-96`](../../packets/world/handshake.js).

### World login

Opcode `0x02` uses `WorldAuthPacket`: fixed Username[14], CharacterName[13], MapID u32, Slot u32, a set of Unknown3-Unknown23 fields, Unknown22[3], Unknown23 u32, and UnknownByte u8. The source names and exact intended count of these fields are ambiguous.

The handler does not authenticate most fields. It initializes character inventory, storage and bank, sends packet `0x16` with `Character`, sends packet `0x17` world/faction data, attaches the socket to the zone, broadcasts state, and sets `authenticated`.

Source: [`world_login.js:178-329`](../../packets/world/world_login.js).

## Zone change

Opcode `0x08`:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 4 | ZoneID u32 LE |
| 4 | 4 | PortalID u32 LE |
| 8 | 4 | Unknown1 u32 LE |
| 12 | 4 | Unknown2 u32 LE |

On success, the server replies with opcode `0x0A`, status `0`, IP[16], and world port u32 LE. Status 1 and 2 indicate failure/unknown error in the comments.

The client reconnects and repeats the world handshake and login. Source: [`zonechange.js:1-231`](../../packets/world/zonechange.js).
