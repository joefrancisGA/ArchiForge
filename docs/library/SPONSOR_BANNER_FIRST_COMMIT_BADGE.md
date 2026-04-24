> **Scope:** Operators and sponsors interpreting the post-commit sponsor banner day badge; not general trial billing or unrelated UI components.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Sponsor banner — “Day N since first commit” badge

## Objective

Explain the small **“Day N since first commit”** badge shown next to **Time to value** on the post-commit **Email this run to your sponsor** banner (`archlucid-ui/src/components/EmailRunToSponsorBanner.tsx`) so buyers and operators know what it measures, when it appears, and how it degrades safely.

## Assumptions

- The tenant row may carry **`dbo.Tenants.TrialFirstManifestCommittedUtc`**, set on the **first golden manifest commit** for **every** tenant via `ITenantRepository.TryMarkFirstManifestCommittedAsync` (column name unchanged). Trial-only **`TrialFirstRunCompleted`** audit + histograms still fire only when **`TrialExpiresUtc`** is set — see **`SqlTrialFunnelCommitHook`**.
- The operator shell calls **`GET /v1/tenant/trial-status`** (via **`/api/proxy/v1/tenant/trial-status`**) with the same registration scope headers as **`TrialBanner`**.
- **Day N** is computed in the browser as **full UTC 24-hour periods** since that timestamp: `floor((nowUtcMillis − firstCommitUtcMillis) / 86400000)`, clamped at zero — so **Day 0** covers the first 24 hours after the recorded commit instant.

## Constraints

- **No new SQL migration** — the column already exists (**081**).
- **No new audit event** — badge render is read-only telemetry (`archlucid.ui.sponsor_banner.first_commit_badge_rendered`) plus optional **`POST /v1/diagnostics/sponsor-banner-first-commit-badge`** for server-side counting.
- **Non-trial tenants** keep a **null** `firstCommitUtc` until their first authority commit (or until ops run the one-shot backfill script below); the badge stays hidden when null.

## Architecture overview

```mermaid
sequenceDiagram
  participant UI as Operator UI (banner)
  participant Proxy as Next.js /api/proxy
  participant API as TenantTrialController
  participant Repo as ITenantRepository
  participant DB as dbo.Tenants

  UI->>Proxy: GET /v1/tenant/trial-status
  Proxy->>API: forwarded request + scope headers
  API->>Repo: GetByIdAsync(tenantId)
  Repo->>DB: SELECT … TrialFirstManifestCommittedUtc
  DB-->>Repo: row
  Repo-->>API: TenantRecord
  API-->>Proxy: TenantTrialStatusResponse (firstCommitUtc)
  Proxy-->>UI: JSON
  UI->>UI: compute Day N; optional telemetry POST
```

## Component breakdown

| Piece | Role |
|-------|------|
| **`TenantRecord.TrialFirstManifestCommittedUtc`** | Persistence model for the funnel anchor timestamp. |
| **`TenantTrialStatusResponse.FirstCommitUtc`** | Wire name **`firstCommitUtc`** (camelCase JSON). |
| **`EmailRunToSponsorBanner`** | Fetches trial status once; renders badge when `firstCommitUtc` parses; never blocks the PDF CTA. |
| **`recordSponsorBannerFirstCommitBadge`** | POSTs bucket to **`/v1/diagnostics/sponsor-banner-first-commit-badge`** (fire-and-forget). |
| **`ArchLucidInstrumentation.SponsorBannerFirstCommitBadgeRenderedTotal`** | Prometheus / OTel counter on the API host. |

## Data flow

1. First golden manifest commit sets **`TrialFirstManifestCommittedUtc`** once (all tiers).
2. **`GET /v1/tenant/trial-status`** projects it to **`firstCommitUtc`** for both **Status = None** (blank trial status) and active trial payloads.
3. The banner reads **`firstCommitUtc`**, computes **N**, shows **“Day N since first commit”**, and emits **one** telemetry increment per mount when a badge is shown.

## Security model

- Same **`ReadAuthority`** gate as the existing trial-status and diagnostics client-error routes.
- **No PII in the badge** — only an integer day count derived from a timestamp the operator already received in JSON.
- Telemetry POST carries only a **low-cardinality bucket** label; **`tenant_id`** is taken from **server-side scope**, not from the browser body.

## Operational considerations

- **Graceful degradation:** network failure, **5xx**, or missing **`firstCommitUtc`** → banner text and PDF button unchanged; **no** “Day NaN”.
- **Stale day count:** the badge does **not** poll; N updates on **full page navigation** only.
- **Backfill (ops):** tenants with manifests but a null pin can be repaired idempotently with **`ArchLucid.Persistence/Scripts/Maintenance/Backfill-FirstManifestCommittedUtc.sql`** (sets the column to **`MIN(CreatedUtc)`** per tenant from **`dbo.GoldenManifests`**). **No column rename** — downstream callers depend on **`TrialFirstManifestCommittedUtc`**.

## Manual verification

```bash
curl -sS -H "Authorization: Bearer <token-with-tenant-scope>" "https://<host>/v1/tenant/trial-status"
```

Expect **`"firstCommitUtc":"…"`** when the column is populated (ISO-8601). When null, the field may be omitted or null depending on serializer settings; the UI treats both as “no badge”.
