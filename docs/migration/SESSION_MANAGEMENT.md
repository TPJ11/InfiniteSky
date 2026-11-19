# Session Management

## Scope

This stage implements in-process authenticated account sessions for the login connection. Prompt 15 can bind that authenticated session ID to a loaded player runtime internally, but does not implement client character/world packets, login-to-world transfer, reconnect recovery, distributed storage, or a persistent session table.

## Identity and ownership

Each session has a generated `Guid` session ID and is owned by one account and one login connection. The thread-safe session store maintains both account-to-session and connection-to-session indexes. Session data contains only account ID, username, connection ID, and creation time; credentials are never copied into a session.

The application service keeps connection authentication state separately from transport state. A new accepted connection begins unauthenticated. Only a successful credential check and successful account-active update can complete the transition to `Authenticated`.

## Lifecycle

| Event | Session action |
|---|---|
| Authentication begins | Mark the connection `Authenticating`; reject another overlapping request |
| Credentials rejected | Create no session; return to `Unauthenticated` |
| Credentials accepted | Atomically claim the account and connection; set persisted `active = 1`; then publish authenticated connection state |
| Session-store/account update fails | Remove any tentative session and return failure |
| Authentication timeout/cancellation during active update | Remove the tentative session and attempt a bounded `active = 0` rollback |
| Owning connection closes | Remove both indexes and invalidate the in-process session |
| Server process exits | All in-process sessions are lost |

There is no established-session expiry timer. The original login flow exposes no authenticated-session TTL; the configurable authentication timeout applies only while a login request is being processed. No login logout packet is invented. Disconnect is the implemented cleanup event.

Ordinary login disconnect removes the C# in-process session but leaves the persistent legacy `active` field unchanged, matching the observed login `logoutUser` branch, which clears it only when `zoneTransfer` is true ([`account.js`](../../db/account.js#L30)). The world server has a separate account-flag cleanup path ([`world.js`](../../world.js#L470)). The later persistence/world-transfer stages must decide how to reconcile stale flags without changing current client behavior silently.

## Duplicate login and concurrency policy

Per-connection synchronization permits only one authentication operation at a time. The session store uses one atomic critical section for account and connection uniqueness, so two simultaneous successful verifications for one account cannot both create sessions. A duplicate active account returns legacy status 5 and closes the incoming connection after the response is written.

This policy is intentionally stricter than the Node login handler. Node checks `account.active` against a live world socket and lets a stale active flag through; this C# foundation instead treats any in-process session as authoritative. The exact legacy behavior cannot be reproduced until world-session lookup and login-to-world transfer are migrated. The policy is explicitly documented rather than treated as proven parity.

There is no session replacement, recovery, or reconnect token behavior. Session IDs are not sent to clients and are used internally for association/diagnostics.

The Prompt 15 `PlayerActivationService` resolves the session by login connection, loads only a character owned by its account and configured server, then rechecks the same session after database I/O. `PlayerRuntime` retains the generated session ID and world connection ID separately from character/account identity. Because world handshake packet handling is not implemented, this is an application boundary rather than a completed transfer-session protocol.

## Production and operational limitations

The session manager is an in-memory singleton scoped to the current Server process. It is not distributed and cannot coordinate multiple server processes. The production account repository is intentionally unavailable until the persistence stage; therefore production login is not operational even though test repositories exercise session creation and cleanup. The persistent `active` field is only a legacy marker and is not the source of truth for the C# in-process session store.

Tests cover create/lookup/remove, same-account races, same-connection repeated requests, timeout rollback, response-before-close ordering, and disconnect cleanup. See [`AUTHENTICATION_IMPLEMENTATION.md`](./AUTHENTICATION_IMPLEMENTATION.md) for wire flow and credential compatibility.
