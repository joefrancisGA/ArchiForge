# Staging trial funnel — reachability status

**Recorded (UTC):** 2026-04-28  
**Method:** `Invoke-WebRequest` from the development environment (no infrastructure changes).

## HTTP endpoints

| URL | Result | Notes |
|-----|--------|--------|
| `GET https://staging.archlucid.net/health/live` | **Not reachable** | DNS resolution failed: host name could not be resolved from this network. |
| `GET https://staging.archlucid.net/health/ready` | **Not reachable** | Same as above. |
| `GET https://staging.archlucid.net/pricing` | **Not reachable** | Same as above. |

## Stripe TEST checkout

Not evaluated: the marketing/pricing pages could not be loaded because the hostname did not resolve locally.

## Follow-up (operator)

When DNS and Front Door routes are available from your network, repeat the checks in [`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`](../runbooks/TRIAL_FUNNEL_END_TO_END.md) and update this file with **Working** / **Degraded** per component.
