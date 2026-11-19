# Authentication Implementation

## Scope and status

This stage ports the login authentication boundary and its in-process authenticated-session state. It does not implement character listing/selection, world entry, world transfer, or production account persistence. The production Server composition deliberately uses `UnavailableAccountAuthenticationRepository`; the host cannot authenticate real accounts until the persistence stage supplies an account repository.

## Original authentication flow

1. The login TCP connection begins unauthenticated. The active login packet registration is opcode `0x03` with a 91-byte payload: 14 bytes of username, 14 bytes of password, then 63 padding bytes. The field lengths come from `LoginPC.UsernameLength` and `PasswordLength` in [`handleLogin.js`](../../packets/login/handleLogin.js#L23), and the actual `LoginPacket` includes the 63-byte tail ([`handleLogin.js`](../../packets/login/handleLogin.js#L48)).
2. The handler looks up `Account.Username` exactly. A database error sends status 1 and closes the socket; an unknown account sends status 2 and closes it ([`handleLogin.js`](../../packets/login/handleLogin.js#L282)).
3. If `active` is set, the handler checks for a matching world socket. An active flag without a live world socket is not sufficient to reject the login. For accounts above level 80, the handler first clears `active` ([`handleLogin.js`](../../packets/login/handleLogin.js#L295)). A matching world socket receives an informational message; the new connection gets status 5 and closes ([`handleLogin.js`](../../packets/login/handleLogin.js#L297)).
4. The active handler compares the stored `Password` directly with the supplied password. The ban checks are commented out. A wrong password sends status 3 without explicitly closing the socket; success marks the socket authenticated, sets `active = 1`, stores the account on the socket, sends status 0, and starts character-slot responses ([`handleLogin.js`](../../packets/login/handleLogin.js#L316)).
5. Login replies are 41 bytes: opcode `0x03` at offset 0, status at offset 1, zero-filled remainder, success username at offset 14, and `****` at offset 36. Success is followed by up to three `0x05` character-slot packets; these are not part of this stage ([`handleLogin.js`](../../packets/login/handleLogin.js#L238)).

The status table in the source also names suspended, payment, version, maintenance, server-full, and unknown-error values. The active `0x03` account handler does not use those statuses. The older `db.Account.loginUser` helper contains a server-admin password bypass, but the active packet handler does not call it ([`db/account.js`](../../db/account.js#L52), [`handleLogin.js`](../../packets/login/handleLogin.js#L187)).

## Account data required

The original account schema defines numeric `_id`, unique `Username`, plaintext `Password`, numeric `active` and `Level`, and additional email, PIN, last-IP, last-login, character-individual, and AP fields ([`account.js`](../../db/account.js#L12)). The C# authentication record retains only the account identifier, username, and transient password bytes. The persistence boundary also exposes the `active` update needed by the observed login path.

The packet handler does not apply the commented ban checks; the PIN fields are not used in the active login packet flow. The `Level > 80` stale-active exception is observed but not reproduced in this stage because the minimal account repository contract does not yet expose level. This, and the lack of the original live-world-socket check, is a documented compatibility gap.

## Credential rule and security

The active legacy handler uses direct string equality. It does not hash, salt, encrypt, normalize, or challenge-response transform the password. The schema stores the password as a string ([`account.js`](../../db/account.js#L14)); the direct comparison is in [`handleLogin.js`](../../packets/login/handleLogin.js#L322). This is insecure plaintext credential storage. No new password format is introduced because that would break existing accounts and requires a separately approved migration.

The C# verifier compares UTF-8 bytes with `CryptographicOperations.FixedTimeEquals` while preserving exact string content. It does not trim, normalize, or change the legacy password rule. The account repository transfers ownership of a transient password byte array to the application service, which clears it after verification. The login packet payload is also cleared after processing. .NET strings are immutable, so the decoded request strings cannot be reliably zeroed; they are not stored in sessions or logged. Authentication logs include outcome and connection/session IDs only, never username, password, password bytes, or stored credential data.

Unknown-account and wrong-password responses remain distinct because the original protocol defines distinct statuses. This preserves compatibility but allows account enumeration.

## C# authentication flow and state

`AuthenticationIncomingPacketSink` accepts login-endpoint opcode `0x03`, decodes the request through the protocol structures, invokes `IAuthenticationService`, encodes a `LegacyLoginReply`, and sends it through `NetworkConnection`. The transport now records whether a connection belongs to the login or world listener, so login authentication does not claim world traffic.

The application service has per-connection states `Unauthenticated`, `Authenticating`, `Authenticated`, and `Closed`. Per-connection locking rejects overlapping requests. A successful credential check creates an in-process session before setting the account active; if the persistence update fails, the session is removed. Cancellation, timeout, and disconnect during active-state persistence attempt a bounded rollback and never return an authenticated result. The application service depends on a connection identifier, not TCP types.

The in-process session store atomically enforces one active account session across login connections. This is stricter than the legacy rule: legacy checks active accounts against live world sockets and permits stale active flags. The stricter policy prevents duplicate C# sessions in the absence of the world-session/transfer system; it is not claimed as exact legacy parity.

## Response mapping

| Application outcome | Legacy status | Connection behavior |
|---|---:|---|
| Authenticated | 0 | Keep open; no character-slot packets are emitted yet |
| Account not found | 2 | Send reply, then close |
| Incorrect password | 3 | Send reply and keep open |
| Duplicate account session / already logged in | 5 | Send reply, then close |
| In-progress duplicate request | 1 | Send reply, keep open |
| Invalid input, repository/session failure, or timeout | 1 | Send reply, then close |
| Connection already closed | — | No response |

The source contains other reply status constants, but this implementation does not invent semantics for unused values. The 41-byte response fixtures are derived from the Node layout and tested. Successful client progression remains incomplete until character-slot packets are implemented.

## Session lifecycle

The session store uses a generated `Guid` session ID and indexes each session by account and connection. A session contains account ID, username, connection ID, and creation time; it never contains credentials. There is no session TTL because no authenticated login-session expiration rule was found in the original handler. The configured timeout limits only an authentication operation, not an established session.

Connection closure removes the in-memory session. It does not clear the persistent account `active` flag on ordinary login disconnect, matching the login server's `logoutUser` behavior: that helper resets the flag only during zone transfer ([`account.js`](../../db/account.js#L30)). The world server has a separate logout path that resets the flag when not transferring zones ([`world.js`](../../world.js#L470)). There is no explicit login logout opcode identified for this stage; disconnect is the available cleanup signal.

## Configuration, logs, and metrics

`InfiniteSky:Authentication:TimeoutSeconds` defaults to 15 and is validated from 1 through 300 seconds. It covers lookup and session establishment; it does not add a client-visible protocol step. No rate limiter or sequential bad-password limit was identified in the original server or added here. The fixed packet size, legacy 14-byte field limits, one in-flight request per connection, network idle timeout, and authentication timeout are the current bounds.

Structured events cover request receipt, outcome, and repository unavailability. Metrics cover attempts, success, rejection, repository/session-store failures, duplicate logins, and active in-process sessions. The default repository logs a startup warning and fails explicitly; it is not an in-memory production account store.

## Verification

Automated coverage includes exact response fixtures, credential match/mismatch and Latin-1 text, invalid fields, unknown accounts, repository/session-store errors, cancellation and timeout rollback, concurrent same-account login, repeated requests on one connection, response delivery before duplicate-login close, and session cleanup on disconnect. The tests use deterministic test repositories and do not require MongoDB.

The feature is verified only for this bounded in-process flow and its tested wire responses. Production authentication is blocked until a real account repository is implemented and validated against representative legacy account data. Character listing and full client login progression are not verified.

## Migration dependencies and risks

- Production account lookup and `active` persistence are blocked on the later persistence stage.
- Legacy plaintext passwords are a severe at-rest and breach risk; changing the format requires an approved compatibility/migration plan.
- Duplicate policy differs from the original because world-socket lookup and the level-based stale-flag exception are not present.
- Login disconnect can leave the persistent `active` flag stale, as in the observed login path.
- The client success sequence is incomplete without character-slot packets.
- Account enumeration remains possible to preserve the established status behavior.
