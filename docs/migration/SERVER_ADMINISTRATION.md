# Server Administration and Operator Commands

## Legacy local CLI

The root Node entry point constructs `cli.js`, which starts a local REPL. CLI commands are separate from in-game `GMCommands` and have no account-level authorization: access is tied to possession of the server process’s console.

| Entry | Source behavior |
|---|---|
| `/help [name]` | Lists registered CLI functions or a command’s help callback. |
| `/reload config\|item\|npc\|monster\|skill\|all` | Calls legacy config/data reload functions. |
| `/createaccount <username> <password> [level]` | Creates an account; optional level must be an integer from 0 through 100. This is the CLI command, not the in-game `/makeaccount`. |
| `/clearguestaccounts` | Removes character records for accounts with usernames matching `Guest`; it does not delete the account records. |
| `/makecsv` | Exports item data to the translation CSV path; other exports in the source are commented. |
| `/eval [javascript]` | Executes JavaScript with `eval`. |
| `#<shell text>` | Runs a shell command using `child_process.exec`. |
| other REPL input | Evaluated using `vm.runInThisContext`. |

`cli/test.js` is commented out. `cli/custom.js` exports a hot-reload prototype and does not define a CLI command. Source links and compatibility statuses are in the [command matrix](./ADMIN_COMMAND_COMPATIBILITY_MATRIX.md#root-local-console-and-operational-commands).

## Legacy configuration is not an admin API

The distributed `config.json-dist` contains `systemAdminIP` and a placeholder `systemAdminPassword`, but the corresponding socket IP check in `db/account.js` is commented. No authenticated remote administration endpoint was identified. The separate web information server is not a command/management API.

## .NET host status

The .NET Generic Host has lifecycle services, login/world TCP listeners, diagnostics, and graceful host shutdown, but it does not read operator command input or register an admin CLI. The world listener’s packet sink does not dispatch world chat. No account creation, data reload, CSV export, or guest cleanup command is implemented.

Arbitrary JavaScript and shell evaluation are intentionally not ported as default .NET behavior. This is an explicit compatibility/security restriction; it does not imply a replacement operator shell exists.

## README compatibility

The older README example that uses `/createaccount` describes the Node REPL, not the .NET server. It has been replaced with a notice that the .NET account/GM/operator command paths are currently unavailable and with links to the source audit.
