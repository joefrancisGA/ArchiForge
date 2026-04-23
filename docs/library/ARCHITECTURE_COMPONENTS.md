> **Scope:** ArchLucid architecture (Components) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


## ArchLucid architecture (Components)

**Canonical poster:** [ARCHITECTURE_ON_ONE_PAGE.md](../ARCHITECTURE_ON_ONE_PAGE.md) · **Operator atlas:** [OPERATOR_ATLAS.md](OPERATOR_ATLAS.md)

**Product name:** **ArchLucid**. Solution/projects use **`ArchLucid.*`**; configuration may still show legacy **`ArchLucid:*`** / **`ArchLucidAuth`** keys until Phase 7 (`docs/ARCHLUCID_RENAME_CHECKLIST.md`).

This document zooms into the most important components inside each container/library. It is not exhaustive; it focuses on the pieces engineers tend to touch when extending “run → export → compare → replay”.

---

### Workflow data access vs authority persistence (both in `ArchLucid.Persistence`)

| Area | Role | Typical types |
|------|------|----------------|
| **`ArchLucid.Persistence.Data.*`** | ADO.NET/Dapper for the **run/commit/agent** workflow: repositories used by `ArchLucid.Application` and HTTP services for requests, runs, tasks, evidence, governance entities, background jobs, `IDbConnectionFactory`, DbUp **`DatabaseMigrator`**, consolidated **`Scripts/ArchLucid.sql`**. | `ArchitectureRequestRepository`, `IRunRepository`, `SqlConnectionFactory` |
| **Rest of `ArchLucid.Persistence`** | **Authority and decisioning** ports: unit of work, orchestration (`AuthorityRunOrchestrator`), snapshot repos for context/graph/findings/manifests, caching decorators (`CachingRunRepository`), archival, retrieval outbox, RLS session context. | `IRunRepository` (`Models.RunRecord`), `IArchLucidUnitOfWork`, `SqlContextSnapshotRepository` |

**Configuration:** SQL security and read-scale-out are grouped under **`SqlServer`** in appsettings (`RowLevelSecurity`, `ReadReplica`). See `ArchLucid.Persistence/Connections/SqlServerOptions.cs`.

**Why two namespaces inside one assembly:** Decisioning/ingestion evolved with explicit persistence abstractions; the application layer uses **`ArchLucid.Persistence.Data.Repositories`** for the run/commit/agent workflow (distinct from Decisioning’s manifest/trace interfaces — see ADR 0010). When adding a feature, follow existing callers: authority chain → orchestration repos; HTTP application services → **`Persistence.Data`** + Application.

---

### `ArchLucid.Api` components

#### Connection bridging (SQL)

- **`SqlScopedResolutionDbConnectionFactory`** (`ArchLucid.Api.DataAccess`): implements **`ArchLucid.Persistence.Data.Infrastructure.IDbConnectionFactory`** for the SQL storage path. **`CreateOpenConnectionAsync`** resolves scoped **`ISqlConnectionFactory`** ( **`ResilientSqlConnectionFactory`** and optional **`SessionContextSqlConnectionFactory`** ) so Dapper repositories under **`ArchLucid.Persistence.Data.Repositories`** share the same resilience/RLS path as **`ArchLucid.Persistence`** without registering **`IDbConnectionFactory`** as scoped (hosted health checks resolve from the root provider). **`CreateConnection`** returns an unopened **`SqlConnection`** for lightweight probes that open explicitly.

#### Dual manifest / trace repository interfaces

- **`ArchLucid.Decisioning.Interfaces.IGoldenManifestRepository`** / **`IDecisionTraceRepository`**: authority-oriented contracts (`SaveAsync`, scoped `GetByIdAsync`). Implemented by **`SqlGoldenManifestRepository`**, **`SqlDecisionTraceRepository`**, and in-memory counterparts; registered in **`AddArchLucidStorage`**.
- **`ArchLucid.Persistence.Data.Repositories.IGoldenManifestRepository`** / **`IDecisionTraceRepository`**: run/commit pipeline contracts (`CreateAsync`, `GetByVersionAsync`, batch traces). Implemented by **`GoldenManifestRepository`**, **`DecisionTraceRepository`** (Dapper); registered in **`RegisterCoordinatorDecisionEngineAndRepositories`** with **fully qualified** interface types so they are not confused with the Decisioning interfaces. When **`ArchLucid:StorageProvider=InMemory`**, the same registration block uses **`InMemoryCoordinatorGoldenManifestRepository`** and **`InMemoryCoordinatorDecisionTraceRepository`** (singleton), plus the other coordinator in-memory Data repos (**`InMemoryArchitectureRequestRepository`**, **`InMemoryArchitectureRunRepository`** with request lookup, **`InMemoryAgentEvaluationRepository`**, **`InMemoryDecisionNodeRepository`**, evidence/execution trace packages, tasks/results, idempotency). **`RegisterRunExportAndArchitectureAnalysis`** registers **`InMemoryRunExportRecordRepository`** in that mode so exports do not require SQL.
- **`ArchLucid.Application.Runs.IRunCommitOrchestrator`** (ADR 0021 Phase 3 prep): write-side façade for **`CommitRunAsync`**. **`RunCommitOrchestratorFacade`** is scoped and delegates to **`IArchitectureRunCommitOrchestrator`** / **`ArchitectureRunCommitOrchestrator`** today so new callers can depend on the façade while the coordinator manifest repository family is retired behind the strangler plan.

#### Governance persistence

- **`IGovernanceApprovalRequestRepository`**, **`IGovernancePromotionRecordRepository`**, **`IGovernanceEnvironmentActivationRepository`**: Dapper repos when **`ArchLucid:StorageProvider`** is Sql; **`InMemoryGovernanceApprovalRequestRepository`**, **`InMemoryGovernancePromotionRecordRepository`**, **`InMemoryGovernanceEnvironmentActivationRepository`** (singleton) in InMemory mode via **`RegisterGovernance`**. **`IGovernanceWorkflowService`** is scoped and uses **`IRunDetailQueryService`** for canonical run reads.

#### Rate limiting on controllers

- Most versioned controllers use **`[EnableRateLimiting("fixed")]`** or **`"expensive"`** / **`"replay"`** where appropriate.
- **`JobsController`**, **`ScopeDebugController`**: **`fixed`** window (job status polling and scope debug).
- **`DemoController`**: **`expensive`** (demo seed mutates data).
- **`AuthDebugController`** (`api/auth/*`) and **`DocsController`** (static HTML): **no** class-level rate limiter — documented in XML remarks; use edge throttling in production if needed.

#### Production configuration safety

- **`ArchLucidConfigurationRules.CollectErrors`**: when **`IWebHostEnvironment.IsProduction()`**, fails startup if **`Cors:AllowedOrigins`** is empty or contains a **`*`** wildcard, if **`WebhookDelivery:UseHttpClient`** is true without **`WebhookDelivery:HmacSha256SharedSecret`**, or if **`BillingProductionSafetyRules`** detect unsafe billing/Marketplace configuration (e.g. **`sk_live_`** without Stripe webhook secret, loopback Marketplace landing URL, **`GaEnabled=true`** without **`MarketplaceOfferId`**).

#### `ArchitectureController`

- **Role**: Single controller that exposes most `/v1/architecture/*` endpoints.
- **Patterns**:
  - Uses ASP.NET API versioning and rate limiting.
  - Uses explicit request/response DTOs in `ArchLucid.Api.Models`.
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

### `ArchLucid.Application` components

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

### `ArchLucid.Decisioning.Merge` — manifest merge

#### `IDecisionEngineService` / `DecisionEngineService`

- **Role**: Merge validated agent results into a coherent manifest.
- **Key responsibilities**:
  - Validate agent results and filter malformed results.
  - Create a base `GoldenManifest` with metadata (runId, request, manifestVersion, parent).
  - Apply agent proposals in deterministic order (by agent type merge order).
  - Apply governance defaults and required controls.
  - Apply decision nodes/evaluations (when provided).

---

### `ArchLucid.KnowledgeGraph` components

- **Role:** Build a **typed directed graph** (`GraphSnapshot`) from a persisted **`ContextSnapshot`**: **`GraphNode`** / **`GraphEdge`**, optional inferred relationships (**`CONTAINS`**, **`PROTECTS`**, **`APPLIES_TO`**, **`RELATES_TO`**, etc.), and **`IGraphValidator`** checks.
- **Entry points:** **`IKnowledgeGraphService`**, **`DefaultGraphBuilder`**, **`DefaultGraphEdgeInferer`**, **`GraphNodeFactory`**.
- **Detail:** `docs/KNOWLEDGE_GRAPH.md`.

### Workflow components (`ArchLucid.Persistence.Data.*`)

#### Repository layer (Dapper)

- **Role**: Persistence for runs, tasks, results, manifests, export records, comparison records, traces, evidence.
- **Pattern**: Each aggregate has an `I*Repository` + `*Repository` implementation; queries are explicit SQL strings.
- **SQL connectivity**: repositories take **`IDbConnectionFactory`**; on the API host with **`ArchLucid:StorageProvider`** = SQL, the registered factory is **`SqlScopedResolutionDbConnectionFactory`**, which delegates async opens to scoped **`ISqlConnectionFactory`** (see Api components above).

#### Contract test coverage (persistence)

- Shared suites under **`ArchLucid.Persistence.Tests/Contracts/`** include runs, comparison records, policy assignments, digests, alert rules, conversation threads/messages, **audit events**, **provenance snapshots**, **authority golden manifests**, **decision traces**, **policy packs**, **architecture run idempotency**, **agent tasks** / **agent results**, **architecture requests**, **architecture runs** (including list + join semantics), **evidence bundles**, **agent evidence packages**, **agent execution traces**, **advisory scan schedules**, and **alert delivery attempts** (each with InMemory + Dapper/SQL subclasses where applicable). SQL golden-manifest tests reuse **`AuthorityRunChainTestSeed`**; data-layer SQL tests reuse **`ArchitectureCommitTestSeed`** (request-only insert, request/run chain, and agent task FKs as needed).
- **`ArchLucid.Persistence.Data.Repositories` — in-memory parity:** besides tasks/results/idempotency/comparison, the solution ships **in-memory** implementations for **requests**, **runs** (optional **`IArchitectureRequestRepository`** for **`ListAsync`** system names), **evidence bundles**, **agent evidence packages**, and **agent execution traces** to support fast contract tests without SQL.

#### `ComparisonRecordRepository`

- Create and read comparison records.
- Query comparison history by run ID, export record ID, or search filters.
- Supports JSON tag filtering via SQL Server **`OPENJSON`** on stored tag arrays.

#### `InMemoryComparisonRecordRepository`

- Used when the API is configured with **`ArchLucid:StorageProvider=InMemory`**: same **`IComparisonRecordRepository`** contract as Dapper/SQL without a database (singleton registration alongside other in-memory repositories).

---

### `ArchLucid.Contracts` components

- **Shared domain enums**: `AgentType`, `ArchitectureRunStatus`, etc.
- **Requests and manifests**:
  - `ArchitectureRequest`
  - `AgentResult`, `ManifestDeltaProposal`
  - `GoldenManifest` and manifest subtypes
- **Persisted metadata records**:
  - `ComparisonRecord` (includes `PayloadJson`)

