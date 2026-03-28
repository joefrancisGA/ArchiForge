## ArchiForge architecture (Context)

### Purpose

ArchiForge is a .NET API that orchestrates AI-assisted architecture design. It accepts an `ArchitectureRequest`, coordinates agent tasks/results, merges results into a versioned manifest, and produces exports, comparisons, and replayable artifacts.

This document is written for **internal engineers** and is intentionally pragmatic: it prioritizes “how the system actually behaves” over strict diagram formalism.

---

### Primary capabilities

- **Run lifecycle**
  - Create a run from an `ArchitectureRequest`
  - Generate agent tasks (topology/cost/compliance/critic)
  - Accept agent results
  - Commit a run to produce a **versioned manifest**

- **Artifacts and exports**
  - Fetch manifests and summaries
  - Export analysis reports (Markdown/DOCX; and specialized consulting DOCX)
  - Persist export records and replay exports later

- **Comparisons and replay**
  - Compare runs (end-to-end) and persist a comparison record
  - Compare export records (export-record diff) and persist a comparison record
  - Replay a persisted comparison record into Markdown/HTML/DOCX/PDF (type-dependent)
  - Verify replays (drift analysis) and optionally persist replay results as new comparison records

---

### System boundary and actors

**ArchiForge (this system)** includes:

- `ArchiForge.Api` (HTTP surface)
- `ArchiForge.Application` (orchestration + formatting/export/replay services)
- `ArchiForge.Data` (repositories + migrations)
- `ArchiForge.DecisionEngine` (merge logic that produces manifests)
- `ArchiForge.Contracts` (shared DTOs and domain contracts)

**Actors / clients**

- **Engineers**: use Swagger or CLI to create runs, inspect artifacts, and replay comparisons/exports.
- **Automation**: CI/CD or scripts that call the API to generate manifests and export evidence.

---

### External dependencies (runtime)

- **Database**
  - SQL Server in production, dev, and **ArchiForge.Api.Tests** integration tests (per-test databases; **DbUp** on host startup).
  - Migrations are applied with DbUp when `ConnectionStrings:ArchiForge` is set.

- **Azure OpenAI (optional)**
  - Used when `AgentExecution:Mode` is not `Simulator`.
  - In simulator mode, the system uses deterministic fake agent outputs for repeatable testing.

- **Local dev dependencies (optional)**
  - Docker compose for SQL Server / Azurite / Redis (`archiforge dev up`)

---

### Key quality attributes (what we optimize for)

- **Reproducibility**
  - Replay semantics exist for exports and comparisons.
  - “Verify” mode detects drift between stored payloads and regenerated payloads.

- **Auditability**
  - Persisted records (runs, manifests, export records, comparison records) provide a durable trail.
  - Replay requests can optionally be persisted as new comparison records.

- **Determinism where possible**
  - Simulator mode enables deterministic runs for tests and local iterations.

- **Pragmatic correctness**
  - The decision engine validates inputs and records errors rather than silently accepting malformed data.

---

### Context ingestion

Multi-source inputs (description, inline requirements, pasted documents, policy/topology/security hints) are normalized through **`ArchiForge.ContextIngestion`** into **`CanonicalObject`** records, deduplicated, and persisted as **`ContextSnapshot`** for the knowledge graph. See **`docs/CONTEXT_INGESTION.md`** for connector order, parsers (`REQ:` / `POL:` / … line prefixes), dedupe rules, and the mapping from **`ArchitectureRequest`** to **`ContextIngestionRequest`**.

---

### Where to go next

- Containers: `docs/ARCHITECTURE_CONTAINERS.md`
- Components: `docs/ARCHITECTURE_COMPONENTS.md`
- Key flows: `docs/ARCHITECTURE_FLOWS.md`
- Data model: `docs/DATA_MODEL.md`
- Context ingestion: `docs/CONTEXT_INGESTION.md`

