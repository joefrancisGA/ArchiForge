## ArchiForge architecture (Components)

This document zooms into the most important components inside each container/library. It is not exhaustive; it focuses on the pieces engineers tend to touch when extending “run → export → compare → replay”.

---

### `ArchiForge.Api` components

#### Connection bridging (SQL)

- **`SqlScopedResolutionDbConnectionFactory`** (`ArchiForge.Api.DataAccess`): implements **`ArchiForge.Data.Infrastructure.IDbConnectionFactory`** for the SQL storage path. **`CreateOpenConnectionAsync`** resolves scoped **`ISqlConnectionFactory`** ( **`ResilientSqlConnectionFactory`** and optional **`SessionContextSqlConnectionFactory`** ) so Dapper repositories under **`ArchiForge.Data.Repositories`** share the same resilience/RLS path as **`ArchiForge.Persistence`** without registering **`IDbConnectionFactory`** as scoped (hosted health checks resolve from the root provider). **`CreateConnection`** returns an unopened **`SqlConnection`** for lightweight probes that open explicitly.

#### Dual manifest / trace repository interfaces

- **`ArchiForge.Decisioning.Interfaces.IGoldenManifestRepository`** / **`IDecisionTraceRepository`**: authority-oriented contracts (`SaveAsync`, scoped `GetByIdAsync`). Implemented by **`SqlGoldenManifestRepository`**, **`SqlDecisionTraceRepository`**, and in-memory counterparts; registered in **`AddArchiForgeStorage`**.
- **`ArchiForge.Data.Repositories.IGoldenManifestRepository`** / **`IDecisionTraceRepository`**: run/commit pipeline contracts (`CreateAsync`, `GetByVersionAsync`, batch traces). Implemented by **`GoldenManifestRepository`**, **`DecisionTraceRepository`** (Dapper); registered in **`RegisterCoordinatorDecisionEngineAndRepositories`** with **fully qualified** interface types so they are not confused with the Decisioning interfaces.

#### Rate limiting on controllers

- Most versioned controllers use **`[EnableRateLimiting("fixed")]`** or **`"expensive"`** / **`"replay"`** where appropriate.
- **`JobsController`**, **`ScopeDebugController`**: **`fixed`** window (job status polling and scope debug).
- **`DemoController`**: **`expensive`** (demo seed mutates data).
- **`AuthDebugController`** (`api/auth/*`) and **`DocsController`** (static HTML): **no** class-level rate limiter — documented in XML remarks; use edge throttling in production if needed.

#### `ArchitectureController`

- **Role**: Single controller that exposes most `/v1/architecture/*` endpoints.
- **Patterns**:
  - Uses ASP.NET API versioning and rate limiting.
  - Uses explicit request/response DTOs in `ArchiForge.Api.Models`.
  - Delegates most business logic to Application and Data services.
- **Replay & comparison library endpoints** (high value to know):
  - `GET /v1/architecture/comparisons` (search with skip/limit + filters)
  - `POST /v1/architecture/comparisons/{id}/replay` (file export)
  - `POST /v1/architecture/comparisons/{id}/replay/metadata` (metadata only)
  - `POST /v1/architecture/comparisons/{id}/drift` (drift analysis)
  - `GET /v1/architecture/comparisons/diagnostics/replay` (in-memory replay activity)

#### AuthN/AuthZ

- **AuthN**: API key authentication (`ApiKeyAuthenticationHandler`)
- **AuthZ policies** (claims):
  - `CanCommitRuns` (`commit:run`)
  - `CanSeedResults` (`seed:results`)
  - `CanReplayComparisons` (`replay:comparisons`)
  - `CanViewReplayDiagnostics` (`replay:diagnostics`)
  - `CanExportConsultingDocx` (`export:consulting-docx`)

#### Observability

- **Logging**: Serilog configuration in `appsettings.json`.
- **Tracing/Metrics**: OpenTelemetry wired in `Program.cs`, exporters controlled by config.
- **Replay diagnostics**: `IReplayDiagnosticsRecorder` stores a ring buffer of recent replay operations and is exposed through the diagnostics endpoint. Retention and sampling are driven by **`ReplayDiagnosticsOptions`** (configuration section **`ReplayDiagnostics`**).
- **Data archival readiness**: **`DataArchivalHostHealthState`** (singleton) captures the last **`DataArchivalHostedService`** iteration outcome; **`DataArchivalHostHealthCheck`** registers as **`data_archival`** on the readiness probe (**Healthy** when archival is off or succeeding; **Degraded** when enabled and the last pass failed). Operator notes: **`docs/runbooks/DATA_ARCHIVAL_HEALTH.md`**.

---

### `ArchiForge.Application` components

#### Run + replay services (core orchestrators)

- **`IReplayRunService` / `ReplayRunService`**
  - Replays an existing run into a new run (used for run replay workflows).

- **`IComparisonReplayService` / `ComparisonReplayService`**
  - Loads a persisted comparison record by ID.
  - Rehydrates the stored payload (`PayloadJson`) into a typed report/diff.
  - Exports the replayed artifact (Markdown/HTML/DOCX/PDF, depending on comparison type).
  - Supports `ReplayMode` (`artifact`, `regenerate`, `verify`) and drift analysis.
  - Supports `PersistReplay` to create a new comparison record for the replay.

- **`IComparisonReplayCostEstimator` / `ComparisonReplayCostEstimator`**
  - Lightweight heuristic for **`GET/POST …/comparisons/{id}/replay/cost-estimate`**: uses comparison type, format, replay mode, optional **`PersistReplay`**, and stored payload size to produce a relative score and band (`low` / `medium` / `high`) without executing replay.

- **`ComparisonRecordPayloadRehydrator`**
  - Deserializes `PayloadJson` into:
    - `EndToEndReplayComparisonReport`
    - `ExportRecordDiffResult`

#### End-to-end replay comparison formatting/export

- **`IEndToEndReplayComparisonService`**
  - Builds a comparison report by comparing two runs.

- **`IEndToEndReplayComparisonSummaryFormatter`**
  - Produces a Markdown summary for the report.

- **`IEndToEndReplayComparisonExportService`**
  - Produces end-to-end exports:
    - Markdown
    - HTML
    - DOCX
    - PDF
  - Supports “profile” variants (e.g., short/executive/detailed).

#### Export-record diff formatting/export

- **`IExportRecordDiffService`**
  - Computes `ExportRecordDiffResult` from two export records.

- **`IExportRecordDiffSummaryFormatter`**
  - Produces Markdown summary for export-record diffs.

- **`IExportRecordDiffExportService`**
  - Produces DOCX export for export-record diff replay.

---

### `ArchiForge.DecisionEngine` components

#### `IDecisionEngineService` / `DecisionEngineService`

- **Role**: Merge validated agent results into a coherent manifest.
- **Key responsibilities**:
  - Validate agent results and filter malformed results.
  - Create a base `GoldenManifest` with metadata (runId, request, manifestVersion, parent).
  - Apply agent proposals in deterministic order (by agent type merge order).
  - Apply governance defaults and required controls.
  - Apply decision nodes/evaluations (when provided).

---

### `ArchiForge.KnowledgeGraph` components

- **Role:** Build a **typed directed graph** (`GraphSnapshot`) from a persisted **`ContextSnapshot`**: **`GraphNode`** / **`GraphEdge`**, optional inferred relationships (**`CONTAINS`**, **`PROTECTS`**, **`APPLIES_TO`**, **`RELATES_TO`**, etc.), and **`IGraphValidator`** checks.
- **Entry points:** **`IKnowledgeGraphService`**, **`DefaultGraphBuilder`**, **`DefaultGraphEdgeInferer`**, **`GraphNodeFactory`**.
- **Detail:** `docs/KNOWLEDGE_GRAPH.md`.

### `ArchiForge.Data` components

#### Repository layer (Dapper)

- **Role**: Persistence for runs, tasks, results, manifests, export records, comparison records, traces, evidence.
- **Pattern**: Each aggregate has an `I*Repository` + `*Repository` implementation; queries are explicit SQL strings.
- **SQL connectivity**: repositories take **`IDbConnectionFactory`**; on the API host with **`ArchiForge:StorageProvider`** = SQL, the registered factory is **`SqlScopedResolutionDbConnectionFactory`**, which delegates async opens to scoped **`ISqlConnectionFactory`** (see Api components above).

#### Contract test coverage (persistence)

- Shared suites under **`ArchiForge.Persistence.Tests/Contracts/`** include runs, comparison records, policy assignments, digests, alert rules, conversation threads/messages, **audit events**, and **provenance snapshots** (each with InMemory + Dapper/SQL subclasses where applicable).

#### `ComparisonRecordRepository`

- Create and read comparison records.
- Query comparison history by run ID, export record ID, or search filters.
- Supports JSON tag filtering via SQL Server **`OPENJSON`** on stored tag arrays.

#### `InMemoryComparisonRecordRepository`

- Used when the API is configured with **`ArchiForge:StorageProvider=InMemory`**: same **`IComparisonRecordRepository`** contract as Dapper/SQL without a database (singleton registration alongside other in-memory repositories).

---

### `ArchiForge.Contracts` components

- **Shared domain enums**: `AgentType`, `ArchitectureRunStatus`, etc.
- **Requests and manifests**:
  - `ArchitectureRequest`
  - `AgentResult`, `ManifestDeltaProposal`
  - `GoldenManifest` and manifest subtypes
- **Persisted metadata records**:
  - `ComparisonRecord` (includes `PayloadJson`)

