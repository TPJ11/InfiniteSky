# Health Monitoring

## Policy

Health checks are in-process and are not exposed over HTTP or any public endpoint. Adding an endpoint requires explicit approval, authentication, and information-disclosure review.

## Readiness

`FoundationReadinessHealthCheck` uses the application lifecycle:

| Lifecycle | Health |
|---|---|
| Created/Initialising/Starting | Degraded |
| Running | Healthy |
| Stopping/Failed/Stopped | Unhealthy |

Readiness means the C# foundation host is usable. It does not mean networking, MongoDB, data loading, world simulation, or gameplay is available.

## Liveness

`FoundationLivenessHealthCheck`:

- healthy during creation/initialization/start/running;
- degraded while stopping;
- unhealthy after terminal failure or stop.

## Unimplemented dependencies

The readiness snapshot explicitly reports:

```text
Networking = DisabledByFoundation
Database = DisabledByFoundation
```

These are not registered as healthy external probes. Enabling either dependency before its implementation fails options validation.

## Health result data

Results include:

- lifecycle phase;
- lifecycle reason;
- current foundation check values.

No secret-bearing configuration or raw payload is included.

## Future checks

Future packet/network, MongoDB, data, and zone services must add separate checks only when they own a real probe. Checks must:

- be bounded by cancellation/timeout;
- avoid expensive world locking;
- distinguish disabled, degraded, and unhealthy;
- avoid exposing credentials or customer/account data;
- be tagged `readiness` or `liveness`;
- have deterministic tests.
