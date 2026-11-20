# Protocol

## Source of truth and supported subset

The wire protocol is defined by the original implementation and documented in [PROTOCOL_SPECIFICATION.md](protocol/PROTOCOL_SPECIFICATION.md), [PACKET_CATALOGUE.md](protocol/PACKET_CATALOGUE.md), [PACKET_FLOWS.md](protocol/PACKET_FLOWS.md), and [CONNECTION_LIFECYCLE.md](protocol/CONNECTION_LIFECYCLE.md). Those files distinguish observed source layouts from unknown or ambiguous behavior.

The current .NET server uses raw TCP for separate login and world listeners (default ports `11091` and `11092`, disabled by default). Incoming fixed-size frames use the legacy 9-byte receive header with opcode at offset 8 and little-endian fields as documented by the source. Server responses are generally direct packet buffers, not generically prefixed with that receive header.

The production C# incoming packet sink handles login opcode `0x03`; the login connection greeting is source-derived. Binary primitives and selected structures have exact fixture tests. No gameplay packet handler is wired to the world listener.

## Compatibility limits

Character-slot serialization, world transfer/authentication, full character/world state tails, compressed-state formats, and most movement/combat/economy/social request/response families are missing or unresolved. The C# world handshake request structure/test does not imply a functioning world handshake. No original client build was available, so protocol compatibility with a specific client is unverified.

Do not treat pcap files with unknown provenance, comments, experimental code, or disabled handlers as authoritative packet behavior. Do not invent error packets or fill unknown tails. See [GAMEPLAY_PROTOCOL_COMPATIBILITY.md](migration/GAMEPLAY_PROTOCOL_COMPATIBILITY.md) and [KNOWN_ISSUES.md](KNOWN_ISSUES.md).
