> **Scope:** ArchLucid architecture (Containers) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


## ArchLucid architecture (Containers)

**Canonical poster:** [ARCHITECTURE_ON_ONE_PAGE.md](../ARCHITECTURE_ON_ONE_PAGE.md) · **Operator atlas:** [OPERATOR_ATLAS.md](OPERATOR_ATLAS.md)

**Product name:** **ArchLucid**. **`ArchLucid.*`** below refers to deployable projects and libraries until the bulk rename phases in `docs/ARCHLUCID_RENAME_CHECKLIST.md`.

This is a pragmatic C4 “containers” view: **deployable processes** and major libraries, with their responsibilities and relationships.

---

### Container: `ArchLucid.Api` (ASP.NET Core Web API)

**Responsibility**

- HTTP surface for all run/execution/export/compare/replay workflows.
- API versioning (`/v1/...`), rate limiting, and API-key auth.
- Wires up DI for Application, Persistence (workflow `Data.*` + authority SQL), Decisioning (merge + validation + governance), Retrieval, ContextIngestion, and related services.
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

- `ArchLucid.Application` (core orchestration + analysis/export/replay services)
- `ArchLucid.Persistence.Data.*` (Dapper repositories for run/commit path, migrations, `IDbConnectionFactory`)
- `ArchLucid.Persistence` (SQL authority repositories, RLS-aware connection factories, queries, advisory/alert persistence)
- `ArchLucid.Decisioning` (governance, advisory, alerts, manifest/decision models, **manifest merge** in `ArchLucid.Decisioning.Merge`, **JSON schema validation** in `ArchLucid.Decisioning.Validation`)
- `ArchLucid.Contracts` (DTOs / domain contracts)

---

### Container: `ArchLucid.Cli` (dotnet tool / CLI)

**Responsibility**

- Human- and script-friendly entry point for common workflows:
  - create projects (`new`)
  - dev infra (`dev up`)
  - run lifecycle (`run`, `status`, `submit`, `commit`, `seed`, `artifacts`)
  - comparisons library (`comparisons list/replay/drift/diagnostics/tag`)

**Depends on**

- `ArchLucid.Api.Client` (NSwag-generated HTTP client types under `ArchLucid.Api.Client.Generated`)
- `ArchLucid.Contracts` (types used for requests/results)
- HTTP calls to `ArchLucid.Api`

---

### Library: `ArchLucid.Application` (application services)

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

- `ArchLucid.Persistence.Data.*` for repositories
- `ArchLucid.Decisioning` (governance, previews, advisory surfaces, manifest merge via `ArchLucid.Decisioning.Merge`)
- `ArchLucid.Contracts` for shared models

---

### Library: `ArchLucid.Decisioning` (governance, advisory, merge, domain models)

**Responsibility**

- Policy packs, effective governance resolution, advisory scanning, digest/alert domain logic.
- **`ArchLucid.Decisioning.Merge`**: `IDecisionEngineService` / `DecisionEngineService`, `DecisionEngineV2`, merge of agent results into `GoldenManifest`.
- **`ArchLucid.Decisioning.Validation`**: JSON Schema validation for agent results and golden manifests (`SchemaValidationService`, `ISchemaValidationService`).
- Authority-path interfaces such as `IGoldenManifestRepository` / `IDecisionTraceRepository` (SQL implementations live in `ArchLucid.Persistence`).
- Manifest sections, findings, and decision-trace models shared with the API and persistence.

**Depends on**

- `ArchLucid.Contracts`, `ArchLucid.Core`, and (where applicable) persistence ports implemented in `ArchLucid.Persistence`.

---

### Library: `ArchLucid.Persistence` (SQL Server authority + operational data)

**Responsibility**

- `SqlGoldenManifestRepository`, `SqlDecisionTraceRepository`, Dapper governance/advisory/alert repositories, and health/resilience around `ISqlConnectionFactory`.
- Row-level security session context application and optional read routing to the Azure SQL failover group read-only listener for authority run lists, governance-resolution reads (assignments / packs / versions), and golden manifest lookup by id.

**Depends on**

- `ArchLucid.Decisioning`, `ArchLucid.ContextIngestion`, `ArchLucid.KnowledgeGraph`, `ArchLucid.ArtifactSynthesis`, `ArchLucid.Retrieval` (as needed for types and orchestration hooks).

---

### Library: `ArchLucid.KnowledgeGraph` (graph snapshots)

**Responsibility**

- Builds typed `GraphSnapshot` from persisted context; validates nodes/edges; supports operator graph views and pagination models.

**Depends on**

- `ArchLucid.ContextIngestion` / contracts for snapshot shapes.

---

### Library: `ArchLucid.ContextIngestion` (context pipeline)

**Responsibility**

- Ingestion connectors, delta summaries, and canonical object models feeding runs and graph construction.

---

### Library: `ArchLucid.Retrieval` (RAG / indexing)

**Responsibility**

- Embedding batches, indexing outbox, vector/search integration (configuration-driven).

---

### Library: `ArchLucid.ArtifactSynthesis` (bundle synthesis + packaging)

**Responsibility**

- `ArtifactSynthesisService` runs registered `IArtifactGenerator` implementations into an `ArtifactBundle`, validates bundles, and supports ZIP packaging / exports (`ArtifactPackagingService`).

**Depends on**

- `ArchLucid.Decisioning` (manifest model), `ArchLucid.Core`.

---

### Library: `ArchLucid.Persistence` — workflow data access (`ArchLucid.Persistence.Data.*`)

**Responsibility**

- Database access (Dapper repositories) for runs, tasks, results, manifests, export records, comparison records, traces, evidence, etc.
- Migration scripts under `ArchLucid.Persistence/Migrations/*` applied by DbUp at startup.
- DB connection factory (`IDbConnectionFactory`) for **SQL Server** (`SqlConnectionFactory`).

**Depends on**

- `ArchLucid.Contracts` (persisted record DTOs)

---

### Library: `ArchLucid.Coordinator` (task generation / orchestration)

**Responsibility**

- Generates the set of agent tasks for a run.
- Coordinates the “run setup” phase (tasks created, initial status transitions).

---

### Library: `ArchLucid.AgentRuntime` / `ArchLucid.AgentSimulator`

**Responsibility**

- Agent runtime handlers implement agent-specific behavior and/or simulated outputs.
- Simulator mode produces deterministic results for repeatable local runs/tests.

---

### Library: `ArchLucid.Contracts`

**Responsibility**

- Shared types: requests/responses, manifests, metadata records, agent messages, enums.
- Keeps API/Application/Data aligned on payload shapes.

---

### Container relationships (high-level)

- `ArchLucid.Cli` → (HTTP) → `ArchLucid.Api`
- `ArchLucid.Api` → `ArchLucid.Application` → `ArchLucid.Persistence.Data.*` / `ArchLucid.Persistence` / `ArchLucid.Decisioning` (including merge/commit via `ArchLucid.Decisioning.Merge`)
- Optional paths: Context ingestion, knowledge graph, retrieval, artifact synthesis (all invoked from application/API layers as configured)
- All projects share models from `ArchLucid.Contracts`

---

### Where to go next

- Components: `docs/ARCHITECTURE_COMPONENTS.md`
- Flows: `docs/ARCHITECTURE_FLOWS.md`
- Data model: `docs/DATA_MODEL.md`

