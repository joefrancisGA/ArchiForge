> **Scope:** Operators and integrators looking up recognized configuration keys and host roles — not secret material, deployment order, or full environment architecture.

# Configuration reference

This document lists operator-facing configuration **keys** (colon paths or environment names) recognized by `archlucid config check` and by `GET /v1/admin/config-summary` (presence only, never secret values). The **canonical registry** is `ConfigurationKeyCatalog` in `ArchLucid.Core`.

## Tooling

- Validate locally: `archlucid config check` (add `--no-api` to skip the API snapshot; use global `--json` for machine-readable output; exit `0` when all *required* keys for the current mode are set, exit `4` when not).
- Server snapshot: `GET /v1/admin/config-summary` (admin API key; same key paths as the catalog, no secret values in the response).

## Hosting roles

- **Api** — HTTP API process (`Hosting:Role=Api`).
- **Worker** — background / job host (`Hosting:Role=Worker`).
- **Combined** — single process running both (`Hosting:Role=Combined`).
- **CLI** — `archlucid` on a developer or automation machine (not a host process).

The **Host roles** column is a hint for where a key is most often relevant; most keys apply to every host unless noted.

## Keys

The **When required** column reflects `ConfigurationKeyRequirement` in code (e.g. SQL connection string when storage is SQL; Azure OpenAI when `AgentExecution:Mode=Real` and the completion client is not `Echo`).

| Section | Key | Source(s) | Default | When required | Host roles | Description |
| --- | --- | --- | --- | --- | --- | --- |
| Hosting | `Hosting:LogStartupConfigurationSummary` | appsettings, env | true | Optional (not mode-gated) | All (Api, Worker, Combined) | Log effective configuration on startup (host). |
| Hosting | `Hosting:Role` | appsettings, env | Combined | Optional (not mode-gated) | All (per process) | Api, Worker, or Combined host process. |
| Metering | `Metering:Enabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Feature metering toggle. |
| ArchLucid | `ArchLucid:Secrets:Provider` | appsettings, env, KeyVault ref | EnvironmentVariable | Optional (not mode-gated) | All (Api, Worker, Combined) | How secrets (connection strings, API keys) are loaded. |
| ArchLucid | `ArchLucid:Secrets:KeyVaultUri` | appsettings, env, KeyVault | empty | Optional (If Key Vault routing) | All (Api, Worker, Combined) | Azure Key Vault base URI when the secrets provider needs it. |
| ArchLucid | `ArchLucid:Secrets:KeyVaultCacheSeconds` | appsettings, env | 300 | Optional (not mode-gated) | All (Api, Worker, Combined) | Secret cache duration for Key Vault access. |
| ArchLucid | `ArchLucid:StorageProvider` | appsettings, env | Sql | Optional (not mode-gated) | All (Api, Worker, Combined) | InMemory (tests) or Sql; unset defaults to Sql in product rules. |
| ConnectionStrings | `ConnectionStrings:ArchLucid` | appsettings, env, KeyVault, user secrets | see default dev | Required — When SQL is active | All (Api, Worker, Combined) | Primary SQL connection string (required when using Sql storage). |
| ArchLucid | `ArchLucid:Persistence:AllowRlsBypass` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Dev-only: bypass SQL row-level security in tests. |
| ArchLucid | `ArchLucid:PublicSite:BaseUrl` | appsettings, env | https://archlucid.net | Optional (not mode-gated) | All (Api, Worker, Combined) | Public marketing / operator link base for emails and exports. |
| ArchLucid | `ArchLucid:Notifications:TrialLifecycle:Owner` | appsettings, env | Hosted | Optional (not mode-gated) | All (Api, Worker, Combined) | Who runs trial notification emails for this tenant class. |
| ArchLucid | `ArchLucid:AgentOutput:QualityGate:Enabled` | appsettings, env | true | Optional (not mode-gated) | All (Api, Worker, Combined) | Enables quality gate for agent output. |
| ArchLucid | `ArchLucid:AgentOutput:QualityGate:StructuralWarnBelow` | appsettings, env | 0.55 | Optional (not mode-gated) | All (Api, Worker, Combined) | Quality gate warn threshold (structural). |
| ArchLucid | `ArchLucid:AgentOutput:QualityGate:SemanticWarnBelow` | appsettings, env | 0.55 | Optional (not mode-gated) | All (Api, Worker, Combined) | Quality gate warn threshold (semantic). |
| ArchLucid | `ArchLucid:AgentOutput:QualityGate:StructuralRejectBelow` | appsettings, env | 0.35 | Optional (not mode-gated) | All (Api, Worker, Combined) | Quality gate reject (structural). |
| ArchLucid | `ArchLucid:AgentOutput:QualityGate:SemanticRejectBelow` | appsettings, env | 0.35 | Optional (not mode-gated) | All (Api, Worker, Combined) | Quality gate reject (semantic). |
| ArchLucid | `ArchLucid:Explanation:Aggregate:FaithfulnessFallbackEnabled` | appsettings, env | true | Optional (not mode-gated) | All (Api, Worker, Combined) | Allow fallback when faithfulness is low. |
| ArchLucid | `ArchLucid:Explanation:Aggregate:MinSupportRatioToTrustLlmNarrative` | appsettings, env | 0.2 | Optional (not mode-gated) | All (Api, Worker, Combined) | Minimum support ratio to trust the LLM narrative block. |
| ArchLucid | `ArchLucid:MermaidCli:Enabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Optionally render Mermaid with external CLI. |
| CosmosDb | `CosmosDb:ConnectionString` | appsettings, env, KeyVault | empty | Optional (When using Cosmos) | All (Api, Worker, Combined) | Optional Cosmos connection when the deployment uses it. |
| CosmosDb | `CosmosDb:DatabaseName` | appsettings, env | ArchLucid | Optional (not mode-gated) | All (Api, Worker, Combined) | Cosmos database name when enabled. |
| CosmosDb | `CosmosDb:GraphSnapshotsEnabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Store graph snapshots in Cosmos when enabled. |
| CosmosDb | `CosmosDb:AgentTracesEnabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Store agent traces in Cosmos when enabled. |
| CosmosDb | `CosmosDb:AuditEventsEnabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Stream audit to Cosmos if configured. |
| HotPathCache | `HotPathCache:Enabled` | appsettings, env | true | Optional (not mode-gated) | All (Api, Worker, Combined) | Hot path cache on/off. |
| HotPathCache | `HotPathCache:Provider` | appsettings, env | Auto | Optional (not mode-gated) | All (Api, Worker, Combined) | Cache backend selection (e.g. Redis, memory, auto). |
| HotPathCache | `HotPathCache:ExpectedApiReplicaCount` | appsettings, env | 1 | Optional (not mode-gated) | Api, Combined | Expected number of API replicas (cache coherency hint). |
| HotPathCache | `HotPathCache:AbsoluteExpirationSeconds` | appsettings, env | 60 | Optional (not mode-gated) | All (Api, Worker, Combined) | Absolute cache TTL in seconds for hot path entries. |
| HotPathCache | `HotPathCache:RedisConnectionString` | appsettings, env, KeyVault | empty | Optional (When using Redis for cache) | All (Api, Worker, Combined) | Redis when Provider selects Redis/Auto+Redis discoverable. |
| AgentExecution | `AgentExecution:Mode` | appsettings, env | Simulator | Optional (not mode-gated) | All (Api, Worker, Combined) | Simulator (offline) or Real (calls Azure OpenAI) — see validation rules. |
| AgentExecution | `AgentExecution:CompletionClient` | appsettings, env | omit or Azure | Optional (not mode-gated) | All (Api, Worker, Combined) | Echo to skip real LLM; see AgentExecution rules. |
| AgentExecution | `AgentExecution:LlmCostEstimation:Enabled` | appsettings, env | true | Optional (not mode-gated) | All (Api, Worker, Combined) | Token cost heuristics for telemetry. |
| AgentExecution | `AgentExecution:LlmCostEstimation:InputUsdPerMillionTokens` | appsettings, env | 0.5 | Optional (not mode-gated) | All (Api, Worker, Combined) | Cost model input (USD per 1M tokens). |
| AgentExecution | `AgentExecution:LlmCostEstimation:OutputUsdPerMillionTokens` | appsettings, env | 1.5 | Optional (not mode-gated) | All (Api, Worker, Combined) | Cost model output (USD per 1M tokens). |
| AgentExecution | `AgentExecution:TraceStorage:BlobPersistenceTimeoutSeconds` | appsettings, env | 30 | Optional (not mode-gated) | All (Api, Worker, Combined) | Timeout for writing trace chunks to durable storage. |
| AgentExecution | `AgentExecution:ReferenceEvaluation:Enabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Reference case evaluation off by default in template. |
| AgentExecution | `AgentExecution:ReferenceEvaluation:ReferenceCasesPath` | appsettings, env | empty | Optional (When reference eval on) | All (Api, Worker, Combined) | Path to reference case fixtures. |
| AgentExecution | `AgentExecution:Resilience:MaxConcurrentHandlers` | appsettings, env | 8 | Optional (not mode-gated) | All (Api, Worker, Combined) | Parallel agent/LLM handler limit. |
| AgentExecution | `AgentExecution:Resilience:PerHandlerTimeoutSeconds` | appsettings, env | 900 | Optional (not mode-gated) | All (Api, Worker, Combined) | Per job timeout. |
| AgentExecution | `AgentExecution:Resilience:LlmCallMaxRetryAttempts` | appsettings, env | 3 | Optional (not mode-gated) | All (Api, Worker, Combined) | LLM call retries. |
| AgentExecution | `AgentExecution:Resilience:LlmCallBaseDelayMilliseconds` | appsettings, env | 500 | Optional (not mode-gated) | All (Api, Worker, Combined) | Exponential backoff base (LLM). |
| AgentExecution | `AgentExecution:Resilience:LlmCallMaxDelaySeconds` | appsettings, env | 10 | Optional (not mode-gated) | All (Api, Worker, Combined) | Maximum delay between LLM retries. |
| AzureOpenAI | `AzureOpenAI:Endpoint` | appsettings, env, KeyVault, AZURE_OPENAI__Endpoint | empty | Required — When Real and not Echo | All (Api, Worker, Combined) | Azure OpenAI resource endpoint (HTTPS). |
| AzureOpenAI | `AzureOpenAI:ApiKey` | env, KeyVault, AZURE_OPENAI__ApiKey | empty | Required — When Real and not Echo | All (Api, Worker, Combined) | Client credential for the Azure OpenAI resource (never log). |
| AzureOpenAI | `AzureOpenAI:DeploymentName` | env, AZURE_OPENAI__DeploymentName | empty | Required — When Real and not Echo | All (Api, Worker, Combined) | Chat/Completion deployment name in Azure OpenAI. |
| AzureOpenAI | `AzureOpenAI:MaxCompletionTokens` | appsettings, env | 0 (4096 default) | Optional (not mode-gated) | All (Api, Worker, Combined) | Upper bound; 0 uses product default (see agents pipeline). |
| LlmDailyTenantBudget | `LlmDailyTenantBudget:Enabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Enforce daily LLM token cap per tenant. |
| LlmDailyTenantBudget | `LlmDailyTenantBudget:MaxTotalTokensPerTenantPerUtcDay` | appsettings, env | 1000000 | Optional (When cap on) | All (Api, Worker, Combined) | Token budget per calendar UTC day. |
| LlmDailyTenantBudget | `LlmDailyTenantBudget:WarnFraction` | appsettings, env | 0.8 | Optional (When cap on) | All (Api, Worker, Combined) | Warn when consumption crosses this fraction of cap. |
| LlmDailyTenantBudget | `LlmDailyTenantBudget:AssumedMaxTotalTokensPerRequest` | appsettings, env | 65536 | Optional (When cap on) | All (Api, Worker, Combined) | Heuristic for reservation math. |
| AgentPrompts | `AgentPrompts:Versions:topology` | appsettings, env | v2026-04 | Optional (not mode-gated) | All (Api, Worker, Combined) | Prompt set version: topology pack. |
| AgentPrompts | `AgentPrompts:Versions:cost` | appsettings, env | v2026-04 | Optional (not mode-gated) | All (Api, Worker, Combined) | Prompt set version: cost pack. |
| AgentPrompts | `AgentPrompts:Versions:compliance` | appsettings, env | v2026-04 | Optional (not mode-gated) | All (Api, Worker, Combined) | Prompt set: compliance pack. |
| AgentPrompts | `AgentPrompts:Versions:critic` | appsettings, env | v2026-04 | Optional (not mode-gated) | All (Api, Worker, Combined) | Prompt set: critic pack. |
| SchemaValidation | `SchemaValidation:AgentResultSchemaPath` | appsettings, content | schemas/... | Optional (not mode-gated) | All (Api, Worker, Combined) | On-disk path to the agent result JSON schema. |
| SchemaValidation | `SchemaValidation:GoldenManifestSchemaPath` | appsettings, content | schemas/... | Optional (not mode-gated) | All (Api, Worker, Combined) | Golden manifest JSON schema file. |
| SchemaValidation | `SchemaValidation:EnableDetailedErrors` | appsettings, env | true | Optional (not mode-gated) | All (Api, Worker, Combined) | Verbose schema errors in early validation (dev). |
| ArchLucidAuth | `ArchLucidAuth:Mode` | appsettings, env | ApiKey | Optional (not mode-gated) | All (Api, Worker, Combined) | Default auth story for the template (dev API key, etc.). |
| ArchLucidAuth | `ArchLucidAuth:Authority` | appsettings, env | empty | Optional (When OIDC in use) | All (Api, Worker, Combined) | Identity provider authority (OIDC) when that mode is enabled. |
| ArchLucidAuth | `ArchLucidAuth:Audience` | appsettings, env | empty | Optional (When OIDC in use) | All (Api, Worker, Combined) | Token audience (OIDC). |
| ArchLucidAuth | `ArchLucidAuth:DevUserId` | appsettings, env | dev-user | Optional (not mode-gated) | All (Api, Worker, Combined) | Principal id for the development loop. |
| ArchLucidAuth | `ArchLucidAuth:DevUserName` | appsettings, env | Developer | Optional (not mode-gated) | All (Api, Worker, Combined) | Display name in dev default principal. |
| ArchLucidAuth | `ArchLucidAuth:DevRole` | appsettings, env | Admin | Optional (not mode-gated) | All (Api, Worker, Combined) | Default role in dev (see security docs). |
| Trial | `Trial:Lifecycle:IntervalMinutes` | appsettings, env | 360 | Optional (not mode-gated) | All (Api, Worker, Combined) | Trial state machine / email tick interval. |
| Trial | `Trial:Lifecycle:ReadOnlyAfterExpireDays` | appsettings, env | 7 | Optional (not mode-gated) | All (Api, Worker, Combined) | Days after expiry before read-only state. |
| Trial | `Trial:Lifecycle:ExportOnlyAfterReadOnlyDays` | appsettings, env | 30 | Optional (not mode-gated) | All (Api, Worker, Combined) | Transition to export-only after read-only for this many days. |
| Trial | `Trial:Lifecycle:PurgeAfterExportOnlyDays` | appsettings, env | 60 | Optional (not mode-gated) | All (Api, Worker, Combined) | Hard delete delay after export-only (policy). |
| Trial | `Trial:Lifecycle:HardPurgeMaxRowsPerStatement` | appsettings, env | 5000 | Optional (not mode-gated) | All (Api, Worker, Combined) | Purge batch size (data retention job). |
| Auth | `Auth:Trial:ExternalIdTenantId` | appsettings, env | empty | Optional (When trial IdP) | All (Api, Worker, Combined) | B2C / external tenant id mapping for self-service sign-up. |
| Auth | `Auth:Trial:LocalIdentity:JwtPrivateKeyPemPath` | appsettings, env | empty | Optional (When local IdP) | All (Api, Worker, Combined) | PEM for HS/RS local JWT signing (path). |
| Auth | `Auth:Trial:LocalIdentity:JwtIssuer` | appsettings, env | empty | Optional (When local IdP) | All (Api, Worker, Combined) | Local JWT issuer string. |
| Auth | `Auth:Trial:LocalIdentity:JwtAudience` | appsettings, env | empty | Optional (When local IdP) | All (Api, Worker, Combined) | Local JWT audience. |
| Auth | `Auth:Trial:LocalIdentity:AccessTokenLifetimeMinutes` | appsettings, env | 60 | Optional (not mode-gated) | All (Api, Worker, Combined) | Access token TTL for local auth. |
| Cors | `Cors:AllowedOrigins:0` | appsettings, env, Cors__* | http://localhost:3000 | Optional (not mode-gated) | Api, Combined | First allowed origin; additional indices use 1,2,… in JSON. |
| RateLimiting | `RateLimiting:Registration:PermitLimit` | appsettings, env | 5 | Optional (not mode-gated) | Api, Combined | Throttling: registration path. |
| RateLimiting | `RateLimiting:Registration:WindowMinutes` | appsettings, env | 60 | Optional (not mode-gated) | Api, Combined | Registration throttling window. |
| RateLimiting | `RateLimiting:FixedWindow:PermitLimit` | appsettings, env | 60 | Optional (not mode-gated) | Api, Combined | Default fixed window permit cap. |
| RateLimiting | `RateLimiting:FixedWindow:WindowMinutes` | appsettings, env | 1 | Optional (not mode-gated) | Api, Combined | Fixed window length in minutes. |
| RateLimiting | `RateLimiting:Replay:Light:PermitLimit` | appsettings, env | 60 | Optional (not mode-gated) | Api, Combined | Light replay throttling (see policies). |
| RateLimiting | `RateLimiting:Replay:Light:WindowMinutes` | appsettings, env | 1 | Optional (not mode-gated) | Api, Combined | Window for light replay throttling. |
| RateLimiting | `RateLimiting:Replay:Heavy:PermitLimit` | appsettings, env | 15 | Optional (not mode-gated) | Api, Combined | Heavy replay throttling (expensive paths). |
| RateLimiting | `RateLimiting:Replay:Heavy:WindowMinutes` | appsettings, env | 1 | Optional (not mode-gated) | Api, Combined | Window for heavy replay throttling. |
| Authentication | `Authentication:ApiKey:Enabled` | appsettings, env | false | Optional (not mode-gated) | Api, Combined | Static X-Api-Key authentication — see `Authentication` startup rules for key material. |
| Authentication | `Authentication:ApiKey:DevelopmentBypassAll` | appsettings, env | false | Optional (not mode-gated) | Api, Combined | DANGER: only for dev — bypass key checks. Must be off in production. |
| Authentication | `Authentication:ApiKey:AdminKey` | env, KeyVault, user secrets | null in template | Optional (If enabled (see custom rule: at least one of Admin/Read keys)) | Api, Combined | High-privilege API key value when `Enabled` (never print in CLI; presence only). |
| Authentication | `Authentication:ApiKey:ReadOnlyKey` | env, KeyVault, user secrets | null in template | Optional (If enabled (see at least one key rule)) | Api, Combined | Read-tier API key when `Enabled` (secret). |
| Billing | `Billing:Provider` | appsettings, env | Stripe | Optional (not mode-gated) | All (Api, Worker, Combined) | Billing integrator: Stripe, marketplace, etc. |
| Billing | `Billing:Stripe:SecretKey` | env, KeyVault | empty | Required — Production billing | All (Api, Worker, Combined) | Stripe live/test secret; required for paid flows in production. |
| Billing | `Billing:Stripe:WebhookSigningSecret` | env, KeyVault | empty | Required — Production | All (Api, Worker, Combined) | Validates `Stripe-Signature` on the webhook path. |
| Billing | `Billing:Stripe:PublishableKey` | appsettings, env, KeyVault | empty | Optional (When checkout UI in app) | All (Api, Worker, Combined) | Publishable key (non-secret but still not echoed by CLI in raw form here). |
| Billing | `Billing:Stripe:PriceIdTeam` | appsettings, env | empty | Optional (When using Stripe) | All (Api, Worker, Combined) | Default price for Team SKU. |
| Billing | `Billing:AzureMarketplace:LandingPageUrl` | appsettings, env | empty | Required — When marketplace GA in prod | All (Api, Worker, Combined) | Azure marketplace landing (production checks when GA). |
| Billing | `Billing:AzureMarketplace:MarketplaceOfferId` | appsettings, env | empty | Required — When marketplace GA in prod | All (Api, Worker, Combined) | Commercial marketplace offer id. |
| LlmPromptRedaction | `LlmPromptRedaction:Enabled` | appsettings, env | true | Optional (not mode-gated) | All (Api, Worker, Combined) | Redact prompts in logging/traces. |
| LlmPromptRedaction | `LlmPromptRedaction:ReplacementToken` | appsettings, env | [REDACTED] | Optional (not mode-gated) | All (Api, Worker, Combined) | Replacement for redacted span. |
| Demo | `Demo:Enabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | In-product demo / synthetic paths. |
| Demo | `Demo:SeedOnStartup` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Seeds the Contoso path when demo is on (see ops guide). |
| DeveloperExperience | `DeveloperExperience:EnableApiExplorer` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Exposes extra OpenAPI/Scalar in non-prod (see security note). |
| DataConsistency | `DataConsistency:OrphanProbeEnabled` | appsettings, env | true | Optional (not mode-gated) | All (Api, Worker, Combined) | Background data consistency scan. |
| DataConsistency | `DataConsistency:OrphanProbeIntervalMinutes` | appsettings, env | 60 | Optional (not mode-gated) | All (Api, Worker, Combined) | Orphan scan cadence. |
| DataConsistency | `DataConsistency:Enforcement:Mode` | appsettings, env | Warn | Optional (not mode-gated) | All (Api, Worker, Combined) | Type default **Warn**; **`ArchLucid.Api/appsettings.json`** ships **Alert** for orphan paging signals (see **`DataConsistencyEnforcementMode`**). |
| DataConsistency | `DataConsistency:Enforcement:MaxRowsPerBatch` | appsettings, env | 500 | Optional (When enforced) | All (Api, Worker, Combined) | Safer cap per remediation batch. |
| DataConsistency | `DataConsistency:Enforcement:AlertThreshold` | appsettings, env | 1 | Optional (not mode-gated) | All (Api, Worker, Combined) | Orphan count threshold to page operators. |
| DataConsistency | `DataConsistency:Enforcement:AutoQuarantine` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | If true, auto quarantine (must be off until approved). |
| AzureDevOps | `AzureDevOps:Enabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Enables work-item / PR status integration. |
| AzureDevOps | `AzureDevOps:Organization` | appsettings, env, KeyVault ref | empty | Optional (If ADO on) | All (Api, Worker, Combined) | DevOps org name (non-secret, still presence-checked). |
| AzureDevOps | `AzureDevOps:Project` | appsettings, env | empty | Optional (If ADO on) | All (Api, Worker, Combined) | Project in Azure DevOps. |
| AzureDevOps | `AzureDevOps:PersonalAccessToken` | env, KeyVault | empty | Optional (If ADO on) | All (Api, Worker, Combined) | PAT for the integration (secret). |
| Serilog | `Serilog:MinimumLevel:Default` | appsettings, env | Information | Optional (not mode-gated) | All (Api, Worker, Combined) | Serilog default minimum (host logging). |
| Logging | `Logging:LogLevel:Default` | appsettings, env | Information | Optional (not mode-gated) | All (Api, Worker, Combined) | Microsoft logger default (framework). |
| Observability | `Observability:Otlp:Enabled` | appsettings, env | false | Optional (not mode-gated) | All (Api, Worker, Combined) | Export OpenTelemetry to OTLP collector (host). |
| Observability | `Observability:Otlp:Endpoint` | env, KeyVault | empty | Required — If OTLP enabled | All (Api, Worker, Combined) | OTLP base URL; required when `Observability:Otlp:Enabled` is true. |
| Email | `Email:Provider` | appsettings, env | Noop | Optional (not mode-gated) | All (Api, Worker, Combined) | Noop, Smtp, or Azure Communication Services (see `Email` namespace). |
| Email | `Email:AzureCommunicationServicesEndpoint` | env, KeyVault | empty | Required — If ACS for email in prod | All (Api, Worker, Combined) | Azure Communication Services **Email** resource endpoint (HTTPS) when that provider is selected (see validation). |
| Environment | `ASPNETCORE_ENVIRONMENT` | env, launchSettings, Service | (unset) | Optional (not mode-gated) | All | ASPNETCORE_ / DOTNET_ENVIRONMENT — cluster role for startup validation. Checked via environment variable, not appsettings path. |
| CLI | `ARCHLUCID_API_URL` | env, archlucid.json | http://localhost:5128 (default) | Optional (When using the CLI) | CLI | Resolves the API base URL; not consumed by the API process. |
| CLI | `ARCHLUCID_API_KEY` | env, archlucid.json (optional) | empty | Optional (If calling protected admin routes from CLI) | CLI | Maps to `X-Api-Key` for admin routes; `config check` never prints the value. |
