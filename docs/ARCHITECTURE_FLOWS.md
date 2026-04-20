> **Scope:** ArchLucid architecture (Key flows) - full detail, tables, and links in the sections below.

## ArchLucid architecture (Key flows)

This doc describes the main runtime flows in “sequence narrative” form. It’s meant to be readable without diagrams.

---

### Flow A: Run lifecycle (request → tasks → results → commit → manifest)

**Goal**: turn an `ArchitectureRequest` into a committed, versioned manifest.

1. **Create run**
   - Client calls `POST /v1/architecture/request` with an `ArchitectureRequest`.
   - API persists the request and run metadata.

2. **Generate tasks**
   - Coordinator generates `AgentTask` records for required agent types (topology/cost/compliance/critic).
   - Run status transitions from `Created` → `TasksGenerated` → `WaitingForResults` (depending on implementation details).

3. **Submit results**
   - Client calls `POST /v1/architecture/run/{runId}/result` for each agent result.
   - API validates the result and persists it.
   - When all required types exist, run transitions to `ReadyForCommit`.

4. **Commit**
   - Client calls `POST /v1/architecture/run/{runId}/commit`.
   - Decision engine merges results into a `GoldenManifest`.
   - Manifest is persisted under a new `ManifestVersion`.
   - Run transitions to `Committed` and points at `CurrentManifestVersion`.

5. **Fetch artifacts**
   - Client can retrieve run status, tasks, results, manifest, summaries, exports, etc.

**Authority (ingestion) path (parallel contract):** For runs driven by context ingestion + graph + findings + decisioning + artifact synthesis, the server executes **`AuthorityPipelineStagesExecutor`** after the run row exists. OpenTelemetry records **five child spans** under the orchestrator’s run activity (`authority.context_ingestion`, `authority.graph`, `authority.findings`, `authority.decisioning`, `authority.artifacts`), each tagged with **`archlucid.stage.name`** for cross-cutting queries; see [BACKGROUND_JOB_CORRELATION.md](BACKGROUND_JOB_CORRELATION.md) §10 and [DUAL_PIPELINE_NAVIGATOR.md](DUAL_PIPELINE_NAVIGATOR.md).

---

### Flow B: Export lifecycle (build → persist record → replay)

**Goal**: build an export artifact and allow it to be replayed later.

1. **Build export**
   - API builds an analysis report and exports it (Markdown/DOCX).
   - Exports are deterministic “as of” the code + dependencies at generation time.

2. **Persist export record**
   - Persist the export artifact and/or its metadata record (`RunExportRecord`).

3. **Replay export**
   - Client requests replay by export record ID.
   - System loads the persisted record and re-exports without re-running the original work.

---

### Flow C: Comparison lifecycle (compare → persist record → replay/export → verify drift)

**Goal**: create comparisons that are persisted, inspectable, replayable, and exportable again.

#### C1: Create and persist an end-to-end run comparison

1. Client compares two runs (end-to-end).
2. API generates:
   - `EndToEndReplayComparisonReport` (structured payload)
   - a Markdown summary
3. If `persist=true`, API writes a `ComparisonRecord`:
   - `ComparisonType = "end-to-end-replay"`
   - `PayloadJson = serialized report`
   - `SummaryMarkdown = summary`

#### C2: Create and persist an export-record diff comparison

1. Client compares two export records.
2. API generates:
   - `ExportRecordDiffResult` (structured payload)
   - a Markdown summary
3. If `persist=true`, API writes a `ComparisonRecord`:
   - `ComparisonType = "export-record-diff"`
   - `PayloadJson = serialized diff`
   - `SummaryMarkdown = summary`

#### C3: Replay a persisted comparison record

1. Client calls:
   - `POST /v1/architecture/comparisons/{comparisonRecordId}/replay` (download file)
   - or `POST /v1/architecture/comparisons/{comparisonRecordId}/replay/metadata` (metadata only)
2. Application service:
   - loads the `ComparisonRecord`
   - **rehydrates** `PayloadJson` into a typed payload
   - generates requested format:
     - end-to-end: Markdown/HTML/DOCX/PDF
     - export-diff: Markdown/DOCX
3. API returns file + headers describing the replay (type, mode, ids, profile, etc).

#### C4: Replay modes (artifact vs regenerate vs verify)

- **artifact** (default): export the stored payload as-is (fastest, no dependency on original runs/exports).
- **regenerate**: rebuild the comparison from source runs/exports and then export (requires source data still exists).
- **verify**: regenerate and compare to the stored payload; returns drift analysis and verification headers.

#### C5: Persist replay (optional)

- If `persistReplay=true`, the replay operation creates a **new** comparison record and returns `PersistedReplayRecordId`.

