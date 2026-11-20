# Reliability Test Results

## Baseline

At Prompt 24 baseline commit `4159f887517fbb34145adfa92651b6d07a24c733`, the full solution restored and built with 0 warnings/errors. The complete test suite passed 188 tests with 0 failures; 2 MongoDB Testcontainers tests were skipped because the opt-in was not set. Exact commands and environment are in [SECURITY_HARDENING_BASELINE.md](./SECURITY_HARDENING_BASELINE.md).

## Existing reliability coverage

- Loopback network tests cover fragmentation/coalescing, invalid/truncated frames, receive/frame bounds, accepted-connection limits, remote disconnect cleanup, ordered single-reader frame dispatch, and listener shutdown.
- Authentication tests cover concurrent same-account login, repeated requests on one connection, disconnect/cancellation, timeout rollback, repository failures, session creation failure, and duplicate cleanup.
- Host tests cover validation failures, hosted-service cancellation, listener startup rollback, host shutdown, and process error exit behavior.
- World tests cover cancellation before publishing zones, registration/query/removal consistency, duplicate activation, and spatial rollback.
- Persistence component tests cover disabled configuration and mapping failures, but do not validate live MongoDB failure/recovery.

These results support only the named tested paths. They do not prove general concurrency safety or production reliability.

## Fault scenarios not verified

No isolated live MongoDB outage, slow database, mid-write connection loss, interrupted inventory/economy save, active-player shutdown, script exception, administrative mutation, world transition cancellation, long-duration soak, overload response, or production deployment restart was tested. The .NET scripting and remote administrative/gameplay paths are not implemented; their absence prevents end-to-end testing rather than proving the original functionality is safe.

Two MongoDB Testcontainers tests remain opt-in and were skipped. Do not report persistence recovery, data integrity under interrupted writes, or lossless shutdown as verified.

## Recovery implications

The supported operator response and limitations are documented in [OPERATIONAL_RECOVERY.md](./OPERATIONAL_RECOVERY.md). The server is not ready for final release preparation: live persistence, full client workflow, and production deployment assumptions remain unresolved.
