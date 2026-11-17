# Administration Migration

## Scope and matrix coverage

`ADM-001` through `ADM-013`: command registry, authorization, information/debug commands, movement, spawning, character/inventory mutation, moderation, CLI, and diagnostics.

## Source files

`GMCommand.js`, every `commands/*.js`, `cli.js`, every `cli/*.js`, `commands/game_gmcommands.js`, FruitSalad commands as migration evidence.

## Target components

`InfiniteSky.Application/AdminCommandDispatcher`, typed command classes, `InfiniteSky.World` admin state commands, and restricted server diagnostic CLI.

## Dependencies

Depends on feature services in the command: world registry, player state, data catalog, inventory, persistence, zones, scripting. Authorization precedes all command porting.

## Implementation order

1. Registry, authorization, aliases, auditing.
2. Help/information commands.
3. save/state and operator-safe diagnostics.
4. character progression/combat commands.
5. movement and target commands.
6. spawn/remove commands.
7. inventory/economy commands.
8. moderation/shutdown.
9. account/mail/import.
10. restricted REPL/CLI.

## Tests

- account level denial and exact allow threshold;
- aliases and invalid command;
- argument parsing;
- every state mutation;
- target by name/ID/zone;
- persistence acknowledgement;
- audit correlation;
- error/redaction;
- no arbitrary OS/JS execution by default.

## Acceptance criteria

- Every tracked active command is either a C# command, scripted through the capability boundary, or an approved `NOT_APPLICABLE` record.
- Account level is checked before any lookup/mutation.
- Destructive operations are audited.
- Operator command completion reports asynchronous save/shutdown state.
- Test/debug commands cannot expose credentials or arbitrary process access in production.

## Risks

Many command files allow low permissions, duplicate names, incomplete scripts, shell execution, and raw VM eval. Preserving command names is important, but preserving insecure arbitrary execution is not.
