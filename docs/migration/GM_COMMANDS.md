# GM and Game-Chat Commands

## Current support

The legacy source has a shared command registry called `GMCommands`, but the registered commands are not uniformly GM-only. Levels range from 0 to 100, and the dispatcher interprets them only as numeric minimum account levels. The registered command list includes gameplay utilities, diagnostics, placeholders, and experimental packet/state mutations.

The .NET server currently supports **no GM or slash command**. The world listener does not dispatch chat packets, account-level authority is absent from `AuthenticatedSession`, and the required world/gameplay services are incomplete. See [COMMAND_SYSTEM_IMPLEMENTATION.md](./COMMAND_SYSTEM_IMPLEMENTATION.md).

## Source behavior requiring careful migration

- Slash interception occurs in normal chat (`0x13`), faction chat (`0x3E`), and whisper (`0x09`). Any message beginning with `/` is consumed as a command, even if the command is unknown.
- `commands` and its `help` alias list only commands whose minimum level is at or below the caller’s account level. This list reflects dynamic registration order, not a stable sorted catalogue.
- The low-level command list is not read-only. Source handlers include XP granting, inventory clearing/filling, state/buff clearing, skill-list clearing, raw packet sends, and equipment changes at account level 0 or 1.
- Numerous command names are partial or stubs. `kick` does not kick; `shutdown` only sends a message; `disguise` reports not implemented; `tonpc` only partially works; `return` has no handler; and removal commands immediately report `Not working...`.
- `/set` is a broad state editor, not a single typed operation. It changes currencies, points, appearance, health/chi, equipment, map, and disguise-related fields; validation and persistence differ by case.
- `commands/test.js` is loaded from the same directory as operational commands. It registers diagnostics and mutations, including duplicate `stats` definitions; its later level-0 registration replaces the earlier level-80 entry.
- `commands/game_gmcommands.js` lists many familiar game-server commands only in comments. Those names are not active commands and must not be added as guessed stubs.

The complete source-backed names, aliases, levels, argument syntax, observed effects, and target statuses are in [ADMIN_COMMAND_COMPATIBILITY_MATRIX.md](./ADMIN_COMMAND_COMPATIBILITY_MATRIX.md).

## No usage claim for the .NET server

The legacy commands shown in the matrix are audit evidence only. Do not use the old README `/createaccount` or `/help` examples as .NET capabilities. The README now identifies the limitation and links to the migration inventory.
