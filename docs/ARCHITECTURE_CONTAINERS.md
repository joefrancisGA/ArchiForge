## ArchiForge architecture (Containers)

This is a pragmatic C4 “containers” view: **deployable processes** and major libraries, with their responsibilities and relationships.

---

### Container: `ArchiForge.Api` (ASP.NET Core Web API)

**Responsibility**

- HTTP surface for all run/execution/export/compare/replay workflows.
- API versioning (`/v1/...`), rate limiting, and API-key auth.
- Wires up DI for Application, Data, DecisionEngine, Decisioning, Persistence, Retrieval, ContextIngestion, and related services.
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
- `ArchiForge.Data` (Dapper repositories for run/commit path, migrations, `IDbConnectionFactory`)
- `ArchiForge.Persistence` (SQL authority repositories, RLS-aware connection factories, queries, advisory/alert persistence)
- `ArchiForge.DecisionEngine` (merge agent results into manifests)
- `ArchiForge.Decisioning` (governance, advisory, alerts, manifest/decision models used by persistence and application)
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
- `ArchiForge.Decisioning` (governance, previews, advisory surfaces)
- `ArchiForge.Contracts` for shared models

---

### Library: `ArchiForge.Decisioning` (governance, advisory, domain models)

**Responsibility**

- Policy packs, effective governance resolution, advisory scanning, digest/alert domain logic.
- Authority-path interfaces such as `IGoldenManifestRepository` / `IDecisionTraceRepository` (SQL implementations live in `ArchiForge.Persistence`).
- Manifest sections, findings, and decision-trace models shared with the decision engine and API.

**Depends on**

- `ArchiForge.Contracts`, `ArchiForge.Core`, and (where applicable) persistence ports implemented in `ArchiForge.Persistence`.

---

### Library: `ArchiForge.Persistence` (SQL Server authority + operational data)

**Responsibility**

- `SqlGoldenManifestRepository`, `SqlDecisionTraceRepository`, Dapper governance/advisory/alert repositories, and health/resilience around `ISqlConnectionFactory`.
- Row-level security session context application and read-replica routing for heavy list queries.

**Depends on**

- `ArchiForge.Decisioning`, `ArchiForge.ContextIngestion`, `ArchiForge.KnowledgeGraph`, `ArchiForge.ArtifactSynthesis`, `ArchiForge.Retrieval` (as needed for types and orchestration hooks).

---

### Library: `ArchiForge.KnowledgeGraph` (graph snapshots)

**Responsibility**

- Builds typed `GraphSnapshot` from persisted context; validates nodes/edges; supports operator graph views and pagination models.

**Depends on**

- `ArchiForge.ContextIngestion` / contracts for snapshot shapes.

---

### Library: `ArchiForge.ContextIngestion` (context pipeline)

**Responsibility**

- Ingestion connectors, delta summaries, and canonical object models feeding runs and graph construction.

---

### Library: `ArchiForge.Retrieval` (RAG / indexing)

**Responsibility**

- Embedding batches, indexing outbox, vector/search integration (configuration-driven).

---

### Library: `ArchiForge.ArtifactSynthesis` (bundle synthesis + packaging)

**Responsibility**

- `ArtifactSynthesisService` runs registered `IArtifactGenerator` implementations into an `ArtifactBundle`, validates bundles, and supports ZIP packaging / exports (`ArtifactPackagingService`).

**Depends on**

- `ArchiForge.Decisioning` (manifest model), `ArchiForge.Core`.

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
- `ArchiForge.Api` → `ArchiForge.Application` → `ArchiForge.Data` / `ArchiForge.Persistence` / `ArchiForge.Decisioning`
- `ArchiForge.Api` → `ArchiForge.DecisionEngine` (merge/commit)
- Optional paths: Context ingestion, knowledge graph, retrieval, artifact synthesis (all invoked from application/API layers as configured)
- All projects share models from `ArchiForge.Contracts`

---

### Where to go next

- Components: `docs/ARCHITECTURE_COMPONENTS.md`
- Flows: `docs/ARCHITECTURE_FLOWS.md`
- Data model: `docs/DATA_MODEL.md`

