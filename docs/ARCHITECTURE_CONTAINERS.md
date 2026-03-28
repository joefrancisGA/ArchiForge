## ArchiForge architecture (Containers)

This is a pragmatic C4 “containers” view: **deployable processes** and major libraries, with their responsibilities and relationships.

---

### Container: `ArchiForge.Api` (ASP.NET Core Web API)

**Responsibility**

- HTTP surface for all run/execution/export/compare/replay workflows.
- API versioning (`/v1/...`), rate limiting, and API-key auth.
- Wires up DI for Application/Data/DecisionEngine services.
- Provides Swagger/OpenAPI docs (with small operation filters for replay examples).

**Key concerns**

- Authentication: API key scheme (`ApiKeyAuthenticationHandler`)
- Authorization policies (claims-based):
  - `CanCommitRuns`, `CanSeedResults`
  - `CanReplayComparisons`, `CanViewReplayDiagnostics`
  - `CanExportConsultingDocx`
- Observability: OpenTelemetry tracing/metrics (config-driven) + structured logging (Serilog)
- Health endpoint: `GET /health`

**Depends on**

- `ArchiForge.Application` (core orchestration + analysis/export/replay services)
- `ArchiForge.Data` (repositories, migrations, DB connection factory)
- `ArchiForge.DecisionEngine` (merge agent results into manifests)
- `ArchiForge.Contracts` (DTOs / domain contracts)

---

### Container: `ArchiForge.Cli` (dotnet tool / CLI)

**Responsibility**

- Human- and script-friendly entry point for common workflows:
  - create projects (`new`)
  - dev infra (`dev up`)
  - run lifecycle (`run`, `status`, `submit`, `commit`, `seed`, `artifacts`)
  - comparisons library (`comparisons list/replay/drift/diagnostics/tag`)

**Depends on**

- `ArchiForge.Contracts` (types used for requests/results)
- HTTP calls to `ArchiForge.Api`

---

### Library: `ArchiForge.Application` (application services)

**Responsibility**

- Orchestrates core operations that the API exposes:
  - analysis report building
  - markdown/html/docx/pdf export generation
  - replay workflows (export replay and comparison replay)
  - drift analysis for replay verification

**Notable subsystems**

- Analysis report services and export services (Markdown/DOCX)
- End-to-end replay comparison: summary formatting + export generation (Markdown/HTML/DOCX/PDF)
- Comparison replay: load persisted record → rehydrate payload → export → optional verify/persist replay
- Export record diff comparison and export (Markdown + DOCX)

**Depends on**

- `ArchiForge.Data` for repositories
- `ArchiForge.DecisionEngine` (sometimes indirectly via other services)
- `ArchiForge.Contracts` for shared models

---

### Library: `ArchiForge.DecisionEngine` (merge + governance logic)

**Responsibility**

- Validates and merges `AgentResult` proposals into a single `GoldenManifest`.
- Applies governance defaults and required controls to relevant components.
- Applies decision nodes/evaluations (when present).

**Depends on**

- `ArchiForge.Contracts` (manifest model, decisions, agent results)

---

### Library: `ArchiForge.Data` (persistence)

**Responsibility**

- Database access (Dapper repositories) for runs, tasks, results, manifests, export records, comparison records, traces, evidence, etc.
- Migration scripts under `ArchiForge.Data/Migrations/*` applied by DbUp at startup.
- DB connection factory (`IDbConnectionFactory`) for **SQL Server** (`SqlConnectionFactory`).

**Depends on**

- `ArchiForge.Contracts` (persisted record DTOs)

---

### Library: `ArchiForge.Coordinator` (task generation / orchestration)

**Responsibility**

- Generates the set of agent tasks for a run.
- Coordinates the “run setup” phase (tasks created, initial status transitions).

---

### Library: `ArchiForge.AgentRuntime` / `ArchiForge.AgentSimulator`

**Responsibility**

- Agent runtime handlers implement agent-specific behavior and/or simulated outputs.
- Simulator mode produces deterministic results for repeatable local runs/tests.

---

### Library: `ArchiForge.Contracts`

**Responsibility**

- Shared types: requests/responses, manifests, metadata records, agent messages, enums.
- Keeps API/Application/Data aligned on payload shapes.

---

### Container relationships (high-level)

- `ArchiForge.Cli` → (HTTP) → `ArchiForge.Api`
- `ArchiForge.Api` → `ArchiForge.Application` → `ArchiForge.Data`
- `ArchiForge.Api` → `ArchiForge.DecisionEngine` (merge/commit)
- All projects share models from `ArchiForge.Contracts`

---

### Where to go next

- Components: `docs/ARCHITECTURE_COMPONENTS.md`
- Flows: `docs/ARCHITECTURE_FLOWS.md`
- Data model: `docs/DATA_MODEL.md`

