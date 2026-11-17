# InfiniteSky .NET 10 Project Structure

## Proposed repository layout

```text
InfiniteSky.slnx
src/
  InfiniteSky.Domain/
  InfiniteSky.Protocol/
  InfiniteSky.GameData/
  InfiniteSky.Persistence/
  InfiniteSky.Networking/
  InfiniteSky.World/
  InfiniteSky.Application/
  InfiniteSky.Scripting/
  InfiniteSky.Server/
tests/
  InfiniteSky.Domain.Tests/
  InfiniteSky.Protocol.Tests/
  InfiniteSky.World.Tests/
  InfiniteSky.GameData.Tests/
  InfiniteSky.Persistence.Tests/
  InfiniteSky.Server.IntegrationTests/
fixtures/
  protocol/
  gamedata/
  mongodb/
tools/
  InfiniteSky.ProtocolDump/
  InfiniteSky.DataCompare/
deploy/
docs/
```

This is deliberately one responsibility per actual boundary. No separate project is created merely to hold a few interfaces or DTOs.

## Project responsibilities

### `InfiniteSky.Domain`

Pure .NET library with no networking, MongoDB, sockets, DI, hosting, or file access.

Contains:

- account/character/guild aggregate models;
- item, equipment, inventory, progression, skill, buff models;
- entity IDs and wire-compatible numeric value types;
- pure stat, progression, damage, inventory, and loot rules;
- domain results and events;
- common exceptions for invalid domain state.

Dependencies: BCL only, ideally no NuGet packages.

### `InfiniteSky.Protocol`

Byte-compatible codec project.

Contains:

- packet opcode definitions;
- frame descriptor and payload sizing;
- `Span<byte>`/`BinaryPrimitives` readers/writers;
- fixed-string encoding;
- login/world wire DTOs;
- character/inventory/state packet structures;
- explicit reserved/unknown-byte fields;
- server packet encoders and client command decoders.

Dependencies: `InfiniteSky.Domain`.

Reason for dependency: wire encoders need domain IDs and values. Domain must never depend on protocol.

### `InfiniteSky.GameData`

Original game-data parsing and immutable runtime catalog.

Contains:

- `.IMG` readers for EXP, item, skill, monster, NPC;
- `.MOB`/`.NPC` spawn readers;
- `.WREGION` readers;
- CSV overlays;
- schema/version validation;
- raw unknown-field retention;
- catalog builder returning immutable indexes.

Dependencies: `InfiniteSky.Domain`.

### `InfiniteSky.Persistence`

MongoDB adapter.

Contains:

- BSON document classes;
- account/character/guild/mail/counter repositories;
- explicit document/domain mappers;
- compatibility readers;
- optimistic concurrency policy;
- save batching/queue integration;
- migration and validation tooling.

Dependencies: `InfiniteSky.Domain`, MongoDB driver.

### `InfiniteSky.Networking`

Production TCP transport, independent of concrete gameplay rules.

Contains:

- `LoginTcpListener`, `WorldTcpListener`;
- per-connection pipeline read/write loops;
- frame parser;
- write queue;
- timeout/rate/size policy;
- connection state and metrics;
- advertised endpoint logic/NAT helpers.

Dependencies: `InfiniteSky.Protocol`, `InfiniteSky.Application`.

Do not place packet business logic in sockets.

### `InfiniteSky.World`

Single-threaded-per-zone simulation.

Contains:

- zone actor/mailbox;
- world and entity collections;
- player/monster/NPC/item runtime entities;
- spatial index;
- actor tick scheduler;
- movement/action intake;
- combat, AI, skills, buffs, loot, spawn, timers;
- zone transfer command coordination;
- visibility output construction.

Dependencies: `InfiniteSky.Domain`, `InfiniteSky.Protocol` only where output packets need to be assembled, `InfiniteSky.GameData`.

It must not depend on concrete MongoDB or TCP.

### `InfiniteSky.Application`

Use-case orchestration and ports.

Contains:

- login and world session application services;
- account/character creation, selection and enter-world workflows;
- session-transfer store abstraction/concrete in-memory implementation;
- command dispatcher;
- persistence interfaces and save coordination;
- connection-facing packet handler registration;
- application results and authorization.

Dependencies: `InfiniteSky.Domain`, `InfiniteSky.GameData`, selected interfaces from related runtime projects.

Keep actual MongoDB and socket implementations outside this project.

### `InfiniteSky.Scripting`

Optional extension boundary.

Contains:

- command/event script DTO contract;
- script host lifecycle;
- process/host isolation;
- reload and failure policy.

It depends on `InfiniteSky.Application` and should be optional in host composition. Core login, world, inventory, and combat do not depend on it.

### `InfiniteSky.Server`

Single executable/Generic Host composition root.

Contains:

- `Program.cs`/`HostApplicationBuilder` setup;
- configuration/options validation;
- DI composition;
- login/world listener hosted services;
- startup data load and readiness;
- logging/OpenTelemetry;
- shutdown/save coordination;
- operational CLI endpoints if required.

Dependencies: all implementation projects required for deployment.

## Test projects

### `InfiniteSky.Domain.Tests`

Fast pure unit tests:

- stat formulas;
- progression/stat point bands;
- inventory overlap and stack rules;
- damage/loot calculations;
- timers and compatibility profile behavior.

### `InfiniteSky.Protocol.Tests`

Byte-level tests:

- every opcode registry entry;
- fixed string truncation/NUL behavior;
- partial/coalesced frame parsing;
- all hex fixtures from the protocol docs;
- malformed length/reserved field tests;
- round-trip server replies;
- source-compatibility modes for ambiguous compressed structures.

### `InfiniteSky.World.Tests`

Use a fake `TimeProvider` and deterministic random source:

- zone actor ordering;
- movement/state replication;
- combat;
- buffs/cooldowns;
- drops/ownership/expiry;
- spawn/respawn;
- transfer;
- spatial radius queries.

### `InfiniteSky.GameData.Tests`

Tiny generated records:

- count/record validation;
- known offsets;
- little-endian values;
- fixed strings;
- unknown-byte preservation;
- CSV overlays.

### `InfiniteSky.Persistence.Tests`

Mapper tests and optional MongoDB integration:

- document-to-domain conversion;
- legacy missing fields;
- Mixed inventory arrays;
- sparse/null slots;
- character bank on Account documents;
- counter allocation.

### `InfiniteSky.Server.IntegrationTests`

Controlled integration:

- in-memory/loopback TCP client;
- actual Pipeline protocol parser;
- login/create/select/world entry;
- MongoDB test container or dedicated test adapter;
- fixture game data;
- graceful disconnect/save.

## Source layout examples

```text
InfiniteSky.Protocol/
  Framing/
    PacketFrame.cs
    PacketRegistry.cs
  Binary/
    FixedStringCodec.cs
    LittleEndianReader.cs
  Login/
    LoginRequest.cs
    LoginRequestCodec.cs
    CharacterInfoCodec.cs
  World/
    WorldHandshake.cs
    ActionPacket.cs
    ItemActionPacket.cs
  Shared/
    CharacterWireCodec.cs
    StorageItemCodec.cs
```

```text
InfiniteSky.World/
  Zones/
    ZoneActor.cs
    ZoneCommand.cs
    ZoneTick.cs
  Entities/
    PlayerActor.cs
    MonsterActor.cs
    NpcActor.cs
    WorldItemActor.cs
  Spatial/
    ZoneSpatialIndex.cs
  Services/
    CombatResolver.cs
    SkillService.cs
    LootService.cs
```

## Naming and API conventions

- C# naming follows standard PascalCase/camelCase.
- Packet names retain documented legacy names in compatibility comments/fixture IDs, not in core domain names.
- Public APIs use strongly typed IDs instead of bare magic integers.
- Cancellation tokens are required for I/O.
- Domain methods return explicit result unions/records, not booleans with side-channel logging.
- Avoid Service/Manager for entities when a precise name exists.
- Prefer source code clarity over source generators except for repetitive logging/packet metadata proven by tests.

## Initial project count

Nine production projects may appear substantial, but each maps to an existing major subsystem and permits protocol/gameplay testing without sockets or MongoDB. Do not create additional abstractions or a project-per-feature. The initial server can publish one executable from this modular monolith.
