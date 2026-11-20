# Integration Test Harness

## Current harness

The reusable host/socket harness is in `tests/InfiniteSky.Server.IntegrationTests`:

- [`NetworkAuthenticationHostIntegrationTests.cs`](../../tests/InfiniteSky.Server.IntegrationTests/NetworkAuthenticationHostIntegrationTests.cs) builds the actual `ServerHostBuilder` host, enables both loopback TCP listeners on dynamically selected ports, replaces only the account repository with a deterministic test implementation, and sends the original fixed-layout login request over `TcpClient`.
- The host test first reads the exact legacy 21-byte login greeting, then reads the 41-byte login reply and asserts session/connection state, duplicate-account rejection, disconnect cleanup, successful reconnect, and cleanup when the host shuts down with a live login connection.
- A second host-level test occupies the world port before startup, observes the bind failure, and confirms that startup rollback released the login port.
- [`NetworkingTests.cs`](../../tests/InfiniteSky.Server.IntegrationTests/NetworkingTests.cs) separately covers partial/coalesced frames, unknown opcodes, buffer bounds, listener send behavior, connection limits, and remote disconnect using real loopback TCP.
- [`AuthenticationTests.cs`](../../tests/InfiniteSky.Server.IntegrationTests/AuthenticationTests.cs) covers the packet sink/auth service over a directly constructed real TCP listener, including legacy responses, duplicate login, and session cleanup.
- [`FoundationHostTests.cs`](../../tests/InfiniteSky.Server.IntegrationTests/FoundationHostTests.cs) covers Generic Host lifecycle, configuration validation, service order, zone/world startup, health and Mongo startup failure.

## Run

From the repository root in PowerShell:

```powershell
dotnet test tests\InfiniteSky.Server.IntegrationTests\InfiniteSky.Server.IntegrationTests.csproj --no-restore
dotnet test tests\InfiniteSky.Server.IntegrationTests\InfiniteSky.Server.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~NetworkAuthenticationHostIntegrationTests
```

No MongoDB or game assets are required for these host/login tests. The Mongo integration tests are opt-in and use Testcontainers; run them only with Docker available and the repository's explicit opt-in setting. They were skipped in the Prompt 20 validation.

## What this harness proves

The host login scenario exercises actual Generic Host composition, DI, the production `NetworkHostedService`, both listener startup paths, real TCP accept/read/write, the receive accumulator, source-derived 21-byte greeting, login packet decoding/encoding, `AuthenticationIncomingPacketSink`, `AuthenticationService`, session registration, duplicate policy, disconnect handling, and host shutdown. The fixed-size 91-byte login request and 41-byte response are encoded using the production protocol structures.

Only the persistence boundary is substituted with a deterministic test repository. Therefore the scenario verifies the host/network/auth/session composition **with test persistence**; it does not verify Mongo query/projection behavior, account indexes, legacy credentials in a real collection, character persistence, or a full client login-to-world journey. Other lifecycle, zone, persistence mapper and gameplay arithmetic tests remain component-scoped.

## Extension contract

Before adding another end-to-end scenario:

1. Use source-backed packet definitions and deterministic fixtures; do not make up opcodes or success responses.
2. Start the actual host and use loopback sockets for transport behavior.
3. Replace only unavailable external dependencies and name each substitution in the scenario.
4. Use isolated data/database resources; never point tests at production data.
5. Assert actual state transitions and cleanup, not only a response byte.
6. Keep blocked character/world/gameplay scenarios blocked until their source data, protocol and application handlers exist.

The harness does not execute Node.js, use an original game client, claim load/scalability results, or provide a general client emulator.
