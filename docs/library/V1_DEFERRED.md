> **Scope:** ArchLucid V1 — deferred and exploratory (doc inventory) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid V1 — deferred and exploratory (doc inventory)

**Audience:** product, pilots, and engineering leads who read scattered docs and need one **intentional** story: what is **shipped for V1** vs what is **explicitly not promised yet**.

**Relationship:** [V1_SCOPE.md](V1_SCOPE.md) defines the **V1 contract** (in scope, non-goals, happy path). **This file** lists areas that docs describe as **partial, follow-up, gap, or Phase-7-style cleanup** so nothing reads as an open-ended roadmap by accident.

**Rules:** No code changes implied here. Items are **documentation-sourced**; treat as **V1.1+ candidates or internal backlog** unless your program promotes them.

---

## 1. Product and learning (implemented storage, deferred “brains”)

| Item | Doc source | Note |
|------|------------|------|
| **First-tenant funnel SQL — retention / purge for `dbo.FirstTenantFunnelEvents`** | [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **40** (ii) | **V1:** **no** automatic row deletion and **no** scheduled prune job for this table. **V1.1:** owner picks retention window + purge or aggregate semantics (supersedes prior “90-day” draft wording for V1). |
| **Product learning — planning bridge** | [CHANGELOG.md](../CHANGELOG.md) §59R | SQL + APIs exist; **deterministic theme-derivation** and **plan-draft builder with priority score** are **intentionally deferred**. |
| **Cross-tenant analytics** | `docs/archive/CHANGE_SET_58R.md` | Aggregation stays **within** tenant/workspace/project unless a future change explicitly adds cross-tenant analytics. |

---

## 2. Compliance narrative: durable audit vs other stores

The [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md) **Known gaps** section currently tracks **zero** open durable-audit omissions for the previously listed mutating areas (analysis reports, export/comparison paths, conversations read-only note, governance via **`GovernanceWorkflowService`**). **2026-04-23:** demo trusted-baseline **`PersistCommittedChainAsync`** and replay commit paths also emit durable **`AuthorityCommittedChainPersisted`** (see matrix durable table). New routes should extend the matrix when **`AuditEventTypes`** grows.

| Area | Doc source |
|------|------------|
| Authority + coordinator + governance + exports + analysis + advisory + alerts + … | [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md) — durable audit table + **Known gaps** notes |

**V1 stance:** Governance workflow **does** dual-write to durable audit (see matrix). Baseline mutation logging remains log-only for some orchestration paths; operators rely on **`IAuditService`** rows in **Audit** UI for the durable channel.

---

## 3. Rename, keys, and platform cleanup (Phase 7)

Operational cleanup is **scheduled and gated**, not “unfinished V1 product.”

| Item | Doc source |
|------|------------|
| Remove legacy **ArchLucid** config / OIDC / env bridges; **ArchLucid.sql → ArchLucid.sql**; Terraform **state mv**; repo / workspace rename | [ARCHLUCID_RENAME_CHECKLIST.md](../ARCHLUCID_RENAME_CHECKLIST.md) **Phase 7** (requires explicit go-ahead); rationale for **7.5–7.8** deferral: [RENAME_DEFERRED_RATIONALE.md](RENAME_DEFERRED_RATIONALE.md). |

---

## 4. Operator experience and CI honesty

| Item | Doc source |
|------|------------|
| **Playwright** operator smoke may use **mocked** `/api/proxy`; it does not replace **SQL-backed** API + UI validation for a given release | [RELEASE_SMOKE.md](RELEASE_SMOKE.md), [V1_SCOPE.md](V1_SCOPE.md) §3 |
| **Audit search** keyset cursor uses **`OccurredUtc` with optional `EventId` tie-break** (`GET /v1/audit/search?beforeUtc=…&beforeEventId=…`); clients must pass both when continuing past same-second events | [AuditController.cs](../../ArchLucid.Api/Controllers/Admin/AuditController.cs), operator audit UI “Load more” |

---

## 5. Infrastructure and organizational polish

Docs describe **templates and gaps** that depend on **customer subscription and process**, not missing product code.

| Item | Doc source |
|------|------------|
| **ACR** / production image store, extending CI to **push** images | [CONTAINERIZATION.md](CONTAINERIZATION.md) |
| Subscription placement, naming, which Terraform roots to enable | Same doc — **organizational** follow-ups |

---

## 6. Atlassian documentation connector — Confluence remains V1.1 (Jira + ServiceNow → V1 as of 2026-05-05)

**Jira** and **ServiceNow** first-party connectors are **in scope for V1 GA** — see [V1_SCOPE.md](V1_SCOPE.md) §2.13 and [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) *Resolved 2026-05-05 (Jira + ServiceNow — promoted to V1 scope)*. They **no longer** appear as V1.1-deferred rows here; MVP semantics (incident vs issue, optional `cmdb_ci`, Jira two-way sync vs ServiceNow outbound-only unless promoted) match the prior §6 table text now mirrored in **V1_SCOPE** §2.13.

**Confluence** stays **explicitly pinned to V1.1** (owner decision 2026-04-24 — [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) Improvement 3 and historical item 11). It was originally listed in [go-to-market/INTEGRATION_CATALOG.md §2](../go-to-market/INTEGRATION_CATALOG.md) as `[Planned]`; pinning avoids open-ended “someday” messaging.

**V1 customer-owned bridges (optional alongside first-party connectors):** Power Automate and Logic Apps walkthroughs that use the same CloudEvents and HMAC contract as [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md) live under **[`docs/integrations/recipes/README.md`](../integrations/recipes/README.md)** — **ServiceNow** and **Jira** recipes remain useful for tenants that prefer Microsoft-automation planes or need interim coverage before connector enablement. Summary hub: [ITSM_BRIDGE_V1_RECIPES.md](ITSM_BRIDGE_V1_RECIPES.md).

| Connector | V1 posture | V1.1 commitment |
|-----------|------------|------------------|
| **Atlassian Confluence** — first-party connector (publish architecture findings or run summaries to a Confluence space; single fixed space key; API token / basic auth) | **Out of V1.** Customers integrate via **CloudEvents webhooks** or **REST API** to push content to Confluence during V1; Logic Apps recipe remains the Azure-first bridge. | **In scope for V1.1.** Minimum viable shape: publish finding summary pages to a single fixed `Confluence:DefaultSpaceKey`. OAuth 2.0 is a follow-on within the V1.1 release window if a buyer requests it. Design intent (space targeting + auth scheme) captured in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) Improvement 3 sub-decisions 3a and 3b. |

**Rules:**

- **Release-window** promises are **not** calendar dates unless [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) pins one.
- **First-party implementation priority:** **ServiceNow** **before** **Jira** for the **V1** ITSM connectors ([PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) *Resolved 2026-04-27* — scope superseded for Jira/ServiceNow only by *Resolved 2026-05-05*). **Confluence** follows in the **V1.1** window after V1 ITSM unless an owner entry reorders.
- New ITSM connectors **must not** widen this table without their own owner decision — **Azure DevOps Work Items** stays in `[Planned]` until separately promoted.
- Each connector **must** consume the same Authority-shaped event payloads the existing webhooks ship; no parallel finding-projection schema per ITSM target.

---

## 6a. Chat-ops connectors — V2 candidates (Resolved 2026-04-23)

| Connector | V1 / V1.1 posture | V2 commitment |
|-----------|--------------------|----------------|
| **Slack** — first-party connector (outbound notification sink with Adaptive Card-equivalent message blocks; aspirational in-Slack action affordances such as acknowledge / approve; Slack-app marketplace listing) | **Out of V1 and V1.1.** **Microsoft Teams** is the supported first-party chat-ops surface for both windows (see [`docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md`](../integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md)). Customers needing Slack during V1 / V1.1 integrate via **CloudEvents webhooks** or **REST API** and bridge to Slack themselves. | **In scope for V2.** Minimum viable shape is parity with the V1 Microsoft Teams connector: outbound notification sink driven by the same `EnabledTriggersJson` per-tenant opt-in matrix, secret material in **Azure Key Vault** with only a secret-name reference in SQL, and the same canonical event-type catalog (no parallel notification model). In-Slack action affordances are **stretch** for V2, not committed. |

**Rules:**

- V2 is a **release-window** promise, not a date — no calendar date is implied here.
- A new chat-ops surface **must not** be added to this table without its own owner decision (e.g. Discord, Mattermost stay at `[Planned]`).
- Slack must consume the same Authority-shaped event payloads the existing webhooks and the Microsoft Teams connector ship; no parallel notification schema per chat surface.

---

## 6b. Commercial — V1.1 candidates (Resolved 2026-04-23)

These commercial milestones are **explicitly release-window-pinned to V1.1** so V1 readiness is no longer measured against them. They were previously open obligations that quality assessments were treating as live V1 gaps; the 2026-04-23 owner decision in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) **Resolved 2026-04-23 (Reference-customer publication scope)** moves them into a named release window so external messaging — and internal scoring — stops reading them as V1 deficits.

| Commercial milestone | V1 posture | V1.1 commitment |
|----------------------|------------|------------------|
| **First named, public reference customer** — at least one row in [`docs/go-to-market/reference-customers/README.md`](../go-to-market/reference-customers/README.md) at `Status: Published`, with a published case study, customer-permissioned logo on the marketing site, and (where the customer agrees) measured ROI delta. Triggers re-rate of the **−15% reference discount** in [`PRICING_PHILOSOPHY.md` § 5.3](../go-to-market/PRICING_PHILOSOPHY.md). | **Out of V1.** No public reference customer is required for V1 GA. The reference-customer table stays alive (rows in `Placeholder` / `Customer review`) and the CI guard [`scripts/ci/check_reference_customer_status.py`](../../scripts/ci/check_reference_customer_status.py) **stays in `continue-on-error: true` warn-mode** for the entire V1 window — it does **not** flip to merge-blocking by V1 GA. V1 quality assessments **do not** charge points against Marketability, Proof-of-ROI Readiness, Differentiability, Trustworthiness, or Procurement Readiness for the absence of a `Published` row. | **In scope for V1.1.** The minimum V1.1 commitment is: at least **one** row at `Status: Published` with a customer-approved case study and a customer-permissioned logo. The `−15%` reference discount becomes a candidate for re-rate at V1.1 per `PRICING_PHILOSOPHY.md` § 5.3. Calendar date is **not** pinned by this decision; pinning a date requires a follow-up owner entry. |
| **Signed design partner engagement** — first design-partner agreement (or equivalent early-adopter commercial terms) under [`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md) / [`ORDER_FORM_TEMPLATE.md`](../go-to-market/ORDER_FORM_TEMPLATE.md), when the buyer accepts design-partner discount and case-study / reference-call deliverables | **Out of V1.** No signed or active design partner is required for V1 GA. V1 supports **sales-led pilots**, internal evaluation, and PLG-style trials without a formal design-partner contract. V1 quality assessments **do not** charge **`(A)`** points for Marketability, Proof-of-ROI, Stickiness, Differentiability, trust pillars, or pilot readiness solely because a design partner is not yet closed. **Do not** use “no design partner yet” as a recurring headline defect, improvement prerequisite, or default “open questions” batch unless the user **explicitly** asked for a GTM pipeline review — then treat as **`(B)` market-motion realism** only (zero weight on **`(A)`**). | **In scope for V1.1** as a commercial motion complementary to the published reference-customer row ([`reference-customers/README.md`](../go-to-market/reference-customers/README.md)). Calendar date is **not** pinned here; pinning requires [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md). |
| **Commerce un-hold (Stripe live keys flipped + Marketplace listing published)** — the external go-live of the self-serve transactability rails: (a) Stripe **live** API keys configured and the production webhook secret rotated (replacing TEST-mode keys for the production environment), (b) the Azure Marketplace SaaS offer transitioned to `Published` in Partner Center, with seller verification, payout account, and tax profile complete, (c) DNS cutover for `signup.archlucid.net` to the production Front Door custom domain. Until this milestone lands, the V1 commercial motion is **sales-led** — `/pricing` displays numbers, `ORDER_FORM_TEMPLATE.md` drives quote-to-cash, and the trial funnel runs in **Stripe TEST mode on staging** as a sales-engineer-led product evaluation (see Improvement 2 in [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](../archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) §3). | **Out of V1.** No live commerce un-hold is required for V1 GA. All wiring stays in place — `BillingStripeWebhookController`, `BillingMarketplaceWebhookController`, `BillingCheckoutController`, `BillingProductionSafetyRules`, `[RequiresCommercialTenantTier]` filter returning **402 Payment Required**, the Marketplace-alignment doc, the `/pricing` page, and the trial signup TEST-mode plumbing are V1-ready and V1-supported. **What is deferred is the act of flipping the live keys and publishing the Marketplace listing**, not the underlying engineering. V1 quality assessments **do not** charge points against Adoption Friction, Decision Velocity, or Commercial Packaging Readiness for the absence of live keys / a published listing. The trial funnel TEST-mode end-to-end work (Improvement 2) stays a live V1 obligation — only the "flip TEST → live" final gate is V1.1-deferred. | **In scope for V1.1.** Minimum V1.1 commitment: (a) Stripe live keys configured with production webhook secret rotated, (b) Marketplace SaaS offer at `Published` with seller verification + payout + tax profile complete, (c) DNS cutover for `signup.archlucid.net`, (d) the `BillingProductionSafetyRules` startup gate passes against the live configuration. Calendar date is **not** pinned by this decision. The Stripe-live-keys flip and the Marketplace `Published` state are both **owner-only** (Partner Center seller verification, tax profile, and payout account cannot be filed by the assistant). |

**Rules:**

- The CI guard's behaviour does **not** change in V1 — staying in warn-mode is the V1 contract. Flipping it to merge-blocking is a V1.1 task, not a V1 hardening task.
- The trial funnel TEST-mode end-to-end work (Improvement 2 in the open assessment) is **not** deferred — it is a V1 obligation and stays in §3 as actionable. Only the **owner-only flip to Stripe live keys** and the **Marketplace `Published` state** are V1.1-deferred.
- The `BillingProductionSafetyRules` startup guard (fails `ASPNETCORE_ENVIRONMENT=Production` when Stripe live key prefix `sk_live_` is configured without a webhook secret, or when Marketplace landing page URL is empty/localhost) stays **shipped in V1**. Its purpose is to make the V1.1 un-hold safe; do not remove it as part of the V1.1 work.
- Quality assessments produced **before** these decisions (e.g. [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](../archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) before its 2026-04-23 re-score addenda) charged points against V1 for these gaps. Future assessments **must not** — see that file's §0.2 (reference-customer) and §0.3 (commerce-un-hold) re-score addenda for the score adjustments applied on 2026-04-23.
- **Design partner (Resolved 2026-05-03):** the same rule applies — future assessments **must not** treat absence of a signed design partner as an **`(A)`** defect or premise repeated “you lack a design partner” narrative; see [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) *Resolved 2026-05-03*.
- These decisions do **not** retract or downgrade other commercial / security milestones — **third-party** pen-test summary publication is **V2** per §6c (owner 2026-05-01); PGP key generation, board-pack PDF endpoint, etc. stay as documented unless a separate owner decision defers them.
- A new commercial milestone **must not** be added to this table without its own owner decision recorded in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md).

---

## 6c. Security and assurance — SOC 2 CPA attestation (post–V1.1 gate), V1.1 (PGP), V2 (third-party pen test) (owner 2026-05-01; SOC scoring clarification 2026-05-03)

**SOC 2 CPA attestation:** A **CPA-issued SOC 2 Type I or Type II examination report** is **not** a **V1** or **V1.1** headline readiness milestone for weighted product assessments. **What is in V1:** honest trust-center rows, CAIQ/SIG/DPA/process templates, internally maintained SOC 2 **self-assessment** and roadmap (`docs/security/SOC2_SELF_ASSESSMENT_2026.md`, `docs/go-to-market/SOC2_ROADMAP.md`, [`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)). **Independent quality assessments must not** deduct **`(A)` V1.1 headline readiness** (see `.cursor/rules/Assessment-Scope-V1_1.mdc`) solely because CPA SOC 2 is not issued. **Still include SOC narrative** — under an explicit **procurement-realism / informational** section with **zero weight** on **`(A)`** — citing self-assessment, roadmap timing, and buyer friction honestly.

**V1 pen testing** is **owner-conducted** (tracked under [`2026-Q2-OWNER-CONDUCTED.md`](../security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md) and related self-assessment docs). There is **no** committed third-party assessor for V1. **Independent quality assessments must not** treat the absence of an **external** third-party pen test or redacted assessor summary as a V1 deficit.

**Third-party** pen-test execution, vendor SoW award, assessor deliverables, and optional Trust Center row refresh for that engagement are **V2** — use [`2026-Q2-SOW.md`](../security/pen-test-summaries/2026-Q2-SOW.md) and [`2026-Q2-REDACTED-SUMMARY.md`](../security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md) as procurement templates when planning V2.

| Assurance milestone | V1 posture | Release-window commitment |
|---------------------|------------|---------------------------|
| **SOC 2 Type I / II report (CPA-issued SOC examination)** — attestation tied to an independent CPA firm opinion; distinct from owner self-assessment. | **Out of V1** and **out of V1.1 as a headline scoring gate.** V1 posture is internally maintained **self-attested** SOC mapping + roadmap honesty + procurement templates (**do not imply** SOC 2 Type II is issued). **`(A)` assessments must not** treat absent CPA SOC 2 as a V1/V1.1 defect. Use **`(B)` procurement-realism`** (zero weight on **`(A)`**) when discussing enterprise RFP friction. | **Post–V1.1** until an explicit owner promotion and calendar decision — track narrative timing in [`SOC2_ROADMAP.md`](../go-to-market/SOC2_ROADMAP.md) and [`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md); pinning dates requires [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md). |
| **Third-party pen-test execution + redacted summary publication** — funded vendor engagement, SoW, assessor draft + final report, optional customer-shareable redacted summary and Trust Center metadata row. | **Out of V1** for **external** / third-party scope. V1 relies on **owner-conducted** testing + in-repo templates; see [`V1_SCOPE.md`](V1_SCOPE.md) §3. **Quality assessments:** do **not** penalize V1 readiness for lacking a third-party pen test. | **V2.** Minimum V2 commitment: select vendor, execute engagement, populate redacted summary working copy when applicable; update Trust Center when published. Calendar date is **not** pinned here. |
| **PGP key drop for `security@archlucid.net` (coordinated-disclosure key)** — owner generates the keypair (or designates a custodian), drops the public key block at `archlucid-ui/public/.well-known/pgp-key.txt`, references it from [`SECURITY.md`](../../SECURITY.md), and updates the marketing `/security` page in the **same single PR** (per owner Q14, 2026-04-23, sixth pass). | **Out of V1.** No PGP key drop required for V1 GA. The recipe at [`docs/security/PGP_KEY_GENERATION_RECIPE.md`](../security/PGP_KEY_GENERATION_RECIPE.md) stays in place; the CI guard that turns green when the key file appears stays in place. V1 quality assessments **do not** charge points against Trustworthiness for the absence of the PGP key. | **V1.1.** Minimum V1.1 commitment: public key block committed at `archlucid-ui/public/.well-known/pgp-key.txt`, `SECURITY.md` key-fingerprint reference updated, marketing `/security` page reference updated — all in the **same PR**. UID is gated on `archlucid.net` domain acquisition; default proposal is `ArchLucid Security <security@archlucid.net>`, but if the domain is never acquired the owner provides the alternate UID at V1.1 planning. Calendar date is **not** pinned by this decision. |

**Rules:**

- These are **release-window** promises, not dates. Pinning calendar dates requires a follow-up owner entry recorded in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md).
- **`(A)` / `(B)` scoring labels:** See `.cursor/rules/Assessment-Scope-V1_1.mdc` — **`(A)` V1.1 headline readiness must not drop** solely for absent **CPA SOC 2** or **ISO 27001 certification**; those belong in **`(B)` procurement realism** unless the user asks for an explicit blended score.
- V1 assessments **must not** penalize the solution for absent **third-party** pen-test publication; that work is **V2**. The **PGP** row remains **V1.1** as above. Pre-2026-05-01 docs that referenced a named third-party vendor for Q2 2026 are **superseded** by this owner decision where they conflict.
- These decisions do **not** retract or downgrade other V1 security obligations — owner-conducted testing and self-assessment, `BillingProductionSafetyRules`, RLS object-name discipline, OWASP ZAP baseline, Gitleaks, STRIDE-style threat modeling, audit-event coverage matrix, all remain V1 obligations.
- A new security or assurance milestone **must not** be added to this table without its own owner decision recorded in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md).

---

## 6d. Agent ecosystem / MCP — V1.1 candidates (scope documentation 2026-04-24)

This section **promotes MCP from backlog-only text to the named V1.1 release window**, aligned with [V1_SCOPE.md §3](V1_SCOPE.md) and the engineering intent in [`MCP_AND_AGENT_ECOSYSTEM_BACKLOG.md`](MCP_AND_AGENT_ECOSYSTEM_BACKLOG.md). It does **not** pin calendar dates; pinning dates still requires an owner entry in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md).

| MCP milestone | V1 posture | V1.1 commitment |
|-----------------|------------|-----------------|
| **Inbound MCP server (membrane)** — stdio and/or Streamable HTTP host process that registers **tenant-scoped, read-mostly** tools (`GetRunStatus`, manifest/provenance/governance summaries, artifact listing, audit slices, etc.) implemented as thin wrappers over **`ArchLucid.Application`** services; **SQL Server + RLS** remain authoritative; **typed audit** rows per tool class; **token / session caps** and **circuit breakers** consistent with existing LLM accounting patterns. | **Out of V1.** No MCP transport in the V1 shipping boundary; pilots and integrators use **REST**, **CLI**, and the **operator UI**. | **In scope for V1.1.** Minimum viable shape per [`MCP_AND_AGENT_ECOSYSTEM_BACKLOG.md`](MCP_AND_AGENT_ECOSYSTEM_BACKLOG.md) §5–§6 (façade projects, tool list, data-flow sketch). **Hard rule:** the authoritative solution **never** takes a compile-time dependency on MCP — the membrane is removable without changing business logic. |
| **Outbound MCP client (ArchLucid calls external tool servers)** | **Out of V1.** | **Out of V1.1** unless separately promoted — backlog default is **V2** with an explicit allowlist and approval-class mapping (see same backlog §5). |

**Rules:**

- Quality assessments **must not** treat absence of MCP as a V1 deficit after this alignment; MCP is a **V1.1** integration surface, not a pilot gate for V1 GA.
- Security posture for MCP matches **private API** assumptions in the backlog (no new public ports that violate the existing **private endpoint / WAF** story; no god-mode SQL principal).
- NuGet **MCP SDK** versioning remains **verify-at-implementation-time** per the backlog's uncertainty statement — pin only when the V1.1 engineering slice starts.

---

## 7. Engineering backlog (not a product roadmap)

| Item | Doc source |
|------|------------|
| Numbered refactors, test hygiene, doc tighten-ups | [NEXT_REFACTORINGS.md](NEXT_REFACTORINGS.md) |

This file is **maintainer hygiene**. It is **not** a commitment to ship listed items to pilots.

---

## 8. When to update this file

- After a changelog entry marks something **“intentionally deferred”** or **“gap.”**
- When **AUDIT_COVERAGE_MATRIX** gains or loses a **Known gaps** row.
- When **Phase 7** rename items move (only with program approval).

**Change control:** Prefer updating **this file** and [V1_SCOPE.md](V1_SCOPE.md) §3 together so external messaging stays aligned.
