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
| **Audit search** keyset cursor uses **`OccurredUtc` only**; tie-breaking for identical timestamps is a known limitation for very large logs | Implementation note (API + UI); treat **EventId tie-break** as a future refinement if needed |

---

## 5. Infrastructure and organizational polish

Docs describe **templates and gaps** that depend on **customer subscription and process**, not missing product code.

| Item | Doc source |
|------|------------|
| **ACR** / production image store, extending CI to **push** images | [CONTAINERIZATION.md](CONTAINERIZATION.md) |
| Subscription placement, naming, which Terraform roots to enable | Same doc — **organizational** follow-ups |

---

## 6. ITSM connectors — V1.1 candidates (Resolved 2026-04-23)

These items are **explicitly promoted to V1.1** rather than left as open-ended "Planned" rows. They were originally listed in [go-to-market/INTEGRATION_CATALOG.md §2](../go-to-market/INTEGRATION_CATALOG.md) as `[Planned]`; the 2026-04-23 owner decisions in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) **Resolved 2026-04-23 (Jira connector scope)** and **Resolved 2026-04-23 (ServiceNow + Slack connector scope)** move them into a named release window so external messaging stops reading as "someday".

| Connector | V1 posture | V1.1 commitment |
|-----------|------------|------------------|
| **Atlassian Jira** — first-party connector (issue create from findings + bi-directional status sync, OAuth 2.0 / API token auth, Atlassian-app marketplace listing) | **Out of V1.** Customers integrate via **CloudEvents webhooks** or **REST API** if Jira workflow is required during V1. | **In scope for V1.1.** Specific surface to be sized inside the V1.1 planning ADR; minimum viable shape is one-way: finding → Jira issue with correlation back-link. Two-way status sync is part of the same V1.1 commitment but may ship as a fast-follow within the V1.1 release window. |
| **ServiceNow** — first-party connector (incident create from findings, optional `cmdb_ci` mapping, OAuth 2.0 / basic-auth, ServiceNow Store listing) | **Out of V1.** Customers integrate via **CloudEvents webhooks** or **REST API** if ServiceNow workflow is required during V1. | **In scope for V1.1.** Minimum viable shape is one-way: finding → ServiceNow `incident` with correlation back-link. **Open V1.1-planning question (do not assume in V1.1 ADR):** whether the same release also ships `cmdb_ci` mapping, or whether `cmdb_ci` ships as a V1.1 fast-follow. Two-way status sync (ServiceNow → ArchLucid) is **not** committed for V1.1 unless an explicit owner decision adds it. |

**Rules:**

- The V1.1 commitment is a **release-window** promise, not a date. Pinning a calendar date requires an owner decision recorded in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md).
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

**Rules:**

- The CI guard's behaviour does **not** change in V1 — staying in warn-mode is the V1 contract. Flipping it to merge-blocking is a V1.1 task, not a V1 hardening task.
- Quality assessments produced **before** this decision (e.g. [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](../QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) before its 2026-04-23 re-score addendum) charged points against V1 for this gap. Future assessments **must not** — see that file's §0.2 *Reference-customer-deferral re-score addendum* for the score adjustments applied on 2026-04-23.
- This decision does **not** retract or downgrade any other commercial milestone — Marketplace listing live, Stripe live keys flipped, executed pen test summary publication, etc. all stay as live V1 obligations unless a separate owner decision defers them.
- A new commercial milestone **must not** be added to this table without its own owner decision recorded in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md).

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
