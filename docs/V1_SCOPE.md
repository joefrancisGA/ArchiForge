# ArchLucid V1 — scope contract

**Audience:** product, engineering, pilots, and operators who need a single, decisive boundary for what “V1” means in **this repository**.

**Status:** Contract for the current codebase and docs. It describes what is **implemented and supportable today**, not a roadmap of net-new capabilities.

**Naming:** User-facing product name is **ArchLucid**. Application configuration uses **`ArchLucid*`** keys post–Phase 7; **historical** SQL migration filenames, some **RLS** object names, and **Terraform resource addresses** may still contain **`archiforge`** until follow-up work ([BREAKING_CHANGES.md](../BREAKING_CHANGES.md), [ARCHLUCID_RENAME_CHECKLIST.md](ARCHLUCID_RENAME_CHECKLIST.md)). See [GLOSSARY.md](GLOSSARY.md) and [README.md](../README.md).

---

## 1. What this document does

- States **what is in V1** (must work for a pilot).
- States **what is out of V1** (deferred, optional, or non-goals).
- Defines the **core operator happy path** and **minimum release checks** aligned with existing scripts and guides.

For deeper flow detail, use [ONBOARDING_HAPPY_PATH.md](ONBOARDING_HAPPY_PATH.md), [ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md), and [DUAL_PIPELINE_NAVIGATOR.md](DUAL_PIPELINE_NAVIGATOR.md).

**Deferred / exploratory inventory (doc-sourced):** [V1_DEFERRED.md](V1_DEFERRED.md) — consolidates partial stories so V1 does not read as open-ended.

---

## 2. In scope for V1

### 2.1 Run lifecycle: request → run → execute → commit

- Create a **run** from a structured **architecture request** (`POST /v1/architecture/request`).
- Drive the run through **execution** (e.g. `POST /v1/architecture/run/{runId}/execute`) so agent work completes under the configured **simulator or real** execution mode.
- **Commit** a **golden manifest** when the run is ready (`POST /v1/architecture/run/{runId}/commit`), with documented state and conflict behavior ([API_CONTRACTS.md](API_CONTRACTS.md)).
- The product supports both the **coordinator string-run** path and the **authority ingestion** path; both converge on **manifests, artifacts, and review** ([DUAL_PIPELINE_NAVIGATOR.md](DUAL_PIPELINE_NAVIGATOR.md)). V1 does not require pilots to master both internals—only to complete an end-to-end outcome they care about (committed manifest + artifacts).

### 2.2 Manifest and artifact review

- **API:** list and download manifest-scoped artifacts; export-related endpoints as documented in OpenAPI/Swagger.
- **CLI:** inspect runs and artifacts (e.g. `artifacts`, `status`) per [CLI_USAGE.md](CLI_USAGE.md).
- **Operator UI (`archlucid-ui`):** runs list, run detail, manifest summary, **artifact list, review, and download** ([operator-shell.md](operator-shell.md)).

### 2.3 Compare

- **Two-run** comparison (structured golden-manifest deltas and legacy diff surfaces) and persistence/replay of comparison records where the API supports it ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md), [COMPARISON_REPLAY.md](COMPARISON_REPLAY.md)).
- Operator UI: **Compare runs** workflow ([operator-shell.md](operator-shell.md)).

### 2.4 Replay

- **Comparison replay** (artifact vs regenerate vs verify modes) for persisted comparison records ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md)).
- **Run replay** (authority chain replay with validation surfaced in the operator shell) ([operator-shell.md](operator-shell.md)).

### 2.5 Graph

- **Knowledge / provenance / architecture graph** exploration for a **single run** in the operator UI ([operator-shell.md](operator-shell.md), [KNOWLEDGE_GRAPH.md](KNOWLEDGE_GRAPH.md)).

### 2.6 Export and package generation

- Build **exports** (e.g. Markdown/DOCX and related formats as implemented) and **replay** from persisted export records ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md)).
- **ZIP** downloads where the API and UI expose bundles ([operator-shell.md](operator-shell.md)).

### 2.7 Deployability

- **Container images** and **docker compose** profiles documented ([CONTAINERIZATION.md](CONTAINERIZATION.md)).
- **SQL Server** persistence via **DbUp** migrations on startup when using Sql storage ([SQL_SCRIPTS.md](SQL_SCRIPTS.md), [BUILD.md](BUILD.md)).
- **Health:** liveness and readiness endpoints; readiness reflects configured dependencies (e.g. SQL when not InMemory) ([README.md](../README.md)).
- **Version identity:** `GET /version` (and related health JSON) for support and release attribution.
- **Infrastructure-as-code** examples exist for Azure-oriented deployment (Terraform modules under `infra/`); exact production topology remains the customer’s responsibility within documented variables and runbooks.

### 2.8 Supportability

- **Correlation:** `X-Correlation-ID` (and related guidance) for log alignment ([API_CONTRACTS.md](API_CONTRACTS.md)).
- **CLI diagnostics:** `doctor`, **support bundle** (review before sharing) ([PILOT_GUIDE.md](PILOT_GUIDE.md), [CLI_USAGE.md](CLI_USAGE.md)).
- **Troubleshooting** and **runbooks** linked from [ARCHITECTURE_INDEX.md](ARCHITECTURE_INDEX.md) (e.g. [TROUBLESHOOTING.md](TROUBLESHOOTING.md)).

### 2.9 Pilot readiness

- Documented **minimum setup**, **first successful run** (Swagger or CLI), and **readiness scripts** ([PILOT_GUIDE.md](PILOT_GUIDE.md), [RELEASE_LOCAL.md](RELEASE_LOCAL.md), [RELEASE_SMOKE.md](RELEASE_SMOKE.md)).
- **Authentication modes** suitable for dev and production-style pilots: development bypass, JWT bearer, API key ([README.md](../README.md)).

### 2.10 Optional but real (still V1, not required for every pilot)

These exist in the repo and may be turned on per environment; they are **not** prerequisites for the core operator happy path:

- **Integration events** (optional Azure Service Bus, transactional outbox, worker consumer with logging handler) ([INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md)).
- **Webhooks** / digest delivery with CloudEvents envelope options (same doc).
- **Governance workflow** tables and APIs where enabled ([DATA_MODEL.md](DATA_MODEL.md), SQL migration history).
- **Alerts**, **advisory scans**, **retrieval indexing**, **Ask** threads — operational features documented elsewhere ([ALERTS.md](ALERTS.md), [ONBOARDING_HAPPY_PATH.md](ONBOARDING_HAPPY_PATH.md)).

---

## 3. Out of scope for V1 (explicit non-goals or V1.1+)

| Area | Rationale |
|------|-----------|
| **Advanced autonomous planning** | Agents are **orchestrated** with explicit tasks and execution modes; V1 does not promise open-ended self-directed multi-step planning beyond what the implemented pipelines already do. |
| **Broad event-bus integrations** | Optional publish/consume paths exist; V1 does **not** include a guaranteed catalog of enterprise integrations, mapping tools, or “any message bus” adapters. Custom consumers are customer-owned. |
| **VS Code (or IDE) shell integration** | No committed product surface for a VS Code–native operator experience; CLI and HTTP remain the primary integration points outside the web UI. |
| **Multi-region active/active product guarantees** | Documentation may describe **tier targets** and failover runbooks ([RTO_RPO_TARGETS.md](RTO_RPO_TARGETS.md)); V1 does not promise a fully specified multi-region SaaS topology out of the box. |
| **Speculative ecosystem** | Marketplace plugins, third-party agent stores, and similar ecosystem features are **not** V1 commitments. |
| **Full UI E2E against every live API configuration** | Playwright operator smoke may use **deterministic mocks**; passing it does not replace SQL-backed API validation ([RELEASE_SMOKE.md](RELEASE_SMOKE.md)). |

---

## 4. Core operator happy path (V1)

This is the shortest **human** journey the product is designed to support end-to-end:

1. **Configure** storage (typically **Sql**), connection string, and auth for the environment ([PILOT_GUIDE.md](PILOT_GUIDE.md)).
2. **Start** the API; confirm **live/ready** and note **version** for any ticket.
3. **Create a run** from a structured request (`POST /v1/architecture/request`).
4. **Execute** the run (`POST /v1/architecture/run/{runId}/execute`) and wait until the run is ready to commit (per API status and [API_CONTRACTS.md](API_CONTRACTS.md)).
5. **Commit** (`POST /v1/architecture/run/{runId}/commit`) to produce a **golden manifest** and **artifacts**.
6. **Review** artifacts in the **operator UI** or via **API/CLI** ([operator-shell.md](operator-shell.md)).
7. As needed: **compare** two runs, **replay** a comparison or authority chain, **explore the graph** for one run, and **export** or download packages.

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

---

**Change control:** When V1 boundaries shift, update **this file** first, then align [PILOT_GUIDE.md](PILOT_GUIDE.md) and [README.md](../README.md) so pilots do not see conflicting messages.
