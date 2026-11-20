# Moderation Implementation Status

## Legacy source findings

No complete root moderation workflow was identified.

| Capability | Root source behavior | .NET status |
|---|---|---|
| Kick | `/kick` requires account level 80 but only replies `Kick here.`; it does not locate or disconnect a target. | **BLOCKED**; no kick behavior exists to port. |
| Ban/IP ban | Login handlers contain comments for account and remote-address bans. No active ban query/enforcement or ban schema was found. | **NOT_APPLICABLE** to current source behavior; do not invent ban persistence or policy. |
| Mute/chat restriction | Character schema has a comment about possibly checking whether a character can talk; no active mute field/check/command was found. | **NOT_APPLICABLE** to current source behavior. |
| Account unlock | `/unlockaccount <username>` or `/unlockaccount __ALL__` sets account `active` to 0 for matching active account records. This is a login/session marker reset, not a ban reversal. | **BLOCKED**; persistence and concurrent-session coordination are absent. |
| Shutdown | `/shutdown` sends `Shutting down server`; the listener stop, drain, save, and exit sequence in the handler is commented. | **BLOCKED**; no source-backed remote shutdown operation. |
| Reports | `/report` logs text and responds with fixed report ID 3; no durable report store or staff delivery is implemented. | **BLOCKED**; no report data model or workflow. |
| Moderation audit | No durable moderation/action record was identified. | **NOT_APPLICABLE** as legacy parity; any new audit must be documented as a separate improvement. |

The login account model does check `active` for existing sessions, but the root itself calls stale active flags an unfinished lookup and comments out ban checks. `unlockaccount` must not be migrated as a way to terminate a connected player: it does not disconnect the live world socket.

Sources: [kick.js](../../commands/kick.js), [shutdown.js](../../commands/shutdown.js), [unlockaccount.js](../../commands/unlockaccount.js), [report.js](../../commands/report.js), [account.js](../../db/account.js#L68-L122), [handleLogin.js](../../packets/login/handleLogin.js#L286-L323), [character.js](../../db/character.js#L145-L151).

## Migration boundary

No new moderation rule, restriction type, database collection, expiration semantics, or enforcement point is introduced in Prompt 18. If a future command is source-verified but its legacy enforcement is missing, the matrix must keep that limitation visible rather than making a database-only change appear to be a complete ban/mute implementation.
