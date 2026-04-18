# ArchLucid — Product Packaging Reference

**Audience:** buyers, pilot operators, sales engineers, and product team members who need a single, authoritative description of what is in each product layer.

**Status:** V1 capability inventory. This document describes what is **implemented and supportable today** — not a roadmap.

**Related:** [V1_SCOPE.md](V1_SCOPE.md) (engineering scope contract) · [CORE_PILOT.md](CORE_PILOT.md) (first-pilot walkthrough) · [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) (how to measure pilot success) · [operator-shell.md](operator-shell.md) (UI reference)

---

## Why three layers?

1. **Explainability.** A buyer needs to understand what they get on day one vs what they unlock for governance or deep investigation.
2. **Time-to-value.** The Core Pilot layer is deliberately narrow so a pilot operator can go from zero to a committed manifest in a single session with no additional configuration.
3. **Packaging clarity.** Advanced Analysis and Enterprise Controls have distinct buyers (architects/analysts vs compliance/security/audit teams). Naming them separately makes that obvious.

For a pilot-success model tied to these layers, see **[PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md)**.

---

## Layer 1 — Core Pilot

> "AI-driven architecture request through committed manifest — visible, auditable, downloadable."

Every pilot starts here. The operator UI presents this layer by default with no progressive disclosure required.

### Capability inventory

| Capability | API surface | UI surface | CLI surface |
|------------|-------------|------------|-------------|
| Create architecture request | `POST /v1/architecture/request` | New run wizard (7-step) | `archlucid run create` |
| Execute run (coordinator or authority path) | `POST /v1/architecture/run/{runId}/execute` | Pipeline timeline (auto-poll) | `archlucid run execute` |
| Commit golden manifest | `POST /v1/architecture/run/{runId}/commit` | Commit run button on run detail | `archlucid run commit` |
| List runs | `GET /v1/architecture/runs` | Runs list (`/runs`) | `archlucid runs list` |
| Run detail and pipeline timeline | `GET /v1/authority/runs/{runId}/pipeline-timeline` | Run detail page | `archlucid run status` |
| Manifest summary | `GET /v1/architecture/manifests/{id}` | Manifest summary tab | — |
| Artifact list and review | `GET /v1/artifacts/manifests/{manifestId}` | Artifacts table + Review page | `archlucid artifacts` |
| Artifact download | `GET /v1/artifacts/…/download` | Download button per artifact | — |
| Bundle ZIP download | `GET /v1/artifacts/manifests/{id}/bundle` | Bundle ZIP button | — |
| DOCX architecture package | `GET /v1/docx/runs/{runId}/architecture-package` | Export button on run detail | — |
| Run-export ZIP | `GET /v1/artifacts/runs/{runId}/export` | Export ZIP button | — |
| Health and readiness | `GET /health/live`, `/health/ready`, `/health` | — | `archlucid doctor` |
| Version identity | `GET /version` | — | `archlucid doctor` |
| Support bundle | — | — | `archlucid support-bundle --zip` |
| Development bypass auth | `appsettings.Development.json` | — | — |
| API key auth | `Authentication:ApiKey:Enabled=true` | — | — |
| JWT bearer / Entra ID auth | `ArchLucidAuth:Mode=JwtBearer` | OIDC sign-in at `/auth/signin` | — |

### Navigation (operator UI)

Sidebar group label: **Core Pilot** (always visible — no disclosure toggle required).

Default links: Home · Onboarding · New run · Runs · Alerts (inbox only).

### How to judge success

A strong Core Pilot result should demonstrate:

- faster movement from request to committed manifest,
- less manual packaging effort,
- and a cleaner path to reviewable artifacts.

Use **[PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md)** for the scorecard and suggested pilot metrics.

---

## Layer 2 — Advanced Analysis

> "Understand what changed, why it changed, and what the architecture looks like."

Available immediately after a first committed run. Enabled by clicking **Show more links** in the operator UI sidebar.

### Capability inventory

| Capability | API surface | UI surface |
|------------|-------------|------------|
| Compare two runs (structured manifest diff) | `POST /v1/architecture/compare` | Compare two runs (`/compare`) |
| Compare two runs (legacy flat diff) | `GET /v1/architecture/compare/legacy` | Compare two runs — flat diff tab |
| Optional AI explanation of diff | Requires AI provider config | Compare two runs — AI narrative section |
| Comparison replay (artifact / regenerate / verify) | `POST /v1/architecture/compare/replay` | Replay a run (`/replay`) |
| Run replay (authority chain re-validation) | `POST /v1/authority/replay` | Replay a run — authority mode |
| Provenance graph (full, decision subgraph, neighborhood) | `GET /v1/graph/runs/{runId}/provenance` | Graph (`/graph`) |
| Architecture graph | `GET /v1/graph/runs/{runId}/architecture` | Graph — architecture mode |
| Natural-language Ask | `POST /v1/ask/threads` | Ask (`/ask`) |
| Advisory scans | `POST /v1/advisory/scans` | Advisory (`/advisory`) |
| Architecture digests | `GET /v1/advisory/digests` | Digests (`/digests`) |
| Digest subscriptions (email delivery) | `POST /v1/advisory/digest-subscriptions` | Subscriptions (`/digest-subscriptions`) |
| Advisory scheduling | `PUT /v1/advisory/schedules` | Schedules (`/advisory-scheduling`) |
| Retrieval indexing and search | `POST /v1/retrieval/index` | Search (`/search`) |
| Pilot feedback rollups | `GET /v1/product-learning/rollups` | Pilot feedback (`/product-learning`) |
| Recommendation learning profiles | `GET /v1/recommendation-learning/profiles` | Recommendation learning (`/recommendation-learning`) |
| Improvement themes and planning | `GET /v1/planning/themes` | Planning (`/planning`) |
| Evolution candidates (before/after) | `GET /v1/evolution-review/candidates` | Evolution candidates (`/evolution-review`) |
| Integration events (Azure Service Bus, CloudEvents) | Outbox → Service Bus topic | — |
| Webhooks and digest delivery | `POST /v1/webhooks/subscriptions` | — |

### Navigation (operator UI)

Sidebar group label: **Advanced Analysis** (visible after **Show more links**).

Extended-tier links: Graph · Compare two runs · Replay a run · Advisory · Recommendation learning · Pilot feedback.

Advanced-tier links: Search · Planning · Evolution candidates · Schedules · Digests · Subscriptions.

---

## Layer 3 — Enterprise Controls

> "Governance, auditability, compliance, and trust for architecture decisions at scale."

Available immediately but requiring extended/advanced sidebar disclosure and typically operator/admin role. Most governance features require explicit enablement per environment.

### Capability inventory

| Capability | API surface | UI surface | Config key |
|------------|-------------|------------|------------|
| Governance approval workflow | `POST /v1/governance/approvals` | Governance workflow (`/governance`) | Migration `017_GovernanceWorkflow.sql` |
| Pre-commit governance gate | Checked on `POST /v1/architecture/run/{runId}/commit` | Pre-commit block message on run detail | `ArchLucid:Governance:PreCommitGateEnabled` |
| Cross-run governance dashboard | `GET /v1/governance/dashboard` | Dashboard (`/governance/dashboard`) | — |
| Governance resolution (effective policy) | `GET /v1/governance/resolution` | Governance resolution (`/governance-resolution`) | — |
| Policy packs (versioned rule sets) | `POST /v1/policy-packs` | Policy packs (`/policy-packs`) | — |
| Append-only audit log (78 typed events) | `GET /v1/audit/events` | Audit log (`/audit`) | — |
| Audit log CSV export | `GET /v1/audit/export` | Export CSV button in audit log | — |
| Compliance drift trend | `GET /v1/compliance/drift` | Compliance chart on governance dashboard | — |
| Row-level security (RLS) tenant isolation | SQL `SESSION_CONTEXT` per request | — | `ArchLucid:TenantIsolation:Enabled` |
| Alert inbox (open / acknowledged) | `GET /v1/alerts` | Alerts (`/alerts`) | — |
| Alert rules | `POST /v1/alert-rules` | Alert rules (`/alert-rules`) | — |
| Alert routing subscriptions | `POST /v1/alert-routing` | Alert routing (`/alert-routing`) | — |
| Composite alert rules | `POST /v1/composite-alert-rules` | Composite rules (`/composite-alert-rules`) | — |
| Alert simulation | `POST /v1/alert-simulation` | Alert simulation (`/alert-simulation`) | — |
| Alert tuning (threshold and noise) | `PUT /v1/alert-tuning` | Alert tuning (`/alert-tuning`) | — |
| Entra ID / JWT bearer RBAC | `ArchLucidAuth:Mode=JwtBearer` | OIDC sign-in + role claims | IdP app registration |
| Private endpoint Terraform modules | `infra/modules/front-door` | — | Azure networking |
| DPA template, subprocessors, SOC 2 roadmap | — | — | [go-to-market/TRUST_CENTER.md](go-to-market/TRUST_CENTER.md) |
| Customer-managed key (CMK) for SQL TDE | `infra/modules/sql-tde-cmk` | — | Azure Key Vault |
| Trial enforcement (seat and run limits) | `GET /v1/tenant/trial-status` | Trial banner in operator shell | `ArchLucid:Trial:*` |
| Billing checkout | `POST /v1/tenant/billing/checkout` | Trial banner — Convert to paid | Stripe bridge |

### Navigation (operator UI)

Sidebar group label: **Enterprise Controls** (partially visible by default; fully surfaced after extended + advanced links).

Essential-tier links: Alerts (inbox).

Extended-tier links: Policy packs · Governance resolution · Governance dashboard.

Advanced-tier links: Alert rules · Alert routing · Composite rules · Alert simulation · Alert tuning · Governance workflow · Audit log.

---

## Progressive disclosure summary

| Sidebar state | What you see |
|--------------|-------------|
| **Default** (no toggles) | Core Pilot links + Alerts inbox + Ask |
| **Show more links** | + Graph · Compare · Replay · Advisory · Recommendation learning · Pilot feedback · Policy packs · Governance resolution · Governance dashboard |
| **Show more + Show advanced links** | + Search · Planning · Evolution candidates · Schedules · Digests · Alert rules · Routing · Composite rules · Simulation · Tuning · Governance workflow · Audit log |

---

## Packaging boundaries — what this document is NOT saying

- This is **not a licensing or entitlement document.** All three layers are available in V1 to all licensed operators.
- This is **not a pricing document.** Pricing tiers (Team / Professional / Enterprise) are defined in `archlucid-ui/public/pricing.json` and `docs/go-to-market/POSITIONING.md`.
- This is **not a commitment to separate binary builds.** All layers ship in the same API and UI; packaging is expressed through progressive disclosure and documentation, not feature flags or separate binaries in V1.

If entitlement-level gating is required in a future commercial release, the progressive disclosure tier system (`nav-tier.ts`, `nav-config.ts`) is the intended extension point.

---

## Change control

When capability assignments change between layers, update:

1. This file (`PRODUCT_PACKAGING.md`) — the canonical inventory.
2. `docs/V1_SCOPE.md` §2 — engineering scope.
3. `archlucid-ui/src/lib/nav-config.ts` — tier assignments in the sidebar.
4. `docs/operator-shell.md` — operator workflow narrative.
5. `README.md` and `archlucid-ui/README.md` — entry-point layer tables.
