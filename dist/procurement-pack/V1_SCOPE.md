> **Scope:** ArchLucid V1 — scope contract - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid V1 — scope contract

**Audience:** product, engineering, pilots, and operators who need a single, decisive boundary for what "V1" means in **this repository**.

**Status:** Contract for the current codebase and docs. It describes what is **implemented and supportable today**, not a roadmap of net-new capabilities.

**Naming:** User-facing product name is **ArchLucid**. Application configuration uses **`ArchLucid*`** keys post–Phase 7; **historical** SQL migration filenames, some **RLS** object names, and **Terraform resource addresses** may still contain **`archlucid`** until follow-up work ([BREAKING_CHANGES.md](../BREAKING_CHANGES.md), [ARCHLUCID_RENAME_CHECKLIST.md](ARCHLUCID_RENAME_CHECKLIST.md)). See [GLOSSARY.md](GLOSSARY.md) and [README.md](../README.md).

**Architecture poster:** [ARCHITECTURE_ON_ONE_PAGE.md](ARCHITECTURE_ON_ONE_PAGE.md) · **Operator atlas:** [OPERATOR_ATLAS.md](OPERATOR_ATLAS.md)

---

## 1. What this document does

- States **what is in V1** (must work for a pilot).
- States **what is out of V1** (deferred, optional, or non-goals).
- Defines the **core operator happy path** and **minimum release checks** aligned with existing scripts and guides.

For deeper flow detail, use [ONBOARDING_HAPPY_PATH.md](ONBOARDING_HAPPY_PATH.md), [ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md), and [CANONICAL_PIPELINE.md](CANONICAL_PIPELINE.md).

**Deferred / exploratory inventory (doc-sourced):** [V1_DEFERRED.md](V1_DEFERRED.md) — consolidates partial stories so V1 does not read as open-ended.

---

## 2. In scope for V1 — organized by product layer

V1 capabilities map to three product layers. See [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) for the full inventory, [CORE_PILOT.md](CORE_PILOT.md) for the first-pilot walkthrough, and [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) for when to stay in Core Pilot versus move to Advanced Analysis or Enterprise Controls.

---

### Layer 1 — Core Pilot

The minimum set every pilot must complete. Delivered by default; no additional configuration beyond API + SQL.

#### 2.1 Run lifecycle: request → execute → commit

- Create a **run** from a structured **architecture request** (`POST /v1/architecture/request`).
- Drive the run through **execution** so agent work completes under the configured **simulator or real** execution mode.
- **Commit** a **golden manifest** (`POST /v1/architecture/run/{runId}/commit`), with documented state and conflict behavior ([API_CONTRACTS.md](API_CONTRACTS.md)).
- Both the **architecture request** path and the **ingestion-backed** path converge on manifests, artifacts, and review ([CANONICAL_PIPELINE.md](CANONICAL_PIPELINE.md)).

#### 2.2 Manifest and artifact review

- **API:** list and download manifest-scoped artifacts; export-related endpoints per OpenAPI/Swagger.
- **CLI:** `artifacts`, `status` per [CLI_USAGE.md](CLI_USAGE.md).
- **Operator UI:** runs list, run detail, manifest summary, artifact review, and download ([operator-shell.md](operator-shell.md)).

#### 2.3 Export and package generation

- **Markdown/DOCX** exports and **replay** from persisted export records ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md)).
- **ZIP** downloads (bundle and run-export) from run detail.

#### 2.4 Deployability and supportability

- **Container images** and **docker compose** profiles ([CONTAINERIZATION.md](CONTAINERIZATION.md)).
- **SQL Server** persistence via DbUp migrations; automatic on startup ([SQL_SCRIPTS.md](SQL_SCRIPTS.md)).
- **Health:** `/health/live`, `/health/ready`, `/health`; `GET /version` for support attribution.
- **Correlation IDs**, **CLI diagnostics** (`doctor`, `support-bundle`), and **Troubleshooting** runbooks.
- **Authentication modes:** development bypass, JWT bearer, API key ([README.md](../README.md)).
- **Infrastructure-as-code** examples (Terraform modules under `infra/`).

---

### Layer 2 — Advanced Analysis

Deeper investigation and comparison tools. Available once you have at least one committed run. In the operator UI, enable via **Show more links** in the sidebar.

#### 2.5 Compare

- **Two-run** comparison: structured golden-manifest deltas + legacy diff + optional AI explanation ([COMPARISON_REPLAY.md](COMPARISON_REPLAY.md)).
- Operator UI: **Compare runs** workflow ([operator-shell.md](operator-shell.md)).

#### 2.6 Replay

- **Comparison replay** (artifact vs regenerate vs verify modes) for persisted comparison records.
- **Run replay** (authority chain re-validation) with validation flags surfaced in the operator shell.

#### 2.7 Graph

- **Knowledge / provenance / architecture graph** for a single run in the operator UI ([KNOWLEDGE_GRAPH.md](KNOWLEDGE_GRAPH.md)).

#### 2.8 Advisory, Q&A, and pilot signals

- **Ask** — natural-language queries against architecture context.
- **Advisory scans** — architecture digests and scheduled scans.
- **Pilot feedback** — rollup and triage of product learning signals.
- **Recommendation learning** — learning profiles per run.
- **Integration events** (optional Azure Service Bus, CloudEvents envelope, webhooks) ([INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md)).

Use this layer when the next question is analytical: what changed, why it changed, what the architecture or provenance graph shows, or how two runs differ.

---

### Layer 3 — Enterprise Controls

Governance, auditability, and compliance tooling. Configuration-driven; most features require explicit enablement per environment. Full surface visible after enabling extended/advanced links in the sidebar.

#### 2.9 Governance workflows

- **Approval workflow** with segregation of duties (self-approval blocked), SLA tracking, and webhook escalation on breach.
- **Pre-commit governance gate** — `ArchLucid:Governance:PreCommitGateEnabled` blocks manifest commit when findings exceed configured severity thresholds ([PRE_COMMIT_GOVERNANCE_GATE.md](PRE_COMMIT_GOVERNANCE_GATE.md)).
- **Policy packs** — versioned rule sets with scope assignments and effective governance resolution.
- **Governance dashboard** — cross-run pending approvals and policy change summary.

#### 2.10 Audit and compliance

- **78 typed audit events** in an append-only SQL store with CSV export ([AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md)).
- **Audit log** — filter by event type, actor, run ID, correlation ID, time window.
- **Row-level security (RLS)** — SQL `SESSION_CONTEXT` tenant isolation ([security/MULTI_TENANT_RLS.md](security/MULTI_TENANT_RLS.md)).
- **Compliance drift trend** — tracking and operator UI chart.

#### 2.11 Alerts

- **Alert rules, routing, composite rules, tuning** — configurable alert pipeline.
- **Alert inbox** — open and acknowledged alerts with correlation to runs and manifests.
- **Alert simulation** — evaluate rules against synthetic payloads.

#### 2.12 Trust and access

- **Entra ID / JWT bearer, API key, RBAC roles** (Admin / Operator / Reader / Auditor).
- **Private endpoints** and WAF Terraform modules; no SMB/445 public exposure.
- **DPA template, subprocessors register, SOC 2 roadmap** ([go-to-market/TRUST_CENTER.md](go-to-market/TRUST_CENTER.md)).

Use this layer when the next question is governance or trust: approvals, policy enforcement, audit evidence, compliance drift, alerts, or operational control.

---

## 3. Out of scope for V1 (explicit non-goals or V1.1+)

| Area | Rationale |
|------|-----------|
| **Advanced autonomous planning** | Agents are **orchestrated** with explicit tasks and execution modes; V1 does not promise open-ended self-directed multi-step planning beyond what the implemented pipelines already do. |
| **Broad event-bus integrations** | Optional publish/consume paths exist; V1 does **not** include a guaranteed catalog of enterprise integrations, mapping tools, or "any message bus" adapters. Custom consumers are customer-owned. |
| **VS Code (or IDE) shell integration** | No committed product surface for a VS Code–native operator experience; CLI and HTTP remain the primary integration points outside the web UI. |
| **Multi-region active/active product guarantees** | Documentation may describe **tier targets** and failover runbooks ([RTO_RPO_TARGETS.md](RTO_RPO_TARGETS.md)); V1 does not promise a fully specified multi-region SaaS topology out of the box. |
| **Speculative ecosystem** | Marketplace plugins, third-party agent stores, and similar ecosystem features are **not** V1 commitments. |
| **Full UI E2E against every live API configuration** | Playwright operator smoke may use **deterministic mocks**; passing it does not replace SQL-backed API validation ([RELEASE_SMOKE.md](RELEASE_SMOKE.md)). |
| **Net-new public HTTP routes that extend only the Coordinator repository family** | After [ADR 0021](adr/0021-coordinator-pipeline-strangler-plan.md) acceptance, new externally-visible surfaces must converge on Authority semantics (or go through the unified read façade) — do not add coordinator-only endpoints without an explicit superseding ADR. |

---

## 4. Core operator happy path (V1)

### 4.1 Core Pilot path — start here

The **Core Pilot path** is the minimum journey every pilot must complete. It maps 1:1 to the **Core Pilot checklist** on the operator UI Home page and to the four steps in [CORE_PILOT.md](CORE_PILOT.md):

1. **Configure** storage (typically **Sql**), connection string, and auth for the environment ([PILOT_GUIDE.md](PILOT_GUIDE.md)).
2. **Start** the API; confirm **live/ready** and note **version** for any ticket.
3. **Create a run** from a structured request (`POST /v1/architecture/request`) — use the seven-step wizard in the operator UI or the CLI.
4. **Execute** the run and wait until it is ready to commit (coordinator-driven; watch the pipeline timeline in the UI or poll the API).
5. **Commit** (`POST /v1/architecture/run/{runId}/commit`) to produce a **golden manifest** and **artifacts**.
6. **Review** the manifest and artifacts in the operator UI (run detail → Artifacts table) or via API/CLI ([operator-shell.md](operator-shell.md)).

This is the complete first-pilot deliverable. Nothing beyond step 6 is required to call a pilot successful.

### 4.2 Advanced Analysis (available but not required for the Core Pilot)

Enable these once you have at least one committed run. In the operator UI, click **Show more links** in the sidebar.

- **Compare** two runs (`/compare`) — structured manifest deltas + legacy diff.
- **Replay** a run (`/replay`) — re-validate the authority chain and surface drift flags.
- **Graph** (`/graph`) — visual provenance or architecture graph for a single run ID.
- **Export** — download bundle ZIP and run-export ZIP from run detail → Artifacts.

Use these when the next question is analytical rather than operational: what changed, why it changed, or how to inspect the result more deeply.

### 4.3 Enterprise Controls (available but not required for the Core Pilot)

Enable extended and advanced links in the sidebar to surface the full Enterprise Controls surface.

- **Governance** — approval workflows, policy packs, pre-commit gate, governance dashboard.
- **Audit** — append-only audit log, CSV export, compliance drift tracking.
- **Alerts** — rules, routing, composite rules, simulation, tuning.

Use these when the next question is governance or trust: approvals, policy enforcement, audit, compliance, or operational control.

Optional: run **readiness** or **release-smoke** before a demo ([PILOT_GUIDE.md](PILOT_GUIDE.md), [RELEASE_SMOKE.md](RELEASE_SMOKE.md)).

---

## 5. Minimum release criteria (V1)

These are **practical gates** already encoded or described in-repo—not an exhaustive test matrix.

| Criterion | Evidence in repo |
|-----------|------------------|
| **Solution builds** in Release | CI and [BUILD.md](BUILD.md) |
| **Core-tier tests** pass for the agreed filter (e.g. fast core / `Suite=Core` conventions) | [TEST_STRUCTURE.md](TEST_STRUCTURE.md), [RELEASE_SMOKE.md](RELEASE_SMOKE.md) |
| **API starts** against Sql configuration; **health/live** and **health/ready** succeed when dependencies are up | [README.md](../README.md), [PILOT_GUIDE.md](PILOT_GUIDE.md) |
| **One scripted end-to-end run** produces a committed manifest and **at least one** artifact descriptor | `release-smoke.ps1` expectations ([RELEASE_SMOKE.md](RELEASE_SMOKE.md)) |
| **Operator UI** builds when Node is in use; Vitest/build steps as per readiness scripts | [RELEASE_SMOKE.md](RELEASE_SMOKE.md), [archlucid-ui/README.md](../archlucid-ui/README.md) |
| **Version and diagnostics** available for handoff (`GET /version`, CLI `doctor`, support bundle discipline) | [PILOT_GUIDE.md](PILOT_GUIDE.md) |

**Not required** for every internal build: Playwright E2E, full integration matrix, performance benchmarks, or full Terraform apply to a live subscription—unless your program explicitly adds them as release gates.

---

## 6. Related documents

| Doc | Use |
|-----|-----|
| [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) | **Three-layer capability inventory:** Core Pilot · Advanced Analysis · Enterprise Controls |
| [CORE_PILOT.md](CORE_PILOT.md) | First-pilot walkthrough (4 steps) |
| [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) | Practical guide for which layer to use next and what can be ignored for now |
| [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) | Actionable pre-handoff checklist (scope freeze, deploy, health, operator flow, exports, recovery) |
| [V1_DEFERRED.md](V1_DEFERRED.md) | Doc inventory: V1.1+ candidates, audit gaps, Phase 7 rename, infra polish, maintainer backlog |
| [PILOT_GUIDE.md](PILOT_GUIDE.md) | Pilot onboarding narrative |
| [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) | Command-oriented operator entry |
| [operator-shell.md](operator-shell.md) | UI workflows and API expectations |
| [ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md) | Export, comparison, replay sequences |
| [API_CONTRACTS.md](API_CONTRACTS.md) | HTTP behavior and policy references |
| [RELEASE_SMOKE.md](RELEASE_SMOKE.md) | Scripted smoke scope and limits |
| [V1_RC_DRILL.md](V1_RC_DRILL.md) | RC drill: full operator path against a running API (`v1-rc-drill.ps1`) |
| [V1_READINESS_SUMMARY.md](V1_READINESS_SUMMARY.md) | Short honest snapshot: done, deferred, risks, pilot bar, post-V1 priorities |
| [V1_REQUIREMENTS_TEST_TRACEABILITY.md](V1_REQUIREMENTS_TEST_TRACEABILITY.md) | Lightweight map from this scope doc to tests, scripts, and data-consistency runbooks |

---

**Change control:** When V1 boundaries shift, update **this file** first, then align [PILOT_GUIDE.md](PILOT_GUIDE.md) and [README.md](../README.md) so pilots do not see conflicting messages.
