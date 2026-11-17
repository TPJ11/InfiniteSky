# Packet Flows

## Login flow

```text
C -> S  TCP login
S -> C  0x00 server list
C -> S  0x03 login request
S -> C  0x03 login result

if success:
S -> C  0x05 slot 0
S -> C  0x05 slot 1
S -> C  0x05 slot 2
```

All three character packets may be sent, even for empty slots. Empty slots have `Exists=0`.

Source: [`handleLogin.js:208-255`](../../packets/login/handleLogin.js).

## Create character flow

```text
C -> S  0x06 character create
S -> C  0x07 character create result

status 0: character was created
status 1: generic failure
status 2: name in use
status 3: clan restriction
status 4: invalid hair/face
status 5: invalid weapon
```

Source: [`handleManageCharacter.js:26-234`](../../packets/login/handleManageCharacter.js).

## Delete character flow

```text
C -> S  0x07 slot/character delete
S -> C  0x08 status
```

On success the server removes the MongoDB character and deletes it from `socket.characters`.

Source: [`handleManageCharacter.js:238-273`](../../packets/login/handleManageCharacter.js).

## Enter world flow

```text
C -> S  0x09 MapID/Slot/UserID
S -> C  0x0A status/IP/Port

C -> S  TCP world
C -> S  0x01 Name/Slot/MapID
S -> C  0x15 handshake success
C -> S  0x02 world authentication
S -> C  0x16 character information
S -> C  0x17 world/faction state
S -> C  0x18-0x?? zone/action state
```

The implementation writes character and zone state after `0x02`. Source: [`handleGameStart.js`](../../packets/login/handleGameStart.js), [`handshake.js`](../../packets/world/handshake.js), [`world_login.js`](../../packets/world/world_login.js).

## Chat flows

### Normal clan chat

```text
C -> S  0x13 Name[13]/Message[51]/Unknown[8]
S -> C  0x2A Name[13]/Message[51] to all clan members
```

Source: [`chat.js:61-84`](../../packets/world/chat.js).

### Faction chat

```text
C -> S  0x3E faction chat request
S -> C  0x2A Name/Message to all clan members
```

Source: [`chat.js:87-106`](../../packets/world/chat.js).

### Whisper

```text
C -> S  0x09 sender/target/message/unknown fields

target exists:
S -> target  0x20 sender/target/message

target absent:
S -> sender  0x1D target/status=1/ZoneID=-1/unknown=-1

target cross-clan and cross-clan whisper disabled:
S -> sender  0x1D target/status=2/ZoneID=-1/unknown=-1
```

Source: [`chat.js:108-150`](../../packets/world/chat.js).

## Party flow

```text
leader C -> S  0x1D CharacterName
S -> invitee  0x33 inviter name

invitee C -> S  0x1F CharacterName/Result/Unk1/Unk2

Result 0:
S -> inviter  0x35 name/result=0
S -> party    0x25 member join
S -> party    0x28 party roster

Result 1:
S -> inviter  0x35 name/result=1

member C -> S 0x0F leave
leader C -> S 0x12 disband
```

Party chat opcode `0x0A` is present but explicitly sends an "not yet implemented" information message.

Source: [`party.js`](../../packets/world/party.js).

## Trade flow

Opcode family `0x26`, `0x28`, `0x2A`, `0x2B`, `0x2C` is partially implemented. Server reply families include `0x3F`, `0x41`, `0x42`, and `0x44`.

The request/response structures are either fixed but incompletely named or commented out. Exact wire fields are therefore unresolved. Source: [`trade.js`](../../packets/world/trade.js).

## Combat and skill flow

Movement/action packets use opcodes `0x03`, `0x04`, and `0x05`. Some implementations are commented and overlap older/newer variants.

Skill-related implementation uses:

- `0x18`: apply buff, with SkillID, SkillLevel, Slot, Unk4, Unk5;
- `0x19`: used skill/item state, with ChiUsage and Skill fields;
- `0x7A`: 62-byte buff timeout/confirmation packet.

Character-state packets are area-broadcast instead of a single documented combat reply. Attack reply opcode `0x2C` exists and is partially used. Source: [`newAction.js`](../../packets/world/newAction.js), [`action.js`](../../packets/world/action.js), [`skillUse.js`](../../packets/world/skillUse.js).

## Zone transfer flow

```text
C -> S  0x08 ZoneID/PortalID/Unknown1/Unknown2
S -> C  0x0A status/IP/Port

if status 0:
  C connects to returned world endpoint
  C -> S 0x01 handshake
  S -> C 0x15 success
  C -> S 0x02 world auth
  S -> C 0x16 character
  S -> C 0x17 world state
```

Source: [`zonechange.js`](../../packets/world/zonechange.js), [`handshake.js`](../../packets/world/handshake.js), [`world_login.js`](../../packets/world/world_login.js).
