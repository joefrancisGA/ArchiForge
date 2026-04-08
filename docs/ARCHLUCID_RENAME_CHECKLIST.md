# ArchiForge → ArchLucid Rename Checklist

Incremental rename plan. One batch is folded into each working session.
See `.cursor/rules/ArchLucid-Rename.mdc` for the standing instruction.

> **Rule**: keep the build green after every batch. Never break CI.

---

## Phase 1 — Display text and documentation (low risk, any session)

- [x] 1.1 Update product name in `archlucid-ui/src/app/layout.tsx` (title, heading)
- [x] 1.2 Update `archlucid-ui/README.md` — product name references
- [x] 1.3 Update root `README.md` — product name, badges, description (2026-04-04)
- [x] 1.4 Update `docs/GLOSSARY.md` (2026-04-05)
- [x] 1.5 Update `docs/ARCHITECTURE_CONTEXT.md`, `docs/ARCHITECTURE_COMPONENTS.md`, `docs/ARCHITECTURE_CONTAINERS.md` (2026-04-05)
- [x] 1.6 Update `docs/DEPLOYMENT.md`, `docs/DEPLOYMENT_TERRAFORM.md`, `docs/CONTAINERIZATION.md` (2026-04-06 — ArchLucid product naming pass where referenced)
- [x] 1.7 Update `docs/PILOT_GUIDE.md`, `docs/OPERATOR_QUICKSTART.md`, `docs/RELEASE_SMOKE.md`, `docs/RELEASE_LOCAL.md` (2026-04-06 — ArchLucid product naming in pilot/release docs; literal `ArchiForge.*` paths and config keys unchanged)
- [x] 1.8 Update onboarding docs: `docs/onboarding/day-one-developer.md`, `day-one-sre.md`, `day-one-security.md`, `docs/CONTRIBUTOR_ONBOARDING.md`, `docs/ONBOARDING_HAPPY_PATH.md` (2026-04-06 — ArchLucid product naming in prose; repo folder, `ArchiForge.*` assemblies, and config keys unchanged)
- [x] 1.9 Update `docs/BUILD.md`, `docs/FORMATTING.md`, `docs/TEST_STRUCTURE.md`, `docs/TEST_EXECUTION_MODEL.md` (2026-04-06 — ArchLucid product naming note / prose; `ArchiForge.*` paths unchanged)
- [x] 1.10 Update remaining docs: ADRs, runbooks, CLI docs, changelogs, and any other `.md` files with stale references (batch as needed) (2026-04-07 — product naming + API/DI/header alignment; Phase 7 literals retained)
  - [x] API SLO doc + synthetic probe runbook link (`docs/API_SLOS.md`, `SLO_PROMETHEUS_GRAFANA.md` §10, `.github/workflows/api-synthetic-probe.yml`) (2026-04-06)
  - [x] AgentRuntime multi-vendor LLM seam: `ILlmProvider`, `LlmProviderDescriptor`, `LlmProviderAuthScheme`; `IAgentCompletionClient` : `ILlmProvider`; DI `ILlmProvider` → `ILlmCompletionProvider` (2026-04-06)
  - [x] Agent prompt versioning: catalog templates + SHA-256, `AgentExecutionTrace` repro fields, `AgentPrompts` release labels, OTel tags (2026-04-06)
  - [x] `docs/CONTAINERIZATION.md` — Docker `restore` / `publish` stages must use `-r linux-musl-x64` with `--no-restore` publish (NETSDK1047) (2026-04-06)
- [x] 1.11 Update `archlucid-ui/docs/*.md` (ARCHITECTURE, COMPONENT_REFERENCE, TESTING_AND_TROUBLESHOOTING, OPERATOR_SHELL_TUTORIAL, etc.) (2026-04-07)
- [x] 1.12 Update `.cursor/rules/Navigation.mdc` and `CSharp-EmbeddedStatements-NoBraces.mdc` (2026-04-06 — Navigation: ArchLucid product naming + `ArchLucidHostingRole.cs` path; CSharp rule: no product literals, verified)

## Phase 2 — Configuration key bridges (medium risk, any session)

- [x] 2.1 Add fallback readers for `ArchiForgeAuth` → `ArchLucidAuth` in auth options binding (2026-04-05)
- [x] 2.2 Add fallback readers for `ArchiForge:StorageProvider` → `ArchLucid:StorageProvider` (2026-04-05)
- [x] 2.3 Add fallback readers for any other `ArchiForge*` config sections (grep `appsettings*.json`) (2026-04-07 — `ResolveArchLucidOptions` loads legacy `ArchiForge` section then overlays `ArchLucid`; `FeatureManagementAuthorityPipelineModeResolver` uses bridge for effective storage mode)
- [x] 2.4 Update `appsettings*.json` files to use new key names — **2026-04-07** (`ArchLucid`, `ArchLucidAuth`, `ConnectionStrings:ArchLucid`, Webhook CloudEvents paths, consulting template strings; bridge still reads legacy `ArchiForge` keys)
- [x] 2.5 Update `.env.example` — `ARCHLUCID_API_KEY` preferred + `ARCHIFORGE_API_KEY` fallback in proxy (2026-04-05)
- [x] 2.6 Add OIDC storage key bridge reads (`archiforge_oidc_*` → `archlucid_oidc_*`) in `session.ts` and `storage-keys.ts` — **2026-04-07**

## Phase 3 — UI directory and package rename (medium risk, any session)

- [x] 3.1 Rename `archiforge-ui/` directory → `archlucid-ui/` — **2026-04-07**
- [x] 3.2 Update `package.json` name field — **2026-04-07**
- [x] 3.3 Update Dockerfile path references (API Dockerfile, docker-compose, CI workflows) — **2026-04-07**
- [x] 3.4 Update `archlucid-ui` references in root `README.md`, docs, and CI — **2026-04-07**
- [x] 3.5 Update TypeScript imports, API route env var names, and correlation header names — **2026-04-07** (`ARCHIFORGE_API_BASE_URL` retained with proxy bridge)

## Phase 4 — Infrastructure rename (medium-high risk, one stack per session)

- [x] 4.1 `infra/terraform-monitoring/` — **2026-04-07** (descriptions + Grafana folder title **ArchLucid**; Terraform resource addresses e.g. `grafana_folder.archiforge` unchanged for state)
- [x] 4.2 `infra/terraform-storage/` — **2026-04-07** (no product literals requiring change)
- [x] 4.3 `infra/terraform-edge/` — **2026-04-07** (no product literals requiring change)
- [x] 4.4 `infra/terraform-container-apps/` — **2026-04-07**
  - [x] Worker `command` → `ArchLucid.Worker.dll` (2026-04-06). Defaults **`archlucid-api`**, **`archlucid-ui`**, **`archlucid-worker`**; template container names updated. Legacy defaults (`law-archiforge-ca`, `cae-archiforge`, …) **unchanged** pending `state mv`.
- [x] 4.5 `infra/terraform-sql-failover/` — **2026-04-07** (defaults unchanged for state)
- [x] 4.6 `infra/terraform-openai/` — **2026-04-07**
- [x] 4.7 `infra/terraform-entra/` — **2026-04-07** (consent strings + default display **ArchLucid API**)
- [x] 4.8 `infra/terraform/` — **2026-04-07** (APIM display **ArchLucid API**, publisher default **ArchLucid**)
- [x] 4.9 `infra/terraform-private/` — **2026-04-07** (PE resource names unchanged for state)
- [x] 4.10 Prometheus — **`archlucid-alerts.yml`**, **`archlucid-slo-rules.yml`** — **2026-04-07** (recording series `archiforge:*` / metrics `archiforge_*` unchanged until Phase 7)
- [x] 4.11 Grafana — **`dashboard-archlucid-*.json`** — **2026-04-07**
- [x] 4.12 CI/CD workflows — **`archlucid-api`**, **`archlucid-ui`** image tags — **2026-04-07**
- [x] 4.13 `docker-compose.yml` — **2026-04-07** (`ConnectionStrings__ArchLucid`, `ArchLucid__*`, `archlucid-api` container)
- [x] 4.14 `stryker-config.json`, `coverage.runsettings`, `.devcontainer/devcontainer.json` — **2026-04-07** (verified: no `archiforge` literals)

## Phase 5 — .NET project directory and file renames (high risk — ASK USER FIRST)

> **Do not start this phase without explicit user confirmation.**
> Strategy: rename directories and `.csproj` files but preserve `<RootNamespace>ArchiForge.*</RootNamespace>`
> so no C# source changes are needed yet. Update `.sln` and `<ProjectReference>` paths.

- [x] 5.1 Rename leaf test projects: `*.Tests` directories and `.csproj` files (2026-04-06 — folders + `ArchLucid.*.Tests.csproj`; `AssemblyName`/`RootNamespace` kept `ArchiForge.*.Tests` until Phase 6)
- [x] 5.2 Rename `ArchiForge.TestSupport` → `ArchLucid.TestSupport`, `ArchiForge.Benchmarks` → `ArchLucid.Benchmarks` (2026-04-06)
- [x] 5.3 Rename `ArchiForge.Cli`, `ArchLucid.Cli.Tests`, `ArchiForge.Backfill.Cli` → `ArchLucid.*` (2026-04-06)
- [x] 5.4 Rename `ArchiForge.Api.Client`, `ArchLucid.Api.Client.Tests` → `ArchLucid.Api.Client*` dirs (2026-04-06)
- [x] 5.5 Rename domain projects: `Core`, `Contracts`, `Persistence`, `Application`, `AgentRuntime`, `AgentSimulator` (2026-04-06)
- [x] 5.6 Rename domain projects: `Coordinator`, `Decisioning`, `KnowledgeGraph`, `Retrieval`, `ContextIngestion`, `Provenance`, `ArtifactSynthesis` (2026-04-06)
- [x] 5.7 Rename host projects: `Host.Core`, `Host.Composition` (2026-04-06)
- [x] 5.8 Rename `ArchiForge.Api`, `ArchiForge.Worker` → `ArchLucid.Api`, `ArchLucid.Worker` (2026-04-06)
- [x] 5.9 `ArchiForge.sln` → `ArchLucid.sln` (paths + CI/scripts/docs) (2026-04-06)
- [x] 5.10 Update `Directory.Build.props` — `ArchLucidApiClientPackageVersion` + `ArchLucidApiClientPackageVersion` bridge (2026-04-06)
- [x] 5.11 Update `templates/archlucid-finding-engine/` directory and contents — **2026-04-07** (`ArchLucidFindingEngine`, `dotnet new archlucid-finding-engine`)
- [x] 5.12 Rename C# files with `ArchiForge` prefix (instrumentation, configuration bridge, policies, UoW, CLI client, etc.) (2026-04-06)
- [x] 5.13 Update NSwag config (`nswag.json`) and regenerate client (`ArchLucidApiClient.g.cs`) (2026-04-06)

## Phase 6 — Bulk namespace rename (high risk — ASK USER FIRST)

> **Do not start this phase without explicit user confirmation.**
> This is a single large pass that updates all `namespace ArchiForge.*` and `using ArchiForge.*` directives.
> Remove `<RootNamespace>` overrides added in Phase 5.

- [x] 6.1 Bulk rename all `namespace ArchiForge.*` → `namespace ArchLucid.*` across product `.cs` files (2026-04-06)
- [x] 6.2 Bulk rename all `using ArchiForge.*` → `using ArchLucid.*` (2026-04-06)
- [x] 6.3 Update XML doc `<see cref="ArchiForge.*">` references where applicable (2026-04-06)
- [x] 6.4 Update string literals: OTel meter/activity names, `X-ArchLucid-*` headers, Swagger titles; legacy **`ArchiForge:*` / `ArchiForgeAuth` / `ArchiForge.sql` / DB name** intentionally retained where operational (2026-04-06); **2026-04-08** follow-up — user-facing/export/CLI/prompt strings → **ArchLucid**; CI guard `scripts/ci/archiforge-rename-allowlist.txt` + `.github/workflows/ci.yml` step after Release build
- [x] 6.5 Remove `<RootNamespace>` overrides — product projects use `ArchLucid.*` aligned with dirs (2026-04-06)
- [x] 6.6 Full `Release` build + fast-core test filter; relaxed flaky bulkhead timing assertion (2026-04-06)
- [x] 6.7 Update `docs/NEXT_REFACTORINGS.md` to reflect completion (2026-04-06)

## Phase 7 — Cleanup and external (operational — ASK USER FIRST)

- [ ] 7.1 Remove config key fallbacks added in Phase 2 (old `ArchiForge*` keys)
- [ ] 7.2 Remove OIDC storage key bridge reads (old `archiforge_oidc_*` keys)
- [ ] 7.3 Remove environment variable fallbacks (old `ARCHIFORGE_*` vars)
- [ ] 7.4 Update SQL master DDL script (`ArchiForge.sql` → `ArchLucid.sql`); add a new migration for database rename if needed
- [ ] 7.5 Terraform `state mv` operations for renamed resources (coordinate with deploy window)
- [ ] 7.6 Rename GitHub repository
- [ ] 7.7 Update Entra ID app registrations (redirect URIs, display names)
- [ ] 7.8 Rename workspace root directory (`c:\ArchiForge\ArchiForge` → `c:\ArchLucid\ArchLucid`)
- [ ] 7.9 Final grep for any remaining `ArchiForge` or `archiforge` references; update this checklist

---

## Progress log

| Date | Batch | Notes |
|------|-------|-------|
| 2026-04-08 | (LLM resilience) | **Per-call Polly retry inside CB decorators** (`LlmCallResilienceDefaults`, `AgentExecution:Resilience` LLM keys); OTel `archlucid_llm_call_retries_total`; Simmy tests + `LlmCallChaosEndToEndTests`; scheduled **`.github/workflows/simmy-chaos-scheduled.yml`**; **`docs/LLM_RETRY_AND_CIRCUIT_BREAKER.md`**. Docker sample `appsettings` Resilience keys aligned. Phase 7 unchanged. |
| 2026-04-08 | (auditability) | **`docs/AUDIT_COVERAGE_MATRIX.md`** + CI guard; **`IAuditRepository.GetFilteredAsync`** + `GET /v1/audit/search` + `GET /v1/audit/event-types`; **`CircuitBreakerAuditEntry`** / optional **`CircuitBreakerGate`** callback + **`CircuitBreakerAuditBridge`** DI; operator UI **`/audit`** + **`ShellNav`**. OpenAPI snapshot regen. Phase 7 unchanged. |
| 2026-04-08 | (explainability) | **`ExplainabilityTraceCompletenessAnalyzer`** + advisory `ResultJson.traceCompleteness` + OTel histogram `archlucid_explainability_trace_completeness_ratio`; engine trace fills (policy/requirement/topology coverage, rules/notes on baseline/cost/requirement/compliance). UI: `ProvenanceGraphDiagram` on coordinator provenance page. **`docs/EXPLAINABILITY_TRACE_COVERAGE.md`**. Phase 7 unchanged. |
| 2026-04-08 | (integration events) | **Single publish entry point:** `AuthorityRunOrchestrator` uses `OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync` for `com.archlucid.authority.run.completed` (transactional enqueue when enabled + `SupportsExternalTransaction`). CI grep + `DependencyConstraintTests` guard direct `IIntegrationEventPublisher.PublishAsync` except `IntegrationEventPublishing` / `IntegrationEventOutboxProcessor`. **JSON Schema** catalog under `schemas/integration-events/`; `IntegrationEventPayloadContractTests` assert payloads against committed schemas. Docs: `INTEGRATION_EVENTS_AND_WEBHOOKS.md`. Phase 7 unchanged. |
| 2026-04-08 | Phase 6.4 cleanup | **String literal rename sweep (~60 replacements):** API/Swagger/Worker startup messages, agent prompts, export/report headings, CLI operator output, decisioning manifest labels, artifact packaging, XML doc comments, validation + Swagger auth description text (new key first, legacy `ArchiForge*` named where still valid). Tests + temp prefixes aligned. **CI:** `Guard — unexpected ArchiForge string literals in C#` after `dotnet build` in `dotnet-fast-core`; allowlist = Phase 7 bridges, `ArchiForge.sql`, connection string / config key literals, test factories. Remaining `ArchiForge` in `.cs` = Phase 7 / compatibility only. |
| 2026-04-07 | (resilience) | **External resilience + CB metrics:** named `CircuitBreakerOptions` (`AzureOpenAI:CircuitBreaker:Completion` / `:Embedding` + shared fallback); `IOptionsFactory` snapshot at keyed `CircuitBreakerGate` construction; OTel counters `archiforge_circuit_breaker_*` on the gate; `Persistence:SqlOpenResilience` for SQL open retries; CLI `httpResilience` in `archiforge.json`. **`docs/RESILIENCE_CONFIGURATION.md`**. Tests: `CircuitBreakerGateMetricsTests`, `SqlOpenResilienceOptionsTests`, CLI handler/options. **`IntegrationEventOutboxProcessorCorrelationTests`** filter activities by `archiforge.outbox_id` so parallel `Suite=Core` runs do not flake. Phase 7 deferred. |
| 2026-04-08 | (CI) | **CodeQL JavaScript job:** `source-root` and npm paths use `ARCHLUCID_UI_DIR=archlucid-ui` (post–Phase 3 rename); verify step fails fast if the folder is missing. |
| 2026-04-08 | (integration events) | **Transactional outbox for all 6 event types** via `OutboxAwareIntegrationEventPublishing`; `IIntegrationEventOutboxRepository.EnqueueAsync` takes `Guid? runId`. **Canonical** `com.archlucid.*` type strings + legacy map (`IntegrationEventTypes`). **Worker** `AzureServiceBusIntegrationEventConsumer` + `LoggingIntegrationEventHandler`; `IntegrationEventsOptions` consumer settings. **Terraform** `infra/terraform-servicebus/`. Docs: `INTEGRATION_EVENTS_AND_WEBHOOKS.md`, AsyncAPI. Tests: outbox-aware, contract payloads, governance outbox path, `LoggingIntegrationEventHandler`. |
| 2026-04-07 | (observability) | **Background job correlation:** new `ActivitySource`s `RetrievalIndexingOutbox`, `IntegrationEventOutbox`, `DataArchival` + OTel registration; `RetrievalIndexingOutboxProcessor`, `IntegrationEventOutboxProcessor`, `DataArchivalCoordinator` set `correlation.id` + Serilog `CorrelationId`; `AdvisoryScanRunner` / `AuthorityRunOrchestrator` push `LogContext`. **`docs/BACKGROUND_JOB_CORRELATION.md`**. Persistence tests: `*CorrelationTests` (`Suite=Core`). Serilog 3.1.1 package for `LogContext` on coordination/integration/runtime/advisory persistence projects. |
| 2026-04-07 | (architecture tests) | **`ArchLucid.Architecture.Tests`**: NetArchTest.Rules 1.3.2, 15× `[Fact]` + `Suite=Core` in `DependencyConstraintTests`; Tier 2/CLI API boundary via `GetReferencedAssemblies()` where namespace prefix would false-positive (`ArchLucid.Api.Client`). **`docs/ARCHITECTURE_CONSTRAINTS.md`** + index link. Project added to **`ArchLucid.sln`**. Phase 7 unchanged (await explicit go-ahead). |
| 2026-04-07 | (bugfix) | **`FindingsJson` read path:** `SqlFindingsSnapshotRepository` calls `FindingPayloadJsonCodec.HydrateJsonElementPayloads` after `FindingsSnapshotMigrator.Apply` so JSON-deserialized findings get typed `Payload` (matches relational read). Tests: `FindingPayloadJsonCodecTests` + `SqlFindingsSnapshotRepositorySqlIntegrationTests.GetById_when_no_FindingRecords_falls_back_to_FindingsJson`. |
| 2026-04-07 | 2.3 | ArchLucid product config section merge + authority resolver uses `ResolveArchLucidOptions` |
| 2026-04-07 | 2.4–2.6, 3.x, 4.x, 5.11 | Appsettings ArchLucid keys + `IOptions`/`ResolveArchLucidOptions` legacy-base fix; OIDC storage bridge; **`archlucid-ui`**; CI images **`archlucid-api`**/**`archlucid-ui`**; Prometheus/Grafana **`archlucid-*`** files; template **`archlucid-finding-engine`**; Terraform display strings (Entra/APIM/monitoring). |
| 2026-04-04 | 1.1 | layout.tsx title + heading → ArchLucid |
| 2026-04-05 | 1.2 | archlucid-ui/README.md product-facing name → ArchLucid (env keys unchanged) |
| 2026-04-05 | 1.4 | GLOSSARY.md title + ArchLucid note; outbox / integration dead-letter terms |
| 2026-04-05 | 2.1–2.2, 2.5 | Config bridges + `ResolveArchiForgeOptions` at early DI sites; UI proxy API key fallback; ops docs (architecture on a page, code map, config sunset, capacity/cost) |
| 2026-04-05 | 1.5 | Architecture context/components/containers titles + ArchLucid product framing; production cost guardrails (`production.tfvars.example`, LA quota note, WAF version var, FinOps playbook §9) |
| 2026-04-05 | (Improvement 5) | Merged `ArchiForge.DecisionEngine` into `ArchLucid.Decisioning` (later flattened to `ArchLucid.Decisioning.Merge` / `ArchLucid.Decisioning.Validation` — see 2026-04-06); added `ArchLucid.Persistence.Runtime` (orchestration, UoW, hot-path cache impls, blob store impls, archival); coordinator repos renamed to `ICoordinatorGoldenManifestRepository` / `ICoordinatorDecisionTraceRepository` in `Persistence.Data.Repositories`. |
| 2026-04-06 | Namespace flatten | Removed nested `Decisioning.DecisionEngine.*` namespaces: code under `ArchLucid.Decisioning/Merge/` and `ArchLucid.Decisioning/Validation/`; tests under `ArchLucid.Decisioning.Tests/Merge/` and `Validation/`; OTel meter `ArchLucid.Decisioning.SchemaValidation` (replaces `ArchiForge.DecisionEngine.SchemaValidation`). |
| 2026-04-06 | Quality batch (six improvements) | Stryker `break:60` (all `stryker-config*.json`); merged line-coverage gate documented in `docs/coverage-exclusions.md` (not Coverlet `<Threshold>` per assembly — see comment in `coverage.runsettings`); CLI `ResolveApiErrorMessage` for typed ProblemDetails; rename `DecisionTrace` → `RunEventTrace` (coordinator) / `RuleAuditTrace` (authority); `GET /v1/authority/runs/{id}/provenance`; UI `/runs/[id]/provenance`; `docs/DUAL_PIPELINE_NAVIGATOR.md`, `docs/CHAOS_TESTING.md`, `CliRetryDelegatingHandlerTests`. Prometheus SLO/outbox alerts already in `infra/prometheus/`. Phase 5–6 bulk project/namespace rename **not** started (explicit scope). |
| 2026-04-06 | 1.7 + CI coverage gate | Phase **1.7** pilot/release docs → ArchLucid product naming (`PILOT_GUIDE`, `OPERATOR_QUICKSTART`, `RELEASE_SMOKE`, `RELEASE_LOCAL`); CI **70%** merged line gate via `scripts/ci/assert_merged_line_coverage_min.py`; `RetrievalIndexingOutboxProcessor` uses `RunDetailDto.DecisionTrace` (fixes stale `RuleAuditTrace` property). |
| 2026-04-06 | **5.1** | All main-solution leaf test projects renamed to **`ArchLucid.*.Tests`** (dirs + csproj); solution + Stryker + Dockerfile + `nswag.json` paths updated; `OpenApiContractSnapshotTests` resolves `ArchLucid.Api.Tests.csproj`; docs: `CI_MIGRATION_CHECKLIST`, `ArchLucid.Api.Client/README`, `Migrations/README`. |
| 2026-04-06 | 1.10 (partial) + cognitive-load | Expanded `docs/DUAL_PIPELINE_NAVIGATOR.md` (side-by-side flow, shared-artifact matrix, coordinator HTTP→commit walkthrough); introduced sealed `RunEventTrace` / `RuleAuditTrace` with abstract `DecisionTrace` + `DecisionTraceJsonConverter` (stable OpenAPI shape); `docs/ONBOARDING_HAPPY_PATH.md` link + corrected `POST /v1/architecture/request`; `docs/ARCHITECTURE_INDEX.md` navigator blurb; `ArchLucid.Contracts.Tests/DecisionTraceJsonRoundTripTests.cs`. |
| 2026-04-06 | **5.2–5.10, 5.12–5.13 + 6.x** | **Phases 5–6:** `git mv` all product projects `ArchiForge.*` → `ArchLucid.*`; **`ArchLucid.sln`**; identifier pass (`AddArchLucid*`, `IArchLucidUnitOfWork`, `ArchLucidConfigurationBridge`, `ResolveSqlConnectionString` ArchLucid→ArchiForge fallback); NSwag **`ArchLucidApiClient`**; **`Directory.Build.props`** `ArchLucidApiClientPackageVersion` + MSBuild bridge; renamed `ArchiForge*.cs` source files; **`X-ArchLucid-*`** headers; CORS policy **`ArchLucid`**; scripts `phase6-rename-identifiers.ps1` / `bulk-replace-archlucid-phase6.ps1`. **Deferred:** template `archlucid-finding-engine` (5.11). |
| 2026-04-06 | (decomposition) | **`ArchitectureRunService`** → thin facade over **`IArchitectureRunCreateOrchestrator`** / **`Execute`** / **`Commit`** (`ArchLucid.Application/Runs/Orchestration/`). **`DecisionEngineService`** delegates to **`DecisionMergeInputGate`**, **`AgentProposalManifestMerger`**, **`DecisionNodeManifestMerger`**, **`ManifestGovernanceMerger`**, **`DecisionMergeTraceRecorder`**, **`GoldenManifestFactory`**, **`DecisionTraceManifestAttachment`**; DI registers merge strategies in **`ServiceCollectionExtensions.CoordinatorAndArtifacts`**. Single-arg **`DecisionEngineService(ISchemaValidationService)`** retained for tests. |
| 2026-04-06 | 1.10 (slice) | **`docs/API_SLOS.md`** — SLO table (99.5% / 5xx budget, p95 guardrail, synthetic canary); **`.github/workflows/api-synthetic-probe.yml`** — scheduled external `GET /health/live` + `GET /version`; **`docs/runbooks/SLO_PROMETHEUS_GRAFANA.md`** §10 + component table row. |
| 2026-04-06 | 1.10 (slice) | **`ILlmProvider`** + **`LlmProviderDescriptor`** / **`LlmProviderAuthScheme`** in `ArchLucid.AgentRuntime`; **`IAgentCompletionClient`** inherits `ILlmProvider`; Azure/echo/fake/wrappers expose `Descriptor`; **`DelegatingLlmCompletionProvider`** overlays telemetry labels; **`ILlmProvider`** registered in host composition. |
| 2026-04-06 | 1.10 (slice) | **Prompt reproducibility:** `Prompts/*SystemPromptTemplate`, **`IAgentSystemPromptCatalog`** / **`CachedAgentSystemPromptCatalog`**, **`AgentPromptReproMetadata`**, trace fields + **`IAgentExecutionTraceRecorder`**; **`AgentPromptActivityTags`**; Simulator traces; NSwag client regen. |
| 2026-04-06 | 1.10 (slice) | **`docs/CONTAINERIZATION.md`** — table + notes: RID-scoped `dotnet restore` matches Alpine `publish -r linux-musl-x64 --no-restore`; aligns with `ArchLucid.Api/Dockerfile` (avoids NETSDK1047 in CI/buildx). |
| 2026-04-06 | Phase 4 slice (4.4 partial) | **`infra/terraform-container-apps/main.tf`** worker `command` → **`ArchLucid.Worker.dll`**; locals comment; **`variables.tf`**, **`terraform.tfvars.example`**, **`README.md`**; **`infra/terraform-entra/variables.tf`** + **`infra/terraform/variables.tf`** descriptions (ArchLucid.Api / API backend). **`terraform fmt`** on `terraform-container-apps`. |
| 2026-04-07 | **1.10 + 1.11** | **Docs + UI docs rename sweep:** `docs/**/*.md` product/DI/telemetry names aligned to **ArchLucid** (`DI_REGISTRATION_MAP` → `Host.Composition`, `AddArchLucid*`, `X-ArchLucid-*`, `SYSTEM_MAP`, ADRs 0010–0011, SQL/runbook prose); **kept** `ArchiForge.sql`, `ArchiForge:*` / `ArchiForgeAuth` / legacy connection string keys / Prometheus alert names / `archiforge_*` metric prefixes where operational. **`archlucid-ui/docs/*.md`** titles + diagrams + examples; **`archlucid-ui/src/app/global-error.tsx`** heading → **ArchLucid**. |
| 2026-04-07 | **2.4–2.6, 3.x, 4.x, 5.11** | **Appsettings** → `ArchLucid` / `ArchLucidAuth` / `ConnectionStrings:ArchLucid` (+ `ResolveArchLucidOptions` legacy-`ArchiForge` base + `IOptions` bridge). **OIDC** sessionStorage `archlucid_oidc_*` + legacy read/migrate. **`archlucid-ui`** dir + CI/CD image names **`archlucid-api`** / **`archlucid-ui`**. **Prometheus/Grafana** files `archlucid-*.yml` / `dashboard-archlucid-*.json`. **Template** `templates/archlucid-finding-engine`. Terraform **human-facing** strings (Entra consent, APIM display, monitoring descriptions); many **resource addresses** / defaults still `archiforge*` until Phase 7 `state mv`. |
