# ArchLucid — Product Packaging Reference

**Audience:** buyers, pilot operators, sales engineers, and product team members who need a single, authoritative description of what is in each product layer.

**Status:** V1 capability inventory. This document describes what is **implemented and supportable today** — not a roadmap.

**Related:** [V1_SCOPE.md](V1_SCOPE.md) (engineering scope contract) · [CORE_PILOT.md](CORE_PILOT.md) (first-pilot walkthrough) · [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) (how to measure pilot success) · [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) (which layer to use next) · [EXECUTIVE_SPONSOR_BRIEF.md](EXECUTIVE_SPONSOR_BRIEF.md) (sponsor-ready summary) · [FUTURE_PACKAGING_ENFORCEMENT.md](FUTURE_PACKAGING_ENFORCEMENT.md) (future packaging map) · [operator-shell.md](operator-shell.md) (UI reference) · [archlucid-ui/README.md](../archlucid-ui/README.md#role-aware-shaping-first-wave) (implemented role-aware shaping)

---

## Why three layers?

1. **Explainability.** A buyer needs to understand what they get on day one vs what they unlock for governance or deep investigation.
2. **Time-to-value.** The Core Pilot layer is deliberately narrow so a pilot operator can go from zero to a committed manifest in a single session with no additional configuration.
3. **Packaging clarity.** Advanced Analysis and Enterprise Controls have distinct buyers (architects/analysts vs compliance/security/audit teams). Naming them separately makes that obvious.

For a pilot-success model tied to these layers, see **[PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md)**. For guidance on when to move between layers, see **[OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md)**. For the **canonical buyer narrative**, see **[EXECUTIVE_SPONSOR_BRIEF.md](EXECUTIVE_SPONSOR_BRIEF.md)**.

---

## What the layer model means today

The layer model describes four different things that should not be confused:

### 1. Narrative packaging

The three layers explain **how to understand the product**:

- **Core Pilot** = first useful pilot result
- **Advanced Analysis** = deeper investigation and comparison
- **Enterprise Controls** = governance, auditability, compliance, and trust operations

This section names and sequences layers for buyers. **Sponsor-level narrative** (why a pilot matters, what success sounds like, what not to claim) lives in **[EXECUTIVE_SPONSOR_BRIEF.md](EXECUTIVE_SPONSOR_BRIEF.md)**; this document stays the **capability inventory**—what ships where—so packaging detail does not replace the brief.

### 2. UI progressive disclosure

The operator shell uses **progressive disclosure** so users do not see the full product surface by default.

- **Core Pilot** links are visible by default.
- **Advanced Analysis** appears after **Show more links**.
- deeper Enterprise Controls surfaces appear after extended or advanced disclosure.

This is the default user-experience model.

### 3. Role-based restriction

Some capabilities are better suited to operator/admin roles, especially in **Enterprise Controls**.

That means some surfaces are shaped not just by navigation tier but also by who should reasonably use them in a real environment.

**Implemented in the operator UI (first wave):** `archlucid-ui` composes **tier** (`nav-tier` / progressive disclosure) with per-link **`requiredAuthority`** on **Advanced Analysis** and **Enterprise Controls** entries in `nav-config.ts`, resolved from **`GET /api/auth/me`** via `current-principal.ts` and `nav-shell-visibility.ts` (see `archlucid-ui/README.md` § *Role-aware shaping*). **Core Pilot** essentials omit `requiredAuthority` so the default path stays visible; extended Core links (graph, compare, replay) set Read or Execute to match API policies. Short rank-aware copy also appears on key Enterprise pages (`EnterpriseControlsContextHints.tsx`). This is **operational accountability** in the shell—**not** the entitlement or pricing model in §4.

**Cognitive framing (V1):** Enterprise routes pair **LayerHeader** (`layer-guidance.ts`) with **short page leads**—often **inspect vs configure** language and first-pilot deferral—so read-heavy summaries are not visually equal to mutation forms. See `archlucid-ui/README.md` (*In-product guidance*).

#### Code seams (operator UI — maintenance map)

Keep **docs**, **`nav-config.ts`**, and **controller policies** aligned when routes move between layers or policy tiers:

| Product packaging layer | `NAV_GROUPS[].id` (`archlucid-ui/src/lib/nav-config.ts`) | Primary modules |
|-------------------------|----------------------------------------------------------|-----------------|
| **Core Pilot** | `runs-review` | `nav-tier.ts`; `requiredAuthority` **omitted** only on essentials; extended links set Read/Execute to match API |
| **Advanced Analysis** | `qa-advisory` | Every link sets `requiredAuthority`; composed by `filterNavLinksForOperatorShell` (`nav-shell-visibility.ts`) |
| **Enterprise Controls** | `alerts-governance` | Every link sets `requiredAuthority`; rank from `current-principal.ts`; **Execute-tier** in-page mutations also use `enterprise-mutation-capability.ts` / `useEnterpriseMutationCapability` |

**Read vs Execute in the UI (numeric):** `AUTHORITY_RANK.ExecuteAuthority` (value **2**) is the shared threshold — **`callerRank < 2`** ⇒ **read tier** (nav may still show `ReadAuthority` destinations; `useEnterpriseMutationCapability()` is **false**). **`callerRank >= 2`** ⇒ **Execute+** for mutation soft-enable and operator-oriented rank cues. Example: **`/governance`** is **`ExecuteAuthority`** in `nav-config.ts` so Readers do not see it in nav; the API remains authoritative if they deep-link (nav shaping never implies POST success).

#### Four UI shaping surfaces (do not merge)

Contributors sometimes treat “Enterprise shaping” as one concern; in code it is **four** boundaries. All are **UI shaping only — API authoritative** (`401`/`403` from **ArchLucid.Api** still win on deep links and POSTs).

1. **Shell link inclusion** — `nav-config.ts` (`tier`, `requiredAuthority`) + **`nav-shell-visibility.ts`** (**tier → authority**, empty groups dropped). Never soft-disables POST controls by itself.
2. **Enterprise mutation soft-enable** — **`useEnterpriseMutationCapability()`** / **`enterprise-mutation-capability.ts`** (Execute+ numeric floor). Align with C# policies for the same buttons; **`true`** does not imply HTTP success.
3. **Route packaging strip** — **`LayerHeader`** + **`layer-guidance.ts`** (when to use, **`enterpriseFootnote`**, Enterprise rank cue line). Uses **`useNavCallerAuthorityRank()`** only for the cue; **does not** import the mutation hook.
4. **Inline rank / reader hints** — **`EnterpriseControlsContextHints.tsx`** (e.g. **`GovernanceResolutionRankCue`**, **`AlertsInboxRankCue`**, **`AuditLogRankCue`** on **`/audit`**, **`EnterpriseControlsExecutePageHint`** on mutation-heavy pages such as **`/policy-packs`**). **Same Execute numeric boundary** as (2) for copy, but a route may combine **(3)+(4)** with **(2)** on different paragraphs — e.g. **Governance resolution**: rank cue from **`useNavCallerAuthorityRank`**, **Change related controls** supplement from **`useEnterpriseMutationCapability()`**; **`enterprise-authority-ui-shaping.test.tsx`** locks the mutation branch.

Shell composition order: **tier first, then authority** (`filterNavLinksForOperatorShell`). **Hardening sequence:** [COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md) Stage 1 describes what shipped without entitlements.

**Cross-surface lock:** `archlucid-ui/src/lib/authority-seam-regression.test.ts` asserts **`enterpriseMutationCapabilityFromRank(rank)`** matches **`ExecuteAuthority`** link visibility for ranks **0–3**, Auditors filter Enterprise nav like Reader rank, and **`normalizeAuthMeResponse`** stays aligned with **`maxAuthorityRankFromMeClaims`**. **`archlucid-ui/src/lib/authority-execute-floor-regression.test.ts`** is a **narrower** guard on the same **Execute floor**: a synthetic **`ExecuteAuthority`** nav row’s visibility must equal the mutation boolean at every representative rank; **`archlucid-ui/src/lib/authority-shaped-ui-regression.test.ts`** walks **every** real **`nav-config`** **`ExecuteAuthority`** row at Read vs Execute rank (catches new catalog links without updating tests). **`alerts-governance`** filtered link counts stay monotonic Read→Execute→Admin; Reader-filtered Enterprise links include **`/alerts`** but not **`/governance`** (packaging ↔ **`nav-config`**). The same file locks **caller rank `0` vs `ReadAuthority`** nav (conservative bootstrap), **`/alerts`** on **`essential`** tier (default Enterprise inbox strip), **stable ordering** after authority filters / in **`listNavGroupsVisibleInOperatorShell`** (sidebar composition), **`LAYER_PAGE_GUIDANCE`** **Enterprise** rows (non-empty **`useWhen`**, **`firstPilotNote`**, **`enterpriseFootnote`**) vs **Advanced Analysis** rows (**no** **`enterpriseFootnote`** — keeps **`LayerHeader`** Enterprise detection aligned with buyer layers), **Enterprise** href **monotonicity** Read→Execute→Admin under **`filterNavLinksByAuthority`**, default Reader shell **Advanced** = **`/ask`** only (tier-before-authority), and **`/governance`** only when **extended and advanced** are on for **Execute** rank. **`nav-shell-visibility.test.ts`** locks **tier → authority** order (Execute rank does not reveal **extended** Enterprise hrefs without disclosure toggles; **Core Pilot** extended **Execute** link **`/replay`** stays behind **Show more** at any rank). **`current-principal.test.ts`** locks **`hasEnterpriseOperatorSurfaces`** and **`maxAuthority`** (via **`requiredAuthorityFromRank(authorityRank)`**) to the same rules as **`nav-authority`**. **`LayerHeader.test.tsx`** requires every **Enterprise Controls** **`layer-guidance`** key to render the rank cue and asserts the Enterprise **`aside`** **`aria-label`** (badge + headline). **`EnterpriseControlsContextHints.authority.test.tsx`** locks rank-gated cues (governance resolution, audit log, **Alerts inbox**, **governance dashboard** reader line, alert tooling) at the **Execute** boundary. **`enterprise-authority-ui-shaping.test.tsx`** locks **`useEnterpriseMutationCapability()`** → **`disabled`** / **`readOnly`** on representative pages (policy packs **Create**, **Alert rules** **Create rule**, alerts triage **Confirm**, **Governance** submit **`#gov-submit-run`** / **`#gov-submit-version`**, **Governance resolution** **Change related controls** reader supplement vs mutation hook + **Refresh** not gated by mutation). **`nav-config.structure.test.ts`** locks packaging-shaped **nav-config** invariants (duplicate **`href`**s; **Core Pilot** essentials omit **`requiredAuthority`**; **`ExecuteAuthority`** links not on **`essential`** tier in Advanced and Enterprise groups).

#### Contributor drift guard (operator UI — keep packaging, nav, and API aligned)

**Rule:** the **API** (`ArchLucid.Api` `[Authorize(Policy = …)]` on controllers) is **authoritative** for 401/403. The operator UI only **shapes** visibility, copy, and soft-disabled controls from **`GET /api/auth/me`** so the default path stays honest.

When you **add or move** an operator route, touch these in order (skip only what does not apply):

1. **C#** — confirm policy names (`ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` in `ArchLucidPolicies`) match the lowest tier that should succeed for the page’s primary workflow.
2. **`archlucid-ui/src/lib/nav-config.ts`** — `tier`, `href`, `requiredAuthority` on the `NavLinkItem` (see file header **Authority** section). Stable **`NAV_GROUPS[].id`** values map to this document’s **Code seams** table above.
3. **Guidance strip** — for routes that should explain “which layer / when,” add or extend a key in **`archlucid-ui/src/lib/layer-guidance.ts`** and render **`LayerHeader`** on the page (`LayerGuidancePageKey` must match the route family).
4. **Enterprise write affordances** — if the page shows POST/toggle UI, keep **`useEnterpriseMutationCapability()`** (Execute+ rank) aligned with the same policies as the buttons (see **`enterprise-mutation-capability.ts`**). If the page also shows rank-gated copy (**`EnterpriseControlsContextHints`** / **`LayerHeader`** rank cue), confirm both seams stay intentional (see **Four UI shaping surfaces** above — e.g. governance resolution). Multi-step forms (e.g. governance workflow inline review) should keep **read-only** fields and reader notes when rank is below Execute so the shell matches nav omission even if UI state is visible. **Inspect-first layout** (e.g. **`flex-col-reverse`** on governance workflow / policy packs when mutation is off, triage deemphasis on alerts) is still **UI only** — extend **`authority-shaped-layout-regression.test.tsx`** when you add parallel column patterns.
5. **This document** — update capability / navigation rows in § Layer inventories when behavior is buyer-visible.

**Light regression tests** (Vitest, not snapshots): `nav-authority.test.ts` (includes Execute-link **`navLinkVisibleForCallerRank`** floor and unrestricted links at conservative rank **0**), `nav-shell-visibility.test.ts` (empty groups; default Reader **Enterprise** strip = **`/alerts`** only when extended/advanced off; **Execute** caller still tier-limited; authority-only empty group; **Core Pilot `/replay`** tier gate), `current-principal.test.ts` (`/me` normalization; **`hasEnterpriseOperatorSurfaces`** vs mutation capability; **`maxAuthority`** vs **`requiredAuthorityFromRank`**), `enterprise-mutation-capability.test.ts`, `use-enterprise-mutation-capability.test.tsx`, `LayerHeader.test.tsx` (footnotes + Enterprise rank cue for every Enterprise **`layer-guidance`** key + conservative caller rank **0** + Enterprise **`aside`** **`aria-label`**), **`authority-seam-regression.test.ts`** (cross-module `/me` rank vs Enterprise nav vs mutation; Core Pilot essential hrefs for Reader; **ExecuteAuthority** rows in Advanced + Enterprise hidden from Read; Auditor vs Reader Enterprise href parity; **ReadAuthority** at rank **0**; **`/alerts`** **`essential`**; ordering; **`LAYER_PAGE_GUIDANCE`** Enterprise vs Advanced **`enterpriseFootnote`**), **`authority-execute-floor-regression.test.ts`** (Execute nav row **≡** mutation boolean; **`alerts-governance`** monotonicity; Reader **`/governance`** omission), **`authority-shaped-ui-regression.test.ts`** (every **`NAV_GROUPS`** **`ExecuteAuthority`** link off at Read / on at Execute; mutation floor monotonicity **0–3**; empty-claims **`/me`** rank; shell bootstrap principals vs mutation flag), **`OperatorNavAuthorityProvider.test.tsx`** (JWT signed-in: **`useNavCallerAuthorityRank`** stays Read while `/me` refetches after rank had reached Execute; `/me` failure → Read), **`EnterpriseControlsReadRankHints.test.tsx`**, **`EnterpriseControlsContextHints.authority.test.tsx`** (rank-gated `EnterpriseControlsExecutePageHint`, `EnterpriseExecutePlusPageCue`, nav group, alert tooling, **governance resolution**, **audit log**, **Alerts inbox**, **governance dashboard** reader cue), **`enterprise-authority-ui-shaping.test.tsx`** (**`useEnterpriseMutationCapability`** → Policy packs / **Alert rules** / Alerts / **Governance** submit field wiring), **`authority-shaped-layout-regression.test.tsx`** (inspect-first **`flex-col-reverse`**, alerts triage deemphasis, alert-routing inspect-before-toggle — **UI hierarchy**, not copy), **`nav-config.structure.test.ts`** (href dedupe; **Core Pilot** essentials omit **`requiredAuthority`**; **ExecuteAuthority** Advanced + Enterprise links not on **essential** tier) — extend when you change rank, filtering, or page-level mutation gates.

This is the operational-usage model.

### 4. Future entitlement or pricing boundaries

The layer model is also the most likely foundation for future commercial packaging.

But in V1, the layer model is **not yet an entitlement engine** and is **not yet a hard pricing boundary**.

This is the future commercialization model.

For the future-state map, see **[FUTURE_PACKAGING_ENFORCEMENT.md](FUTURE_PACKAGING_ENFORCEMENT.md)**.

---

## Layer 1 — Core Pilot

> "AI-driven architecture request through committed manifest — visible, auditable, downloadable."

Every pilot starts here. The operator UI presents this layer by default with no progressive disclosure required. **Home**, **onboarding**, and **run detail** copy keep **Advanced Analysis** and **Enterprise Controls** explicitly **optional to first-pilot proof** so deeper shaping does not widen the default mental model.

**Anti-creep rule:** Core Pilot is the default wedge and first buying motion. Advanced Analysis and Enterprise Controls should only be introduced when the next analytical or governance question actually requires them.

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

Default links in the **Core Pilot** sidebar group: Home · Onboarding · New run · Runs. The **Alerts** inbox lives under **Enterprise Controls** (same tier/authority rules as other Enterprise links).

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

**Not required for first-pilot success:** this layer exists to answer deeper analytical questions after Core Pilot proves value.

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

**Not required for first-pilot success:** this layer exists for governance, audit, policy, and operational trust questions after the Core Pilot wedge is already clear.

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

The operator UI also adds **lightweight in-product hints** (sidebar captions under each group, a `LayerHeader` strip on key Advanced Analysis / Enterprise routes, a post-checklist nudge on Home, and an optional post-commit strip on run detail) so operators can route by layer without re-reading this doc. See [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) for the full decision matrix.

---

## Packaging boundaries — what this document is NOT saying

- This is **not a licensing or entitlement document.** All three layers are available in V1 to all licensed operators.
- This is **not a pricing document.** Pricing tiers (Team / Professional / Enterprise) are defined in `archlucid-ui/public/pricing.json` and `docs/go-to-market/POSITIONING.md`.
- This is **not a commitment to separate binary builds.** All layers ship in the same API and UI; packaging is expressed through progressive disclosure and documentation, not feature flags or separate binaries in V1.

If entitlement-level gating is required in a future commercial release, the progressive disclosure tier system (`nav-tier.ts`, `nav-config.ts`) is the intended extension point.

---

## Packaging today vs future commercial enforcement

### Packaging today

In V1, packaging is expressed through:

- product narrative,
- operator guidance,
- UI progressive disclosure,
- and role-appropriate usage.

That means the layer model is already useful for buyers and pilots today even though it is not yet a hard commercial gate.

### What remains intentionally soft in V1

In V1, the following are intentionally **not** hard-enforced commercial boundaries:

- separate binaries,
- feature entitlements,
- pricing-enforced capability gating,
- distinct deployment artifacts per tier.

### What future commercial enforcement would build on

If future commercialization requires stronger packaging, the natural extension points are:

- `nav-tier.ts` and `nav-config.ts` for visibility rules,
- role-aware UI and API shaping,
- pricing/plan definitions,
- future entitlement or billing controls.

The current layer model is therefore a **foundation for future commercialization**, not merely a documentation convenience. For the fuller future-state map, see **[FUTURE_PACKAGING_ENFORCEMENT.md](FUTURE_PACKAGING_ENFORCEMENT.md)**.

---

## Change control

When capability assignments change between layers, update:

1. This file (`PRODUCT_PACKAGING.md`) — the canonical inventory.
2. `docs/V1_SCOPE.md` §2 — engineering scope.
3. `archlucid-ui/src/lib/nav-config.ts` — tier assignments in the sidebar.
4. `docs/operator-shell.md` — operator workflow narrative.
5. `README.md` and `archlucid-ui/README.md` — entry-point layer tables.
