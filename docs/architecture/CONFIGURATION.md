# Configuration

## Providers and precedence

The Generic Host loads:

1. `appsettings.json`;
2. `appsettings.{Environment}.json`;
3. normal process environment variables;
4. command-line values;
5. environment variables prefixed by `INFINITESKY_`.

The latter permits deployment-specific names while standard ASP.NET Core environment variables continue to work. Later providers override earlier values.

## Strongly typed root

All server options bind from `InfiniteSky`.

| Section | Settings |
|---|---|
| `Application` | application name, shutdown timeout |
| `Hosting` | concurrent service start/stop policy |
| `Diagnostics` | logical service source name |
| `Authentication` | maximum login lookup/session-establishment duration |
| `Networking` | bind address, login/world ports, and listener bounds; listeners disabled by default |
| `Database` | provider and secret connection string; disabled in this stage |
| `World` | legacy tick interval |
| `Zones` | future zone IDs |
| `Gameplay` | `LegacyParity` or `Corrected` |
| `Logging` | Microsoft logging levels/categories |

## Defaults

The tracked configuration:

- uses `InfiniteSky` as application/diagnostics name;
- allows 30 seconds for shutdown;
- starts/stops hosted services sequentially;
- limits authentication lookup and session establishment to 15 seconds;
- sets world tick to 100 ms for future compatibility;
- leaves network/database disabled by default;
- loads no zones;
- selects `LegacyParity`.

## Validation

`InfiniteSkyOptionsValidator` runs on startup. It rejects:

- blank application/diagnostic names;
- invalid shutdown/tick values, network address/ports, or connection limits;
- invalid/duplicate zone IDs;
- unsupported gameplay profiles;
- database enabled before a persistence adapter exists.

Host option mapping is tested, including `HostOptions.ShutdownTimeout`, service concurrency, and background-service failure policy.

## Environment examples

```text
INFINITESKY_InfiniteSky__Application__Name=InfiniteSky-Node
INFINITESKY_InfiniteSky__Application__ShutdownTimeoutSeconds=45
INFINITESKY_InfiniteSky__Authentication__TimeoutSeconds=15
INFINITESKY_InfiniteSky__Database__ConnectionString=...
DOTNET_ENVIRONMENT=Production
```

Database values are represented only as configuration placeholders. Do not commit a connection string. Enabling the database section currently fails options validation intentionally.

## Authentication

`InfiniteSky:Authentication:TimeoutSeconds` defaults to 15 seconds and must be between 1 and 300. It bounds account lookup and session establishment only; it is not an authenticated-session expiry setting. No production account repository is configured yet, so the Server reports authentication as unavailable rather than accepting accounts from a test or in-memory store.

## Reload policy

No runtime mutation is enabled. Future reloadable settings must be explicitly marked and registered with `IOptionsMonitor`; ports, protocol mode, data catalogs, lifecycle policy, and zone topology require controlled restart.
