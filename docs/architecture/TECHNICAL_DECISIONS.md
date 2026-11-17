# InfiniteSky Technical Decisions

## ADR-001 — Preserve the legacy wire protocol as a boundary

**Decision**

Implement packet framing, field order, endian types, fixed strings, reply asymmetry, and opcode behavior in a standalone `InfiniteSky.Protocol` project.

**Reason**

The current client cannot be changed in place. Legacy traffic uses a 9-byte receive header with opcode at byte 8, direct server reply packets, fixed payload sizes, little-endian values, and UTF-8 helpers that currently behave as byte/character pass-through. These details must be verified independently of the new engine.

**Consequences**

- Protocol tests are byte-level and run in CI.
- Unknown/reserved fields remain explicit.
- Gameplay classes never inspect protocol buffers.
- Compressed state wrappers receive separate definitions only after fixtures validate them.

## ADR-002 — Start as a modular monolith

**Decision**

Deploy one .NET 10 server process containing login, world, game data, persistence, and zone actors.

**Reason**

The original transfer queue, account online status, guild/party state, and zone objects rely on one process. Distribution would require solving session transfer, zone ownership, idempotency, and account uniqueness before replacing the engine.

**Consequences**

- Simpler debugging and rollback.
- Clear internal boundaries permit later splitting.
- One process owns global coordination.
- Horizontal scaling is not promised in the first release.

## ADR-003 — Use zone actors, not global locks

**Decision**

Each zone owns one asynchronous mailbox and serially mutates players, monsters, NPCs, items, spatial state, buffs, and timers.

**Reason**

Gameplay is naturally partitioned by map. The legacy Node.js event loop serialized state, while many future C# designs could accidentally allow packet handlers to mutate the same entities concurrently.

**Consequences**

- Zone logic is single-threaded and deterministic.
- Different zones may run concurrently.
- Blocking database I/O is forbidden in a zone actor.
- Transfers require a clear source-to-target ownership protocol.
- Do not use a third-party actor framework initially.

## ADR-004 — Separate persistent state from runtime state

**Decision**

`Character`/`Account` domain aggregates represent persistent state. `PlayerActor` and other actor classes contain ephemeral connection, movement, target, cooldown, animation, buff, and visibility state.

**Reason**

The original code attaches socket and packet state to Mongoose documents, duplicates Real/runtime position, and mixes timers with data. A separation produces smaller saves and testable gameplay.

**Consequences**

- Explicit snapshots/mappers are required.
- Packet state does not leak into MongoDB accidentally.
- Runtime fields survive only according to checkpoint/disconnect policy.

## ADR-005 — Keep protocol and domain packet names separate

**Decision**

Use domain-oriented C# names for commands/results. Preserve opcode names and legacy labels in codec/fixture comments and tests.

**Reason**

Opcode `Action`, `ItemAction`, and generic unknown field names are not reliable domain semantics.

**Consequences**

- Refactoring rules does not rename wire types.
- Packet mapping is an explicit translation layer.
- New business rules can be tested without binary fixtures.

## ADR-006 — Use hand-written codecs with shared primitives

**Decision**

Hand-write protocol codecs over `Span<byte>` and `BinaryPrimitives`. Generate or template code only if repeated layout becomes sufficiently large and validated.

**Reason**

The protocol contains unknown tails, conditional item layouts, asymmetric requests/replies, and experimental compressed formats. Runtime reflection-based serializers are harder to verify at exact offsets and offer little benefit.

**Consequences**

- Slightly more codec code.
- Exact offsets and field sizes are visible in one place.
- Malformed input and no-allocation fast paths are straightforward.
- Packet fixture tests guard every structural change.

## ADR-007 — Use Pipelines for ingress and one ordered writer per connection

**Decision**

Use `System.IO.Pipelines` for TCP reads and a bounded outbound channel/PipeWriter for each connection.

**Reason**

Fragments and coalesced packets are normal for TCP. The legacy `CachedBuffer` mutates a shared array and discards all data on unknown packets. Pipelines provide bounded reassembly and backpressure without packet handlers writing concurrently.

**Consequences**

- Frame parsing is transport-independent and testable.
- One writer preserves packet ordering.
- Slow clients are bounded, then disconnected according to policy.
- No domain code receives a `Socket`.

## ADR-008 — MongoDB remains the first persistent store

**Decision**

Use the official MongoDB driver and explicit BSON documents/mappers, retaining collection compatibility where practical.

**Reason**

Existing persisted accounts, characters, guilds, counters, and banks are MongoDB/BSON. Changing the database at the same time as protocol and runtime would multiply migration risk.

**Consequences**

- No SQL migration is part of phase one.
- Existing `Mixed` arrays require tolerant readers.
- Legacy passwords/PINs are migrated only through a separate security policy; the C# server initially supports compatibility verification.
- Mappers and import/dry-run tooling are required.

## ADR-009 — No generic repository abstraction

**Decision**

Define aggregate-specific repository ports such as `IAccountRepository` and `ICharacterRepository`; do not introduce `IRepository<T>` or a generic specification framework.

**Reason**

Account duplicate login, character bank storage, counters, sparse arrays, and guild membership each have distinct persistence behavior.

**Consequences**

- Fewer misleading abstractions.
- Tests stub required methods directly.
- Implementations remain explicit.

## ADR-010 — Bounded asynchronous persistence

**Decision**

Zone actors publish immutable character snapshots to a bounded persistence queue. MongoDB calls occur outside gameplay loops.

**Reason**

Legacy handlers call `save()` throughout packet processing, coupling gameplay latency to database behavior. A server loop cannot depend on network/database latency for each movement packet.

**Consequences**

- Dirty/save state must be explicit.
- Critical economy/inventory operations require confirmation/flush semantics.
- Save failures have retry/visibility.
- Graceful shutdown drains pending saves.

## ADR-011 — Stateless immutable game catalog

**Decision**

Load `.IMG`, translation, spawn, and region data once at startup, validate it, then expose read-only/frozen indexes.

**Reason**

These definitions are static game rules/data. The original loader uses global prototypes and late-loaded mutable arrays, making dependency order and reload timing fragile.

**Consequences**

- Data changes require controlled reload/restart.
- Startup detects required corruption/truncation.
- Tests can construct small catalogs.
- Unknown source fields are retained in separate raw bytes/diagnostic projections.

## ADR-012 — Explicit compatibility profiles

**Decision**

Introduce at least:

- `LegacyParity`: reproduce known current wire-visible behavior, including explicitly documented defects;
- `Corrected`: server-validated gameplay for controlled clients/tests or post-parity rollout.

**Reason**

Several desired improvements change mechanics: server authoritative movement, fixing double monster damage subtraction, real respawn timers, buff expiration, Chi cost, and stricter inventory transactions. They cannot be silently mixed into a protocol migration.

**Consequences**

- Rules receive a profile/context explicitly.
- Byte compatibility tests and gameplay behavior tests are separate.
- Operations can deploy protocol-compatible code before enabling fixes.
- The profile is startup configuration and prominent in diagnostics.

## ADR-013 — Time, randomness, and persistence are injectable

**Decision**

Use `TimeProvider`, an injectable deterministic random source, and explicit repository interfaces in gameplay services.

**Reason**

Without fake time/randomness, tests of buffs, loot, expiration, AI, respawn, and persistence are flaky or slow.

**Consequences**

- Tick/timeout tests are immediate.
- Production uses the system time provider and cryptographic/standard random policy as appropriate.
- Security tokens use cryptographic randomness, never gameplay RNG.

## ADR-014 — Scripting is isolated and not the implementation language

**Decision**

Core C# owns packets, stat rules, inventory, combat, persistence, and zone simulation. Optional scripts can observe approved events and issue typed commands through isolation.

**Reason**

The legacy global VM shares raw globals, globals into `vm.runInThisContext`, file watching, arbitrary eval-like access, and hot mutation. This is difficult to secure, test, and debug.

**Consequences**

- Existing bundled behavior is ported to C# first.
- Scripts cannot access sockets, raw global objects, Mongo documents, or process APIs.
- Reload failure does not corrupt active game state.
- Scripting is not required for basic server startup.

## ADR-015 — Options validation at startup

**Decision**

Use strongly typed options with startup validation. Production configuration rejects placeholder IPs, malformed NAT masks, missing ports, invalid data paths, and unsafe limits.

**Reason**

The original template contains placeholder strings, a password, permissive `main` globals, and ports/interfaces that are used without a schema.

**Consequences**

- Fail-fast startup.
- Clear validation error messages.
- Runtime reload is restricted to values that can safely change.

## ADR-016 — Structured diagnostics without credentials

**Decision**

Use structured logging, source-generated log methods on hot paths, OpenTelemetry traces/metrics, and bounded opt-in hex diagnostics.

**Reason**

The analyzed server logs exceptions, payloads, account objects, and broad state inconsistently. Operational investigation requires correlation and latency visibility, while credentials and arbitrary packets should never appear in normal logs.

**Consequences**

- Logs use connection/zone/account/character correlation IDs.
- Passwords, PINs, transfer credentials, and database strings are redacted.
- Metrics expose frame rejection, queue pressure, tick duration, connection phases, and save latency.

## ADR-017 — Avoid infrastructure overengineering

**Decision**

Do not initially add a broker, gRPC, Kubernetes operator, ECS framework, CQRS/event sourcing, distributed cache, or generic mediator.

**Reason**

The existing system runs as a legacy Node.js process and has known protocol/gameplay uncertainty. These frameworks would obscure behavior and slow byte-level parity.

**Consequences**

- Simple project dependencies and local debugging.
- A later scale-out design can reuse protocol/domain boundaries.
- Infrastructure can be added only when a measured requirement exists.

## ADR-018 — Test fixtures define compatibility

**Decision**

Every known packet and key gameplay decision receives a stable fixture:

- hexadecimal request/reply fixtures;
- exact decoded records;
- deterministic stat/loot/damage examples;
- representative legacy BSON documents;
- integration transcript for login-to-world entry.

**Reason**

Much of the protocol is reverse-engineered and large records contain unresolved tails. Passing unit tests that reconstruct intended data from the implementation itself are insufficient.

**Consequences**

- Captures and expected offsets become reviewable artifacts.
- Unknown fields cannot be silently lost.
- Migration can compare JavaScript and .NET outputs at the byte/record level.

## Migration sequence

1. Add protocol readers/writers and hex fixture tests.
2. Add game-data readers and data validation tests.
3. Add domain models, stat/progression/inventory rules with deterministic tests.
4. Add BSON mappers and dry-run migration comparison.
5. Add login flow over loopback TCP with existing packets.
6. Add world transfer, character packet, and zone entry.
7. Add movement/state replication in `LegacyParity`.
8. Port inventory, skills, drops, parties/guilds, and admin commands.
9. Add natural monster spawn/AI and quests only after their data/packets are established.
10. Enable selected `Corrected` behaviors behind configuration with client validation.

## Open architecture questions

- Which `.pac` captures are complete canonical protocol packets rather than experiments?
- Are bytes 0-7 of the 9-byte receive header ever meaningful to the client, even if Node.js ignores them?
- What exact variable/compressed packet formats are sent by the client and server?
- Which MongoDB collection names/version assumptions exist in deployed servers?
- Which original game-data directories are licensed and available in deployment?
- Are world and login listeners deployed behind NAT with one or multiple instances?
- Which legacy behavior defects clients depend on?
- Which quest and NPC interaction packets are known outside the tracked repository?

These remain gated research items; the C# design preserves a place for them without inventing answers.
