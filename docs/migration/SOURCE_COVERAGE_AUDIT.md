# Source Coverage Audit

## Audit objective

This audit reconciles the tracked repository's functional source roots with `docs/FEATURE_PARITY_MATRIX.md`. It distinguishes server/runtime behavior, data, operator tooling, experimental runtimes, research assets, documentation, and vendored code.

## Coverage summary

| Source area | Classified scope | Matrix coverage |
|---|---|---|
| Root runtime `.js` | Host, login/world, zone, database, game data, protocol, CLI, web, utility, quadtree, game loop | `INF`, `NET`, `PRO`, `PER`, `DAT`, `WLD`, `ZON`, `ENT`, `ADM` |
| `packets/login/` | Login packet codecs and handlers | `PRO-005..012`, `GP-001..003` |
| `packets/world/` | World protocol and gameplay handlers | `PRO-013..031`, `GP-007..038` |
| `generic/` | Shared binary structures, static data, stats, entities, combat, AI | `PRO-003..004`, `PRO-015`, `DAT-002..010`, `DOM-003..014`, `GP-006..023` |
| `db/` | Mongo schemas and repositories | `PER-002..008`, `DOM-001..006` |
| `commands/` | GM command functionality | `GP-032`, `ADM-001..011` |
| `cli/` and `cli.js` | Operator REPL, commands, shell/JS eval | `ADM-012..013`, `INF-013` |
| `vmscript/` | VM load/watch/reload/dependencies and CLI globals | `SCR-001..008` |
| `restruct/` | Binary primitive library, tests, docs, vendor helper code | `PRO-003..004`; implementation uses tested primitive semantics rather than a vendored port |
| `data/packets/` | Experimental/diagnostic packet captures | `PRO-032..033`, `INT-007` |
| `data/spawninfo/` | NPC/MOB spawn corpus | `DAT-007`, `ZON-005`, `GP-023` |
| `data/world/` | Region corpus | `DAT-008`, `ZON-003..004` |
| `data/translation/` | CSV overlay corpus | `DAT-009` |
| `FruitSalad/Processes/` | Alternate login/world/zone, editors, logger, command and event prototypes | `INF-014..015`, `SCR-003/006`, `INT-011..012`; unresolved behavior remains P2/P3 |
| `FruitSalad/Modules/` | RPC, database/cache, data, packet, nav, restruct, utilities | `SCR-001/003`, `DAT-011`, `PRO`, `INT-016` |
| `FruitSalad/Grunt/` and `Gruntfile.js` | Imports, imports-to-Mongo, CSV, translation, nav generation, documentation | `DAT-011`, `INT-016` |
| `FruitSalad/Generic/`, `Commands/`, `Scripts/` | Alternate domain/data/command/event experiments | `DOM`, `GP`, `ADM`, `SCR-006` |
| `FruitSalad/Documentation/` and public vendor assets | Generated docs and browser dependencies; not runtime server parity | Research/support only; no matrix runtime ID required |
| `TSX_Client/` | Legacy client/reversing code and third-party binary tooling | `INT-013`; research dependency, never server runtime |
| `plugins/` references | Optional Dropbox/GitHub/Google Drive behavior claimed in README; source absent/incomplete | `SCR-004`, `SCR-007`; formal blocker/exclusion required |
| `README.md`, setup docs, batch/package manifests | Deployment and operator workflow | `INF-013`, `INF-016`, `INT-014..015` |
| `LICENSE`, `.gitignore`, editor/linter metadata | Repository policy/tool metadata | No runtime feature; governance/deployment context only |

## Root-file reconciliation

| Root file | Classification |
|---|---|
| `main.js` | `INF-001/002/003/004/005/007/008/009`, `SCR-004` |
| `server.js` | `INF-018`; original exports an unused/empty abstraction |
| `login.js` | `INF-003`, `NET-001/005/009/010`, login application flows |
| `world.js` | `INF-004/007`, `WLD-001..010`, `ENT-001..008`, gameplay dependencies |
| `zone.js` | `ZON-001..008`, entity and spawn behaviors |
| `QuadTree.js` | `ZON-002/006`, `ENT-007..008` |
| `CachedBuffer.js` | `NET-002`, `PRO-001` |
| `PacketCollection.js` | `PRO-002`, `SCR-002` |
| `protoload.js` | `INF-017`; unfinished prototype registry |
| `GameInfoLoader.js` | `DAT-001/009` |
| `GameStep.js` | `INF-007`, `WLD-001` |
| `GMCommand.js` | `ADM-001/002`, `SCR-005` |
| `cli.js` and `cli/` | `ADM-012..013` |
| `db.js` and `db/` | `PER-001..012` |
| `util.js` | `INF-002/008/009`, `NET-006` |
| `sandbox.js` | `SCR-008` |
| `webserver.js` | `INF-012` |
| `package.json`, `cmd.bat`, `README.md` | `INF-013/016`, `INT-014/015` |

## Relevant directories audit

The following search roots were explicitly reviewed for feature-bearing source:

```text
commands/
cli/
db/
generic/
packets/login/
packets/world/
vmscript/
restruct/
data/
FruitSalad/Processes/
FruitSalad/Modules/
FruitSalad/Generic/
FruitSalad/Commands/
FruitSalad/Grunt/
TSX_Client/
docs/
```

No directory is presumed production merely because it is tracked. FruitSalad and TSX assets require explicit support decisions.

## Unmapped artifact policy

The matrix may omit a tracked file only when it is:

1. generated/vendor content;
2. raw research evidence represented by a corpus feature;
3. license/editor/build metadata without runtime behavior;
4. a duplicate generated copy of a mapped implementation; or
5. an approved `NOT_APPLICABLE` deviation.

Any new hand-written source file discovered after this audit must either receive a feature ID or an approved exclusion before a migration milestone can claim complete source coverage.

## Acceptance check for future updates

At every milestone:

1. list tracked first-party JavaScript/TypeScript/source files;
2. compare each against this audit and the parity matrix;
3. record newly discovered file/class/function behavior;
4. reject duplicate ID and missing acceptance criteria;
5. recount status totals and update `MIGRATION_STATUS.md`.
