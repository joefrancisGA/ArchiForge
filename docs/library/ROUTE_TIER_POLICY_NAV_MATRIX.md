> **Scope:** Authoritative crosswalk of HTTP route families → commercial tier gate (if any), ASP.NET authorization policy, and operator nav visibility — for procurement reviewers and contributors avoiding “UI link implies HTTP access” confusion.

# Route, tier, policy, and navigation matrix

This matrix complements **[PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md)** four-boundary rules. **HTTP behavior** is defined by controllers and **`CommercialTenantTierFilter`**; **nav visibility** is defined by **`archlucid-ui/src/lib/nav-config.ts`** pipeline (**tier → authority** in **`nav-shell-visibility.ts`**). Cells cite source; use **verify pending** when an attribute was not re-checked in the same edit.

## Pilot-critical routes (sample)

| HTTP route family | Commercial tier gate (`RequiresCommercialTenantTier`) | Primary policy (`[Authorize(Policy=…)]`) | Nav (`href` · tier · `requiredAuthority`) | Notes |
| --- | --- | --- | --- | --- |
| `GET/POST /v1/pilots/*` (excluding Standard-only actions) | None on controller base (`PilotsController` uses `ReadAuthority`) | Mix: base **ReadAuthority**; `PUT scorecard/baselines`, `POST closeout` **ExecuteAuthority** | Pilot: `/`, `/reviews/new`, `/reviews?projectId=default` · essential · (unset) | `POST …/sponsor-one-pager` **Standard** per PRODUCT_PACKAGING §4 |
| `GET/POST /v1/runs/*`, run detail APIs | None on typical read paths (verify per action) | **ReadAuthority** / **ExecuteAuthority** per action | Pilot essential + extended **`/governance/findings`** | Compare PRODUCT_PACKAGING Layer A/B inventories |
| `GET /v1/tenant/trial-status` | None | **ReadAuthority** (`TenantTrialController.GetTrialStatusAsync`) | (indirect — Home / trial widgets) | Class `[Authorize]` + action policy |
| `POST /v1/diagnostics/core-pilot-rail-step` | None | **`[AllowAnonymous]`** on action (overrides controller **ReadAuthority**); **fixed** rate limit | N/A | Core Pilot checklist counter only (`ClientErrorTelemetryController`) |

## Operate routes (sample)

| HTTP route family | Tier gate | Policy | Nav | Notes |
| --- | --- | --- | --- | --- |
| `POST /v1/governance/approval-requests` | **Standard** (per PRODUCT_PACKAGING §4 inventory) | **ExecuteAuthority** typical | **`operate-governance`** links · extended+ | Sub-tier → **404** |
| `GET/POST /v1/alerts/*` | **Standard** min for many mutations (verify controller) | Mixed Read/Execute | **`/alerts`** · tier **essential** for hub | Tier gate per Commercial filter |
| `GET/POST /v1/compare`, `/v1/replay` | **Standard** (per packaging doc) | Mixed | `/compare`, `/replay` · extended | Progressive disclosure |

## Single source of truth order

1. **Code:** `ArchLucid.Api` controllers + **`CommercialTenantTierFilter`**.  
2. **Nav:** `nav-config` builders + **`nav-shell-visibility`**.  
3. **Narrative:** **[PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md)** — update this matrix when buyer-visible behavior changes.

**Related:** **[PROCUREMENT_FAST_LANE.md](../go-to-market/PROCUREMENT_FAST_LANE.md)** (procurement skim), **[NAV_CONFIG_CONTRACT.md](NAV_CONFIG_CONTRACT.md)** if present.
