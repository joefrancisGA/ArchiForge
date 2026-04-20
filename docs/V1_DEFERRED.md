> **Scope:** ArchLucid V1 — deferred and exploratory (doc inventory) - full detail, tables, and links in the sections below.

# ArchLucid V1 — deferred and exploratory (doc inventory)

**Audience:** product, pilots, and engineering leads who read scattered docs and need one **intentional** story: what is **shipped for V1** vs what is **explicitly not promised yet**.

**Relationship:** [V1_SCOPE.md](V1_SCOPE.md) defines the **V1 contract** (in scope, non-goals, happy path). **This file** lists areas that docs describe as **partial, follow-up, gap, or Phase-7-style cleanup** so nothing reads as an open-ended roadmap by accident.

**Rules:** No code changes implied here. Items are **documentation-sourced**; treat as **V1.1+ candidates or internal backlog** unless your program promotes them.

---

## 1. Product and learning (implemented storage, deferred “brains”)

| Item | Doc source | Note |
|------|------------|------|
| **Product learning — planning bridge** | [CHANGELOG.md](CHANGELOG.md) §59R | SQL + APIs exist; **deterministic theme-derivation** and **plan-draft builder with priority score** are **intentionally deferred**. |
| **Cross-tenant analytics** | `docs/archive/CHANGE_SET_58R.md` | Aggregation stays **within** tenant/workspace/project unless a future change explicitly adds cross-tenant analytics. |

---

## 2. Compliance narrative: durable audit vs other stores

The [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md) **Known gaps** section currently tracks **zero** open durable-audit omissions for the previously listed mutating areas (analysis reports, export/comparison paths, conversations read-only note, governance via **`GovernanceWorkflowService`**). New routes should extend the matrix when **`AuditEventTypes`** grows.

| Area | Doc source |
|------|------------|
| Authority + coordinator + governance + exports + analysis + advisory + alerts + … | [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md) — durable audit table + **Known gaps** notes |

**V1 stance:** Governance workflow **does** dual-write to durable audit (see matrix). Baseline mutation logging remains log-only for some orchestration paths; operators rely on **`IAuditService`** rows in **Audit** UI for the durable channel.

---

## 3. Rename, keys, and platform cleanup (Phase 7)

Operational cleanup is **scheduled and gated**, not “unfinished V1 product.”

| Item | Doc source |
|------|------------|
| Remove legacy **ArchLucid** config / OIDC / env bridges; **ArchLucid.sql → ArchLucid.sql**; Terraform **state mv**; repo / workspace rename | [ARCHLUCID_RENAME_CHECKLIST.md](ARCHLUCID_RENAME_CHECKLIST.md) **Phase 7** (requires explicit go-ahead); rationale for **7.5–7.8** deferral: [RENAME_DEFERRED_RATIONALE.md](RENAME_DEFERRED_RATIONALE.md). |

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

## 6. Engineering backlog (not a product roadmap)

| Item | Doc source |
|------|------------|
| Numbered refactors, test hygiene, doc tighten-ups | [NEXT_REFACTORINGS.md](NEXT_REFACTORINGS.md) |

This file is **maintainer hygiene**. It is **not** a commitment to ship listed items to pilots.

---

## 7. When to update this file

- After a changelog entry marks something **“intentionally deferred”** or **“gap.”**
- When **AUDIT_COVERAGE_MATRIX** gains or loses a **Known gaps** row.
- When **Phase 7** rename items move (only with program approval).

**Change control:** Prefer updating **this file** and [V1_SCOPE.md](V1_SCOPE.md) §3 together so external messaging stays aligned.
