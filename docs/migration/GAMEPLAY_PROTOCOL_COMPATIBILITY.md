# Gameplay Protocol Compatibility

## Existing .NET protocol boundary

The .NET protocol project has bounded little-endian readers/writers, fixed framing and a small set of explicit structures. `LegacyAttackPacketReply` represents a 56-byte reply body matching the source `WorldPC.AttackPacketReply` fields in `packets/world/action.js`. A structure definition alone is not a registered incoming gameplay flow.

The world registry currently registers only the verified fixed-layout handshake, zone change, item action descriptor and skill/buff descriptors. It does not register the source movement/action 0x03-0x05 handlers, character action state, level-up/0x2F messages, item action replies, quest/social packets, or full world Character state. In the root source 0x05 is an action-state handler that only inspects target/distance and broadcasts; it does not apply combat damage. The original source itself has overlapping registrations and several captured/unknown packet structures.

## Required evidence

Before any gameplay packet is called compatible:

1. Record the original opcode, direction, payload size, field widths/order/signedness and framing context from root registration and `restruct`.
2. Use original captures or fixtures; do not infer missing offsets from packet names.
3. Verify outbound ordering and recipient rules with controlled client transcripts, including state/spatial updates.
4. Verify persistence and rollback for state-mutating messages.

Root movement handlers use action packet state and area broadcasts. A dormant combat handler body constructs compressed 0x2C, but its registrations are commented out and the active action handler does not apply damage. Item action replies use 0x2B. The root XP method emits 0x2F and 0x2E when invoked. These source facts do not establish complete C# wire support or an active client combat flow.

## Compatibility status

No gameplay packet handler or packet fixture was added in Prompt 16. No original-client gameplay compatibility is claimed. The synthetic XP and stat-allocation tests verify arithmetic only; they do not prove packet order, field layouts, persistence, game-data compatibility, or full player behavior. `GP-007..038` remain unmigrated, while `GP-004` and `GP-005` are `IN_PROGRESS`; protocol rows remain governed by their individual matrix statuses.
