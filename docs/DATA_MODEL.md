## ArchiForge data model (pragmatic)

This document summarizes the persisted data model used by ArchiForge. It is based on the migration scripts in `ArchiForge.Data/Migrations/*` and the `ArchiForge.Contracts.Metadata` records.

**SQL mechanics (how scripts run, idempotency, change workflow):** see **[SQL_SCRIPTS.md](SQL_SCRIPTS.md)** — canonical reference for `ArchiForge.sql`, DbUp migrations, and Persistence bootstrap.

---

### High-level storage principles

- **Runs are the primary unit of work**: a run references a request, has tasks/results, and can be committed into a manifest version.
- **Artifacts are persisted for audit/replay**:
  - **Manifests** are versioned and persisted.
  - **Export records** persist export artifacts and enable replay.
  - **Comparison records** persist comparison payloads and enable replay/export/verification.
- **Structured payloads are stored as JSON** in NVARCHAR columns (`RequestJson`, `ResultJson`, `ManifestJson`, `PayloadJson`, etc.).
- **Authority entities** (ContextSnapshots, GraphSnapshots, FindingsSnapshots, GoldenManifests, ArtifactBundles) now have **relational child tables** alongside their JSON columns. The persistence layer reads from relational tables first and falls back to JSON when relational rows are absent, governed by `PersistenceReadMode`. See **[SqlRelationalBackfill.md](SqlRelationalBackfill.md)** for operating modes, backfill, and cutover.

---

### Core tables (from `001_InitialSchema.sql`)

#### `ArchitectureRequests`

- **Key**: `RequestId`
- **Stores**: request metadata + `RequestJson`
- **Why it matters**: the source input for a run; used for auditability and report generation.

#### `ArchitectureRuns`

- **Key**: `RunId`
- **Fields**: `RequestId`, `Status`, `CreatedUtc`, `CompletedUtc`, `CurrentManifestVersion`
- **Why it matters**: tracks lifecycle state and ties together tasks/results/manifests.

#### `AgentTasks`

- **Key**: `TaskId`
- **Fields**: `RunId`, `AgentType`, `Objective`, `Status`, timestamps, optional `EvidenceBundleRef`
- **Why it matters**: defines what each agent is expected to do for a run.

#### `AgentResults`

- **Key**: `ResultId`
- **Fields**: `TaskId`, `RunId`, `AgentType`, `Confidence`, `ResultJson`
- **Why it matters**: the proposals/evidence that feed the decision engine to produce manifests.

#### `GoldenManifestVersions`

- **Key**: `ManifestVersion`
- **Fields**: `RunId`, `SystemName`, `ManifestJson`, `ParentManifestVersion`, `CreatedUtc`
- **Why it matters**: immutable, versioned architecture output.

#### `EvidenceBundles`

- **Key**: `EvidenceBundleId`
- **Fields**: `RequestDescription`, `EvidenceJson`
- **Why it matters**: packaged evidence used for reporting and explainability.

#### `DecisionTraces`

- **Key**: `TraceId`
- **Fields**: `RunId`, `EventType`, `EventDescription`, `EventJson`
- **Why it matters**: audit trail of important events and decision points.

#### `AgentEvidencePackages`

- **Key**: `EvidencePackageId`
- **Fields**: run/request/system/environment/provider + `EvidenceJson`
- **Why it matters**: canonical evidence container for explainability and exports.

---

### Authority chain / context & graph (`ArchiForge.Data/SQL/ArchiForge.sql`)

These tables support the persisted authority pipeline (context → graph → findings → decisions → artifacts). They complement the legacy `ArchiForge.Data` API schema. The same DDL is applied at runtime via `Scripts/ArchiForge.sql` (linked from `ArchiForge.Data/SQL/ArchiForge.sql` in the Persistence build output).

#### `ContextSnapshots`

- **Key**: `SnapshotId`
- **Fields**: `RunId`, `ProjectId`, `CreatedUtc`, `CanonicalObjectsJson`, `DeltaSummary`, optional warnings/errors/source hashes (JSON columns as applicable)
- **Why it matters**: durable **normalized context** after multi-connector ingestion; **`ProjectId`** + **`CreatedUtc`** index supports **latest snapshot per project** (used for connector delta messaging and future diff features).
- **Relational children**: `ContextSnapshotCanonicalObjects` (and properties), warnings, errors, source hashes. Read path governed by `PersistenceReadMode` — see **[SqlRelationalBackfill.md](SqlRelationalBackfill.md)**.
- **Pipeline detail**: `docs/CONTEXT_INGESTION.md`

#### `GraphSnapshots`, `FindingsSnapshots`, …

Linked to runs and context snapshots; see the authority section in `ArchiForge.Data/SQL/ArchiForge.sql` for full DDL. **Graph** node/edge JSON and semantics: **`docs/KNOWLEDGE_GRAPH.md`**.

---

### Comparison records (`002_ComparisonRecords.sql` + `003_ComparisonRecords_LabelAndTags.sql`)

#### `ComparisonRecords`

- **Key**: `ComparisonRecordId`
- **Type**: `ComparisonType` (e.g., `end-to-end-replay`, `export-record-diff`)
- **Linkage**:
  - run comparisons: `LeftRunId`, `RightRunId` (and optional manifest versions)
  - export comparisons: `LeftExportRecordId`, `RightExportRecordId`
- **Content**:
  - `SummaryMarkdown` (optional stored summary)
  - `PayloadJson` (the source-of-truth payload for replay)
- **Metadata**:
  - `Format` (typically `json+markdown`)
  - `Notes`, `CreatedUtc`
  - `Label` (optional)
  - `Tags` (optional JSON array stored as string)

**Why it matters**

Comparison replay is built on `PayloadJson` as the durable artifact. This enables:

- replay/export without recomputing comparisons
- regenerate/verify (drift analysis) when original sources exist

---

### Decision engine v2 persistence (`004_DecisionNodes_And_Evaluations.sql`)

#### `DecisionNodes`

- **Key**: `DecisionId`
- **Fields**: `RunId`, `Topic`, `SelectedOptionId`, `Confidence`, `Rationale`, `DecisionJson`, `CreatedUtc`
- **Why it matters**: captures “final decisions” as structured records, separate from raw agent results.

#### `AgentEvaluations`

- **Key**: `EvaluationId`
- **Fields**: `RunId`, `TargetAgentTaskId`, `EvaluationType`, `ConfidenceDelta`, `Rationale`, `EvaluationJson`, `CreatedUtc`
- **Why it matters**: supports evaluation/critique loops and downstream auditing of confidence adjustments.

---

### Product learning (58R) — `ProductLearningPilotSignals`

- **Key**: `SignalId` (`UNIQUEIDENTIFIER`)
- **Scope**: `TenantId`, `WorkspaceId`, `ProjectId` (same pattern as advisory recommendations and policy assignments).
- **Purpose**: capture **pilot or product-team** judgments on outputs — **trusted**, **rejected**, **revised**, or **needs follow-up** — without changing agent behavior in this change set.
- **Optional links**: `ArchitectureRunId` (FK to `ArchitectureRuns`), `AuthorityRunId` (correlation only; no FK), `ManifestVersion`, `ArtifactHint`, `PatternKey` (normalized bucket for rollups), `DetailJson` for structured notes.
- **Triage**: `TriageStatus` supports a lightweight internal backlog (`Open`, `Triaged`, `Backlog`, `Done`, `WontFix`).
- **Access**: `ArchiForge.Persistence.ProductLearning.IProductLearningPilotSignalRepository` (Dapper SQL / in-memory).
- **Rollups (58R Prompt 3):** same repository exposes scoped aggregations (run feedback rollups, artifact outcome trends, repeated comment prefixes, improvement-opportunity candidates) built with explicit SQL / deterministic in-memory equivalents — see `ProductLearningSignalAggregations`.
- **Triage services (58R Prompt 4):** `IProductLearningDashboardService` composes `LearningDashboardSummary` (counts, rollups, trends, ranked opportunities, merged triage queue) using threshold options in `ProductLearningTriageOptions` — no LLM.
- **Snapshot field `TopRejectedRevisedRollups`:** reserved; aggregation does **not** populate it in 58R (avoids an extra query unused by dashboard/report). Repository method `ListTopRejectedRevisedArtifactRollupsAsync` remains for direct callers or future UI.

---

### Common IDs and relationships (mental model)

- `RequestId` → `RunId` → `ManifestVersion`
- `RunId` → `TaskId` → `ResultId`
- `RunId` → (`DecisionNodes`, `AgentEvaluations`, `DecisionTraces`, evidence)
- `ComparisonRecordId` ties to:
  - runs (left/right run IDs), or
  - export records (left/right export record IDs)

