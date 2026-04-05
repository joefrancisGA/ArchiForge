# ArchLucid (ArchiForge) — Comprehensive Quality Assessment

**Date**: 2026-04-04
**Scope**: Full codebase — .NET backend, Next.js UI, Terraform IaC, CI/CD, documentation
**Method**: Evidence-based static analysis across 22 quality dimensions

> Dimensions are ordered **weakest first, strongest last**.

---

## Scoring Summary

| # | Dimension | Score | Trend |
|---|-----------|-------|-------|
| 1 | Interoperability | 42 | — |
| 2 | Deployability | 48 | Up |
| 3 | Availability | 52 | — |
| 4 | Usability | 55 | Up |
| 5 | Cost-Effectiveness | 58 | Up |
| 6 | Scalability | 60 | — |
| 7 | Cognitive Load | 62 | Up |
| 8 | Performance | 63 | — |
| 9 | Evolvability | 65 | — |
| 10 | AI/Agent Readiness | 66 | — |
| 11 | Manageability | 67 | — |
| 12 | Data Consistency | 68 | — |
| 13 | Reliability | 69 | Up |
| 14 | Extensibility | 72 | — |
| 15 | Observability | 73 | — |
| 16 | Deployability (CI only) | — | — |
| 17 | Policy & Governance Alignment | 74 | — |
| 18 | Architectural Integrity | 76 | Up |
| 19 | Testability | 78 | — |
| 20 | Modularity | 79 | Up |
| 21 | Security | 80 | Up |
| 22 | Documentation | 82 | — |
| 23 | Maintainability | 83 | Up |
| | **Weighted Average** | **67** | |

---

## Detailed Assessment (Weakest → Strongest)

---

### 1. Interoperability — 42 / 100

**Justification**: The system communicates externally almost exclusively via webhook HTTP POSTs (Slack, Teams, on-call) and a REST/OpenAPI surface. There is no event bus, no Service Bus/Event Grid integration, no GraphQL or gRPC endpoint, no standardized event schema (CloudEvents), and no published SDK beyond the NSwag-generated `ArchiForge.Api.Client`. The CLI calls the REST API directly. There are no documented integration patterns for third-party tools to consume architecture decisions, findings, or governance state changes.

**Evidence**:
- Webhook delivery: `IWebhookPoster`, `AlertSlackWebhookDeliveryChannel`, `AlertTeamsWebhookDeliveryChannel`, `DigestSlackWebhookDeliveryChannel` — outbound-only HTTP POSTs.
- No `IEventPublisher`, no Service Bus or Event Grid references in code or Terraform.
- NSwag client (`ArchiForge.Api.Client`) is generated but not published to NuGet in CI (the `publish-api-client.yml` workflow exists but is workflow_dispatch only).

**Tradeoffs**: Keeping integration simple avoids operational complexity of a message broker. But it limits adoption in enterprises where event-driven architectures are the norm.

**Recommendations**:
1. Publish CloudEvents-formatted webhook payloads for governance state changes and run completions.
2. Add an Azure Service Bus or Event Grid output adapter behind an `IEventPublisher` abstraction.
3. Publish the API client NuGet package automatically in CI.
4. Consider a lightweight gRPC surface for high-throughput agent-to-host communication.

---

### 2. Deployability — 48 / 100

**Justification**: CI is excellent (multi-tier tests, IaC validation, SBOMs, vulnerability scanning, container builds). However, CD is explicitly a **placeholder** — no actual deployment pipeline exists. There is no automated staging or production deployment, no blue/green or canary strategy, no rollback automation. The Terraform stacks are validated but never applied in any workflow. Container images are built for smoke testing but never pushed to a registry.

**Evidence**:
- `.github/workflows/cd.yml`: `echo "CD scaffold"` placeholder steps, `docker build` smoke only.
- No `azure/login`, no `terraform apply`, no registry push in any workflow.
- 9 Terraform stacks validated but none applied automatically.
- Dockerfiles are well-optimized (multi-stage, non-root, healthchecks).
- `docker-compose.yml` supports local dev with optional full-stack profile.

**Tradeoffs**: Not automating deployment reduces risk of accidental production changes and keeps infrastructure ops manual. But it means every deployment is a manual, error-prone process.

**Recommendations**:
1. Wire `cd.yml` to push images to ACR and run `terraform apply` with plan-and-approve gates.
2. Implement blue/green deployment using Container Apps revisions.
3. Add automated rollback on health check failure post-deploy.
4. Add environment-scoped secrets and `OIDC` federated credentials for Azure login.

---

### 3. Availability — 52 / 100

**Justification**: Infrastructure supports geo-redundant SQL (`terraform-sql-failover`), Container Apps auto-scaling (API min 2 replicas), and health checks (`/health/ready`, `/health/live`). However, there is no multi-region app deployment, no Front Door failover origin group, no Redis HA (Compose-only single node), no documented RTO/RPO testing, and the worker runs at min 1 replica with no partition-aware scaling.

**Evidence**:
- SQL failover group: `terraform-sql-failover/main.tf` (`azurerm_mssql_failover_group`).
- Container Apps: API `min_replicas = 2`, Worker `min_replicas = 1`.
- Health checks: `SqlConnectionHealthCheck`, `BlobStorageHealthCheck`, `SchemaFilesHealthCheck`, `ProcessTempDirectoryHealthCheck`, `ComplianceRulePackHealthCheck`, `DataArchivalHostHealthCheck`.
- `docs/RTO_RPO_TARGETS.md` exists but is brief.
- No multi-region Container Apps or Front Door active-active.
- Redis is Compose-only; no Azure Cache for Redis in Terraform.

**Tradeoffs**: Single-region plus SQL geo-failover is cost-effective for early stages. Multi-region app deployment adds significant complexity.

**Recommendations**:
1. Add Azure Cache for Redis (Standard tier) to Terraform for production cache HA.
2. Add a secondary Container Apps environment in a paired region with Front Door active-passive routing.
3. Conduct and document a failover drill with measured RTO/RPO.
4. Increase worker `min_replicas` to 2 for zero-downtime deploys.

---

### 4. Usability — 55 / 100

**Justification**: The UI has a clean Tailwind/shadcn-style foundation, OIDC sign-in, and operator shell navigation. But it is still feature-sparse: page routes exist for runs, manifests, artifacts, comparisons, replay, governance, ask, and search, but the UX is primarily data tables with minimal progressive disclosure, no guided workflows, no inline help, no keyboard shortcuts, and no accessibility audit.

**Evidence**:
- Tailwind + shadcn-style: `tailwind.config.ts`, `components.json`, `Button` component.
- OIDC flow: `/auth/signin`, `/auth/callback`, `AuthPanel`.
- Shell navigation: `ShellNav` with grouped links.
- E2E tests cover operator journeys but the UI itself has sparse interactive affordances.
- No `aria-*` annotations beyond default Radix primitives.

**Tradeoffs**: The product targets operators and architects (expert users), so power-user density is acceptable. But onboarding new operators is friction-heavy without guided flows.

**Recommendations**:
1. Add a guided "first run" wizard that walks through creating an architecture run end-to-end.
2. Add keyboard shortcuts for common actions (new run, compare, approve).
3. Conduct a WCAG 2.1 AA accessibility audit on the shell.
4. Add contextual help tooltips on governance and decision engine concepts.

---

### 5. Cost-Effectiveness — 58 / 100

**Justification**: Good foundations exist — consumption budgets for SQL, Container Apps, and OpenAI are defined in Terraform (disabled by default). LLM completion caching, token quotas, and circuit breakers reduce OpenAI spend. Container Apps use Consumption plan. But several cost controls are disabled by default, there is no Log Analytics `daily_quota_gb`, no reserved instance strategy, and the hot-path cache defaults to in-memory (no Redis cost, but also no shared cache benefit across replicas).

**Evidence**:
- Consumption budgets: `terraform-sql-failover/consumption_budget.tf`, `terraform-container-apps/consumption_budget.tf`, `terraform-openai/main.tf` — all `count = 0` by default.
- LLM cost controls: `LlmTokenQuotaWindowTracker`, `CachingAgentCompletionClient`, `CircuitBreakingAgentCompletionClient`.
- `LlmTokenQuota` disabled by default in `appsettings.json`.
- Container Apps: Consumption tier, right-sized defaults (API 0.5 vCPU, Worker 0.25 vCPU).
- Log Analytics: `sku = "PerGB2018"`, no `daily_quota_gb` cap.
- No Azure Advisor or Cost Management automation.

**Tradeoffs**: Disabling budgets by default prevents alert fatigue in dev/test. But production deployments have no cost guardrails unless the operator explicitly enables them.

**Recommendations**:
1. Enable consumption budgets by default in production `tfvars` examples.
2. Add `daily_quota_gb` to Log Analytics workspace to prevent runaway ingestion costs.
3. Enable `LlmTokenQuota` by default with sensible limits.
4. Document a FinOps review cadence in runbooks.

---

### 6. Scalability — 60 / 100

**Justification**: Container Apps auto-scaling is configured (HTTP concurrency for API, queue depth for Worker). SQL read replicas are supported. Background job processing uses Azure Storage Queues with configurable scaling. But there is no read-through cache invalidation strategy, no CQRS separation for hot read paths, the `ListRunSummariesAsync` API lacks server-side pagination in some endpoints, and there is no CDN for static UI assets beyond Front Door pass-through.

**Evidence**:
- API scaling: `api_max_replicas = 5`, `api_scale_concurrent_requests = 10`.
- Worker scaling: `worker_max_replicas = 20`, queue-depth scaling rule.
- Read replica support: `SqlServerOptions` with `FailoverGroupReadOnlyListenerConnectionString`.
- Hot-path cache: Memory or Redis with TTL, but no invalidation on write.
- `SqlPagingSyntax` exists but not uniformly applied to all list endpoints.

**Tradeoffs**: Current defaults are appropriate for low-to-medium load. The architecture supports horizontal scaling but has not been load-tested at scale.

**Recommendations**:
1. Add server-side pagination to all list endpoints (runs, comparisons, manifests).
2. Implement cache invalidation on write for hot-path cached entities.
3. Add a CDN origin group in Front Door for static UI assets.
4. Conduct load testing with `scripts/load/` and document baseline throughput.

---

### 7. Cognitive Load — 62 / 100

**Justification**: The `Host.Core` to `Host.Composition` split reduced DI complexity. Clear domain boundaries across 15+ projects help. But the sheer number of projects (42 `.csproj`), the deep Persistence project (297 `.cs` files), the large Decisioning project (239 files), and the ongoing product rename (ArchiForge → ArchLucid) create navigational friction. The Navigation rule helps but covers only high-traffic paths.

**Evidence**:
- 42 `.csproj` files across the solution.
- `ArchiForge.Persistence`: 297 `.cs` files with mixed concerns (repositories, orchestration, blob store, caching, archival, connections, migrations).
- `ArchiForge.Decisioning`: 239 `.cs` files (findings, alerts, governance, compliance, advisory).
- `.cursor/rules/Navigation.mdc` covers ~10 entry points.
- Naming: `ArchiForge.DecisionEngine` vs `ArchiForge.Decisioning` distinction is non-obvious.
- DI composition root: 8 partial files — manageable but requires knowing the decomposition.

**Tradeoffs**: Fine-grained project boundaries enable independent testing and clear ownership. But 42 projects is at the upper edge for a single-team product.

**Recommendations**:
1. Consider merging `DecisionEngine` into `Decisioning` — the distinction is unclear to newcomers.
2. Split `Persistence` into `Persistence.Sql` (Dapper repos) and `Persistence.Domain` (orchestration, caching, archival).
3. Add a `SYSTEM_MAP_QUICK.md` one-page visual for the top 10 types a new developer touches.
4. Complete the ArchLucid rename to eliminate dual-name confusion.

---

### 8. Performance — 63 / 100

**Justification**: Async throughout (controllers, repositories, background services). Dapper for minimal ORM overhead. Hot-path caching (memory or Redis). SQL connection resilience with Polly. LLM completion caching to avoid redundant OpenAI calls. But there is no response compression middleware, no output caching, the Dockerfile does not enable ReadyToRun/AOT for cold-start optimization, and benchmark coverage is minimal (2 micro-benchmarks).

**Evidence**:
- Async: All repository methods, all controller actions, all background services.
- Dapper: Lightweight data access, parameterized queries.
- Caching: `HotPathCacheOptions` (TTL 60s), `LlmCompletionResponseCacheOptions` (TTL 600s).
- Resilience: `SqlOpenResilienceDefaults` (3 retries, 200ms base, exponential + jitter).
- Benchmarks: `AgentDispatchMicroBenchmarks`, `SimulatedParallelBatchBenchmarks` — 2 files only.
- No `UseResponseCompression()`, no `UseOutputCache()`, no ReadyToRun in Dockerfile.

**Tradeoffs**: Dapper + async provides a strong baseline. The absence of response compression is negligible when Front Door handles compression at the edge.

**Recommendations**:
1. Add response compression middleware for non-edge deployments.
2. Enable ReadyToRun (`-p:PublishReadyToRun=true`) in Dockerfile for cold-start.
3. Expand benchmark suite to cover repository query hot paths and decision engine merges.
4. Add output caching for stable read endpoints (manifests, golden paths).

---

### 9. Evolvability — 65 / 100

**Justification**: Clean interface abstractions (`IFindingEngine`, `IContextConnector`, `IAgentHandler`) enable new feature plugins. The `dotnet new` template for finding engines lowers contribution friction. Feature flags (`Microsoft.FeatureManagement`) allow staged rollouts. But there is no API versioning strategy (single `/v1` namespace, no `Asp.Versioning`), no schema evolution strategy for the knowledge graph or golden manifest JSON, and the Contracts project mixes DTOs with service interfaces.

**Evidence**:
- Extension points: `IFindingEngine`, `IContextConnector`, `IAgentHandler`, `IAlertDeliveryChannel`, `IDigestDeliveryChannel`.
- Template: `templates/archiforge-finding-engine/` with `.template.config/template.json`.
- Feature flags: `ServiceCollectionExtensions.FeatureManagement.cs`, `FeatureManagementAuthorityPipelineModeResolver`.
- API: Single `/openapi/v1.json`, no `Asp.Versioning.Mvc` package.
- Contracts: 130 files mixing `ISimulationEngine`, `ICandidateChangeSetService` with DTOs.

**Tradeoffs**: Single API version keeps things simple while the product is pre-GA. But adding versioning later is a breaking change.

**Recommendations**:
1. Add `Asp.Versioning.Mvc` and introduce `/v2` before GA.
2. Separate Contracts into `Contracts.Models` (DTOs) and `Contracts.Services` (interfaces).
3. Define a JSON schema for `GoldenManifest` and `GraphSnapshot` with explicit versioning.
4. Document the extension point registration pattern in an ADR.

---

### 10. AI/Agent Readiness — 66 / 100

**Justification**: Strong agent execution model with `IAgentHandler` per agent type, `RealAgentExecutor` for production, `DeterministicAgentSimulator` for testing. LLM completion caching, token quotas, circuit breakers, and execution trace recording are production-grade. But agents are tightly coupled to Azure OpenAI (no abstraction for alternative LLM providers), the simulator is deterministic-only (no stochastic/adversarial mode), and there is no agent observability dashboard or LLM cost attribution per tenant/run.

**Evidence**:
- Agents: `TopologyAgentHandler`, `CostAgentHandler`, `ComplianceAgentHandler`, `CriticAgentHandler`.
- Executor: `RealAgentExecutor` (concurrent task dispatch), `DeterministicAgentSimulator` (fake scenarios).
- Trace bridge: `SimulatorExecutionTraceRecordingExecutor`.
- LLM: `AzureOpenAiCompletionClient` — directly coupled to Azure OpenAI SDK.
- Quotas: `LlmTokenQuotaWindowTracker` per tenant.
- Metrics: `ArchiForgeInstrumentation.RecordLlmTokenUsage` counters exist.
- No `ILlmProvider` abstraction; no multi-model routing.

**Tradeoffs**: Azure OpenAI coupling simplifies deployment and avoids provider abstraction overhead. But it prevents using Anthropic, local models, or OpenAI-direct as alternatives.

**Recommendations**:
1. Introduce `ILlmCompletionProvider` abstraction to decouple from Azure OpenAI SDK.
2. Add per-run LLM cost attribution in the execution trace (token counts × model pricing).
3. Add a stochastic simulator mode for chaos testing agent interactions.
4. Build an LLM cost/latency dashboard in Grafana using existing Prometheus metrics.

---

### 11. Manageability — 67 / 100

**Justification**: Configuration validation at startup catches misconfigurations before the app serves traffic. Feature flags enable runtime feature toggling. Health check endpoints provide readiness/liveness probes. Runbooks cover 13 operational scenarios. But there is no admin CLI for common ops tasks (restart workers, drain queues, force-migrate), the CLI `doctor` command is limited, and there is no centralized configuration management (no Azure App Configuration).

**Evidence**:
- Startup validation: `ArchiForgeConfigurationRules.CollectErrors()` — 30+ rules, fail-fast.
- Health checks: 6 health check implementations, `/health/ready`, `/health/live`.
- Runbooks: 13 documented operational procedures.
- CLI: `doctor` command checks health, support-bundle collects diagnostics.
- No Azure App Configuration in Terraform or code.
- No admin queue-drain or worker-restart CLI commands.

**Tradeoffs**: App Configuration adds operational dependency and cost. The current approach (appsettings + Key Vault references) is simpler.

**Recommendations**:
1. Add CLI commands for queue drain, migration status, and cache flush.
2. Consider Azure App Configuration for centralized feature flag management.
3. Add a `/admin/diagnostics` endpoint (behind `AdminAuthority` policy) for runtime config inspection.
4. Add structured startup diagnostics to the support bundle.

---

### 12. Data Consistency — 68 / 100

**Justification**: Strong transactional patterns — `IArchiForgeUnitOfWork` for orchestrator writes, `TransactionScope` in application services, `IDbTransaction` in repositories. Idempotency via `IArchitectureRunIdempotencyRepository` with unique violation detection. Optimistic concurrency via `ROWVERSION` on runs. But `TransactionScope` and explicit `IDbTransaction` coexist (dual transaction patterns), archival uses soft-delete without cascading to child tables, and there is no outbox pattern for cross-boundary consistency (e.g., run completion → webhook delivery).

**Evidence**:
- UoW: `IArchiForgeUnitOfWork` in `AuthorityRunOrchestrator`.
- TransactionScope: `ArchitectureRunService`, `GovernanceWorkflowService.ActivateAsync`.
- Idempotency: `ArchitectureRunIdempotencyRepository.TryInsertAsync` — unique constraint.
- Optimistic concurrency: `RunRecord.RowVersion`, `RunConcurrencyConflictException`.
- Soft archival: `ArchivedUtc` column, no cascade documented.
- No outbox pattern for webhook delivery.

**Tradeoffs**: Dual transaction patterns work but increase cognitive load. An outbox would add complexity but guarantee at-least-once delivery.

**Recommendations**:
1. Standardize on `IArchiForgeUnitOfWork` and deprecate raw `TransactionScope` usage.
2. Add a transactional outbox for webhook/alert delivery to guarantee at-least-once.
3. Document the archival cascade strategy (what happens to child records when a run is archived?).
4. Add integration tests for concurrent write conflict scenarios.

---

### 13. Reliability — 69 / 100

**Justification**: Circuit breakers on OpenAI calls, SQL retry policies with exponential backoff + jitter, graceful shutdown with 45s drain, and health checks for all critical dependencies. Rate limiting protects against abuse. But there is no bulkhead isolation (a slow OpenAI call can exhaust the thread pool), no timeout policy on HTTP calls to external services, no dead-letter queue for failed background jobs, and the data archival hosted service has no retry on failure.

**Evidence**:
- Circuit breaker: `CircuitBreakerGate` for completion + embedding clients.
- SQL resilience: `SqlOpenResilienceDefaults` (3 retries, exponential + jitter).
- Graceful shutdown: `GracefulShutdownNotificationHostedService`, `HostOptions.ShutdownTimeout = 45s`.
- Rate limiting: `AddArchiForgeRateLimiting()` with fixed/expensive/replay policies.
- No `HttpClient` timeout configuration for webhooks.
- No dead-letter queue in `BackgroundJobRepository`.
- `DataArchivalHostedService` logs errors but does not retry individual archival batches.

**Tradeoffs**: Adding bulkhead isolation adds significant complexity. The current approach is adequate for moderate load.

**Recommendations**:
1. Add `HttpClient` timeout policies (30s) for webhook delivery.
2. Implement a dead-letter mechanism for background jobs that fail after max retries.
3. Add a bulkhead policy on the OpenAI completion path to limit concurrent calls.
4. Add retry logic to `DataArchivalCoordinator` for individual batch failures.

---

### 14. Extensibility — 72 / 100

**Justification**: Well-defined extension points for all major domain concepts: `IFindingEngine` (new analysis), `IContextConnector` (new data sources), `IAgentHandler` (new AI agents), `IAlertDeliveryChannel` (new notification targets), `IDigestDeliveryChannel` (new digest formats). The `dotnet new` template scaffolds a complete finding engine project. DI registration is centralized and discoverable. But extension registration requires modifying the composition root (no plugin discovery), and there is no runtime extension loading.

**Evidence**:
- Finding engines: `ComplianceFindingEngine`, `TopologyCoverageFindingEngine`, etc. — register in `ServiceCollectionExtensions.Decisioning.cs`.
- Context connectors: `DocumentConnector`, `InfrastructureDeclarationConnector`, etc. — ordered pipeline.
- Agent handlers: 4 registered in `ServiceCollectionExtensions.AgentsGovernanceRetrieval.cs`.
- Alert channels: Slack, Teams, on-call webhooks.
- Template: `templates/archiforge-finding-engine/`.

**Tradeoffs**: Compile-time registration is safe and debuggable. Dynamic plugin loading adds security and versioning risks.

**Recommendations**:
1. Add `Microsoft.Extensions.DependencyInjection`-based assembly scanning for `IFindingEngine` implementations.
2. Document the extension authoring workflow (template → implement → register → test) in an ADR.
3. Consider a plugin manifest for opt-in finding engines without recompilation.

---

### 15. Observability — 73 / 100

**Justification**: Comprehensive observability stack — Serilog structured logging with correlation IDs, OpenTelemetry tracing (ASP.NET Core, HTTP, SQL, custom `ActivitySource`), Prometheus metrics with authenticated scrape endpoint, custom counters/histograms for authority runs and LLM tokens, Grafana dashboards, and Prometheus alert rules. But there is no distributed tracing export to Jaeger/Tempo by default, no SLO burn-rate alerting (only threshold alerts), the Grafana dashboards are committed JSON (no Terraform provisioning), and there is no log-based alerting.

**Evidence**:
- Logging: `ArchiForgeSerilogConfiguration`, `CorrelationIdMiddleware` (`LogContext.PushProperty`).
- Tracing: `ObservabilityExtensions.AddOpenTelemetry()` — ASP.NET, HTTP, SQL, custom sources.
- Metrics: `ArchiForgeInstrumentation` — `Meter("ArchiForge")`, counters for runs, tokens, alert evaluation.
- Prometheus: Scrape endpoint with optional Basic auth, `archiforge-alerts.yml`, `archiforge-slo-rules.yml`.
- Grafana: 3 dashboard JSON files in `infra/grafana/`.
- OTLP exporter: Optional in `ObservabilityExtensions`.
- No Jaeger/Tempo default configuration.

**Tradeoffs**: Prometheus + Grafana is cost-effective and well-understood. Adding Jaeger adds another service to operate.

**Recommendations**:
1. Add a default OTLP exporter configuration pointing to Azure Monitor or Tempo.
2. Implement SLO burn-rate alerting rules using the existing SLO rules.
3. Provision Grafana dashboards via Terraform (`azurerm_dashboard_grafana` + dashboard provisioning).
4. Add log-based alerts for critical error patterns (auth failures, circuit breaker opens).

---

### 16. Policy & Governance Alignment — 74 / 100

**Justification**: Rich governance model — approval workflows, promotion gates, environment activation with transaction-protected deactivation of predecessors. Compliance finding engines evaluate against rule packs. RLS enforces tenant isolation at the SQL layer. Provenance graph links decisions back to evidence. But governance rules are JSON-only (no policy-as-code engine like OPA), there is no audit log export, and the governance preview does not include a "what-if" dry-run mode.

**Evidence**:
- Governance: `GovernanceWorkflowService` with submit/approve/reject/promote/activate lifecycle.
- Compliance: `default-compliance.rules.json`, `ComplianceFindingEngine`.
- RLS: `RlsSessionContextApplicator`, `sp_set_session_context`, production-enforced.
- Provenance: `ProvenanceBuilder`, `IProvenanceQueryService`, decision subgraph navigation.
- No OPA or Rego integration.
- Audit: `IAuditService` with `AuditEventTypes.Governance.*` — records events but no export.

**Tradeoffs**: JSON rule packs are simpler than OPA but less expressive. Enterprise customers may require policy-as-code integration.

**Recommendations**:
1. Add an audit log export mechanism (CSV, Azure Event Hub, or SIEM integration).
2. Consider OPA/Rego integration for enterprise policy evaluation.
3. Add a "dry-run" mode to governance preview that shows what would happen without committing.

---

### 17. Architectural Integrity — 76 / 100

**Justification**: Clean layered architecture — Core (no dependencies), Contracts (shared models), domain projects (Decisioning, AgentRuntime, KnowledgeGraph, etc.), Application (orchestration), Persistence (data access + UoW), Host.Core (infrastructure), Host.Composition (DI), and thin hosts (Api, Worker). Dependency flow is consistently inward. The hub-and-spoke from Host.Composition to domain projects is intentional and well-documented. But Persistence is a wide dependency node (references 8 domain projects), and the Contracts project bleeds service interfaces alongside DTOs.

**Evidence**:
- Worker references only Host.Core + Host.Composition (thin host).
- Application references Contracts, Coordinator, Persistence, DecisionEngine, AgentSimulator (application above infrastructure).
- Persistence references Contracts, Core, Provenance, ArtifactSynthesis, ContextIngestion, Decisioning, KnowledgeGraph, Retrieval — 8 projects.
- Contracts: 130 files, mixes `ISimulationEngine` with DTOs.

**Tradeoffs**: Persistence's wide reach is a consequence of being the integration layer (repositories for all aggregates). Splitting it would reduce coupling but increase project count.

**Recommendations**:
1. Extract Persistence orchestration/caching into a separate `Persistence.Runtime` project.
2. Move service interfaces out of Contracts into their owning domain projects.
3. Add an Architecture Decision Record documenting the intended dependency flow.

---

### 18. Testability — 78 / 100

**Justification**: 17 test projects, ~437 test files. xUnit + FluentAssertions + Moq throughout. Contract tests via OpenAPI snapshot comparison. Integration tests with SQL Server service containers in CI. UI tests with Vitest + Playwright. Stryker mutation testing on Persistence (weekly). Coverage reporting with PR comments. But test coverage thresholds are not enforced (no minimum gate), Stryker covers only one project, there is no load/performance test in CI, and some test projects appear thin (e.g., `Contracts.Tests` has 1 `.csproj` reference).

**Evidence**:
- 17 test projects, xUnit framework.
- Coverage: Cobertura + ReportGenerator + PR comment.
- Stryker: `stryker-config.json` targeting Persistence, weekly schedule.
- Integration: SQL Server 2022 service container, `WebApplicationFactory` for API tests.
- UI: Vitest unit tests, Playwright E2E with mock API server.
- No coverage minimum gate in CI (warning only).

**Tradeoffs**: Not enforcing a coverage gate avoids blocking PRs for non-critical code. But it allows coverage regression.

**Recommendations**:
1. Add a minimum coverage gate (e.g., 70%) that fails the PR check.
2. Extend Stryker to Application and AgentRuntime projects.
3. Add a load test job in CI using `scripts/load/`.
4. Add property-based testing for decision engine merge logic.

---

### 19. Modularity — 79 / 100

**Justification**: Strong project decomposition — 15+ domain projects with clear boundaries. Host.Composition splits DI into 8 subsystem partial files. Worker and API share composition but diverge on pipeline. Extension points use per-domain interfaces. The `dotnet new` template enables finding engine development in isolation. But the composition root is still a single static class (not per-feature assemblies), and Persistence is a monolith.

**Evidence**:
- 15+ domain projects with independent test projects.
- `ServiceCollectionExtensions` — 8 partial files by subsystem.
- Worker/API diverge only on pipeline and web layer.
- Template for isolated finding engine development.
- Persistence: 297 files in one project.

**Tradeoffs**: Current modularity is excellent for a single-team product. Further splitting would be warranted if multiple teams own different subsystems.

**Recommendations**:
1. Split Persistence into focused subprojects if team boundaries emerge.
2. Consider making each subsystem's DI registration a standalone extension method in its own assembly.

---

### 20. Security — 80 / 100

**Justification**: Strong multi-layer security posture. API key validation uses constant-time comparison (SHA-256 + `FixedTimeEquals`). JWT bearer auth with Entra ID. RBAC with policies (`ReadAuthority`, `ExecuteAuthority`, `AdminAuthority`) and granular permission claims. RLS at the SQL layer (production-enforced). FluentValidation on all inputs. CSP headers on UI. Security headers middleware (X-Content-Type-Options, X-Frame-Options, Referrer-Policy). CORS with explicit origin allowlist. CI includes gitleaks, Trivy (IaC + container images), CodeQL, NuGet vulnerability audit, CycloneDX SBOMs. Private endpoints for SQL and Blob. OIDC nonce validation prevents replay. But CSP still uses `unsafe-inline` + `unsafe-eval`, CORS defaults to deny-all (good) but has no production example, and there is no WAF enabled by default.

**Evidence**:
- Constant-time key comparison: `ConstantTimeKeyEquals` in `ApiKeyAuthenticationHandler`.
- JWT: `AddJwtBearer` with `ValidateAudience`, `RoleClaimType`.
- RBAC: `ArchiForgePolicies`, `ArchiForgeRoles`, `ArchiForgeRoleClaimsTransformation`.
- RLS: `RlsSessionContextApplicator`, enforced in production via `ArchiForgeConfigurationRules`.
- Input validation: `AddFluentValidationAutoValidation()`.
- CSP: `script-src 'self' 'unsafe-inline' 'unsafe-eval'` (Next.js limitation).
- CI security: gitleaks, Trivy, CodeQL, NuGet audit, SBOMs.
- Private endpoints: `terraform-private/network.tf`.
- WAF: `enable_front_door_waf = false` by default.
- OIDC nonce: `readNonceFromPayload`, validated in `CallbackClient.tsx`.
- Prometheus auth: `PrometheusScrapeAuthMiddleware` with `FixedTimeEquals`.

**Tradeoffs**: `unsafe-inline`/`unsafe-eval` in CSP is a necessary compromise for Next.js App Router hydration. WAF is disabled to avoid cost in dev/test.

**Recommendations**:
1. Enable WAF in production `tfvars` by default with OWASP 3.2 managed ruleset.
2. Migrate CSP to strict nonces for production builds (Next.js supports `nonce` prop).
3. Add rate limiting on the `/auth/callback` endpoint to prevent token exchange abuse.
4. Add secret rotation automation via runbook or Azure Automation.

---

### 21. Documentation — 82 / 100

**Justification**: Extensive documentation corpus — ~100 markdown files covering architecture (context, containers, components, flows), deployment, operations, security, testing, onboarding (day-one developer, SRE, security), CLI usage, API contracts, ADRs, runbooks (13 operational procedures), pilot guide, troubleshooting, and changelogs. The Navigation rule provides a quick-start map. But some docs reference stale names (ArchiForge → ArchLucid rename in progress), there are no auto-generated API docs beyond the OpenAPI spec, and the ADR series is small (11 entries) for the project's complexity.

**Evidence**:
- ~100 `.md` files across `docs/`, `archiforge-ui/docs/`, project READMEs.
- Onboarding: `day-one-developer.md`, `day-one-sre.md`, `day-one-security.md`.
- Runbooks: 13 operational procedures in `docs/runbooks/`.
- Architecture: C4-style context/container/component diagrams.
- ADRs: `docs/adr/` — 11 entries.
- `.cursor/rules/Navigation.mdc` — quick-start for developers.

**Tradeoffs**: Comprehensive docs require maintenance. The rename will touch ~800 occurrences in docs alone.

**Recommendations**:
1. Add auto-generated API docs (Swagger UI or ReDoc) hosted at `/docs`.
2. Expand the ADR series to cover key decisions (caching strategy, agent execution model, governance workflow design).
3. Complete the ArchLucid rename across all documentation (Phases 1.2–1.12 of the checklist).

---

### 22. Maintainability — 83 / 100

**Justification**: Highest-scoring dimension. Consistent coding conventions enforced by `.editorconfig` and Cursor rules. Modular project structure with clear boundaries. Comprehensive test suite with coverage reporting. Structured DI composition. Configuration validation catches issues at startup. DbUp migrations with checksum integrity. CI enforces formatting, security scanning, and test passage. The codebase follows user rules (LINQ preference, concrete types, null checks, one class per file, modular methods). But the ongoing rename and the 42-project solution add maintenance friction.

**Evidence**:
- `.editorconfig`, Cursor rules for coding style.
- CI: `terraform fmt -check`, `dotnet build`, full test suite, coverage.
- DbUp: Checksum-validated migrations, `DatabaseMigrator.Run`.
- Configuration validation: `ArchiForgeConfigurationRules` — 30+ rules.
- Consistent patterns: Repository + UoW + DI + configuration options.
- Test coverage: PR comments, scheduled mutation testing.

**Tradeoffs**: The maintenance cost of 42 projects is offset by clear boundaries and independent testability.

**Recommendations**:
1. Add a pre-commit hook for `dotnet format` to catch style issues before CI.
2. Add architectural fitness functions (ArchUnit-style) to enforce dependency rules.
3. Complete the ArchLucid rename to reduce dual-name maintenance burden.

---

## Top 5 Improvements

| Priority | Action | Impact | Effort |
|----------|--------|--------|--------|
| **1** | **Wire the CD pipeline** — push images to ACR, `terraform apply` with plan gates, blue/green revisions | Deployability: 48 → ~70. Eliminates manual deployment risk. | 2–3 sessions |
| **2** | **Add interoperability layer** — publish CloudEvents webhooks, Service Bus output adapter, auto-publish NuGet client | Interoperability: 42 → ~60. Enables enterprise integration. | 3–4 sessions |
| **3** | **Enable production cost guardrails by default** — budgets on, `LlmTokenQuota` on, Log Analytics daily cap, WAF on | Cost-Effectiveness: 58 → ~70, Security: 80 → ~84. Prevents bill shock and blocks OWASP top-10 at the edge. | 1 session |
| **4** | **Add multi-region availability** — secondary Container Apps env, Front Door active-passive, Redis HA | Availability: 52 → ~68, Reliability: 69 → ~75. Required for production SLA. | 3–4 sessions |
| **5** | **Introduce LLM provider abstraction + cost attribution** — `ILlmCompletionProvider`, per-run token cost tracking, Grafana LLM dashboard | AI/Agent Readiness: 66 → ~78. Unlocks model portability and FinOps visibility. | 2 sessions |
