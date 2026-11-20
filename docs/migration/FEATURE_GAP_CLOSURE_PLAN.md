# Feature Gap Closure Plan

## Goal and boundaries

This order closes only source-verified migration gaps and respects each feature's dependencies. It does not authorize full-system integration, original-client certification, broad security redesign, or work on Prompt 20. The [gap register](./REMAINING_FEATURE_GAPS.md) maps each open matrix feature ID to its missing behavior and blocker; the [feature parity matrix](../FEATURE_PARITY_MATRIX.md) remains authoritative.

## Closure order

| Order | Work package | Feature IDs | Entry criteria | Required closure evidence / stop condition |
|---:|---|---|---|---|
| 0 | Preserve authenticated account authority | `AUTH-003`, `SES-001` | Account lookup and session store exist | Prompt 19 now projects Mongo `Level`, maps legacy numeric forms/default 0, rejects malformed/non-finite values, and carries the value in the authenticated session. Mapper/session tests pass. Do not claim command parity. |
| 1 | Acquire authoritative artifacts and lock schemas | `DAT-001..011`, `PER-002..006`, `PER-008`, `PER-011`, `INT-006..008` | Isolated data/database access; no production writes | Obtain sanitized representative BSON, actual collection/index metadata, `.IMG/.WREGION` assets and checksums, and canonical packet captures. Until then keep full mappers/readers and compatibility claims blocked. Never fabricate missing fields. |
| 2 | Finish bounded data and packet primitives | `DAT-001..010`, `PRO-007..017`, `PRO-032..033` | Artifacts from order 1; frame/primitive foundation | Implement readers and codecs by record/opcode with unknown-byte retention, size guards, exact golden fixtures, malformed/truncation tests. Unknown Character/world structures stop at preserved bytes, not guessed fields. |
| 3 | Complete account/character persistence slices | `PER-003..005`, `PER-008..012`, `PER-013..014` | Representative BSON and exact collection naming; `DAT`/domain schemas | Add targeted repositories and save coordination without document replacement. Validate field/type preservation, per-server bank behavior, sequence concurrency, transaction failure, cancellation, queue pressure and shutdown durability. |
| 4 | Complete login, character lifecycle and transfer prerequisites | `NET-006..009`, `PRO-008..010`, `GP-001..003`, `ENT-013..020` | Orders 2-3, identified endpoint policy and world dispatch ownership | Implement each source-backed packet and state transition; maintain account/character ownership; cover stale session, invalid slot and transfer expiry. Stop where source behavior/capture evidence is ambiguous. |
| 5 | Connect world/zone/player lifecycle | `WLD-001..010`, `ZON-004..008`, `ENT-001..021`, `INF-005/007` | Character packet and transfer paths plus loaded zone data | Introduce no duplicate managers. Connect incoming state to existing zone/player managers with single ownership, exact cleanup and persistence. Verify actor/order/time semantics and transition rollback using controlled tests. |
| 6 | Add verified gameplay slices in dependency order | `DAT-002..006`, `DOM-007..011`, `GP-004..024`, `PRO-018..025` | Required data, domain rules, packet codecs and persistence | Start with proven progression/stat/visible packet actions. Then movement, inventory, equipment, consumables, skills, combat and loot only where active source rules and fixtures are available. Keep dormant/experimental handlers and missing mechanics blocked. |
| 7 | Add source-supported social, commerce and events | `DOM-015`, `GP-025..038`, `PRO-020..031`, `PER-005/006` | Order 5; individual protocol and persistence contracts | Implement separately testable party, guild, friend, chat, trade, duel, NPC and event behavior. Do not invent quest progression, ignore lists, player mail, moderation policy, or event rewards absent from source. |
| 8 | Migrate selected operational/script behavior | `SCR-001..013`, `ADM-001..013`, `INF-012..018` | Concrete supported source module/tool and bounded .NET owner | Prefer native services for source-backed behavior; do not recreate shared VM, arbitrary shell/eval, or disabled commands. Require an authenticated active-character context for GM operations, actual local operator boundary for console work, and verified output/effects. |
| 9 | Reconcile source coverage and establish release baseline | `INT-001..016` | Earlier source-backed slices have implementation and focused tests | Recount every matrix ID, classify originals' stubs/absence, check dependency injection/start/stop and non-production integration fixtures. This is a readiness audit, not the Prompt 20 full integration campaign or Prompt 21 client certification. |

## Priority gates

1. No P0 item moves to `VERIFIED` without exact source/capture or representative data plus executable tests.
2. `BLOCKED` items remain blocked until the named input/dependency exists; do not translate missing assets into synthetic “production” fixtures.
3. Keep `IN_PROGRESS` for isolated calculations such as `GP-004/005` until data, runtime, packet, and persistence layers are connected.
4. `NOT_APPLICABLE` remains limited to the evidence-backed missing external plugin implementation; difficulty is not a reason to use it.
5. Any compatibility correction is documented separately and tested against the original behavior.
6. Do not start Prompt 20 under this plan. The readiness report must show the outstanding P0 list and ask for approval before that stage.
