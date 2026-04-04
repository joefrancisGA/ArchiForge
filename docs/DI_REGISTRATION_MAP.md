# DI registration map (Host.Core)

**Purpose:** One-page map from extension methods and partial `ServiceCollectionExtensions` files to the capabilities they register, including configuration gates and key sections.

**Entry point:** `ArchiForge.Host.Core.Startup.ServiceCollectionExtensions.AddArchiForgeApplicationServices` orchestrates everything below (after options on the main partial).

**Source files:**

| File | Responsibility |
|------|----------------|
| `ServiceCollectionExtensions.cs` | `AddArchiForgeApplicationServices` — calls all `Register*` methods in order |
| `ServiceCollectionExtensions.FeatureManagement.cs` | `AddArchiForgeFeatureManagement` |
| `ArchiForgeStorageServiceCollectionExtensions.cs` | `AddArchiForgeStorage` |
| `ServiceCollectionExtensions.SchedulingAndAlerts.cs` | Advisory schedules, digests, alerts, retrieval/authority pipeline workers, archival host |
| `ServiceCollectionExtensions.DataHealthAndJobs.cs` | In-memory `IDbConnectionFactory`, health checks, background jobs |
| `ServiceCollectionExtensions.ApplicationPipeline.cs` | Run export, analysis, replay/drift, **core run pipeline** (`IArchitectureRunService`, …), context ingestion + knowledge graph |
| `ServiceCollectionExtensions.Decisioning.cs` | Finding engines, decision engine, manifest builders/validators |
| `ServiceCollectionExtensions.CoordinatorAndArtifacts.cs` | Coordinator, decision engine services, **`ArchiForge.Persistence.Data.Repositories`** for runs/tasks/manifests, artifact synthesis |
| `ServiceCollectionExtensions.AgentsGovernanceRetrieval.cs` | Agent execution (Simulator vs Real), governance repos, retrieval + embeddings |

---

## `AddArchiForgeApplicationServices` (order)

Cross-cutting options bound on the main partial (not exhaustive): `Demo`, `BatchReplay`, `ApiDeprecation`, `DataArchival`, `HostLeaderElection`.

1. `IDemoSeedService`
2. **`AddArchiForgeFeatureManagement`** → `FeatureManagement` section
3. **`AddArchiForgeStorage`** → **`ArchiForge:StorageProvider`** (`Sql` vs `InMemory`) — see below
4. **`RegisterAdvisoryScheduling`** — **role:** `Combined` \| `Worker` → `AdvisoryScanHostedService`
5. **`RegisterDigestDelivery`** → `WebhookDelivery` (+ HTTP client `ArchiForgeWebhooks`)
6. **`RegisterAlerts`** — evaluators, channels, dispatcher, `IAlertService`
7. **`RegisterDataInfrastructure`** — only when **`ArchiForge:StorageProvider=InMemory`**: registers `IDbConnectionFactory` → `SqlConnectionFactory` (test/local SQL helpers)
8. **`RegisterBackgroundJobs`** → **`BackgroundJobs`** (`Mode`: `Durable` vs in-memory); **role** gates queue processor vs API queue sender — see below
9. **`RegisterRunExportAndArchitectureAnalysis`** — gated **`IRunExportRecordRepository`** by `StorageProvider`
10. **`RegisterComparisonReplayAndDrift`** → `ReplayDiagnostics`
11. **`RegisterRunReplayManifestAndDiffs`** — `IArchitectureRunService`, replay, diffs, exports, `IActorContext`, audit
12. **`RegisterContextIngestionAndKnowledgeGraph`** — connectors, parsers, `IContextIngestionService`, graph builder/service
13. **`RegisterDecisioningEngines`** — findings orchestrator, rule engine, manifest services, compliance pack loader
14. **`RegisterCoordinatorDecisionEngineAndRepositories`** — gated workflow repos (`ArchiForge.Persistence.Data.Repositories`) by **`StorageProvider`**
15. **`RegisterArtifactSynthesis`**
16. **`RegisterAgentExecution`** → **`AgentExecution:Mode`** (`Simulator` vs Real), `AzureOpenAI:*`, `LlmTokenQuota`, `LlmTelemetry`, `AgentPromptCatalog`
17. **`RegisterGovernance`** → **`ArchiForge:StorageProvider`** for governance repos (InMemory singletons vs SQL scoped)
18. **`RegisterRetrieval`** → `Retrieval:VectorIndex` (`AzureSearch` vs in-memory), `AzureOpenAI:Embedding*`, `AzureOpenAI:CircuitBreaker`
19. **`RegisterRetrievalIndexingOutbox`** — **role:** `Combined` \| `Worker` → hosted outbox + authority pipeline work processors
20. **`RegisterDataArchivalHostedService`** — **role:** `Combined` \| `Worker`
21. **`RegisterArchiForgeHealthChecks`** — SQL, schema, compliance pack, blob, temp dir; archival check when worker/combined

---

## `AddArchiForgeStorage` (`ArchiForgeStorageServiceCollectionExtensions`)

**Gate:** `ArchiForge:StorageProvider`

### `InMemory`

- Singleton in-memory persistence for authority stores (context/graph/findings/manifest/trace/artifact/run, audit, provenance, advisory, alerts, policy packs, conversations, product learning, evolution, pipeline work, retrieval outbox).
- **`IArchiForgeUnitOfWorkFactory`** → `InMemoryArchiForgeUnitOfWorkFactory`
- **`IAsyncAuthorityPipelineModeResolver`** → `DisabledAsyncAuthorityPipelineModeResolver` (async/deferred pipeline path effectively off)
- **`IAuthorityRunOrchestrator`**, **`IAuthorityPipelineStagesExecutor`**, **`IDataArchivalCoordinator`**, host leader lease **NoOp**
- Optional: distributed cache + LLM completion response store when configured

### `Sql` (default production shape)

- **`ConnectionStrings:ArchiForge`** required
- **`SqlServer`** section — RLS session context, read-replica routing (`ReadReplicaRoutedConnectionFactory` for list/governance/manifest lookup routes)
- **`ISqlConnectionFactory`** — resilient open + optional `SessionContextSqlConnectionFactory`
- Dapper/SQL implementations for the same repository surface as InMemory
- **`IAsyncAuthorityPipelineModeResolver`** → `FeatureManagementAuthorityPipelineModeResolver` (coordinates with **`FeatureManagement`**)
- **`IData.Infrastructure.IDbConnectionFactory`** → `SqlScopedResolutionDbConnectionFactory` (scoped resolution for legacy Data access)
- Schema bootstrap via `ISqlConnectionFactory` + embedded `ArchiForge.sql`
- **`ArtifactLargePayload`**, **`HotPathCache`**, LLM completion cache options as applicable

---

## `RegisterBackgroundJobs`

**Config:** `BackgroundJobs:Mode` — `Durable` uses Azure Queue + `BackgroundJobRepository` + blob result accessor; otherwise in-memory queue + hosted pump.

**Gate:** `ArchiForgeHostingRole`

| Role | Behavior |
|------|----------|
| `Worker` | If durable: durable infra + `BackgroundJobQueueProcessorHostedService`; else nothing extra |
| `Api` / `Combined` | If durable: durable infra + `DurableBackgroundJobQueue` + notify sender; else `InMemoryBackgroundJobQueue` + hosted service cast |
| Other | Early return after registering executor only where applicable |

---

## `RegisterAgentExecution`

**Gate:** `AgentExecution:Mode`

| Value | Registrations (high level) |
|-------|----------------------------|
| `Simulator` | `DeterministicAgentSimulator`, `SimulatorExecutionTraceRecordingExecutor` as `IAgentExecutor`, fake completion client |
| Other / Real | `RealAgentExecutor`, agent handlers (topology, cost, compliance, critic), parsers; optional `AzureOpenAI:*` + circuit breaker + quota-wrapped `IAgentCompletionClient` |

Related options: `AgentPromptCatalog`, `LlmTokenQuota`, `LlmTelemetry`.

---

## `RegisterGovernance`

**Gate:** `ArchiForge:StorageProvider=InMemory` → singleton in-memory governance repos; else scoped Dapper repos.

Always: `IGovernanceWorkflowService` scoped.

---

## `RegisterRetrieval`

- **`RetrievalEmbeddingCapOptions`** section
- **`Retrieval:VectorIndex`** — `AzureSearch` vs in-memory vector index
- Embeddings: full `AzureOpenAI:Embedding*` + `AzureOpenAI:CircuitBreaker` → Azure embedding service; else `FakeEmbeddingService`

---

## Dual `IGoldenManifestRepository` / `IDecisionTraceRepository` (important)

- **Decisioning / authority (Persistence)** types are registered in **`AddArchiForgeStorage`** (e.g. SQL or InMemory authority stores).
- **Coordinator / application workflow (Data)** types with the **same interface names** are registered in **`RegisterCoordinatorDecisionEngineAndRepositories`** — see comments in that file. ADR 0004 documents the split.

---

## Mental model: who calls whom for a new run

HTTP **`POST /v1/architecture/request`** → `RunsController` → `IArchitectureRunService` → `ICoordinatorService` → `IAuthorityRunOrchestrator` (unit of work, optional deferred pipeline work) → application-layer persistence of run/request/tasks in a separate transaction scope. Details: [day-one-developer.md — Mental model](onboarding/day-one-developer.md#mental-model-post-v1architecturerequest).

---

**Last reviewed:** 2026-04-04
