> **Scope:** DI registration map (Host.Composition) - full detail, tables, and links in the sections below.

# DI registration map (Host.Composition)

**Purpose:** One-page map from extension methods and partial `ServiceCollectionExtensions` files to the capabilities they register, including configuration gates and key sections.

**Entry point:** `ArchLucid.Host.Composition.Startup.ServiceCollectionExtensions.AddArchLucidApplicationServices` orchestrates everything below (after options on the main partial of the same class).

**Source files** (under `ArchLucid.Host.Composition/`):

| File | Responsibility |
|------|----------------|
| `Startup/ServiceCollectionExtensions.cs` | `AddArchLucidApplicationServices` — calls all `Register*` methods in order |
| `Startup/ServiceCollectionExtensions.FeatureManagement.cs` | `AddArchLucidFeatureManagement` — **`IFeatureFlags`** → **`FeatureManagementFeatureFlags`** |
| `Configuration/ArchLucidStorageServiceCollectionExtensions.cs` | `AddArchLucidStorage` |
| `Startup/ServiceCollectionExtensions.SchedulingAndAlerts.cs` | Advisory schedules, digests, integration event publisher, alerts, retrieval/authority pipeline workers, integration-event outbox, archival host |
| `Startup/ServiceCollectionExtensions.DataHealthAndJobs.cs` | Health checks, background jobs (`IDbConnectionFactory` is owned by **`AddArchLucidStorage`** — InMemory → **`UnsupportedRelationalDbConnectionFactory`**, Sql → **`SqlScopedResolutionDbConnectionFactory`**) |
| `Startup/ServiceCollectionExtensions.ApplicationPipeline.cs` | Run export, analysis, replay/drift, **core run pipeline** (`IArchitectureRunService`, …), context ingestion + knowledge graph |
| `Startup/ServiceCollectionExtensions.Decisioning.cs` | Finding engines, decision engine, manifest builders/validators |
| `Startup/ServiceCollectionExtensions.CoordinatorAndArtifacts.cs` | Coordinator, decision engine services, **`ArchLucid.Persistence.Data.Repositories`** for runs/tasks/manifests, artifact synthesis |
| `Startup/ServiceCollectionExtensions.AgentsGovernanceRetrieval.cs` | Agent execution (Simulator vs Real), governance repos, retrieval + embeddings |
| `Startup/ServiceCollectionExtensions.CosmosPolyglotPersistence.cs` | Optional **`CosmosDb`** graph / agent traces / audit when flags enabled — **`RegisterCosmosPolyglotPersistence`** (runs last so registrations can override SQL defaults) |

---

## `AddArchLucidApplicationServices` (order)

Cross-cutting options bound on the main partial (not exhaustive): `Demo`, `BatchReplay`, `ApiDeprecation`, `DataArchival`, `HostLeaderElection`.

1. `IDemoSeedService`
2. **`AddArchLucidFeatureManagement`** → `FeatureManagement` section ( **`IFeatureFlags`** — see § **`IFeatureFlags` (feature management)** below)
3. **`AddArchLucidStorage`** → **`ArchLucid:StorageProvider`** (`Sql` vs `InMemory`) — see below (Phase 7: prefer **`ArchLucid:StorageProvider`** when bridges are removed)
4. **`RegisterAdvisoryScheduling`** — **role:** `Combined` \| `Worker` → `AdvisoryScanHostedService`
5. **`RegisterDigestDelivery`** → `WebhookDelivery` (+ HTTP client named **`ArchLucidWebhooks`**)
6. **`RegisterIntegrationEventPublishing`** → `IntegrationEvents` / Service Bus publisher (or null publisher when unset)
7. **`RegisterAlerts`** — evaluators, channels, dispatcher, `IAlertService`
8. **`RegisterBackgroundJobs`** → **`BackgroundJobs`** (`Mode`: `Durable` vs in-memory); **role** gates queue processor vs API queue sender — see below
9. **`RegisterRunExportAndArchitectureAnalysis`** — gated **`IRunExportRecordRepository`** by `StorageProvider`
10. **`RegisterComparisonReplayAndDrift`** → `ReplayDiagnostics`
11. **`RegisterRunReplayManifestAndDiffs`** — `IArchitectureRunService`, replay, diffs, exports, `IActorContext`, audit
12. **`RegisterContextIngestionAndKnowledgeGraph`** — connectors, parsers, `IContextIngestionService`, graph builder/service
13. **`RegisterDecisioningEngines`** — findings orchestrator, rule engine, manifest services, compliance pack loader
14. **`RegisterCoordinatorDecisionEngineAndRepositories`** — gated workflow repos (`ArchLucid.Persistence.Data.Repositories`) by **`StorageProvider`**; **`IRunExplanationSummaryService`** — see **`CachingRunExplanationSummaryService`** below
15. **`RegisterArtifactSynthesis`**
16. **`RegisterAgentExecution`** → **`AgentExecution:Mode`** (`Simulator` vs Real), `AzureOpenAI:*`, **`ArchLucid:FallbackLlm`** (optional), `LlmTokenQuota`, `LlmTelemetry`, `AgentPromptCatalog`
17. **`RegisterRetrieval`** → `Retrieval:VectorIndex` (`AzureSearch` vs in-memory), `AzureOpenAI:Embedding*`, `AzureOpenAI:CircuitBreaker`
18. **`RegisterGovernance`** → **`ArchLucid:StorageProvider`** for governance repos (InMemory singletons vs SQL scoped)
19. **`RegisterRetrievalIndexingOutbox`** — **role:** `Combined` \| `Worker` → hosted outbox + authority pipeline work processors
20. **`RegisterIntegrationEventOutbox`** — **role:** `Combined` \| `Worker` → `IntegrationEventOutboxHostedService`
21. **`RegisterIntegrationEventConsumer`** — **role:** `Worker` only — **`AzureServiceBusIntegrationEventConsumer`** + **`LoggingIntegrationEventHandler`** when Service Bus consumption is enabled (**`ServiceCollectionExtensions.SchedulingAndAlerts.cs`**)
22. **`RegisterDataArchivalHostedService`** — **role:** `Combined` \| `Worker`
23. **`RegisterArchLucidHealthChecks`** — SQL, schema, compliance pack, blob, temp dir; archival check when worker/combined
24. **`RegisterCosmosPolyglotPersistence`** — optional Cosmos overrides when **`CosmosDb:*`** features enabled (**`ServiceCollectionExtensions.CosmosPolyglotPersistence.cs`**); invoked **after** health checks so last registration wins for overlapping repository ports

---

## `AddArchLucidStorage` (`ArchLucidStorageServiceCollectionExtensions`)

**Gate:** `ArchLucid:StorageProvider` (Phase 7: **`ArchLucid:StorageProvider`**)

**Config (both modes):** **`AuthorityPipeline`** section → **`AuthorityPipelineOptions`** (`PipelineTimeout`; **`00:00:00`** disables timeout).

### `InMemory`

- Singleton in-memory persistence for authority stores (context/graph/findings/manifest/trace/artifact/run, audit, provenance, advisory, alerts, policy packs, conversations, product learning, evolution, pipeline work, retrieval outbox).
- **`IArchLucidUnitOfWorkFactory`** → `InMemoryArchLucidUnitOfWorkFactory`
- **`IAsyncAuthorityPipelineModeResolver`** → `DisabledAsyncAuthorityPipelineModeResolver` (async/deferred pipeline path effectively off)
- **`IAuthorityRunOrchestrator`**, **`IAuthorityPipelineStagesExecutor`**, **`IDataArchivalCoordinator`**, host leader lease **NoOp**
- Optional: distributed cache + LLM completion response store when configured

### `Sql` (default production shape)

- **`ConnectionStrings:ArchLucid`** required (see host configuration docs for any deprecated key warnings at startup)
- **`SqlServer`** section — RLS session context, read-replica routing (`ReadReplicaRoutedConnectionFactory` for list/governance/manifest lookup routes)
- **`ISqlConnectionFactory`** — resilient open + optional `SessionContextSqlConnectionFactory`
- Dapper/SQL implementations for the same repository surface as InMemory
- **`IAsyncAuthorityPipelineModeResolver`** → `FeatureManagementAuthorityPipelineModeResolver` (coordinates with **`FeatureManagement`**)
- **`IData.Infrastructure.IDbConnectionFactory`** → `SqlScopedResolutionDbConnectionFactory` (scoped resolution for legacy Data access)
- Schema bootstrap via `ISqlConnectionFactory` + embedded **`ArchLucid.sql`** (Phase 7: rename script coordinated with deploy)
- **`ArtifactLargePayload`**, **`HotPathCache`**, LLM completion cache options as applicable

---

## `RegisterBackgroundJobs`

**Config:** `BackgroundJobs:Mode` — `Durable` uses Azure Queue + `BackgroundJobRepository` + blob result accessor; otherwise in-memory queue + hosted pump.

**Gate:** `ArchLucidHostingRole`

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
| Other / Real | `RealAgentExecutor`, agent handlers (topology, cost, compliance, critic), parsers; optional `AzureOpenAI:*` + circuit breaker + quota-wrapped `IAgentCompletionClient`; when **`ArchLucid:FallbackLlm:Enabled`** and keys are complete, scoped **`IAgentCompletionClient`** is **`FallbackAgentCompletionClient`** (outermost) over **two** stacks (primary + fallback **`AzureOpenAiCompletionClient`**, each with its own **`OpenAiCompletion`** / **`OpenAiCompletionFallback`** gate) |

Related options: `AgentPromptCatalog`, `LlmTokenQuota`, `LlmTelemetry`, **`FallbackLlmOptions`** (**`ArchLucid:FallbackLlm`**).

### `IFeatureFlags` (feature management)

| Registration | Lifetime | Notes |
|--------------|----------|--------|
| **`IFeatureFlags`** → **`FeatureManagementFeatureFlags`** | Singleton | Registered in **`AddArchLucidFeatureManagement`**; **`FeatureManagementAuthorityPipelineModeResolver`** consumes **`IFeatureFlags`** (not **`IFeatureManager`**). |

### `FallbackAgentCompletionClient` (conditional)

| Condition | Registration | Lifetime |
|-----------|----------------|----------|
| **`ArchLucid:FallbackLlm:Enabled`** is **`true`** and Endpoint, ApiKey, DeploymentName are set | Scoped **`IAgentCompletionClient`** outer decorator | Scoped (per request scope) |
| Otherwise | Prior Azure real-client pipeline unchanged | Scoped |

---

## `RegisterGovernance`

**Gate:** `ArchLucid:StorageProvider=InMemory` → singleton in-memory governance repos; else scoped Dapper repos.

Always: `IGovernanceWorkflowService` scoped.

Also: `IGovernanceDashboardService` → `GovernanceDashboardService` scoped; `IComplianceDriftTrendService` → `ComplianceDriftTrendService` scoped.

---

## `RegisterRetrieval`

- **`RetrievalEmbeddingCapOptions`** section
- **`Retrieval:VectorIndex`** — `AzureSearch` vs in-memory vector index
- Embeddings: full `AzureOpenAI:Embedding*` + `AzureOpenAI:CircuitBreaker` → Azure embedding service; else `FakeEmbeddingService`

---

## Dual `IGoldenManifestRepository` / `IDecisionTraceRepository` (important)

- **Decisioning / authority (Persistence)** types are registered in **`AddArchLucidStorage`** (e.g. SQL or InMemory authority stores).
- **Coordinator / application workflow (Data)** types with the **same interface names** are registered in **`RegisterCoordinatorDecisionEngineAndRepositories`** — see comments in that file. ADR 0004 documents the split.

---

## `IRunExplanationSummaryService` + `CachingRunExplanationSummaryService` (hot path)

Registered in **`RegisterCoordinatorDecisionEngineAndRepositories`** (`ServiceCollectionExtensions.CoordinatorAndArtifacts.cs`), mirroring **`CachingRunRepository`** / **`CachingGoldenManifestRepository`**: **`IHotPathReadCache`** is only registered when **`HotPathCache:Enabled`** is true in **`AddArchLucidStorage`**.

| **`HotPathCache:Enabled`** | `IRunExplanationSummaryService` registration |
|----------------------------|---------------------------------------------|
| **false** | **`RunExplanationSummaryService`** directly (scoped) |
| **true** | Scoped concrete **`RunExplanationSummaryService`** + scoped **`CachingRunExplanationSummaryService`** decorator implementing **`IRunExplanationSummaryService`**, delegating to **`GetOrCreateAsync`** on **`IHotPathReadCache`**. Cache key: `explanation:aggregate:{runId}:{hex(RowVersion)}` after **`IAuthorityQueryService.GetRunDetailAsync`**; **no** cache when detail is null (returns null) or **`GoldenManifest`** is missing (returns null); if **`Run.RowVersion`** is missing, delegates to the inner service without caching (same TTL behavior as other hot-path entries: **`HotPathCache:AbsoluteExpirationSeconds`**). |

---

## Mental model: who calls whom for a new run

HTTP **`POST /v1/architecture/request`** → `RunsController` → `IArchitectureRunService` → `ICoordinatorService` → `IAuthorityRunOrchestrator` (unit of work, optional deferred pipeline work) → application-layer persistence of run/request/tasks in a separate transaction scope. Details: [day-one-developer.md — Mental model](onboarding/day-one-developer.md#mental-model-post-v1architecturerequest).

---

**Last reviewed:** 2026-04-12
