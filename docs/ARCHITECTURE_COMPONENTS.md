## ArchiForge architecture (Components)

This document zooms into the most important components inside each container/library. It is not exhaustive; it focuses on the pieces engineers tend to touch when extending “run → export → compare → replay”.

---

### `ArchiForge.Api` components

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
- **Replay diagnostics**: `IReplayDiagnosticsRecorder` stores a ring buffer of recent replay operations and is exposed through the diagnostics endpoint.

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

#### `ComparisonRecordRepository`

- Create and read comparison records.
- Query comparison history by run ID, export record ID, or search filters.
- Supports JSON tag filtering via SQL Server **`OPENJSON`** on stored tag arrays.

---

### `ArchiForge.Contracts` components

- **Shared domain enums**: `AgentType`, `ArchitectureRunStatus`, etc.
- **Requests and manifests**:
  - `ArchitectureRequest`
  - `AgentResult`, `ManifestDeltaProposal`
  - `GoldenManifest` and manifest subtypes
- **Persisted metadata records**:
  - `ComparisonRecord` (includes `PayloadJson`)

