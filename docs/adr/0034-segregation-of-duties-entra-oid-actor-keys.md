> **Scope:** Why governance SoD uses Entra JWT `tid`/`oid`-derived canonical keys plus additive DB columns—**not** display names alone—not API-key ergonomics redesign.

# ADR 0034 — Segregation of duties: Entra `oid`-normalized actor keys

## Status

Accepted (2026-04-29)

## Context

`IActorContext.GetActor()` surfaced the mutable **display** identity (`ClaimTypes.Name`, short JWT `name`, etc.). A principal could authenticate as two different **display** strings while still representing the **same** Entra object (interactive user vs delegated service principal semantics in CI/SP flows), defeating ordinal compare on display strings (`GovernanceSegregationRules` / `GovernanceWorkflowService`).

Separately, **`http://schemas.microsoft.com/identity/claims/objectidentifier` (oid)** together with **`tid`** (tenant) is stable enough for JWT/OIDC segregation keys within one tenant boundary.

## Decision

1. **`IActorContext.GetActorId()`** returns **`jwt:{tid}:{oid}`** (`jwt:{oid}` if `tid` absent). When JWT claims omit `oid` (e.g. static API-key auth), **`GetActorId()`** falls back to **`GetActor()`** behavior so existing API-key callers keep a deterministic string—but **canonical Entra segregation is JWT-only.**

2. **Persistence (Option B).** **`dbo.GovernanceApprovalRequests`** adds **`RequestedByActorKey`** and **`ReviewedByActorKey`** (nullable **`NVARCHAR(256)`**) alongside existing display columns. Submission and approve/reject write both.

3. **Comparison.** **`GovernanceSegregationRules.IsSameActorForReview`** compares **JWT-prefixed** keys ordinally ignoring case **only when both** the stored submission key and reviewer key look JWT-canonical (`StartsWith` **`jwt:`**); otherwise compares display strings (**case-insensitive**) for backward compatibility / API-key traffic.

4. **Audit.** **`GovernanceSelfApprovalBlocked`** payloads include **`requestedByActorKey`** and **`attemptedReviewerActorKey`** for investigations.

Migration: **`ArchLucid.Persistence/Migrations/130_GovernanceApprovalRequests_ActorKeys.sql`** (+ rollback / **`ArchLucid.sql`** parity).

## Alternatives considered

- **Option A (reuse display columns):** rejected — auditors prefer readable **`RequestedBy`**; stuffing raw OID-derived strings there harms readability and breaks operator expectations without enrichment.

- **Accept residual risk:** rejected as primary outcome — documented dual-display bypass was reproducible with tests; mitigation implemented.

## Consequences

- **Security:** Closes Entra JWT **same-oid / different-display** self-approval. **Does not** merge two principals that legitimately have **distinct** **`oid`** values for the **same human** (user account vs separate SP)—that remains organization policy / future product scope.

- **Scalability / cost:** Two extra narrow string columns per approval row — negligible versus workflow payload and audit traffic.

- **Reliability:** DB migration additive only; **`NULL`** legacy rows fall back to display comparison until backfill (if adopted operationally).

- **API-key residual:** **Display-string** SoD only when **`oid`** missing—documented constraint for non-production automation.

## References

- [`docs/library/GOVERNANCE.md`](../library/GOVERNANCE.md) — segregation section
- [`ArchLucid.Application/Common/ActorContext.cs`](../../ArchLucid.Application/Common/ActorContext.cs)
- [`ArchLucid.Application/Governance/GovernanceSegregationRules.cs`](../../ArchLucid.Application/Governance/GovernanceSegregationRules.cs)
