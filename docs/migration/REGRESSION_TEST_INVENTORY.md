# Prompt 22 Regression Test Inventory

## Evidence levels

The evidence labels describe the strongest evidence present in a test group; they do not replace migration statuses.

| Level | Meaning |
|---|---|
| A | Real original-client behavior compared with the migrated server. |
| B | Differential execution against the original Node.js server. |
| C | Source-derived binary/BSON/data fixture with an exact asserted result. |
| D | Source-derived unit tests of rules, field mappings, or state transitions. |
| E | Implementation-only tests without independent original behavior evidence. |

No Level A or Level B test ran. Source inspection and simulated sockets are not Level A/B evidence.

## Project inventory

| Test project | Scope | Baseline result |
|---|---|---:|
| `InfiniteSky.Domain.Tests` | Domain models, identifiers, value semantics and isolated progression arithmetic | 50 passed |
| `InfiniteSky.Protocol.Tests` | Binary primitives, framing descriptors and fixed structures | 24 passed |
| `InfiniteSky.GameData.Tests` | Legacy zone resource decoding and catalog validation | 5 passed |
| `InfiniteSky.World.Tests` | World clock/runtime, zone registry/spatial index and internal player lifecycle | 22 passed |
| `InfiniteSky.Persistence.Tests` | BSON mappers and Mongo persistence configuration; two opt-in container tests | 23 passed, 2 skipped |
| `InfiniteSky.Server.IntegrationTests` | Configuration, host lifecycle, authentication, networking and player activation | 62 passed |

After the Prompt 22 handshake test was added, the final suite reported 187 passed, 0 failed and the same 2 skipped; `InfiniteSky.Protocol.Tests` then reported 25 passed. The baseline counts above intentionally preserve the pre-change result.

The inventory below uses test classes as test groups. A group can contain several xUnit cases/theory rows; the per-project totals above are the executed case totals.

## Test-group inventory

| Test identifier | Subsystem / level | Behavior covered | Source or fixture provenance | Dependencies, result and limitations |
|---|---|---|---|---|
| `Domain/CharacterModelTests` | Domain; unit; D | Character defaults, sparse ordered collections, nullable fields, equipment variants and guild member roles | Original account/character/guild definitions and structures; inline synthetic values | Passed. Model semantics only; no full legacy BSON round-trip. |
| `Domain/DefinitionTests` | Domain; unit; C/D | Source enum values, record fields, collection sizes and fractional definition values | Source constants and inline source-derived records | Passed. No original `.IMG` catalog is loaded. |
| `Domain/EntityModelTests` | Domain; unit; D | Monster/NPC/world-item defaults, target sentinels and reference identity | Original entity/spawn semantics; inline values | Passed. Runtime entities are not connected to client packet flows. |
| `Domain/IdentifierTests` | Domain; unit; D | Typed identity, unsigned values, negative counter rejection and zone-zero handling | Source-backed identifier semantics; inline values | Passed. |
| `Domain/LegacyExperienceAwarderTests` | Gameplay arithmetic; unit; D | Award application, level thresholds, point bands, missing records and level cap | Synthetic progression records exercising the source-derived arithmetic | Passed. Not connected to original game data, handlers, persistence or packets. |
| `Domain/LegacyStatPointAllocatorTests` | Gameplay arithmetic; unit; D | Allocation key mapping, requested-value overrides, insufficient points and invalid keys | Source-derived allocation rules; inline values | Passed. Reset/recalculation/packet behavior is absent. |
| `Domain/ValueObjectTests` | Domain; unit; D | Position, direction, rotation and grid numeric/equality semantics | Inline numeric boundary values based on source types | Passed. |
| `Protocol/BinaryPrimitiveTests` | Protocol; unit/protocol compatibility; C/D | Little-endian primitives, float handling, fixed-string padding/NUL/encoding, and reader offsets | Source-derived expected bytes and values | Passed. No full packet/session transcript. |
| `Protocol/FrameAndStructureTests` | Protocol; unit/protocol compatibility; C/D | Fixed structures, packet sizes, login and greeting bytes, strings, envelope fields and world handshake | Consumed: `login-greeting.hex.txt`, `login-request.hex.txt`, login reply fixtures, and `world-handshake.hex.txt`; the latter derives from `packets/world/handshake.js` | Final 25 passed. Other checked-in fixture files are not loaded by current tests. No compressed-state fixture or complete packet registry. |
| `GameData/LegacyZoneResourceLoaderTests` | Game data; component; C/D | Count-prefixed NPC and signed region record parsing, optional/malformed files, catalog IDs | Synthetic `.NPC`/`.WREGION` records and tracked `data/spawninfo/001.NPC` | Passed. No `.IMG` catalogs or tracked `.WREGION` corpus. |
| `Persistence/LegacyAccountAuthenticationDocumentMapperTests` | Persistence compatibility; component; C/D | Account BSON field mapping, optional/null values, numeric BSON types, malformed values and error classification | `tests/InfiniteSky.Persistence.Tests/Fixtures/legacy-account-document.json` is synthetic; additional BSON values are inline | Passed. Does not connect to MongoDB or represent a production export. |
| `Persistence/MongoAccountAuthenticationRepositoryIntegrationTests` | Persistence integration; C; failure/cancellation | Repository query/update, index, active reset, guild read and cancellation against Testcontainers MongoDB | Inline synthetic BSON inserted into an isolated container | **Skipped** by the opt-in gate. No DB result is claimed. |
| `Persistence/MongoCharacterDocumentMapperTests` | Persistence compatibility; component; C/D | Supported character fields, optional values and malformed identity/inventory fields | Inline synthetic BSON | Passed. Unknown-field preservation is tested only for targeted repository updates, not full document round-trip. |
| `Persistence/MongoCharacterRepositoryIntegrationTests` | Persistence integration; C; failure recovery | Ownership filters, location update and unrelated-field preservation against Testcontainers MongoDB | Inline synthetic BSON inserted into an isolated container | **Skipped** by the opt-in gate. No DB result is claimed. |
| `Persistence/MongoGuildDocumentMapperTests` | Persistence compatibility; component; C/D | Legacy guild fields, null member slots, defaults and malformed roles | Inline synthetic BSON | Passed. No live guild collection read/write test ran. |
| `Persistence/MongoPersistenceContextTests` | Persistence; component/failure; E | Disabled context behavior and database-name/connection-string validation | Inline configuration | Passed. Does not prove live Mongo connectivity or recovery. |
| `World/WorldClockTests` | World engine; component; D/E | Sequential tick dispatch, cancellation and unavailable dependency readiness | Controlled in-process tick handler; system time and short delays | Passed. Uses real timing rather than a fake time provider; repeat runs found no failure, but this remains timing-sensitive. |
| `World/WorldRuntimeTests` | World engine; unit; D/E | Lifecycle transitions, readiness, guild lookup and tick accounting/configuration | Inline runtime state | Passed. One root world only; no world network entry. |
| `World/ZoneManagementTests` | Zones/spatial/players; component; C/D; concurrency/failure recovery | Spatial bounds/type filtering, lifecycle, cancellation, duplicate registration, player activation, rollback and concurrent access | Inline zone descriptors and empty/failing resource loaders | Passed. Internal candidate/query behavior only; not client visibility or multi-zone gameplay. |
| `Server/ApplicationLifecycleTests` | Infrastructure; unit; E | Lifecycle transitions, observer order and readiness failure | In-process observers | Passed. Target lifecycle only. |
| `Server/AuthenticationTests` | Authentication/sessions; component/socket; D/E; concurrency/failure recovery | Credential outcomes, duplicate login races, timeout/cancellation rollback, repository failures, disconnect cleanup and redaction | Deterministic in-memory repositories and direct loopback TCP; source-derived login request/reply layouts | Passed. Not backed by live MongoDB or an original client. |
| `Server/BackgroundServiceTests` | Infrastructure; component; E; failure recovery | Cancellation and background exception lifecycle behavior | Controlled in-process hosted services | Passed. Does not establish leak freedom for all production workers. |
| `Server/ConfigurationTests` | Infrastructure; unit; E | Options binding, environment override, required values and invalid limit rejection | Inline configuration | Passed. |
| `Server/DependencyInjectionTests` | Infrastructure; component; E | Service lifetime/registration and shutdown ordering | Real service collection, no external database | Passed. Registration is not runtime compatibility evidence. |
| `Server/DiagnosticsTests` | Diagnostics; component; E | Activity creation and disposal | In-process diagnostics | Passed. Not performance or observability acceptance evidence. |
| `Server/FailureHandlingTests` | Infrastructure; component; E; failure recovery | Host observer failure, readiness failure and fatal configuration exit code | In-process host; intentional failure | Passed. Does not cover partial Mongo failure after startup or active gameplay save failures. |
| `Server/FoundationHostTests` | Infrastructure/world; host component; E | Host startup/stop, service order, zone readiness, health, configuration rejection and unreachable Mongo startup | Actual Generic Host; controlled zone loader; deliberately unreachable local DB endpoint case | Passed. No successful live Mongo initialization. |
| `Server/NetworkAuthenticationHostIntegrationTests` | Authentication/networking; socket-level integration; C/D/E; failure recovery | Real host and loopback login greeting/request/reply, duplicate, reconnect, shutdown cleanup and listener-start rollback | Login golden fixture and deterministic test account repository | Passed. Persistence is substituted; no character/world journey or original-client observation. |
| `Server/NetworkingTests` | Networking; component/socket-level; C/D/E; concurrency | Partial/coalesced/multiple frames, unknown opcode, bounds, loopback send/accept, connection limit and disconnect cleanup | Source-derived framing rules and inline frames | Passed. Queue saturation, idle-timeout expiration and client interoperability are not tested. |
| `Server/PlayerActivationServiceTests` | Player lifecycle/persistence boundary; component; D/E; failure recovery | Ownership, session validity, saved location, activation and disconnect save | In-memory session/repository and zone loader | Passed. Does not write MongoDB or process a world packet. |

## Test categories present or absent

| Category | Inventory finding |
|---|---|
| Unit | Present in Domain, Protocol, World and configuration/mapper tests. |
| Component | Present for host lifecycle, auth, world/zone, game data and BSON mapping. |
| Integration | Real host/socket login integration is present; it substitutes persistence. Two separate real-Mongo Testcontainers integrations are present but skipped. |
| Socket-level end-to-end | Present for the bounded login path over loopback TCP only. It is not a complete login-to-world workflow. |
| Persistence compatibility | Synthetic BSON mapping is exercised; live Mongo account/character repositories are skipped. |
| Protocol compatibility | Source-derived golden binary fixtures are present for a limited packet/structure subset. |
| Differential | No executable Node-vs-.NET test exists or ran. |
| Original-client | No client executable/build was available; no original-client test ran. |
| Concurrency | Present for account login races, spatial/zone registry operations, duplicate player activation and concurrent socket sends. |
| Failure recovery | Present for selected lifecycle, authentication, listener startup and zone initialization failures. |
| Property-based | No property-based testing library or generated invariant suite is configured. |
| Performance smoke | No performance/benchmark project or measured performance baseline exists. |
