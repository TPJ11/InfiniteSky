# Connection Lifecycle

## Login connection

1. TCP connect to login port.
2. Server writes server-list packet `0x00`.
3. Client sends login packet `0x03` with fixed Username[14], Password[14], and pad[63].
4. Server checks MongoDB account data:
   - database failure: status 1;
   - account missing: status 2;
   - password mismatch: status 3;
   - active world duplicate: status 5;
   - success: status 0.
5. Success sends 0-3 character-slot packets `0x05`.

The password is plaintext in both database and network packet. Source: [`handleLogin.js:187-344`](../../packets/login/handleLogin.js).

## Character management

While logged in:

- `0x06` creates a character.
- `0x07` deletes a character.
- `0x09` requests transfer to the world server.

Character create uses a fixed slot index and validates clan, name, hair, face, and starting weapon. Character delete removes the MongoDB document. Game start prepares character state and puts the login socket in the world transfer queue.

Source: [`handleManageCharacter.js`](../../packets/login/handleManageCharacter.js), [`handleGameStart.js`](../../packets/login/handleGameStart.js).

## Login-to-world transfer

1. Client sends `0x09` with MapID, slot, and UserID.
2. Login server chooses LAN, NAT, loopback, or external IP and world port.
3. Login server replies `0x0A`.
4. On status 0, client connects to world server.
5. Client sends `0x01` handshake with name, slot, and MapID.
6. World finds the transfer queue entry and copies account/character state.
7. World writes handshake success `0x15`.
8. Client sends `0x02` world auth packet.
9. World sends `0x16` character info, `0x17` world state, zone state, and nearby state.

Source: [`handleGameStart.js`](../../packets/login/handleGameStart.js), [`handshake.js`](../../packets/world/handshake.js), [`world_login.js`](../../packets/world/world_login.js).

## Normal world session

The socket is authenticated and registered in `World.Zone.Clients`. Gameplay packets arrive by opcode and are handled through packet scripts.

Area updates use:

- `Zone.QuadTree`;
- `Zone.sendToAllAreaLocation`;
- `config.viewable_action_distance`.

The game loop emits `step` every 100 ms. Zone state updates QuadTree and monster/NPC interval broadcasts occur every 4 seconds. Source: [`main.js:173-181`](../../main.js), [`GameStep.js:7-24`](../../GameStep.js), [`zone.js:28-30,234-250,362-372,459-468`](../../zone.js).

## Zone transfer

The client sends opcode `0x08` with ZoneID, PortalID, and two unknown u32 fields. The world server searches zones and portal regions, sets the character's target location, adds it to transfer queue, and replies `0x0A`.

On status 0, the client opens a new connection to the returned IP/port and repeats:

- `0x01` handshake;
- `0x02` world authentication.

Source: [`zonechange.js`](../../packets/world/zonechange.js).

## Disconnect

Login socket cleanup removes the socket, deletes its login index entry, and calls account logout.

World socket cleanup removes it from its zone/world/client arrays and may preserve account state for zone transfer. `zoneTransferLogoutTimer` defaults to 300,000 ms. Source: [`login.js:16-62`](../../login.js), [`world.js:621-704`](../../world.js).
