# ArchiForge glossary

**Last reviewed:** 2026-04-04

Short definitions for domain terms used across the codebase, docs, and runbooks. Deeper context is linked from each entry.

---

## Architecture run (run)

The top-level work unit: an **`ArchitectureRequest`** submitted by an operator or integrator that passes through ingestion, knowledge-graph build, findings, decisioning, artifact synthesis, and optional agent invocation, then results in a committed **golden manifest**. Tracked in **`dbo.Runs`** (GUID-keyed authority table). The older **`dbo.ArchitectureRuns`** (string `RunId`) is legacy — see **`docs/DATA_CONSISTENCY_MATRIX.md`**.

## Artifact bundle

A ZIP package produced by **`ArtifactPackagingService`** that collects all artifacts for a run (diagrams, DOCX, JSON manifests). Downloadable via `GET /v1/.../bundle`. Large bundles are stored in **Azure Blob Storage** when `ArtifactLargePayload:Enabled = true`; smaller payloads stay in SQL. See **`docs/DATA_MODEL.md`**.

## Authority run orchestrator

**`IAuthorityRunOrchestrator`** — the in-process pipeline that executes the full ingestion → graph → findings → decisioning → artifact synthesis sequence for a single run. Called by the coordinator; runs inside a SQL unit of work. Distinct from the legacy "coordinator" execution path.

## Comparison replay

Re-executing a stored run against new or updated comparison logic (or comparing two runs' outputs) without re-invoking agents. Powered by **`IComparisonReplayService`** and related interfaces. See **`docs/COMPARISON_REPLAY.md`**.

## Context snapshot

A serialized point-in-time capture of structured context (infrastructure declarations, requirements, topology) ingested via **`IContextConnector`** implementations. Stored as **`dbo.ContextSnapshots`**; the knowledge graph is built from it. See **`docs/CONTEXT_INGESTION.md`**.

## Decision trace

A structured log of every decisioning step during a run: which rules fired, which findings applied, what the outcome was. Persisted in two flavors: coordinator-layer (`IDecisionTraceRepository` in **`ArchiForge.Data`**) and authority-layer (`IDecisionTraceRepository` in **`ArchiForge.Decisioning`**). See **ADR 0010** (`docs/adr/0010-dual-manifest-trace-repository-contracts.md`).

## Effective governance

The **merged** `PolicyPackContentDocument` that results from applying all applicable policy pack assignments to a scope (project → workspace → tenant precedence). Computed by **`IEffectiveGovernanceResolver`**. Drives alerts, compliance filtering, and advisory defaults. See **ADR 0007** (`docs/adr/0007-effective-governance-merge.md`) and `GET /v1/governance-resolution`.

## Evidence bundle

The collection of agent outputs, findings, and decision traces assembled during a run and referenced in the final golden manifest. Accessed via **`IEvidenceBundleRepository`** and the `GET /v1/.../evidence` API.

## Finding

A structured observation produced by a **finding engine** about the architecture under review. Examples: missing security coverage, policy applicability gap, cost constraint violation. Typed via **`FindingPayload`** subtypes and stored as **`dbo.FindingsSnapshots`**. See **`docs/FINDINGS_TYPED_SCHEMA.md`**.

## Finding engine

A class implementing **`IFindingEngine`** (`ArchiForge.Decisioning`) that analyses a **context snapshot / knowledge graph** and returns a list of **findings**. Multiple engines run in parallel inside **`FindingsOrchestrator`**. Add new engines via DI registration or the `dotnet new archiforge-finding-engine` template.

## Golden manifest

The committed, versioned output of a completed architecture run — the "source of truth" artifact that governance, comparison, and advisory flows operate on. Stored in **`dbo.GoldenManifests`** (with optional blob offload for large payloads). Has a `ROWVERSION` column for optimistic concurrency.

## Hosting role

**`Hosting:Role`** configuration (`Api` / `Worker` / `Combined`). Controls which hosted services and controllers are activated in a process. Production typically splits API and Worker; local dev defaults to Combined. See **ADR 0001** (`docs/adr/0001-hosting-roles-api-worker-combined.md`) and **`docs/DEPLOYMENT_TERRAFORM.md`**.

## Knowledge graph

A typed graph of nodes and edges derived from a **context snapshot** by **`IKnowledgeGraphService`**. Nodes represent infrastructure elements, requirements, policies; edges represent relationships and inferences. Used by finding engines and displayed via the graph API. See **`docs/KNOWLEDGE_GRAPH.md`**.

## Leader election

A SQL-backed lease mechanism (**`IHostLeaderLeaseRepository`**, `dbo.HostLeaderLeases`) that ensures only one worker instance processes a given outbox at a time. Background services call `RunLeaderWorkAsync` before polling. Prevents duplicate advisory scans or outbox re-processing in horizontally scaled deployments.

## Outbox (transactional outbox)

A SQL table (`dbo.AuthorityPipelineWorkOutbox`, `dbo.RetrievalIndexingOutbox`) where deferred work is **enqueued atomically inside the same transaction** as the triggering operation. A background worker polls and processes rows. Prevents lost work on crash. See **ADR 0004** (`docs/adr/0004-transactional-outbox-retrieval-indexing.md`).

## Policy pack

A versioned document (`PolicyPackContentDocument`) that bundles compliance rules, advisory defaults, and alert rule ids. Created and published via `PolicyPacksController`; assigned to scopes (tenant/workspace/project) via **`IPolicyPackAssignmentRepository`**. Active packs are merged into **effective governance** at evaluation time.

## Scope (tenant / workspace / project)

The three-level hierarchical identifier that isolates data between customers and teams. Passed in JWT claims or headers (`x-tenant-id`, `x-workspace-id`, `x-project-id`). All primary authority tables carry these columns. RLS enforces isolation at the SQL layer when `ApplySessionContext = true`. See **`docs/security/MULTI_TENANT_RLS.md`**.

## Simulator mode / Real mode

**`AgentExecution:Mode`** setting. **`Simulator`** (default) uses `DeterministicAgentSimulator` — no Azure OpenAI calls, deterministic outputs, safe for tests and CI. **`Real`** uses `RealAgentExecutor` + Azure OpenAI completions. See **ADR 0005** (`docs/adr/0005-llm-completion-pipeline.md`) and **`docs/OPERATIONS_LLM_QUOTA.md`**.

## Storage provider

**`ArchiForge:StorageProvider`** (`InMemory` / `Sql`). Switches the entire persistence graph between in-memory singletons (dev/test) and Dapper SQL repositories (production). See **ADR 0011** (`docs/adr/0011-inmemory-vs-sql-storage-provider.md`).

## Unit of work (UoW)

**`IArchiForgeUnitOfWork`** — wraps a SQL connection + transaction. Repositories that implement `SupportsExternalTransaction` can enlist in the same transaction as the calling orchestrator, ensuring e.g. authority commit + outbox enqueue are atomic. Created by **`IArchiForgeUnitOfWorkFactory`**.
