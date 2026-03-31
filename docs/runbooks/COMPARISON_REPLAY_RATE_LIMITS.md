# Runbook: Comparison replay — light vs heavy and rate limits

## Policy summary

**`AddArchiForgeRateLimiting`** registers a **`replay`** partition policy:

- **Light** (default formats): higher permit count per window.
- **Heavy** (e.g. **`docx`**, **`pdf`** query): lower permit count per longer window.

**Batch replay** (`POST .../comparisons/replay/batch`) also uses the **`replay`** rate limiter; each request can trigger multiple replays internally, so operators should keep **`ComparisonReplay:Batch:MaxComparisonRecordIds`** aligned with **`RateLimiting:Replay:*`** windows.

Partition key combines the authenticated user (if any) or remote IP with **`light`** vs **`heavy`**.

## Operator actions

1. **429 on replay:** Confirm client is not batching heavy exports on a shared NAT IP. Prefer authenticated callers so limits partition by identity.
2. **Tune config:** Adjust **`RateLimiting:Replay:Light:*`** and **`RateLimiting:Replay:Heavy:*`** in app settings (see **`InfrastructureExtensions.AddArchiForgeRateLimiting`**).

## References

- **`docs/COMPARISON_REPLAY.md`** — functional replay behavior.
- **`docs/API_CONTRACTS.md`** — validation and problem responses for replay endpoints.
