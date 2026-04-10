# ArchLucid V1 — readiness summary

**Audience:** release owners, pilot leads, and executives who need a **short, honest** picture of where the repo stands for a **V1 / pilot** cut—not a marketing sheet.

**Basis:** This reflects **what the repository actually contains today** (code, docs, scripts, checklists). It does **not** certify a specific customer environment until you run your own gates.

---

## One-paragraph verdict

The codebase ships a **working V1-shaped product**: HTTP API, SQL persistence (DbUp), operator UI, CLI, health/version, support bundle, compare/replay/export surfaces, and documented pilot paths. **Operational completeness** (your deploy, auth, SQL, and recovery drills) is **your checklist**, not something the repo can sign for you. Remaining gaps are mostly **platform hygiene** (Terraform `state mv`, repo/Entra renames), **compliance/audit coverage** in specific flows, and **honesty about UI E2E** (Playwright often mock-backed).

---

## What is done (in-repo, supportable)

| Area | Evidence |
|------|-----------|
| **Core operator path** | Request → execute → commit → manifest/artifacts; documented in [V1_SCOPE.md](V1_SCOPE.md) §4, [PILOT_GUIDE.md](PILOT_GUIDE.md), [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md). |
| **Automation gates** | `run-readiness-check.ps1` (build + fast core + UI unit/build), `release-smoke.ps1` (optional full path with SQL + CLI quick run), `package-release.ps1` ([RELEASE_LOCAL.md](RELEASE_LOCAL.md), [RELEASE_SMOKE.md](RELEASE_SMOKE.md)). |
| **RC environment drill** | `v1-rc-drill.ps1` + [V1_RC_DRILL.md](V1_RC_DRILL.md): two runs, compare, authority replay, export ZIP, doctor, support bundle—against a **running** API. |
| **Diagnostics** | `GET /health/*`, `GET /version`, CLI `doctor`, `support-bundle` ([CLI_USAGE.md](CLI_USAGE.md), [TROUBLESHOOTING.md](TROUBLESHOOTING.md)). |
| **Breaking-change trail** | Phase 7 rename and config surface documented in [BREAKING_CHANGES.md](../BREAKING_CHANGES.md); integration events **canonical `com.archlucid.*` only**. |
| **Deploy artifacts** | Dockerfiles, compose profiles, Terraform modules under `infra/` ([CONTAINERIZATION.md](CONTAINERIZATION.md), [DEPLOYMENT_TERRAFORM.md](DEPLOYMENT_TERRAFORM.md)). |
| **Release checklist** | Actionable boxes in [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) (scope, deploy, health, flows, exports, recovery). |

---

## What is intentionally deferred

| Item | Why / pointer |
|------|----------------|
| **Terraform `state mv`** (Phase **7.5**) | Resource **addresses** may still contain `archiforge`; human-facing defaults moved where safe—coordinate with deploy window ([ARCHLUCID_RENAME_CHECKLIST.md](ARCHLUCID_RENAME_CHECKLIST.md)). |
| **GitHub repo rename, Entra apps, workspace path** (Phase **7.6–7.8**) | External / org coordination; checklist marks **deferred**. |
| **Product-learning “brains”** | Storage/API exist; deterministic theme derivation and plan-draft builder called out as deferred ([V1_DEFERRED.md](V1_DEFERRED.md) §1). |
| **Full audit parity** | Some mutating flows do not emit `dbo.AuditEvents`; documented as **known gaps** ([AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md), [V1_DEFERRED.md](V1_DEFERRED.md) §2). |
| **Multi-region SaaS guarantees** | Docs describe targets; not a boxed V1 product promise ([V1_SCOPE.md](V1_SCOPE.md) §3). |
| **Enterprise integration catalog** | Optional events/webhooks exist; custom consumers are customer-owned ([V1_SCOPE.md](V1_SCOPE.md) §3). |

---

## What risks remain

| Risk | Mitigation (in-repo) |
|------|----------------------|
| **Environment-specific failure** | Run [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) + [V1_RC_DRILL.md](V1_RC_DRILL.md) on **your** staging stack; capture `/version` and support bundle. |
| **Auth mismatch** | Scripts such as `v1-rc-drill.ps1` assume **DevelopmentBypass** unless you extend them; JWT/API key pilots must follow [README.md](../README.md). |
| **UI E2E vs live API** | Playwright operator smoke may use **mocks**; do not treat it as SQL-backed UI proof ([RELEASE_SMOKE.md](RELEASE_SMOKE.md)). |
| **DB / RLS legacy names** | Historical migrations and some **RLS object names** still reference older tokens; breaking-change doc lists them ([BREAKING_CHANGES.md](../BREAKING_CHANGES.md)). |
| **Compliance expectations** | If pilots need **audit UI parity** for every export path, read [AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md) before promising coverage. |

---

## What is good enough for pilot / V1

**Good enough** means: you can run the **documented happy path**, support it with **version + health + bundle**, and **not** promise deferred items above.

Minimum bar (already described in-repo):

1. **Release build** + agreed **Core** test filter ([TEST_STRUCTURE.md](TEST_STRUCTURE.md)).
2. **API up** on **Sql**; **DbUp** clean on fresh DB ([SQL_SCRIPTS.md](SQL_SCRIPTS.md)).
3. **One scripted E2E** (`release-smoke.ps1`) or equivalent manual path + **`v1-rc-drill.ps1`** on the target URL.
4. **Pilot docs** read ([PILOT_GUIDE.md](PILOT_GUIDE.md)) and **known issues** attached ([V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) §9).

If those pass **in the environment you hand off**, the repo is **aligned** with its own V1 contract ([V1_SCOPE.md](V1_SCOPE.md)). If they do not, the product is not “wrong”—the **environment or process** is not ready.

---

## What should be first after V1

Ordered by **typical leverage**, not mandatory roadmap:

1. **Phase 7.5–7.8** when org is ready: Terraform `state mv`, repo rename, Entra alignment, workspace conventions ([ARCHLUCID_RENAME_CHECKLIST.md](ARCHLUCID_RENAME_CHECKLIST.md)).
2. **Live API + operator UI** validation pass where Playwright mocks are insufficient (record outcome in release notes).
3. **Audit coverage** closes you care about for compliance ([AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md)).
4. **Product learning** deferred “brains” if pilots depend on planning bridge UX ([V1_DEFERRED.md](V1_DEFERRED.md) §1).
5. **Maintainer backlog** ([NEXT_REFACTORINGS.md](NEXT_REFACTORINGS.md))—engineering hygiene, not pilot-blocking by default.

---

## Related documents

| Doc | Use |
|-----|-----|
| [V1_SCOPE.md](V1_SCOPE.md) | Contract: in scope, out of scope, minimum release criteria |
| [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) | Executable gates before handoff |
| [V1_RC_DRILL.md](V1_RC_DRILL.md) | Staged API drill |
| [V1_DEFERRED.md](V1_DEFERRED.md) | Doc-sourced deferrals and gaps |
| [BREAKING_CHANGES.md](../BREAKING_CHANGES.md) | Phase 7 operator migration |

**Change control:** Update this file when **V1 boundaries** or **deferral reality** shifts; keep [V1_SCOPE.md](V1_SCOPE.md) the normative contract.
