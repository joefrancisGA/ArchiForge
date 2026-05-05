> **Scope:** ArchLucid — Product Packaging Reference - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Product Packaging Reference

**Audience:** buyers, pilot operators, sales engineers, and product team members who need a single, authoritative description of what is in each product layer.

**Status:** V1 capability inventory. This document describes what is **implemented and supportable today** — not a roadmap.

**Related:** [V1_SCOPE.md](V1_SCOPE.md) (engineering scope contract) · [CORE_PILOT.md](../CORE_PILOT.md) (first-pilot walkthrough) · [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) (how to measure pilot success) · [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) (which layer to use next) · [EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) (sponsor-ready summary) · [FUTURE_PACKAGING_ENFORCEMENT.md](FUTURE_PACKAGING_ENFORCEMENT.md) (future packaging map) · [operator-shell.md](operator-shell.md) (UI reference) · [archlucid-ui/README.md](../../archlucid-ui/README.md#role-aware-shaping-first-wave) (implemented role-aware shaping)

---

## Hosted SaaS entry URLs

**Staging:** `https://staging.archlucid.net` — pre-production Front Door + Container Apps funnel (marketing → self-service signup → Stripe checkout when configured → operator shell). **Production:** `https://archlucid.net` when custom hostnames and managed certificates are attached per [REFERENCE_SAAS_STACK_ORDER.md](REFERENCE_SAAS_STACK_ORDER.md) and `infra/apply-saas.ps1`. **Smoke:** hosted liveness `Invoke-RestMethod https://staging.archlucid.net/health/live`; full repo gate remains **`pwsh ./release-smoke.ps1`** (local API E2E; see [RELEASE_SMOKE.md](RELEASE_SMOKE.md)).

---

## Hosted SaaS reliability (packaging)

Buyer-facing materials for **Professional** and **Enterprise** tiers cite a **99.9% monthly availability target** for the **hosted API and operator UI** (pre-contractual target, not a guarantee until a customer-specific SLA is signed). Full definitions, exclusions, and measurement notes: **[SLA_TARGETS.md](SLA_TARGETS.md)**.

---

## Why two buyer layers?

1. **Explainability.** A buyer only needs to hold **Pilot** (first useful outcome) vs **Operate** (everything after proof — analysis and governance/trust in one mental bucket).
2. **Time-to-value.** **Pilot** stays deliberately narrow so an operator can go from zero to a committed manifest in a single session without extra configuration.
3. **Cognitive load.** The operator shell still uses **two nav groups** under **Operate** (`operate-analysis` and `operate-governance`) for progressive disclosure and contributor seams — but the **buyer story** is a single **Operate** layer; **Execute+** rank reveals write affordances without a third product name.

For a pilot-success model tied to these layers, see **[PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md)**. For guidance on when to move between layers, see **[OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md)**. For the **canonical buyer narrative**, see **[EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md)**.

---

## What the layer model means today

The layer model describes several concepts that should not be confused:

### 1. Narrative packaging

Two layers explain **how to understand the product**:

- **Pilot** = first useful pilot result (request → run → commit → review)
- **Operate** = deeper investigation, governance, auditability, compliance, and trust — **Execute+** surfaces are revealed only when the caller’s rank satisfies **`ExecuteAuthority`** (same numeric floor as mutation soft-enable)

This section names and sequences layers for buyers. **Sponsor-level narrative** (why a pilot matters, what success sounds like, what not to claim) lives in **[EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md)**; this document stays the **capability inventory**—what ships where—so packaging detail does not replace the brief.

### Buyer vocabulary — explicit hybrid (V1 Pilot)

**Owner decision (2026-05-01):** **Architecture review** is the dominant buyer-facing phrase. **Explicit hybrid** means:

1. **Pilot surfaces (default path):** Use **architecture review** language for primary headings, hero CTAs, and nav labels that denote the Core Pilot workflow (start/list/review an architecture review).
2. **Technical spine:** Keep **run** where it matches the API, persisted identifiers, support bundles, logs, and precise diagnostics; show **Run ID** (or equivalent) in metadata or secondary text, not as the only hero label.
3. **Bridge copy:** In first-session or empty-state context, include **one plain sentence** that **each architecture review is tracked as one run** in the product so reviewers do not treat the two words as different objects.

**Scope:** Copy and presentation only — **do not** rename REST paths, DTO field names, or database entities for this rule.

### 2. UI progressive disclosure

The operator shell uses **progressive disclosure** so users do not see the full product surface by default.

- **Pilot** links are visible by default.
- **Operate · analysis** (`operate-analysis`) appears after **Show more links**.
- **Operate · governance** (`operate-governance`) surfaces deepen after extended or advanced disclosure.

- *(Operator preference)* sidebar **Navigation preset (UI only)** rotates focused vs fuller Pilot shortcuts using **`operator-nav-preset`** while keeping the tier / authority seams above authoritative.

This is the default user-experience model.

### 3. Role-based restriction

Some capabilities are better suited to operator/admin roles, especially in **Operate (governance and trust)**.

That means some surfaces are shaped not just by navigation tier but also by who should reasonably use them in a real environment.

**Implemented in the operator UI (first wave):** `archlucid-ui` composes **tier** (`nav-tier` / progressive disclosure) with per-link **`requiredAuthority`** on **Operate** nav groups in `nav-config.ts`, resolved from **`GET /api/auth/me`** via `current-principal.ts` and `nav-shell-visibility.ts` (see `archlucid-ui/README.md` § *Role-aware shaping*). **Pilot** essentials omit `requiredAuthority` so the default path stays visible; extended Pilot links (graph, compare, replay) set Read or Execute to match API policies. Short rank-aware copy appears on key Operate pages (`OperateCapabilityHints.tsx`). This is **operational accountability** in the shell—**not** the entitlement or pricing model in §4.

**Cognitive framing (V1):** **Operate** routes pair **LayerHeader** (`layer-guidance.ts`) with **short page leads**—often **inspect vs configure** language and first-pilot deferral—so read-heavy summaries are not visually equal to mutation forms. See `archlucid-ui/README.md` (*In-product guidance*).

#### Code seams (operator UI — maintenance map)

Keep **docs**, **`nav-config.ts`**, and **controller policies** aligned when routes move between layers or policy tiers:

| Buyer layer | `NAV_GROUPS[].id` (`archlucid-ui/src/lib/nav-config.ts`) | Primary modules |
|-------------|----------------------------------------------------------|-----------------|
| **Pilot** | `pilot` | `nav-tier.ts`; `requiredAuthority` **omitted** only on essentials; extended links set Read/Execute to match API |
| **Operate · analysis** | `operate-analysis` | Every link sets `requiredAuthority`; composed by `filterNavLinksForOperatorShell` (`nav-shell-visibility.ts`) |
| **Operate · governance** | `operate-governance` | Every link sets `requiredAuthority`; rank from `current-principal.ts`; **Execute-tier** in-page mutations use `operate-capability.ts` / `useOperateCapability()` (deprecated: `enterprise-mutation-capability.ts` / `useEnterpriseMutationCapability`) |

**Read vs Execute in the UI (numeric):** `AUTHORITY_RANK.ExecuteAuthority` (value **2**) is the shared threshold — **`callerRank < 2`** ⇒ **read tier** (nav may still show `ReadAuthority` destinations; **`useOperateCapability()`** is **false**). **`callerRank >= 2`** ⇒ **Execute+** for mutation soft-enable and operator-oriented rank cues. Example: **`/governance`** is **`ExecuteAuthority`** in `nav-config.ts` so Readers do not see it in nav; the API remains authoritative if they deep-link (nav shaping never implies POST success).

#### Two UI shaping surfaces (do not merge)

Contributors sometimes collapse **visibility** and **capability**; in code they stay **two** boundaries. All are **UI shaping only — API authoritative** (`401`/`403` from **ArchLucid.Api** still win on deep links and POSTs).

1. **Visibility** — `nav-config.ts` (`tier`, `requiredAuthority`) + **`nav-shell-visibility.ts`** (**tier → authority**, empty groups dropped) + **`useNavSurface().layerGuidance`** / **`LayerHeader`** (when to use; **`enterpriseFootnote`** marks **Operate · governance** rows for typography; **Execute+** rank cue on **`LayerHeader`** only when **`callerAuthorityRank >= ExecuteAuthority`**). Never soft-disables POST controls by itself.
2. **Capability** — **`useOperateCapability()`** / **`operate-capability.ts`** (Execute+ numeric floor; deprecated shim: **`useEnterpriseMutationCapability`**) + **`OperateCapabilityHints.tsx`** (inline rank / reader hints). Align with C# policies for the same buttons; **`true`** does not imply HTTP success. **`operate-authority-ui-shaping.test.tsx`** locks representative pages (**`disabled`** / **`readOnly`**).

Shell composition order: **tier first, then authority** (`filterNavLinksForOperatorShell`). **Hardening sequence:** [COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md) Stage 1 describes what shipped without entitlements.

##### Composed surface (preferred — `useNavSurface`)

The two shaping surfaces above are implemented by **`nav-shell-visibility.ts`**, **`enterprise-mutation-capability.ts`** / **`operate-capability.ts`**, **`layer-guidance.ts`**, and **`OperateCapabilityHints.tsx`** (deprecated re-export file: **`EnterpriseControlsContextHints.tsx`**). New operator routes **should** consume **Visibility** fields through **`useNavSurface(routeKey)`** (`archlucid-ui/src/lib/use-nav-surface.ts`) and **Capability** through **`useOperateCapability()`** where mutations are shown:

```ts
const surface = useNavSurface("policy-packs");
const canMutatePacks = surface.mutationCapability;
const layerBlock = surface.layerGuidance;
const navGroups = surface.links;
const navHint = surface.contextHints.enterpriseNavGroupHint;
```

`surface` returns `{ links, mutationCapability, layerGuidance, contextHints, callerAuthorityRank, showExtended, showAdvanced, mounted }`. The Execute floor used by `mutationCapability` is the same numeric floor used to branch every `contextHints.*` field (asserted by `archlucid-ui/src/lib/use-nav-surface.test.ts`), so a route can no longer drift between "button disabled" and "rank cue text" by accident. **`LayerHeader`** itself was migrated to `useNavSurface(pageKey)` so adding an **Operate** route only requires defining the `LayerGuidancePageKey` once and passing it through.

The composed hook is **UI shaping only** — `mutationCapability === true` does not imply HTTP success; `[Authorize(Policy = …)]` on **ArchLucid.Api** still returns 401/403.

**Cross-surface lock:** `archlucid-ui/src/lib/authority-seam-regression.test.ts` asserts **`operateCapabilityFromRank(rank)`** (deprecated alias **`enterpriseMutationCapabilityFromRank`**) matches **`ExecuteAuthority`** link visibility for ranks **0–3**, Auditors filter **Operate · governance** nav like Reader rank, and **`normalizeAuthMeResponse`** stays aligned with **`maxAuthorityRankFromMeClaims`**. **`archlucid-ui/src/lib/authority-execute-floor-regression.test.ts`** is a **narrower** guard on the same **Execute floor**: a synthetic **`ExecuteAuthority`** nav row’s visibility must equal the mutation boolean at every representative rank; **`archlucid-ui/src/lib/authority-shaped-ui-regression.test.ts`** walks **every** real **`nav-config`** **`ExecuteAuthority`** row at Read vs Execute rank (catches new catalog links without updating tests). **`operate-governance`** filtered link counts stay monotonic Read→Execute→Admin; Reader-filtered **Operate · governance** links include **`/alerts`** but not **`/governance`** (packaging ↔ **`nav-config`**). The same file locks **caller rank `0` vs `ReadAuthority`** nav (conservative bootstrap), **`/alerts`** on **`essential`** tier (default governance inbox strip), **stable ordering** after authority filters / in **`listNavGroupsVisibleInOperatorShell`** (sidebar composition), **`LAYER_PAGE_GUIDANCE`** rows with **`enterpriseFootnote`** (non-empty **`useWhen`**, **`firstPilotNote`**, footnote) vs **Operate · analysis** rows (**no** **`enterpriseFootnote`** — keeps **`LayerHeader`** rank-cue detection aligned with `layer-guidance`), **Operate** href **monotonicity** Read→Execute→Admin under **`filterNavLinksByAuthority`**, default Reader shell **Operate · analysis** = **`/ask`** only (tier-before-authority), and **`/governance`** only when **extended and advanced** are on for **Execute** rank. **`nav-shell-visibility.test.ts`** locks **tier → authority** order (Execute rank does not reveal **extended** Operate hrefs without disclosure toggles; **Pilot** extended **Execute** link **`/replay`** stays behind **Show more** at any rank). **`current-principal.test.ts`** locks **`hasEnterpriseOperatorSurfaces`** and **`maxAuthority`** (via **`requiredAuthorityFromRank(authorityRank)`**) to the same rules as **`nav-authority`**. **`LayerHeader.test.tsx`** requires every **Operate · governance** **`layer-guidance`** key that carries **`enterpriseFootnote`** to render the Execute rank cue and asserts the Operate **`aside`** **`aria-label`** (badge + headline). **`OperateCapabilityHints.authority.test.tsx`** locks rank-gated cues (governance resolution, audit log, **Alerts inbox**, **governance dashboard** reader line, alert tooling) at the **Execute** boundary. **`operate-authority-ui-shaping.test.tsx`** locks **`useOperateCapability()`** → **`disabled`** / **`readOnly`** on representative pages (policy packs **Create**, **Alert rules** **Create rule**, alerts triage **Confirm**, **Governance** submit **`#gov-submit-run`** / **`#gov-submit-version`**, **Governance resolution** **Change related controls** reader supplement vs mutation hook + **Refresh** not gated by mutation). **`nav-config.structure.test.ts`** locks packaging-shaped **nav-config** invariants (duplicate **`href`**s; **Pilot** essentials omit **`requiredAuthority`**; **`ExecuteAuthority`** links not on **`essential`** tier in **`operate-analysis`** and **`operate-governance`** groups).

#### Contributor drift guard (operator UI — keep packaging, nav, and API aligned)

**Rule:** the **API** (`ArchLucid.Api` `[Authorize(Policy = …)]` on controllers) is **authoritative** for 401/403. The operator UI only **shapes** visibility, copy, and soft-disabled controls from **`GET /api/auth/me`** so the default path stays honest.

When you **add or move** an operator route, touch these in order (skip only what does not apply):

1. **C#** — confirm policy names (`ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` in `ArchLucidPolicies`) match the lowest tier that should succeed for the page’s primary workflow.
2. **`archlucid-ui/src/lib/nav-config.ts`** — `tier`, `href`, `requiredAuthority` on the `NavLinkItem` (see file header **Authority** section). Stable **`NAV_GROUPS[].id`** values map to this document’s **Code seams** table above.
3. **Guidance strip** — for routes that should explain “which layer / when,” add or extend a key in **`archlucid-ui/src/lib/layer-guidance.ts`** and render **`LayerHeader`** on the page (`LayerGuidancePageKey` must match the route family).
4. **Operate write affordances** — if the page shows POST/toggle UI, keep **`useOperateCapability()`** (Execute+ rank; deprecated **`useEnterpriseMutationCapability()`**) aligned with the same policies as the buttons (see **`operate-capability.ts`**). If the page also shows rank-gated copy (**`OperateCapabilityHints`** / **`LayerHeader`** rank cue), confirm both **Capability** and **Visibility** seams stay intentional (see **Two UI shaping surfaces** above — e.g. governance resolution). Multi-step forms (e.g. governance workflow inline review) should keep **read-only** fields and reader notes when rank is below Execute so the shell matches nav omission even if UI state is visible. **Inspect-first layout** (e.g. **`flex-col-reverse`** on governance workflow / policy packs when mutation is off, triage deemphasis on alerts) is still **UI only** — extend **`authority-shaped-layout-regression.test.tsx`** when you add parallel column patterns.
5. **This document** — update capability / navigation rows in § Layer inventories when behavior is buyer-visible.

**Light regression tests** (Vitest, not snapshots): `nav-authority.test.ts` (includes Execute-link **`navLinkVisibleForCallerRank`** floor and unrestricted links at conservative rank **0**), `nav-shell-visibility.test.ts` (empty groups; default Reader **Operate · governance** strip = **`/alerts`** only when extended/advanced off; **Execute** caller still tier-limited; authority-only empty group; **Pilot `/replay`** tier gate), `current-principal.test.ts` (`/me` normalization; **`hasEnterpriseOperatorSurfaces`** vs mutation capability; **`maxAuthority`** vs **`requiredAuthorityFromRank`**), `enterprise-mutation-capability.test.ts`, `use-enterprise-mutation-capability.test.tsx`, `LayerHeader.test.tsx` (footnotes + Execute rank cue for every **`enterpriseFootnote`** **`layer-guidance`** key + conservative caller rank **0** + Operate **`aside`** **`aria-label`**), **`authority-seam-regression.test.ts`** (cross-module `/me` rank vs Operate nav vs mutation; Pilot essential hrefs for Reader; **ExecuteAuthority** rows in **`operate-analysis`** + **`operate-governance`** hidden from Read; Auditor vs Reader Operate href parity; **ReadAuthority** at rank **0**; **`/alerts`** **`essential`**; ordering; **`LAYER_PAGE_GUIDANCE`** footnote vs non-footnote rows), **`authority-execute-floor-regression.test.ts`** (Execute nav row **≡** mutation boolean; **`operate-governance`** monotonicity; Reader **`/governance`** omission), **`authority-shaped-ui-regression.test.ts`** (every **`NAV_GROUPS`** **`ExecuteAuthority`** link off at Read / on at Execute; mutation floor monotonicity **0–3**; empty-claims **`/me`** rank; shell bootstrap principals vs mutation flag), **`OperatorNavAuthorityProvider.test.tsx`** (JWT signed-in: **`useNavCallerAuthorityRank`** stays Read while `/me` refetches after rank had reached Execute; `/me` failure → Read), **`EnterpriseControlsReadRankHints.test.tsx`**, **`OperateCapabilityHints.authority.test.tsx`** (rank-gated `EnterpriseControlsExecutePageHint`, `EnterpriseExecutePlusPageCue`, nav group, alert tooling, **governance resolution**, **audit log**, **Alerts inbox**, **governance dashboard** reader cue), **`operate-authority-ui-shaping.test.tsx`** (**`useOperateCapability`** / deprecated **`useEnterpriseMutationCapability`** → Policy packs / **Alert rules** / Alerts / **Governance** submit field wiring), **`authority-shaped-layout-regression.test.tsx`** (inspect-first **`flex-col-reverse`**, alerts triage deemphasis, alert-routing inspect-before-toggle — **UI hierarchy**, not copy), **`nav-config.structure.test.ts`** (href dedupe; **Pilot** essentials omit **`requiredAuthority`**; **ExecuteAuthority** **`operate-analysis`** + **`operate-governance`** links not on **essential** tier), **`src/lib/deprecation-shims.test.ts`** (**`@deprecated`** TSDoc on public shims) — extend when you change rank, filtering, or page-level mutation gates.

This is the operational-usage model.

### 4. Future entitlement or pricing boundaries

The layer model is also the most likely foundation for future commercial packaging.

In V1, the layer model is still **not** the full commercial entitlement matrix (SKU ↔ every endpoint). **However**, **Operate (governance and trust)** and **Operate (analysis workloads)** HTTP surfaces are now **broadly** hard-gated on `dbo.Tenants.Tier` via `[RequiresCommercialTenantTier]`: **Standard** minimum covers governance and policy packs, governance resolution/preview, manifests, planning graph/comparison, authority compare/replay/exports/artifacts/run comparisons/provenance, advisory and digest schedules, learning/product-learning/recommendation-learning, evolution, retrieval/ask/conversations/explain, finding inspect/feedback, alerts and alert rules/routing/composite/tuning/simulation, pilot board pack and related PDFs, tenant cost/value/ROI/digest preferences/customer-success, notification and Teams integration preferences, operator diagnostics, Pilots **sponsor-one-pager** (`POST` action), and **tenant value report DOCX**. **Enterprise** minimum applies to **audit CSV export** (`ExportAudit` on `AuditController` only; other audit list/search routes are not tier-gated). Sub-tier tenants receive **HTTP 404 Not Found** with a generic RFC 9457 problem body (`ProblemTypes.ResourceNotFound`) so capabilities are not disclosed — see `ArchLucid.Api/Filters/CommercialTenantTierFilter.cs`. **Trial** seat/run limits still use **HTTP 402** (`TrialLimitFilter`). UI shaping remains separate; deep links still hit the API gate.

### Four boundary rules

| Boundary | What it controls | Failure mode |
|----------|------------------|--------------|
| **Commercial tier** | Whether the tenant has bought the product layer. | Sub-tier deep links return **404** to avoid capability disclosure. |
| **Authority / role** | Whether the caller may read, execute, or administer. | Missing role returns **401/403** from the API. |
| **Progressive disclosure** | Whether the operator shell shows the link by default. | Hidden links are a usability choice, not authorization. |
| **Trial limits** | Whether a trial has seats/runs left. | Trial exhaustion returns **402** with the trial-limit problem body. |

Buyer-facing copy must not imply that a visible UI link grants access. Contributor changes that add Operate routes should update the tier gate, API policy, navigation row, and seam tests together.

**Route ↔ tier ↔ policy ↔ nav table:** [ROUTE_TIER_POLICY_NAV_MATRIX.md](ROUTE_TIER_POLICY_NAV_MATRIX.md)

This is the future commercialization model.

For the future-state map, see **[FUTURE_PACKAGING_ENFORCEMENT.md](FUTURE_PACKAGING_ENFORCEMENT.md)**.

---

## Layer A — Pilot

> "AI-driven architecture request through committed manifest — visible, auditable, downloadable."

Every pilot starts here. The operator UI presents this layer by default with no progressive disclosure required. **Home**, **onboarding**, and **run detail** copy keep **Operate (analysis workloads)** and **Operate (governance and trust)** explicitly **optional to first-pilot proof** so deeper shaping does not widen the default mental model.

**Marketing proof (no operator install):** the public **`/demo/preview`** page (and **`GET /v1/demo/preview`**) shows a read-only commit-page projection of the latest committed **demo seed** run — complementing the operator-shell **`/demo/explain`** route, which focuses on provenance + citations side-by-side. See **`docs/DEMO_PREVIEW.md`**.

**Real-tenant proof / PoV packs:** before any run-sourced artifact crosses the customer trust boundary, assign a redaction profile from **`docs/library/PROOF_PACK_REDACTION_PROFILES.md`** (default external posture: **`customer-approved-external`** with written approver + checklist).

**Anti-creep rule:** **Pilot** is the default wedge and first buying motion. **Operate** should only be introduced when the next analytical or governance question actually requires it.

### Capability inventory

| Capability | API surface | UI surface | CLI surface |
|------------|-------------|------------|-------------|
| Public demo commit page (read-only) | `GET /v1/demo/preview` | Marketing `/demo/preview` | — |
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

Sidebar group label: **Pilot** (`pilot` — always visible — no disclosure toggle required).

Default links in the **Pilot** sidebar group: Home · Onboarding · New run · Runs. The **Alerts** inbox lives under **Operate · governance** (`operate-governance`; same tier/authority rules as other governance links).

### How to judge success

A strong **Pilot** result should demonstrate:

- faster movement from request to committed manifest,
- less manual packaging effort,
- and a cleaner path to reviewable artifacts.

Use **[PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md)** for the scorecard and suggested pilot metrics.

---

## Layer B — Operate

### B.1 — Analysis slice (`operate-analysis`)

> "Understand what changed, why it changed, and what the architecture looks like."

Available immediately after a first committed run. Enabled by clicking **Show more links** in the operator UI sidebar.

**Not required for first-pilot success:** this slice exists to answer deeper analytical questions after **Pilot** proves value.

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

Sidebar group label: **Operate** — analysis (`operate-analysis`; visible after **Show more links**).

Extended-tier links: Graph · Compare two runs · Replay a run · Advisory · Recommendation learning · Pilot feedback.

Advanced-tier links: Search · Planning · Evolution candidates · Schedules · Digests · Subscriptions.

---

### B.2 — Governance and trust slice (`operate-governance`)

> "Governance, auditability, compliance, and trust for architecture decisions at scale."

Available immediately but requiring extended/advanced sidebar disclosure and typically operator/admin role. Most governance features require explicit enablement per environment.

**Not required for first-pilot success:** this slice exists for governance, audit, policy, and operational trust questions after the **Pilot** wedge is already clear.

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
| Tenant value report (sponsor DOCX) | `POST /v1/value-report/{tenantId}/generate` | Value report (`/value-report`) + run detail “Generate sponsor report” | `ValueReport:Computation` |
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
| DPA template, subprocessors, SOC 2 roadmap | — | — | [go-to-market/TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md) |
| Customer-managed key (CMK) for SQL TDE | `infra/modules/sql-tde-cmk` | — | Azure Key Vault |
| Trial enforcement (seat and run limits) | `GET /v1/tenant/trial-status` | Trial banner in operator shell; sponsor banner may read **`firstCommitUtc`** for “Day N since first commit” | `ArchLucid:Trial:*` |
| Billing checkout | `POST /v1/tenant/billing/checkout` | Trial banner — Convert to paid | Stripe bridge |

### Navigation (operator UI)

Sidebar group label: **Operate** — governance (`operate-governance`; partially visible by default; fully surfaced after extended + advanced links).

Essential-tier links: Alerts (inbox).

Extended-tier links: Policy packs · Governance resolution · Governance dashboard.

Advanced-tier links: Alert rules · Alert routing · Composite rules · Alert simulation · Alert tuning · Governance workflow · Audit log · Value report.

---

## Progressive disclosure summary

| Sidebar state | What you see |
|--------------|-------------|
| **Default** (no toggles) | Pilot links + Alerts inbox + Ask |
| **Show more links** | + Graph · Compare · Replay · Advisory · Recommendation learning · Pilot feedback · Policy packs · Governance resolution · Governance dashboard |
| **Show more + Show advanced links** | + Search · Planning · Evolution candidates · Schedules · Digests · Alert rules · Routing · Composite rules · Simulation · Tuning · Governance workflow · Audit log · Value report |

The operator UI also adds **lightweight in-product hints** (sidebar captions under each group, a `LayerHeader` strip on key **Operate** routes, a post-checklist nudge on Home, and an optional post-commit strip on run detail) so operators can route by layer without re-reading this doc. See [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) for the full decision matrix.

---

## Packaging boundaries — what this document is NOT saying

- This is **not a licensing or entitlement document.** Both buyer layers are available in V1 to all licensed operators.
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
