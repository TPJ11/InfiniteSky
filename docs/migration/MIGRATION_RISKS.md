# InfiniteSky Migration Risks

## Classification

- Severity: Critical, High, Medium, Low.
- Likelihood: High, Medium, Low.
- Facts are directly evidenced in source/docs. Assumptions are plausible but not proven and are labeled.
- A blocker is a current unknown/incomplete area; it is not automatically a matrix status change until implementation is scheduled/reviewed.

| ID | Fact or assumption | Risk | Severity | Likelihood | Affected IDs | Mitigation |
|---|---|---|---|---|---|---|
| R-001 | Fact | Full Character/world packets contain large unknown or unnamed tails; only partial offsets are known | Critical | High | PRO-015..017, INT-001 | Preserve reserved bytes, create captures, compare generated/Node/client bytes, do not infer field meaning |
| R-002 | Fact | Compression/state envelope is incomplete and `.pac` corpus includes experiments | Critical | High | PRO-032..033, PRO-018..019 | Classify captures by opcode/hash; require client/server golden traces before parity release |
| R-003 | Fact | Login/world session uses name/account lookup rather than a cryptographic transfer token | High | High | NET-008..009, INT-005 | Default legacy mode; one-time store; add secured mode only with client migration |
| R-004 | Fact | Passwords/PINs are stored and transmitted as plaintext strings | High | High | AUTH-002..003, PRO-006, PRO-011, PER-002 | Keep production lookup disabled until persistence is available; redact; plan a separate password-hardening migration |
| R-005 | Fact | Account `active` reset/login/logout conditions are inconsistent | High | High | AUTH-003, SES-002..003, PER-007, NET-009 | Explicit state machine and race tests; document stale-flag behavior; define startup recovery in persistence |
| R-006 | Fact | Character/BSON arrays are Mongoose Mixed with sparse/null/hole behavior | Critical | High | PER-003, PER-008..011, GP-019 | Tolerant mappers, representative fixtures, dry-run field comparison |
| R-007 | Fact | Packet inventory/storage/bank layout differs (20/12 bytes and pet variants) | Critical | High | PRO-015, PRO-023, GP-017..019 | Conditional codecs, table-driven item type fixtures, atomic conservation tests |
| R-008 | Fact | Monster record fields from ~180-880 are largely `UnknownNNN` and drive drop guesses | Critical | High | DAT-005, GP-022 | Retain raw bytes; only map validated chance/ID pairs; capture deterministic outputs |
| R-009 | Fact | Monster runtime references `info.Damage`/`Defense` while loaded data names `AttackPower`/`DefensePower` | High | High | DOM-012, GP-010 | Mapping decision fixture and explicit compatibility behavior |
| R-010 | Fact | Legacy action handler is commented while `newAction.js` is active, so intended combat differs from active behavior | Critical | High | PRO-018, GP-009..011 | Label both profile paths; compare client expectation; port active behavior before enabling corrected combat |
| R-011 | Fact | Monster combat subtracts HP twice and immediately restores full HP on death | High | High | GP-010, GP-023 | Legacy trace preserves if required; corrected profile fixes and is separately tested |
| R-012 | Fact | XP is awarded by damage and level overflow arithmetic is unusual | High | Medium | DOM-007, GP-004, GP-010 | Golden vectors; explicit legacy arithmetic; corrected formula only after balance approval |
| R-013 | Fact | Active movement accepts client positions with no speed/terrain collision validation | Critical | High | GP-007..008, ZON-008 | Legacy profile for client parity; corrected validation staged after client testing |
| R-014 | Fact | Terrain loading is commented and navigation data is experimental | Critical | High | ZON-008, GP-008 | Do not claim authoritative terrain/collision; identify required data; unit test boundaries |
| R-015 | Fact | Monster `.MOB` loading and AI stepping are not active | Critical | High | ZON-005, GP-012, GP-023 | Classify natural spawning as incomplete; implement corrected system behind profile and evidence |
| R-016 | Fact | Character respawn is empty and death mechanics are incomplete | Critical | High | GP-024 | Client/capture research for penalties/destinations; conservative documented temporary behavior |
| R-017 | Fact | Quest file has only a captured 0x45 request and no active handler | Critical | High | GP-025, PRO-031 | Block quest parity until protocol/data/state/reward semantics are available |
| R-018 | Fact | No generic NPC interaction system exists; dialogue/shop behavior is scattered | High | High | GP-021, GP-026, ENT-005 | Catalog NPC types/packet actions with captures; keep supported interactions explicit |
| R-019 | Fact | Buff application does not deduct calculated Chi cost; 0x7A does not clear state | High | High | GP-015..016 | Legacy profile reproduces; corrected fake-time cost/expiry tests |
| R-020 | Fact | `updateSkills()` is empty despite skill bonus fields existing | High | Medium | DOM-008, GP-006, GP-015 | Separate known equipment modifiers from unsupported skill bonuses; no silent invention |
| R-021 | Fact | Zone removal uses `slice` without assignment, leaving clients in array | High | High | WLD-003, ZON-006, ENT-001 | Always remove in C#; add profile-visible difference/deviation record |
| R-022 | Fact | Entity references mix Monster/Monsters and NPC/monster counters | High | High | ENT-004, ENT-007, ZON-005 | One ID allocator and explicit entity collections; fixture IDs for clients |
| R-023 | Fact | Timers/broadcasts are based on 4s/5s/3m intervals and 100ms global step | High | Medium | INF-007, WLD-006, ENT-006, ENT-005 | Inject time; fake-time tests; monotonic timer buckets |
| R-024 | Fact | JavaScript numbers, float32 unpack, and repeated `/100` conversions may differ from .NET rounding | High | Medium | DOM-008, GP-010, GP-022 | Golden decimal/float vectors; define floor/round semantics; preserve wire float32 |
| R-025 | Fact | Direct `.save()` calls can interleave, lose state, or block event handling | High | High | PER-009, PER-012 | Aggregate snapshots, bounded queue, critical acknowledgements, optimistic checks |
| R-026 | Assumption | Production MongoDB may contain fields/collections absent from current schema examples | Critical | Medium | PER-011, INT-006 | Require production-like dump; unknown-field audit and preservation strategy |
| R-027 | Fact | Collection/name/casing and database version are not guaranteed by repository | High | Medium | PER-001..011 | Read-only discovery and configured collection mapping before migration |
| R-028 | Fact | VMScript uses `vm.runInThisContext`, file watches, globals, and optional process child scripts | Critical | High | SCR-001..008 | Isolated capability model; port core to C#; extension disabled by default |
| R-029 | Fact | Console CLI permits arbitrary shell and JavaScript execution | Critical | High | ADM-012..013, INF-013 | Production restriction, authenticated local operator, audit and explicit developer-only tools |
| R-030 | Fact | Commands have duplicate names/low privilege levels and test-only scripts | High | High | ADM-003..013 | Registry duplicate detection, audit every required level, remove raw test commands by approval |
| R-031 | Fact | FruitSalad is a separate incomplete multi-process/runtime/editor stack | High | High | INF-014..015, INT-011..013, SCR-003 | Inventory by capability, do not merge blindly; decide support/exclusion per matrix item |
| R-032 | Assumption | The original client expects server object/node IDs to be in a specific range or lifetime | High | Medium | ENT-007, ZON-002, PRO-019 | Capture actual IDs; generation/allocator compatibility tests |
| R-033 | Fact | Shared opcode values serve different packets by direction/subsystem (for example 0x1D, 0x2A) | High | High | PRO-020..031 | Descriptor key includes listener/direction/opcode; integration fixtures |
| R-034 | Fact | Login and world reply framing differs from receive framing | Critical | High | PRO-001, PRO-004, NET-002..003 | Separate reader/writer framing; never apply generic request header to replies |
| R-035 | Fact | Original config contains placeholders, a default password, and insecure admin IP semantics | High | High | INF-002, ADM-001 | Startup validation, secrets provider, no production placeholders |
| R-036 | Assumption | Some behavior is client-derived rather than server-verified because the original server is incomplete | High | High | GP-007..028 | Use actual client tests/captures before changing rules; compatibility profiles |
| R-037 | Fact | Web `/info` exposes QuadTree/state-oriented data | Medium | Medium | INF-012, INF-009 | Shape compatibility optional; restrict/authenticate and redact operational output |
| R-038 | Fact | No formal packet, performance, or load test exists in top-level `npm test` path | High | High | INT-007..009 | New fixture/integration/benchmark suites are release gates |
| R-039 | Fact | Startup data outside tracked source may be incomplete or externally supplied | Critical | High | DAT-001..010, INT-008 | Manifest required files/hashes; fail clearly; do not bundle unavailable proprietary data |
| R-040 | Assumption | Zone client counts and throughput targets are not documented | Medium | High | INT-009, WLD-002, ZON-006 | Establish production target and benchmark 10/100/target concurrency before final budgets |
| R-041 | Fact | Party/trade/duel/guild code has partial validation and disconnect cleanup | High | Medium | GP-027..030 | Transaction/session state machine, timeout and disconnect tests |
| R-042 | Fact | Trade, guild, shop, event opcodes have many unknown fixed fields | High | High | PRO-026..031, GP-028..036 | Preserve fields, packet captures, do not assign semantics without evidence |
| R-043 | Fact | Item transformations and drop rates use undocumented magic values/duplicates | High | High | GP-022, GP-033 | deterministic fixtures, data audit, config/profile differences |
| R-044 | Fact | `Mongoose`/`mongodb` and old Node dependencies are legacy-era and may not run on modern environments | High | Medium | INF-014, INT-014 | .NET migration removes runtime dependency; Node remains fixture/oracle only if reproducible |
| R-045 | Assumption | .NET/C# numeric and allocation behavior will expose timing/ordering differences even when formulas match | Medium | High | WLD-001, GP-004..023, INT-009 | Golden deterministic traces, profile tick and load tests |
| R-046 | Fact | The tracked data includes NPC/MOB spawns but no WREGION map-region assets; missing legacy files are treated as optional | High | High | DAT-008, ZON-001, ZON-003..004, INT-008 | Report missing resources as degraded health, keep portal/safe-region behavior unverified, require real deployment assets before client transfer tests |
| R-047 | Fact | QuadTree query pruning uses a strict circle/cell intersection while candidate inclusion is inclusive and object-size unaware at the pruning stage | Medium | Medium | ZON-002, ZON-006, INT-002 | Preserve the observed boundary in LegacyParity tests; evaluate any corrected broad phase separately with client evidence |

## Highest-priority research and evidence

1. Full login/world/character state capture and legacy client transcript.
2. Real representative BSON account/character/guild dump.
3. Complete `.IMG/.NPC/.MOB/.WREGION` data manifest and checksums.
4. Packet captures for combat, transfer, trade, guild, quest, and compressed state.
5. Declared concurrent-user and tick-budget targets.
6. Confirmation of which FruitSalad editors/processes are considered supported functionality.
