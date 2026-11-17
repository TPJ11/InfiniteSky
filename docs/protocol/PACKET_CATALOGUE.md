# Packet Catalogue

This catalogue lists every actively registered login/world opcode found in `packets/login/*.js` and `packets/world/*.js`. Comments and files with only debugging captures are marked incomplete.

## Framing rule

For client packets, the 9-byte receive header is excluded from payload sizes. The opcode is at header byte 8. Fixed reply packets are listed as implemented server writes.

## Login packets

| Opcode | Dir | Payload | Fields/behavior | Evidence |
|---:|---|---:|---|---|
| `0x00` | S->C | 21 | Server list: 4 entries of 1 unknown byte + 4-byte player count | [`onConnected.js`](../../packets/login/onConnected.js) |
| `0x03` | C->S | 91 | Username[14], Password[14], pad[63] | [`handleLogin.js`](../../packets/login/handleLogin.js) |
| `0x03` | S->C | 41 | result opcode/status/padded username/padded password marker | [`handleLogin.js:238-255`](../../packets/login/handleLogin.js) |
| `0x04` | C->S | 8 | Keepalive; no payload fields used | [`handleLogin.js:344`](../../packets/login/handleLogin.js) |
| `0x04` | S->C | 2 | PIN ID/status | [`handleLogin.js:65-68,150-172`](../../packets/login/handleLogin.js) |
| `0x05` | S->C | Character struct + 3 | ID u8, Slot u8, Exists u8, Character, Unknown u8 | [`handleLogin.js:31-36`](../../packets/login/handleLogin.js) |
| `0x06` | C->S | 170 | Slot u8, Clan u32, Name[13], Gender u32, Hair u32, Face u32, 84-byte ignore, WeaponID u32, 52-byte ignore | [`handleManageCharacter.js:26-234`](../../packets/login/handleManageCharacter.js) |
| `0x07` | C->S | 21 | Slot u8, Clan u32, Namepart[4] | [`handleManageCharacter.js:238-273`](../../packets/login/handleManageCharacter.js) |
| `0x07` | S->C | Character struct + 2 | ID u8, Status u8, Character, Unknown u8 | [`handleManageCharacter.js:20-23`](../../packets/login/handleManageCharacter.js) |
| `0x08` | S->C | 2 | Character-delete result ID/status | [`handleManageCharacter.js:7-8,269-273`](../../packets/login/handleManageCharacter.js) |
| `0x09` | C->S | 13 | MapID u32, Slot u8, UserID[8] | [`handleGameStart.js:15-135`](../../packets/login/handleGameStart.js) |
| `0x0A` | C->S | 9 | MapID u32, Slot u8, Port u32 | [`handleGameStart.js:137-150`](../../packets/login/handleGameStart.js) |
| `0x0A` | S->C | 21 | ID u8, status u8, IP[16], Port u32 | [`handleGameStart.js:9-12,126-135`](../../packets/login/handleGameStart.js) |
| `0x0B` | C->S | fixed/unknown | Gift-code request; exact structure incomplete | [`handleGift.js:10-34`](../../packets/login/handleGift.js) |
| `0x0C` | C->S/S->C | 81 reply | Gift reply includes ID, code and ten GiftItem records | [`handleGift.js:5-8,33-34`](../../packets/login/handleGift.js) |
| `0x0D` | S->C | 5 | ID u8, signed/unsigned 32-bit Value | [`handleGift.js:50`](../../packets/login/handleGift.js) |
| `0x12` | C->S | 18 | Pin[4], pad1, NewPin[4], pad1, pad8; handler unimplemented | [`handleLogin.js:58-63,180-185`](../../packets/login/handleLogin.js) |
| `0x13` | C->S | 13 | Pin[4], pad1, pad8 | [`handleLogin.js:53-56,150-178`](../../packets/login/handleLogin.js) |

## World lifecycle packets

| Opcode | Dir | Payload | Fields/behavior | Evidence |
|---:|---|---:|---|---|
| `0x01` | C->S | 18 | Name[13], Slot u8, MapID u32 | [`handshake.js:5-96`](../../packets/world/handshake.js) |
| `0x02` | C->S | source-ambiguous | Username[14], CharacterName[13], MapID u32, Slot u32, Unknown3-Unknown23, Unknown22[3], Unknown23 u32, UnknownByte u8 | [`world_login.js:178-329`](../../packets/world/world_login.js) |
| `0x02` | S->C | 2+ | World login handler data; character/world packets follow | [`world_login.js:250-285`](../../packets/world/world_login.js) |
| `0x08` | C->S | 16 | ZoneID u32, PortalID u32, Unknown1 u32, Unknown2 u32 | [`zonechange.js:1-231`](../../packets/world/zonechange.js) |
| `0x0A` | S->C | 21 | status u8, IP[16], Port u32 | [`zonechange.js:227-231`](../../packets/world/zonechange.js) |

## Active gameplay/social registrations

| Opcode | Payload | Meaning | Evidence |
|---:|---:|---|---|
| `0x03` | large/unknown | Movement/action variant | [`newAction.js:1-39`](../../packets/world/newAction.js), [`action.js:1424`](../../packets/world/action.js) |
| `0x04` | large/unknown | Movement/action variant | [`newAction.js:13-38`](../../packets/world/newAction.js) |
| `0x05` | large/unknown | Movement/action variant | [`newAction.js:40-69`](../../packets/world/newAction.js) |
| `0x07` | 8 fixed | Friends refresh request | [`friends.js:52-55`](../../packets/world/friends.js) |
| `0x09` | 81 | Whisper: NameTo[13], NameFrom[13], Message[51], two signed u32 | [`chat.js:108-150`](../../packets/world/chat.js) |
| `0x0A` | 86 | Party chat; parsed but "not implemented" | [`party.js:429-438`](../../packets/world/party.js) |
| `0x0F` | 8 fixed | Leave party | [`party.js:349-354`](../../packets/world/party.js) |
| `0x12` | 8 fixed | Disband party | [`party.js:341-347`](../../packets/world/party.js) |
| `0x13` | 72 | Name[13], Message[51], Unknown[8] | [`chat.js:61-84`](../../packets/world/chat.js) |
| `0x14` | 16 | Item-use 16-byte variant | [`ItemUse16Byte.js:1-18`](../../packets/world/ItemUse16Byte.js) |
| `0x16` | 8 fixed | Spend stat points | [`statpoints.js:9-110`](../../packets/world/statpoints.js) |
| `0x17` | unknown | Equip check | [`equipcheck.js:12-30`](../../packets/world/equipcheck.js) |
| `0x18` | 20 | Skill/buff apply: SkillID, SkillLevel, Slot, Unk4, Unk5 | [`skillUse.js:280-305`](../../packets/world/skillUse.js) |
| `0x19` | 24 | Used item/skill state: 6 u32 fields | [`skillUse.js:280-296`](../../packets/world/skillUse.js) |
| `0x1D` | 21 | Party invite: CharacterName[13] + pad[8] | [`party.js:280-325`](../../packets/world/party.js) |
| `0x1F` | 25 | Party join result: Name[13], Result u8, Unk1 u32, Unk2 u32 | [`party.js:356-404`](../../packets/world/party.js) |
| `0x20` | unknown | Duel family; active/relevant comments | [`duel.js:86-228`](../../packets/world/duel.js) |
| `0x22` | unknown | Duel accept/deny | [`duel.js:108-125,184-199`](../../packets/world/duel.js) |
| `0x24` | unknown | Duel end/cancel | [`duel.js:169-174`](../../packets/world/duel.js) |
| `0x26` | unknown | Trade invite/request | [`trade.js:79-91`](../../packets/world/trade.js) |
| `0x28` | unknown | Trade response | [`trade.js:93-119`](../../packets/world/trade.js) |
| `0x2A` | unknown | Trade stage | [`trade.js:121-134`](../../packets/world/trade.js) |
| `0x2B` | unknown | Trade item stage | [`trade.js:136-162`](../../packets/world/trade.js) |
| `0x2C` | unknown | Trade finish stage | [`trade.js:165-188`](../../packets/world/trade.js) |
| `0x2E` | unknown | Refill bottle | [`refill_bottle.js:4-38`](../../packets/world/refill_bottle.js) |
| `0x34` | unknown | Open personal shop | [`openshop.js:237-264`](../../packets/world/openshop.js) |
| `0x35` | unknown | Open-shop browse/action | [`openshop.js:267-282`](../../packets/world/openshop.js) |
| `0x36` | unknown | Open-shop buy/action | [`openshop.js:284-425`](../../packets/world/openshop.js) |
| `0x38` | unknown | Friends | [`friends.js:5-50`](../../packets/world/friends.js) |
| `0x39` | unknown | Generate antique | [`generate_antique.js:14-83`](../../packets/world/generate_antique.js) |
| `0x3A` | unknown | Enchant | [`enchant.js:47-191`](../../packets/world/enchant.js) |
| `0x3C` | unknown | Book of clear fog | [`bookofclearfog.js:1-22`](../../packets/world/bookofclearfog.js) |
| `0x3E` | 72/81 | Faction chat | [`chat.js:87-106`](../../packets/world/chat.js) |
| `0x3F` | unknown | Guild/trade invite family | [`guild.js:301-333`](../../packets/world/guild.js) |
| `0x41` | unknown | Guild/trade response family | [`guild.js:338-417`](../../packets/world/guild.js) |
| `0x42` | unknown | Lucky ticket/trade start family | [`luckyticket.js:17-28`](../../packets/world/luckyticket.js) |
| `0x43` | 33 observed | Change scroll; not registered | [`changescrolls.js:2-51`](../../packets/world/changescrolls.js) |
| `0x4D` | unknown | Nangi track family | [`nangi_track.js:139-151`](../../packets/world/nangi_track.js) |
| `0x4F` | unknown | Nangi track | [`nangi_track.js:41-71`](../../packets/world/nangi_track.js) |
| `0x50` | unknown | Nangi track | [`nangi_track.js:74-119`](../../packets/world/nangi_track.js) |
| `0x51` | unknown | Guild | [`guild.js:110-126`](../../packets/world/guild.js) |
| `0x59` | unknown | Trader | [`trader.js:22-33`](../../packets/world/trader.js) |
| `0x61` | unknown | Combine | [`combine.js:22-123`](../../packets/world/combine.js) |
| `0x62` | unknown | Gambler dice | [`gambler.js:14-120`](../../packets/world/gambler.js) |
| `0x63` | 16 | Item-use variant | [`ItemUse16Byte.js:15-18`](../../packets/world/ItemUse16Byte.js) |
| `0x74` | unknown | Guild | [`guild.js:196-299`](../../packets/world/guild.js) |
| `0x75` | 8 fixed | AP shop request | [`ap_shop.js:5-12`](../../packets/world/ap_shop.js) |
| `0x7A` | 62 | Buff timeout/confirmation: 15 u32 + 2 u8 | [`skillUse.js:305-314`](../../packets/world/skillUse.js) |
| `0x7B` | unknown | Misc | [`misc.js:21-26`](../../packets/world/misc.js) |
| `0x88` | unknown | Guild | [`guild.js:444-456`](../../packets/world/guild.js) |
| `0x89` | unknown | Guild | [`guild.js:451-456`](../../packets/world/guild.js) |
| `0x8F` | unknown | Guild | [`guild.js:458-476`](../../packets/world/guild.js) |
| `0x91` | unknown | Guild | [`guild.js:128-174`](../../packets/world/guild.js) |
| `0x5E` | unknown | Guild | [`guild.js:427-442`](../../packets/world/guild.js) |
| `0x8B` | unknown | Misc opcode numeric 139 | [`misc.js:10-18`](../../packets/world/misc.js) |

## Known server replies

| Opcode | Structure/meaning | Evidence |
|---:|---|---|
| `0x15` | Raw handshake success string | [`handshake.js:96`](../../packets/world/handshake.js) |
| `0x16` | Character info, inventory/storage/bank | [`world_login.js:250-285`](../../packets/world/world_login.js) |
| `0x17` | World/faction state packet | [`world_login.js:265-285`](../../packets/world/world_login.js) |
| `0x1D` | Whisper/friend status | [`chat.js:124-150`](../../packets/world/chat.js) |
| `0x20` | Whisper message | [`chat.js:134-140`](../../packets/world/chat.js) |
| `0x25` | Party member join | [`party.js:119-126`](../../packets/world/party.js) |
| `0x26` | Party member leave | [`party.js:141-148`](../../packets/world/party.js) |
| `0x28` | Party roster | [`party.js:211-258`](../../packets/world/party.js) |
| `0x29` | Party disband | [`party.js:165-172`](../../packets/world/party.js) |
| `0x2A` | Chat/faction chat reply | [`chat.js:80-101`](../../packets/world/chat.js) |
| `0x2B` | Item action reply | [`itemactions.js:46-95`](../../packets/world/itemactions.js) |
| `0x2C` | Attack reply | [`action.js:187-203,1327`](../../packets/world/action.js) |
| `0x33` | Party invite | [`party.js:300-303`](../../packets/world/party.js) |
| `0x35` | Party invite result | [`party.js:305-308`](../../packets/world/party.js) |
| `0x38` | Duel family | [`duel.js:56-79`](../../packets/world/duel.js) |
| `0x3F` | Trade/guild request family | [`trade.js:86-91`](../../packets/world/trade.js) |
| `0x41` | Trade/guild response family | [`trade.js:100-115`](../../packets/world/trade.js) |
| `0x42` | Trade start/lucky-ticket family | [`trade.js:106-110`](../../packets/world/trade.js) |
| `0x44` | Trade item accept | [`trade.js:144-150`](../../packets/world/trade.js) |
| `0x54` | Guild invite client response | [`guild.js:388-407`](../../packets/world/guild.js) |
| `0x58` | Guild inviter response | [`guild.js:315-318`](../../packets/world/guild.js) |
| `0x5A` | Guild invite status | [`guild.js:346-375`](../../packets/world/guild.js) |
| `0x5B` | Lucky ticket response | [`luckyticket.js:25-28`](../../packets/world/luckyticket.js) |
| `0x55` | Book of clear fog result | [`bookofclearfog.js:7-20`](../../packets/world/bookofclearfog.js) |
| `0x81` | Gambler dice reply | [`gambler.js:115-120`](../../packets/world/gambler.js) |
| `0x84` | Unsigned key/value/stat reply | [`statpoints.js:98-101`](../../packets/world/statpoints.js) |
| `0x8E` | Shop/trade reply | [`openshop.js:393-406`](../../packets/world/openshop.js) |
| `0x9D` | Guild create response | [`guild.js:189-290`](../../packets/world/guild.js) |
| `0x9E` | AP shop: ID u8 + AP i32LE | [`ap_shop.js:11`](../../packets/world/ap_shop.js) |
| `0xBE` | Guild packet variant | [`guild.js:458-476`](../../packets/world/guild.js) |

## Hex fixtures

### Login greeting

```text
00 00 E8 03 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00
```

This encodes packet ID `0x00`, entry Unk `0`, PlayerCount `1000`, and four zero-filled remaining bytes for the 4 entries. Source: [`onConnected.js`](../../packets/login/onConnected.js).

### Login request

Nine-byte receive header:

```text
00 00 00 00 00 00 00 00 03
```

Payload:

```text
50 6C 61 79 65 72 4E 61 6D 65 00 00 00 00
53 65 63 72 65 74 31 32 33 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00
```

The 9 header bytes are inferred zero except for opcode. Source: [`handleLogin.js:48-50,187-188`](../../packets/login/handleLogin.js).

### Login success

```text
03 00
00 00 00 00 00 00 00 00 00 00 00 00
50 6C 61 79 65 72 4E 61 6D 65 00 00
00 00 00 00 00 00 00 00
2A 2A 2A 2A
00
```

Source: [`handleLogin.js:238-255`](../../packets/login/handleLogin.js).
