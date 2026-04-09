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

Some mutating flows persist data but **do not** always emit a row in **`dbo.AuditEvents`** (operator **Audit** UI). That is a **known documentation gap**, not a hidden feature.

| Area | Doc source |
|------|------------|
| Analysis reports, some export/comparison paths, conversations, DOCX export surface, extra **GovernanceController** routes | [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md) — **Known gaps** table |

**V1 stance:** Governance workflow **does** dual-write to durable audit (see matrix). Other areas may use **separate tables/services**; closing the gap is **incremental hardening**, not a V1 blocker unless your compliance program says otherwise.

---

## 3. Rename, keys, and platform cleanup (Phase 7)

Operational cleanup is **scheduled and gated**, not “unfinished V1 product.”

| Item | Doc source |
|------|------------|
| Remove legacy **ArchiForge** config / OIDC / env bridges; **ArchiForge.sql → ArchLucid.sql**; Terraform **state mv**; repo / workspace rename | [ARCHLUCID_RENAME_CHECKLIST.md](ARCHLUCID_RENAME_CHECKLIST.md) **Phase 7** (requires explicit go-ahead) |

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
