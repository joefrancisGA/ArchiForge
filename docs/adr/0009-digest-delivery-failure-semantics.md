> **Scope:** ADR 0009: Digest delivery failure semantics - full detail, tables, and links in the sections below.

# ADR 0009: Digest delivery failure semantics

- **Status:** Accepted (v1)
- **Date:** 2026-04-04

## Context

**`IDigestDeliveryDispatcher`** delivers architecture digests to multiple subscriptions and channels. Operators need durable attempt history and clear failure signals without failing the entire advisory scan.

## Decision

- Each subscription delivery creates a **`DigestDeliveryAttempt`** row (**`Started`** → **`Succeeded`** or **`Failed`**).
- **Success** updates the subscription's **`LastDeliveredUtc`** and audits **`DigestDeliverySucceeded`**.
- **Non-cancellation failures** set **`Failed`**, **`ErrorMessage`**, audit **`DigestDeliveryFailed`**, and increment OTel counter **`digest_delivery_failed`** with tag **`channel`** (same pattern for success counter).
- **`OperationCanceledException`** propagates to honor host shutdown / cancellation.

## Consequences

- **Positive:** Partial failures on one channel do not roll back other subscriptions; metrics support SLOs per channel.
- **Trade-off:** Callers must not assume "digest ran" implies "all channels succeeded" — inspect attempt rows or audits.
