# Client Protocol Captures

## Capture inventory

| Artifact | Size / integrity | Metadata-only observation | Provenance and disposition |
|---|---|---|---|
| [`t.pcap`](../../FruitSalad/Data/packets/t.pcap) | 3,165 bytes; SHA-256 `ba1ee0dfdf3bef681a6d9dda6cbf896d0c7c4778f3e69f3d763a481a6262e8bb` | Classic pcap 2.4, Ethernet/IPv4/TCP, 18 frames between 2015-08-22 01:44:42 and 01:44:48 UTC. The frames comprise one bidirectional flow with server-side port `11092`; no login-port flow is present. | Capture tool, client build, capture operator, account, and server provenance are undocumented. It is not treated as original-client or Node-vs-.NET evidence. |

Only pcap structure, timestamps, lengths, and TCP endpoint ports were inspected. Raw payloads and endpoint addresses are intentionally not reproduced. The file may contain sensitive application data; do not publish it, copy packet bytes from it into fixtures, or use it for credential assertions without authorization and sanitization.

## Capture results

- No client session was captured during this stage.
- No `tshark`/Wireshark executable was available on `PATH`; no new traffic was recorded.
- The existing capture cannot establish a client build, login behavior, authentication success, packet order, encryption, or compatibility.
- The packet capture is not a fixture source until its owner/provenance and data sensitivity are resolved. It remains unchanged in the repository.

## Future capture procedure

When an authorized original client and isolated test setup are available:

1. Record the client build/hash, server commit, OS, configuration, test scenario, and capture time.
2. Capture only the isolated test traffic and use synthetic accounts with non-reusable credentials.
3. Keep the unredacted capture private and access-controlled. Do not commit credentials, session tokens, public endpoint details, or private account data.
4. Create only a redacted derived fixture after verifying exact packet bytes and provenance; record which bytes/fields were redacted and why.
5. Compare the client request and server response against the original Node behavior where the reference server can safely run. Do not normalize unknown or differing bytes.
6. Store fixture descriptions and expected state transitions alongside deterministic test data; never mark real-client compatibility from a simulated socket alone.

No capture software, driver, privileged access, or client instrumentation was installed or run for this stage.
