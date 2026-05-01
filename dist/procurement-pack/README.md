<!-- **Scope:** Repository overview; single onboarding entry `docs/START_HERE.md`; deeper engineering index `docs/ARCHITECTURE_INDEX.md` + `docs/library/`. -->

# ArchLucid

## Documentation spine

Start at **[docs/START_HERE.md](docs/START_HERE.md)**. **Contributor** onboarding stays on these five active docs (install bodies live under `docs/engineering/`):

1. **[docs/engineering/INSTALL_ORDER.md](docs/engineering/INSTALL_ORDER.md)** — toolchain order and verification
2. **[docs/engineering/FIRST_30_MINUTES.md](docs/engineering/FIRST_30_MINUTES.md)** — Docker-first first run
3. **[docs/CORE_PILOT.md](docs/CORE_PILOT.md)** — guided pilot inside the product
4. **[docs/ARCHITECTURE_ON_ONE_PAGE.md](docs/ARCHITECTURE_ON_ONE_PAGE.md)** — system map
5. **[docs/PENDING_QUESTIONS.md](docs/PENDING_QUESTIONS.md)** — open decisions affecting implementation

**Architecture Decision Records:** **[docs/adr/README.md](docs/adr/README.md)**. Catalogue for the five-doc spine (why these five): **[docs/FIRST_5_DOCS.md](docs/FIRST_5_DOCS.md)**.

Buyers: `docs/BUYER_FIRST_30_MINUTES.md` · Sponsors: `docs/EXECUTIVE_SPONSOR_BRIEF.md` · Security / trust: `docs/trust-center.md` · Depth: `docs/library/` · `docs/ARCHITECTURE_INDEX.md` · Archived root-era snapshots: `docs/archive/root-superseded-2026-05-01/README.md` · Contributor persona table: `docs/library/CONTRIBUTOR_PERSONA_TABLE.md`.

**Quick persona routing:** buyer / evaluator (`archlucid.net` + sponsor brief + Core Pilot); contributor — spine above, then `docs/ARCHITECTURE_INDEX.md` once something runs locally.

[![Hosted SaaS probe](https://github.com/joefrancisGA/ArchLucid/actions/workflows/hosted-saas-probe.yml/badge.svg)](https://github.com/joefrancisGA/ArchLucid/actions/workflows/hosted-saas-probe.yml)

ArchLucid shortens the path from an architecture request to a reviewable, defensible architecture package, helping teams ship committed manifests, reviewable artifacts, and governance evidence with less manual assembly.

**Try in 60 seconds** (repo root; requires .NET 10 SDK + Docker):

```bash
dotnet run --project ArchLucid.Cli -- try
```

Windows Docker-only helper: `.\scripts\demo-start.ps1`

<details>
<summary><strong>Quick doc links (personas)</strong> — START_HERE, sponsor brief, architecture poster</summary>

| Doc | Open this when… |
|-----|-----------------|
| **[`docs/START_HERE.md`](docs/START_HERE.md)** | You need the **single canonical first-30-minutes** buyer / operator path |
| **[`docs/EXECUTIVE_SPONSOR_BRIEF.md`](docs/EXECUTIVE_SPONSOR_BRIEF.md)** | You are a **sponsor, procurement partner, or outward buyer** |
| **[`docs/ARCHITECTURE_ON_ONE_PAGE.md`](docs/ARCHITECTURE_ON_ONE_PAGE.md)** | You want the **architecture poster** (C4-style system map) |

</details>

**Deeper dive index:** [`docs/ARCHITECTURE_INDEX.md`](docs/ARCHITECTURE_INDEX.md) · bulk reference markdown now lives under [`docs/library/`](docs/library/).

<details>
<summary><strong>Deeper docs</strong> — full README (install, personas, layers, API, CLI, tests, architecture)</summary>

ArchLucid shortens the path from an architecture request to a reviewable, defensible architecture package. It helps teams produce committed manifests, reviewable artifacts, and stronger governance evidence with less manual assembly and less ambiguity about what changed and why.

At the product level, ArchLucid is an AI-assisted architecture workflow system: it coordinates topology, cost, and compliance analysis to produce manifests, artifacts, and evidence that architects, reviewers, and governance stakeholders can actually use.

**Canonical buyer narrative:** For sponsor-facing and outward buyer messaging, start with **[docs/EXECUTIVE_SPONSOR_BRIEF.md](docs/EXECUTIVE_SPONSOR_BRIEF.md)**. The rest of this repository should stay aligned with that summary rather than competing with it.

**Repository layout:** Source lives under **`ArchLucid.*`** projects, **`archlucid-ui/`**, and **`docs/`**. Local packaging writes to **`artifacts/`** (gitignored). See **[docs/REPO_HYGIENE.md](docs/library/REPO_HYGIENE.md)** for what to commit vs regenerate. User-visible changes are tracked in **[docs/CHANGELOG.md](docs/CHANGELOG.md)**; the breaking-only narrative continues to live in **[BREAKING_CHANGES.md](BREAKING_CHANGES.md)**.

## Getting started

> **Audience.** This README and the documents linked from it are for **ArchLucid contributors and internal operators** building, testing, or operating ArchLucid itself. **Buyers / evaluators / sponsors / customers** never run Docker, SQL, .NET, Node, or any local CLI — they sign up at **`archlucid.net`** and use the in-product operator UI. Start with **[docs/START_HERE.md](docs/START_HERE.md)**; canonical outward narrative: **[docs/EXECUTIVE_SPONSOR_BRIEF.md](docs/EXECUTIVE_SPONSOR_BRIEF.md)**.

**Canonical install order (contributor / internal operator):** **[docs/engineering/INSTALL_ORDER.md](docs/engineering/INSTALL_ORDER.md)** — what to install, in what order, for local dev vs Azure pilot.

**Pick your contributor persona.** If you have never run ArchLucid on this machine, **Docker-only first-run:** **[docs/engineering/FIRST_30_MINUTES.md](docs/engineering/FIRST_30_MINUTES.md)** needs nothing but Docker.

<details>
<summary><strong>Contributor persona table</strong> — who starts where (deeper than READ_THIS_FIRST)</summary>

Full table (unchanged): **[docs/library/CONTRIBUTOR_PERSONA_TABLE.md](docs/library/CONTRIBUTOR_PERSONA_TABLE.md)**.

</details>

**Customer path (no install):** follow **[docs/START_HERE.md](docs/START_HERE.md)** §2. **Security / GRC (single URL):** **[docs/trust-center.md](docs/trust-center.md)**. **Architecture poster:** **[docs/ARCHITECTURE_ON_ONE_PAGE.md](docs/ARCHITECTURE_ON_ONE_PAGE.md)**. **Operator atlas:** **[docs/OPERATOR_ATLAS.md](docs/library/OPERATOR_ATLAS.md)**. Deeper maps: **[docs/ARCHITECTURE_INDEX.md](docs/ARCHITECTURE_INDEX.md)**, **[docs/V1_SCOPE.md](docs/library/V1_SCOPE.md)**, **[docs/PILOT_ROI_MODEL.md](docs/library/PILOT_ROI_MODEL.md)**, **[docs/OPERATOR_DECISION_GUIDE.md](docs/library/OPERATOR_DECISION_GUIDE.md)**, **[docs/FUTURE_PACKAGING_ENFORCEMENT.md](docs/library/FUTURE_PACKAGING_ENFORCEMENT.md)**, **[docs/go-to-market/reference-customers/README.md](docs/go-to-market/reference-customers/README.md)**, **[docs/PENDING_QUESTIONS.md](docs/PENDING_QUESTIONS.md)**, **[docs/archive/README.md](docs/archive/README.md)**.

## Product layers

ArchLucid ships as **two** buyer-facing capability layers: **Pilot** and **Operate**.

**Default buying motion:** start with **Pilot** so a team can move from request to committed manifest and reviewable artifacts quickly. Add **Operate** only when real analytical or governance questions require deeper surfaces.

**First-pilot rule:** if you are evaluating whether ArchLucid creates value, stay on the **Pilot** path first. Treat **Operate** as a follow-on maturity path, not a co-equal Day-1 requirement.

| Layer | What it covers | Why it matters | How to reach it |
|-------|---------------|----------------|-----------------|
| **Pilot** | Create run → execute → commit → review manifest and artifacts | Proves fast path from request to reviewable output with less manual packaging effort | Default sidebar and home page |
| **Operate** | Compare, replay, graph, Ask, advisory, pilot feedback **and** governance, policy packs, audit log, compliance drift, alerts | Deeper design investigation plus governance and operational trust when the organization is ready | **Show more links** and extended/advanced sidebar disclosure; role-aware **UI shaping** (not entitlements) — see [docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](docs/library/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md) §4 and [archlucid-ui/README.md](archlucid-ui/README.md#seam-maintenance-anti-drift) |

Full capability inventory: **[docs/PRODUCT_PACKAGING.md](docs/library/PRODUCT_PACKAGING.md)** (§3 *Two UI shaping surfaces* — **Visibility** via **`useNavSurface()`** (nav + **`LayerHeader`** / layer guidance) and **Capability** via **`useOperateCapability()`** (Execute+ mutation soft-enable + **`OperateCapabilityHints`**); *Contributor drift guard* + *Cross-surface lock* — keep **`nav-config.ts`**, **`nav-shell-visibility.ts`** (**tier → authority**), **`current-principal.ts`** (`/me` read-model), **`layer-guidance.ts` / `LayerHeader`**, **`operate-capability.ts`** / **`useOperateCapability()`** (and deprecated **`useEnterpriseMutationCapability()`** shims), **Vitest** seam tests including **`authority-seam-regression.test.ts`**, **`authority-execute-floor-regression.test.ts`**, and **`authority-shaped-ui-regression.test.ts`** (catalog **`ExecuteAuthority`** rows + mutation floor invariants), and **API** policies aligned). First-pilot walkthrough: **[docs/CORE_PILOT.md](docs/CORE_PILOT.md)**. **Measurement companion:** **[docs/PILOT_ROI_MODEL.md](docs/library/PILOT_ROI_MODEL.md)**. **Usage guidance:** **[docs/OPERATOR_DECISION_GUIDE.md](docs/library/OPERATOR_DECISION_GUIDE.md)**. **Canonical buyer narrative:** **[docs/EXECUTIVE_SPONSOR_BRIEF.md](docs/EXECUTIVE_SPONSOR_BRIEF.md)**. Future packaging map: **[docs/FUTURE_PACKAGING_ENFORCEMENT.md](docs/library/FUTURE_PACKAGING_ENFORCEMENT.md)**. **Operator UI shaping only:** [archlucid-ui/README.md](archlucid-ui/README.md#seam-maintenance-anti-drift) — nav and soft-disable follow **`/me`**; **ArchLucid.Api** still returns **401/403**. **Page-level mutation + layout seams:** Vitest **`archlucid-ui/src/app/(operator)/operate-authority-ui-shaping.test.tsx`** (hook → **`disabled`** / **`readOnly`**), **`archlucid-ui/src/app/(operator)/authority-shaped-layout-regression.test.tsx`** (inspect-first layout when mutation is off).

## Pilot onboarding (56R)

**Product boundary (V1):** [docs/V1_SCOPE.md](docs/library/V1_SCOPE.md). **Pre-handoff checklist:** [docs/V1_RELEASE_CHECKLIST.md](docs/library/V1_RELEASE_CHECKLIST.md). **Commands:** [docs/OPERATOR_QUICKSTART.md](docs/library/OPERATOR_QUICKSTART.md). **Measurement companion and success criteria:** [docs/PILOT_ROI_MODEL.md](docs/library/PILOT_ROI_MODEL.md). **Layer decision guidance:** [docs/OPERATOR_DECISION_GUIDE.md](docs/library/OPERATOR_DECISION_GUIDE.md). **Canonical buyer narrative:** [docs/EXECUTIVE_SPONSOR_BRIEF.md](docs/EXECUTIVE_SPONSOR_BRIEF.md). **Narrative (archived):** [docs/archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md](docs/archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md). **Fix issues:** [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md). **Package an RC:** [docs/RELEASE_LOCAL.md](docs/library/RELEASE_LOCAL.md).

**Before a handoff or demo:** `run-readiness-check.cmd` or `.\run-readiness-check.ps1`. For **API + CLI quick run + artifacts** in one script, set **`ARCHLUCID_SMOKE_SQL`** and run **`release-smoke.cmd`** ([docs/RELEASE_SMOKE.md](docs/library/RELEASE_SMOKE.md)); optional UI E2E: **`.\release-smoke.ps1 -RunPlaywright`** ([archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md](archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md#8-e2e-tests-playwright)).

> **Release smoke vs live Playwright:** **`release-smoke.ps1`** (with or without **`-RunPlaywright`**) does **not** replace CI’s **`live-api-*.spec.ts`** (**SQL-backed browser**) gates — **`-RunPlaywright`** runs **mock-backed** **`npm run test:e2e`**, separate from the smoke API process. Table: **[docs/library/RELEASE_SMOKE.md](docs/library/RELEASE_SMOKE.md#release-smoke-ui-sql-parity)** · live path **[docs/library/LIVE_E2E_HAPPY_PATH.md](docs/library/LIVE_E2E_HAPPY_PATH.md)**.

**Hosted SaaS URLs:** staging funnel `https://staging.archlucid.net`; production `https://archlucid.net` when Front Door hostnames are wired (see [docs/REFERENCE_SAAS_STACK_ORDER.md](docs/library/REFERENCE_SAAS_STACK_ORDER.md), `infra/apply-saas.ps1`). **Public liveness (hosted):** `Invoke-RestMethod https://staging.archlucid.net/health/live` (or `/health/ready`). **`release-smoke.ps1`** still starts a **local** API for the E2E block; use **`-ApiBaseUrl`** / **`-BaseUrl`** only when that process is not on the default `http://localhost:5128` ([docs/RELEASE_SMOKE.md](docs/library/RELEASE_SMOKE.md)).

**Build / version:** **`GET /version`** on the API, or **`dotnet run --project ArchLucid.Cli -- doctor`**. **Diagnostics:** **`dotnet run --project ArchLucid.Cli -- support-bundle --zip`** (review before sharing). **Reporting issues:** [docs/PILOT_GUIDE.md#when-you-report-an-issue](docs/library/PILOT_GUIDE.md#when-you-report-an-issue) (version, correlation ID, logs, bundle).

## Operator quick start

- **Health:** `GET /health/live` (liveness), `GET /health/ready` (readiness: DB when using Sql storage, schema files, compliance rule pack, temp dir), `GET /health` (all checks). See [docs/engineering/BUILD.md](docs/engineering/BUILD.md) for startup vs migration failure behavior.
- **Versioned API:** Routes are under `/v1/...`. Send optional **`X-Correlation-ID`** on requests for support correlation (see [docs/API_CONTRACTS.md](docs/library/API_CONTRACTS.md)).
- **Auth:** Configure **`ArchLucidAuth`**: shipped **`appsettings.json`** defaults to **`ApiKey`** mode with API keys **disabled** (fail closed) until you enable keys; **`appsettings.Development.json`** switches to **`DevelopmentBypass`** when `ASPNETCORE_ENVIRONMENT=Development`. Production samples use **`JwtBearer`**. Policies map to `ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` (see **API authentication** below).
- **SMB / storage:** Do not expose file shares (SMB, port 445) on the public internet; use private endpoints and controlled boundaries for any Azure storage or hybrid file access.
- **Cost-aware pilot / unit economics:** [docs/deployment/PILOT_PROFILE.md](docs/deployment/PILOT_PROFILE.md), [docs/deployment/PER_TENANT_COST_MODEL.md](docs/deployment/PER_TENANT_COST_MODEL.md).

### Integration events (optional Azure Service Bus)

ArchLucid publishes JSON integration events to an Azure Service Bus topic for
lifecycle hooks (run completion, governance, alerts, advisory scans).
- **Event catalog:** [`schemas/integration-events/catalog.json`](schemas/integration-events/catalog.json)
- **Payload schemas:** [`schemas/integration-events/*.v1.schema.json`](schemas/integration-events/)
- **AsyncAPI spec:** [`docs/contracts/archlucid-asyncapi-2.6.yaml`](docs/contracts/archlucid-asyncapi-2.6.yaml)
- **Full reference:** [`docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`](docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md)

## Prerequisites

See **[docs/engineering/INSTALL_ORDER.md](docs/engineering/INSTALL_ORDER.md)** for the pinned toolchain (.NET SDK from [`global.json`](global.json), Docker, Node **22** per CI, SQL) and verification commands.

## Operator UI (`archlucid-ui`)

A thin Next.js shell organized around **two** product layers: **Pilot** (runs, commit, manifest, artifacts) visible by default; **Operate** (analysis, replay, graph, advisory **and** governance, audit, alerts, policy) via progressive disclosure.

**Keep the default mental model narrow:** **Pilot** is the default path. **Operate** is a follow-on layer for specific analytical or governance questions, not required for first-pilot success.

**Role-aware shaping (first wave, implemented):** the UI composes **disclosure tier first**, then optional per-link **`requiredAuthority`** (same names as API policies: `ReadAuthority` / `ExecuteAuthority` / `AdminAuthority`) using **`GET /api/auth/me`** via the proxy (`archlucid-ui/src/lib/current-principal.ts` + **`OperatorNavAuthorityProvider`** for a single in-shell read-model and rank; **Visibility** composition in **`nav-shell-visibility.ts`** and **`useNavSurface()`**). That is **operational accountability** (who should see operator/admin surfaces)—**not** pricing, billing, or entitlements. **The API still returns 401/403** (and **404** for tier-hidden routes that must not be enumerated); **the shell** must not be treated as authorization. **Contributor maintenance map** (which TS modules map to which packaging layer): [docs/PRODUCT_PACKAGING.md](docs/library/PRODUCT_PACKAGING.md) §3 — *Code seams (operator UI — maintenance map)* and *Contributor drift guard*. **Cross-module Vitest:** [`archlucid-ui/src/lib/authority-seam-regression.test.ts`](archlucid-ui/src/lib/authority-seam-regression.test.ts) (tier ∩ rank, Operate monotonicity, progressive disclosure); [`archlucid-ui/src/lib/authority-execute-floor-regression.test.ts`](archlucid-ui/src/lib/authority-execute-floor-regression.test.ts) (Execute nav row vs mutation boolean). Do not bypass or duplicate this stack ad hoc; see [archlucid-ui/README.md](archlucid-ui/README.md#seam-maintenance-anti-drift), [docs/operator-shell.md](docs/library/operator-shell.md), and [docs/PRODUCT_PACKAGING.md](docs/library/PRODUCT_PACKAGING.md#what-the-layer-model-means-today) (role-based restriction vs future entitlement). **57R:** Playwright operator-journey smoke uses **deterministic mocks** (no live C# API in that suite) — see [archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md](archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md#8-e2e-tests-playwright).

## API authentication (`ArchLucidAuth`)

Configure in `appsettings.*` under **`ArchLucidAuth`**:

| Mode | Purpose |
|------|---------|
| **`ApiKey`** | **Shipped base JSON:** scheme is registered with **`Enabled=false`** / **`DevelopmentBypassAll=false`** so callers are **rejected** until operators enable keys (**`Authentication:ApiKey:Enabled=true`** plus **`AdminKey`** / **`ReadOnlyKey`** or env). **Operational:** header **`X-Api-Key`** validates against configured keys. |
| **`DevelopmentBypass`** | **`appsettings.Development.json`** when **`ASPNETCORE_ENVIRONMENT=Development`**: every request is authenticated as **`DevUserId`** with **`DevRole`**. |
| **`JwtBearer`** | Production-style JWT validation using **`Authority`** and optional **`Audience`**. Map app roles to **`Admin`** / **`Operator`** / **`Reader`** in your IdP. |

Role claims are mapped to legacy **`permission`** claims via `ArchLucidRoleClaimsTransformation` so existing policies (`CanCommitRuns`, etc.) keep working. Policies: **`ReadAuthority`** (Reader+), **`ExecuteAuthority`** (Operator+), **`AdminAuthority`** (Admin only). Debug principal: **`GET /api/auth/me`**.

## Development environment (`archlucid dev up`)

From the ArchLucid repo directory (or any directory containing `docker-compose.yml`), run:

```bash
dotnet run --project ArchLucid.Cli -- dev up
```

This starts SQL Server, Azurite, and Redis in Docker (default profile — for hot-reload development). To run the full stack (API + UI in containers too): `docker compose --profile full-stack up -d --build`. For the **internal-operator Docker-only demo** with Contoso demo seed and simulator agents (used by sales for seller-led demos; **not** the customer first-run path), use `.\scripts\demo-start.ps1` or see [docs/go-to-market/DEMO_QUICKSTART.md](docs/go-to-market/DEMO_QUICKSTART.md). See [docs/engineering/CONTAINERIZATION.md](docs/engineering/CONTAINERIZATION.md).

Use this connection string with the API:

```
Server=localhost,1433;Database=ArchLucid;User Id=sa;Password=ArchLucid_Dev_Pass123!;TrustServerCertificate=True;
```

## Database Setup

1. Create a database (for example `ArchLucid`, or a pilot-specific name), or use `archlucid dev up` to run SQL Server in Docker.
2. Migrations run automatically on startup via [DbUp](https://dbup.readthedocs.io/). Scripts in `ArchLucid.Persistence/Migrations/` are applied in order; add new `00x_Description.sql` files for schema changes. **Greenfield** empty catalogs replay **`001`–`050`** once (then stamp `SchemaVersions` so DbUp continues at **`051`**); see **[docs/SQL_SCRIPTS.md](docs/library/SQL_SCRIPTS.md)** §4.0. If the connection string is set and migration fails, the API throws and does not start (no fallback). Integration tests use **SQL Server** (per-test databases; **DbUp** runs on the test host). Full detail: **[docs/SQL_SCRIPTS.md](docs/library/SQL_SCRIPTS.md)** (consolidated `ArchLucid.sql`, Persistence bootstrap, two “run” tables). Governance workflow tables ship as **`038_GovernanceWorkflow.sql`** (after graph parent tables at **`017_GraphSnapshots_ParentTables.sql`**).

### Optional: Contoso trusted-baseline demo (Corrected 50R)

For a deterministic **baseline vs hardened** story (runs, manifests, governance approvals, environment activations; export history row optional), see **[docs/demo-quickstart.md](docs/library/demo-quickstart.md)** and the honesty boundary in **[docs/TRUSTED_BASELINE.md](docs/library/TRUSTED_BASELINE.md)**. Summary: set `ArchLucid:StorageProvider` to `Sql`, configure `Demo:Enabled` / `Demo:SeedOnStartup` (Development only for automatic startup seed), or call **`POST /v1.0/demo/seed`** when `Demo:Enabled` is true. Startup logs label schema bootstrap, DbUp, and demo seed in order.

## Secrets (development)

**Do not commit connection strings or API keys.** The API project has [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) enabled. In Development, configuration is loaded from User Secrets after `appsettings.*.json`.

From the repo root:

```bash
cd ArchLucid.Api

# Required: database connection (use your own connection string or the dev Docker one below)
dotnet user-secrets set "ConnectionStrings:ArchLucid" "Server=localhost,1433;Database=ArchLucid;User Id=sa;Password=ArchLucid_Dev_Pass123!;TrustServerCertificate=True;"

# Optional: only if using real agents (AgentExecution:Mode != Simulator) with Azure OpenAI
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o"
```

**Production:** Use environment variables or your hosting provider’s secret store (e.g. Azure Key Vault, AWS Secrets Manager). Do not use User Secrets in production.

## Running the API

```bash
dotnet run --project ArchLucid.Api
```

The API listens on the URLs configured for the project (default `http://localhost:5128`; see `ArchLucid.Api/Properties/launchSettings.json`).

**API versioning:** Routes use a URL path segment **`v1`** (e.g. `/v1/architecture/...`). The default API version is **1.0** when unspecified; **`api-supported-versions`** (and related) response headers report supported versions ([Asp.Versioning.Mvc](https://github.com/dotnet/aspnet-api-versioning)).

**Correlation ID:** Send **`X-Correlation-ID`** on requests to tie logs and tracing to a client trace. If omitted, the server assigns a value (from the request trace id) and returns it on the response. Middleware applies the same id across the pipeline.

In Development:

- **Swagger UI**: `/swagger`
- **Health checks**: `GET /health/live` for process-only liveness; `GET /health/ready` for dependencies before taking traffic; `GET /health` for the full set. Returns 503 when any included check is Unhealthy. Use for load balancers and runbooks. CLI: `archlucid doctor`.

**Rate limiting:** Applied to `/v1/architecture/*` endpoints. When a policy limit is exceeded, the API returns **429 Too Many Requests**.

| Policy     | Use case              | Default limit (per client/window) | Config keys |
|-----------|------------------------|-----------------------------------|-------------|
| `fixed`   | General endpoints     | 60/min (default)                  | `RateLimiting:FixedWindow:PermitLimit`, `WindowMinutes`, `QueueLimit` |
| `expensive` | Execute, commit, replay | 20/min                         | `RateLimiting:Expensive:*` |
| `replay`  | Comparison replay     | Light (markdown/html) 60/min; heavy (docx/pdf) 15/min | `RateLimiting:Replay:Light:*`, `RateLimiting:Replay:Heavy:*` |

Override in `appsettings.json` or via environment variables.

**CORS:** Configure allowed origins with **`Cors:AllowedOrigins`** (array of origins). The API uses the policy name **`ArchLucid`** (`UseCors("ArchLucid")`). If the array is empty or missing, no origins are allowed (`SetIsOriginAllowed(_ => false)`). Use this for SPA or cross-origin API clients.

**Authentication:** Send the **`X-Api-Key`** header with every request to protected endpoints. Config: **`Authentication:ApiKey:Enabled`** (default `false`; when `false`, all requests are treated as authenticated with full permissions for local dev). When enabled, set **`Authentication:ApiKey:AdminKey`** and optionally **`Authentication:ApiKey:ReadOnlyKey`** (e.g. in User Secrets or environment). Authorization policies require these permission claims: **`commit:run`**, **`seed:results`**, **`export:consulting-docx`**, **`replay:comparisons`**, **`replay:diagnostics`**. Admin key receives all; read-only key receives a subset.

## Running Tests

Full **54R** tier list, copy-paste commands, SQL variables, and **`archlucid-ui`** Vitest/Playwright: **[docs/TEST_STRUCTURE.md](docs/library/TEST_STRUCTURE.md)**. CI job mapping: **[docs/TEST_EXECUTION_MODEL.md](docs/library/TEST_EXECUTION_MODEL.md)**.

**Common entry points (repo root):**

```bash
dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration&Category!=GoldenCorpusRecord"
```

```bash
dotnet test ArchLucid.sln
```

```bash
cd archlucid-ui && npm ci && npm test
```

**ArchLucid.Api.Tests** integration tests need a reachable **SQL Server**; **`ArchLucidApiFactory`** creates ephemeral databases and runs **DbUp**. See **[docs/engineering/BUILD.md](docs/engineering/BUILD.md)** for CPM, connection strings, and DecisionEngine’s Microsoft.Extensions bundle.

**Notable API behavior:** comparison replay with `replayMode: verify` returns **422** (problem+json with drift fields) when regenerated output does not match the stored comparison—not HTTP 200 with a failure flag. End-to-end run compare uses **`#run-not-found`** when a run ID is missing. See [docs/API_CONTRACTS.md](docs/library/API_CONTRACTS.md).

## API Flow

1. **Create run** – `POST /v1/architecture/request`  
   Submit an `ArchitectureRequest` (system name, environment, cloud provider, constraints). Returns a run and agent tasks.

2. **Submit agent results** – `POST /v1/architecture/run/{runId}/result`  
   Submit results from topology, cost, and compliance agents.

3. **Commit** – `POST /v1/architecture/run/{runId}/commit`  
   Merge results and produce a versioned manifest. Requires at least one agent result per run.

4. **Get manifest** – `GET /v1/architecture/manifest/{version}`  
   Retrieve a committed manifest by version.

Other endpoints:

- `GET /v1/architecture/run/{runId}` – Fetch run status, tasks, and results
- `POST /v1/architecture/run/{runId}/seed-fake-results` – (Development only) Seed deterministic fake results for smoke testing
- `POST /v1/architecture/run/{runId}/analysis-report` – Build an analysis report (evidence, manifest, diagram, summary, determinism, diffs)
- `POST /v1/architecture/run/{runId}/analysis-report/export/docx` – Export the analysis report as a Word document (DOCX)

### Analysis report and DOCX export

The DOCX export produces a stakeholder-grade Word report: run metadata, evidence, manifest details, **architecture diagram**, summary, and optional determinism/diff sections. The architecture diagram is rendered from Mermaid to PNG and embedded in the document when a diagram renderer is available.

**Diagram rendering:**

- **Default (no renderer):** The API uses `NullDiagramImageRenderer`, which returns no image. The DOCX then includes the Mermaid source as a code block with the message *"Diagram image rendering was not available. Mermaid source is included below."*
- **Mermaid CLI (mmdc):** To embed a PNG of the diagram, install [Mermaid CLI](https://github.com/mermaid-js/mermaid-cli) (`npm install -g @mermaid-js/mermaid-cli`) and ensure `mmdc` is on PATH. In `Program.cs`, register the real renderer instead of the null one:

  ```csharp
  builder.Services.AddScoped<IDiagramImageRenderer, MermaidCliDiagramImageRenderer>();
  ```

  Then DOCX exports will contain an embedded PNG of the diagram. If `mmdc` is not installed or fails, the export still succeeds and falls back to Mermaid source in the document.

## CLI (ArchLucid.Cli)

The ArchLucid CLI is wired to the ArchLucid API over HTTP: all of `run`, `status`, `commit`, `seed`, and `artifacts` call the API. It lets you create projects, run architecture requests, and inspect results. For a full command and config reference, see [docs/CLI_USAGE.md](docs/library/CLI_USAGE.md). Run commands with:

```bash
dotnet run --project ArchLucid.Cli -- <command> [options]
```

## Comparison replay

ArchLucid can persist comparison records (end-to-end run comparisons and export-record diffs) and later **replay** them to regenerate summaries or export artifacts (Markdown, HTML, DOCX, PDF). Replays can also be run in **verify** mode to detect drift between stored and regenerated comparisons, and can optionally be **persisted as new comparison records** for a full audit trail.

For details, including replay modes, supported formats, headers, and example curl commands, see [docs/COMPARISON_REPLAY.md](docs/library/COMPARISON_REPLAY.md).

## Decisioning: typed findings

The decisioning layer uses a **Finding envelope** plus **strongly typed payloads per category**, and the decision engine extracts those typed payloads when building manifest decisions and warnings.

See [docs/DECISIONING_TYPED_FINDINGS.md](docs/library/DECISIONING_TYPED_FINDINGS.md).

### Prerequisites

- .NET 10 SDK
- ArchLucid API running (e.g. `dotnet run --project ArchLucid.Api`)
- For `run`, `status`, `commit`, `seed`, `artifacts`: a project directory with `archlucid.json` and `inputs/brief.md`

### Commands

| Command | Description |
|---------|-------------|
| `new <projectName>` | Create a new project skeleton with `archlucid.json`, `inputs/brief.md`, `outputs/`, and Terraform stubs |
| `dev up` | Start SQL Server, Azurite, and Redis via Docker Compose (requires `docker-compose.yml` in repo root) |
| `run` | Submit an architecture request to the API. Reads `archlucid.json` and `inputs/brief.md` |
| `run --quick` | Same as `run`, then seeds fake results and commits in one step (Development only) |
| `status <runId>` | Show run status, tasks, and submitted results |
| `submit <runId> <result.json>` | Submit an agent result for a run (JSON file must match AgentResult schema) |
| `seed <runId>` | Seed fake agent results for a run (Development only; for smoke testing) |
| `commit <runId>` | Merge results and produce a versioned manifest |
| `artifacts <runId>` | Fetch and display the committed manifest for a run |
| `artifacts <runId> --save` | Same, and save the manifest to `outputs/manifest-{version}.json` (requires project dir) |
| `health` | Check connectivity to the ArchLucid API (`GET /health`). Use to verify the API is running before run/status/commit/seed/artifacts. |
| `doctor` / `check` | Run local project checks and print `GET /health/live`, `/health/ready`, and `/health` (readiness diagnostics). |

### Typical workflow

```bash
# 1. Create a new project
dotnet run --project ArchLucid.Cli -- new MyProject
cd MyProject

# 2. Edit inputs/brief.md with your architecture brief (min 10 chars)

# 3. Start the API (in another terminal)
cd .. && dotnet run --project ArchLucid.Api

# 4a. Full flow: create run, submit agent results, then commit
dotnet run --project ArchLucid.Cli -- run
dotnet run --project ArchLucid.Cli -- status <runId>
dotnet run --project ArchLucid.Cli -- submit <runId> topology-result.json
# ... submit more results (cost, compliance) as needed ...
dotnet run --project ArchLucid.Cli -- commit <runId>
dotnet run --project ArchLucid.Cli -- artifacts <runId>

# 4b. Quick dev flow: create run, seed fake results, and commit in one step
dotnet run --project ArchLucid.Cli -- run --quick
dotnet run --project ArchLucid.Cli -- artifacts <runId>
```

### Configuration

- **API URL**: Set `apiUrl` in `archlucid.json` or the `ARCHLUCID_API_URL` environment variable. Default: `http://localhost:5128`.

### Installing as a global .NET tool

Package and install the CLI locally:

```bash
# From the solution root
dotnet pack ArchLucid.Cli/ArchLucid.Cli.csproj -c Release -o nupkg

# Install globally
dotnet tool install -g ArchLucid.Cli --add-source ./nupkg

# Run (no need for dotnet run)
archlucid new MyProject
archlucid run
archlucid status <runId>
```

To update: `dotnet tool update -g ArchLucid.Cli --add-source ./nupkg`

## Project Structure

| Project | Description |
|---------|-------------|
| ArchLucid.Api | ASP.NET Core Web API, controllers, health checks |
| ArchLucid.Application | Analysis report building, DOCX/Markdown export, diagram image rendering (Mermaid → PNG) |
| ArchLucid.Contracts | DTOs, request/response types, manifest models |
| ArchLucid.Coordinator | Run creation, task generation |
| ArchLucid.Decisioning | Governance, findings, comparisons, alerts — plus manifest merge (`ArchLucid.Decisioning.Merge`) and JSON schema validation (`ArchLucid.Decisioning.Validation`) |
| ArchLucid.Persistence (`Data.*` sub-namespaces) | Workflow Dapper repos, DbUp migrations, `IDbConnectionFactory`, consolidated `Scripts/ArchLucid.sql` |
| ArchLucid.Cli | ArchLucid CLI: `new`, `run`, `status`, `commit`, `seed`, `artifacts`, `dev up` |

## Architecture docs (internal)

**Canonical poster (start here):** [`docs/ARCHITECTURE_ON_ONE_PAGE.md`](docs/ARCHITECTURE_ON_ONE_PAGE.md) — C4 system context + containers, ownership table, happy-path trace. **Operator action map:** [`docs/OPERATOR_ATLAS.md`](docs/library/OPERATOR_ATLAS.md) — every major UI route with API + CLI + authority hints.

For deeper dives after the poster:

- `docs/ARCHITECTURE_INDEX.md` – full doc map and indexes everything below.
- `docs/ARCHITECTURE_CONTEXT.md` – high-level system context and qualities.
- `docs/ARCHITECTURE_CONTAINERS.md` – projects/containers and their responsibilities.
- `docs/ARCHITECTURE_COMPONENTS.md` – key components to touch when changing behavior.
- `docs/ARCHITECTURE_FLOWS.md` – run, export, and comparison/replay flows.
- `docs/DATA_MODEL.md` – core tables/records and relationships.
- `docs/COMPARISON_REPLAY.md` – comparison replay API, modes, and recipes.
- `docs/HOWTO_ADD_COMPARISON_TYPE.md` – step-by-step for introducing new comparison types.
- `docs/RUNBOOK_REPLAY_DRIFT.md` – debugging replay/drift verification issues.
</details>
