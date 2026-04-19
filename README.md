# ArchLucid

ArchLucid shortens the path from an architecture request to a reviewable, defensible architecture package. It helps teams produce committed manifests, reviewable artifacts, and stronger governance evidence with less manual assembly and less ambiguity about what changed and why.

At the product level, ArchLucid is an AI-assisted architecture workflow system: it coordinates topology, cost, and compliance analysis to produce manifests, artifacts, and evidence that architects, reviewers, and governance stakeholders can actually use.

**Canonical buyer narrative:** For sponsor-facing and outward buyer messaging, start with **[docs/EXECUTIVE_SPONSOR_BRIEF.md](docs/EXECUTIVE_SPONSOR_BRIEF.md)**. The rest of this repository should stay aligned with that summary rather than competing with it.

**Repository layout:** Source lives under **`ArchLucid.*`** projects, **`archlucid-ui/`**, and **`docs/`**. Local packaging writes to **`artifacts/`** (gitignored). See **[docs/REPO_HYGIENE.md](docs/REPO_HYGIENE.md)** for what to commit vs regenerate.

## Getting started

**Pick your persona (canonical Day-1):** **[docs/onboarding/day-one-developer.md](docs/onboarding/day-one-developer.md)** (engineering), **[docs/onboarding/day-one-sre.md](docs/onboarding/day-one-sre.md)** (platform), **[docs/onboarding/day-one-security.md](docs/onboarding/day-one-security.md)** (security), **[docs/OPERATOR_QUICKSTART.md](docs/OPERATOR_QUICKSTART.md)** (copy-paste commands). A short redirect hub lives at **[docs/START_HERE.md](docs/START_HERE.md)**. Deeper maps: **[docs/ARCHITECTURE_INDEX.md](docs/ARCHITECTURE_INDEX.md)**.

## Key documentation

| Doc | Purpose |
|-----|---------|
| **[docs/EXECUTIVE_SPONSOR_BRIEF.md](docs/EXECUTIVE_SPONSOR_BRIEF.md)** | **Canonical buyer narrative:** what ArchLucid does, what a pilot proves, and why expansion matters |
| **[docs/PILOT_ROI_MODEL.md](docs/PILOT_ROI_MODEL.md)** | **Measurement companion:** what to measure, what success looks like, and how sponsors can justify a pilot without turning the ROI model into a second buyer story |
| **[docs/OPERATOR_DECISION_GUIDE.md](docs/OPERATOR_DECISION_GUIDE.md)** | **Usage guidance:** when to stay in Core Pilot and when Advanced Analysis or Enterprise Controls are worth using, without turning the guide into a second buyer or ROI brief |
| **[docs/OPERATOR_QUICKSTART.md](docs/OPERATOR_QUICKSTART.md)** | **Operator Day-1:** health, curl, CLI, smoke/tests (commands only) |
| **[docs/onboarding/day-one-developer.md](docs/onboarding/day-one-developer.md)** | **Developer Day-1:** toolchain, local API + SQL, Core tests, one small PR |
| **[docs/onboarding/day-one-sre.md](docs/onboarding/day-one-sre.md)** | **SRE / Platform Day-1:** health model, deploy order, Terraform validate, migrations posture |
| **[docs/onboarding/day-one-security.md](docs/onboarding/day-one-security.md)** | **Security Day-1:** trust boundaries, authZ, RLS, supply chain |
| **[docs/FUTURE_PACKAGING_ENFORCEMENT.md](docs/FUTURE_PACKAGING_ENFORCEMENT.md)** | **Future packaging map:** how today’s layer model could evolve into stronger commercial boundaries later |
| **[docs/archive/README.md](docs/archive/README.md)** | **Archive index** — historical write-ups (including superseded long-form onboarding bodies) |

Everything else (architecture index, V1 scope, SQL reference, runbooks, etc.) is linked from **[docs/ARCHITECTURE_INDEX.md](docs/ARCHITECTURE_INDEX.md)** and the Day-1 docs above.

## Product layers

ArchLucid ships as three distinct capability layers.

**Default buying motion:** start with **Core Pilot** so a team can move from request to committed manifest and reviewable artifacts quickly. Only then expand into **Advanced Analysis** or **Enterprise Controls** when a real analytical or governance question requires them.

**First-pilot rule:** if you are evaluating whether ArchLucid creates value, stay on the **Core Pilot** path first. Treat deeper layers as follow-on maturity paths, not as co-equal Day-1 requirements.

| Layer | What it covers | Why it matters | How to reach it |
|-------|---------------|----------------|-----------------|
| **Core Pilot** | Create run → execute → commit → review manifest and artifacts | Proves fast path from request to reviewable output with less manual packaging effort | Default sidebar and home page |
| **Advanced Analysis** | Compare runs, replay authority chains, explore provenance graphs, Ask Q&A, advisory scans, pilot feedback | Helps architects and reviewers understand what changed, why it changed, and what needs attention | **Show more links** in the sidebar |
| **Enterprise Controls** | Governance approvals, policy packs, audit log, compliance drift, alerts and rules | Helps governance, audit, and security stakeholders trust and operationalize architecture decisions | Extended and advanced sidebar links; role-aware **UI shaping** (not entitlements) — see [docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md) §4 and [archlucid-ui/README.md](archlucid-ui/README.md#seam-maintenance-anti-drift) |

Full capability inventory: **[docs/PRODUCT_PACKAGING.md](docs/PRODUCT_PACKAGING.md)** (§3 *Four UI shaping surfaces* — shell vs mutation hook vs **`LayerHeader`** vs inline cues; *Contributor drift guard* + *Cross-surface lock* — keep **`nav-config.ts`**, **`nav-shell-visibility.ts`** (**tier → authority**), **`current-principal.ts`** (`/me` read-model), **`layer-guidance.ts` / `LayerHeader`** (**`LAYER_PAGE_GUIDANCE`** Enterprise **`enterpriseFootnote`** vs Advanced; Enterprise strip **`aria-label`**), **`enterprise-mutation-capability.ts`** / **`useEnterpriseMutationCapability()`** (Execute+ soft-disable), **Vitest** seam tests including **`authority-seam-regression.test.ts`**, **`authority-execute-floor-regression.test.ts`**, and **`authority-shaped-ui-regression.test.ts`** (catalog **`ExecuteAuthority`** rows + mutation floor invariants), and **API** policies aligned). First-pilot walkthrough: **[docs/CORE_PILOT.md](docs/CORE_PILOT.md)**. **Measurement companion:** **[docs/PILOT_ROI_MODEL.md](docs/PILOT_ROI_MODEL.md)**. **Usage guidance:** **[docs/OPERATOR_DECISION_GUIDE.md](docs/OPERATOR_DECISION_GUIDE.md)**. **Canonical buyer narrative:** **[docs/EXECUTIVE_SPONSOR_BRIEF.md](docs/EXECUTIVE_SPONSOR_BRIEF.md)**. Future packaging map: **[docs/FUTURE_PACKAGING_ENFORCEMENT.md](docs/FUTURE_PACKAGING_ENFORCEMENT.md)**. **Operator UI shaping only:** [archlucid-ui/README.md](archlucid-ui/README.md#seam-maintenance-anti-drift) — nav and soft-disable follow **`/me`**; **ArchLucid.Api** still returns **401/403**. **Page-level mutation + layout seams:** Vitest **`archlucid-ui/src/app/(operator)/enterprise-authority-ui-shaping.test.tsx`** (hook → **`disabled`** / **`readOnly`**), **`archlucid-ui/src/app/(operator)/authority-shaped-layout-regression.test.tsx`** (inspect-first layout when mutation is off).

## Pilot onboarding (56R)

**Product boundary (V1):** [docs/V1_SCOPE.md](docs/V1_SCOPE.md). **Pre-handoff checklist:** [docs/V1_RELEASE_CHECKLIST.md](docs/V1_RELEASE_CHECKLIST.md). **Commands:** [docs/OPERATOR_QUICKSTART.md](docs/OPERATOR_QUICKSTART.md). **Measurement companion and success criteria:** [docs/PILOT_ROI_MODEL.md](docs/PILOT_ROI_MODEL.md). **Layer decision guidance:** [docs/OPERATOR_DECISION_GUIDE.md](docs/OPERATOR_DECISION_GUIDE.md). **Canonical buyer narrative:** [docs/EXECUTIVE_SPONSOR_BRIEF.md](docs/EXECUTIVE_SPONSOR_BRIEF.md). **Narrative (archived):** [docs/archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md](docs/archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md). **Fix issues:** [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md). **Package an RC:** [docs/RELEASE_LOCAL.md](docs/RELEASE_LOCAL.md).

**Before a handoff or demo:** `run-readiness-check.cmd` or `.\run-readiness-check.ps1`. For **API + CLI quick run + artifacts** in one script, set **`ARCHLUCID_SMOKE_SQL`** and run **`release-smoke.cmd`** ([docs/RELEASE_SMOKE.md](docs/RELEASE_SMOKE.md)); optional UI E2E: **`.\release-smoke.ps1 -RunPlaywright`** ([archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md](archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md#8-e2e-tests-playwright)).

**Build / version:** **`GET /version`** on the API, or **`dotnet run --project ArchLucid.Cli -- doctor`**. **Diagnostics:** **`dotnet run --project ArchLucid.Cli -- support-bundle --zip`** (review before sharing). **Reporting issues:** [docs/PILOT_GUIDE.md#when-you-report-an-issue](docs/PILOT_GUIDE.md#when-you-report-an-issue) (version, correlation ID, logs, bundle).

## Operator quick start

- **Health:** `GET /health/live` (liveness), `GET /health/ready` (readiness: DB when using Sql storage, schema files, compliance rule pack, temp dir), `GET /health` (all checks). See [docs/BUILD.md](docs/BUILD.md) for startup vs migration failure behavior.
- **Versioned API:** Routes are under `/v1/...`. Send optional **`X-Correlation-ID`** on requests for support correlation (see [docs/API_CONTRACTS.md](docs/API_CONTRACTS.md)).
- **Auth:** Configure **`ArchLucidAuth`**: shipped **`appsettings.json`** defaults to **`ApiKey`** mode with API keys **disabled** (fail closed) until you enable keys; **`appsettings.Development.json`** switches to **`DevelopmentBypass`** when `ASPNETCORE_ENVIRONMENT=Development`. Production samples use **`JwtBearer`**. Policies map to `ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` (see **API authentication** below).
- **SMB / storage:** Do not expose file shares (SMB, port 445) on the public internet; use private endpoints and controlled boundaries for any Azure storage or hybrid file access.

### Integration events (optional Azure Service Bus)

ArchLucid publishes JSON integration events to an Azure Service Bus topic for
lifecycle hooks (run completion, governance, alerts, advisory scans).
- **Event catalog:** [`schemas/integration-events/catalog.json`](schemas/integration-events/catalog.json)
- **Payload schemas:** [`schemas/integration-events/*.v1.schema.json`](schemas/integration-events/)
- **AsyncAPI spec:** [`docs/contracts/archlucid-asyncapi-2.6.yaml`](docs/contracts/archlucid-asyncapi-2.6.yaml)
- **Full reference:** [`docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`](docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full) with a database for ArchLucid
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional; for `archlucid dev up`)
- Node.js 22+ (optional; for the operator UI in `archlucid-ui` — aligns with CI)

## Operator UI (`archlucid-ui`)

A thin Next.js shell organized around the three product layers: **Core Pilot** (runs, commit, manifest, artifacts) visible by default; **Advanced Analysis** (compare, replay, graph, advisory) and **Enterprise Controls** (governance, audit, alerts, policy) via progressive disclosure.

**Keep the default mental model narrow:** Core Pilot is the default path. Advanced Analysis and Enterprise Controls are follow-on layers for specific analytical or governance questions, not required for first-pilot success.

**Role-aware shaping (first wave, implemented):** the UI composes **disclosure tier first**, then optional per-link **`requiredAuthority`** (same names as API policies: `ReadAuthority` / `ExecuteAuthority` / `AdminAuthority`) using **`GET /api/auth/me`** via the proxy (`archlucid-ui/src/lib/current-principal.ts` + **`OperatorNavAuthorityProvider`** for a single in-shell read-model and rank; composition in **`nav-shell-visibility.ts`**). That is **operational accountability** (who should see operator/admin surfaces)—**not** pricing, billing, or entitlements. **The API still returns 401/403**; the shell must not be treated as authorization. **Contributor maintenance map** (which TS modules map to which packaging layer): [docs/PRODUCT_PACKAGING.md](docs/PRODUCT_PACKAGING.md) §3 — *Code seams (operator UI — maintenance map)* and *Contributor drift guard*. **Cross-module Vitest:** [`archlucid-ui/src/lib/authority-seam-regression.test.ts`](archlucid-ui/src/lib/authority-seam-regression.test.ts) (tier ∩ rank, Enterprise monotonicity, progressive disclosure); [`archlucid-ui/src/lib/authority-execute-floor-regression.test.ts`](archlucid-ui/src/lib/authority-execute-floor-regression.test.ts) (Execute nav row vs mutation boolean). Do not bypass or duplicate this stack ad hoc; see [archlucid-ui/README.md](archlucid-ui/README.md#seam-maintenance-anti-drift), [docs/operator-shell.md](docs/operator-shell.md), and [docs/PRODUCT_PACKAGING.md](docs/PRODUCT_PACKAGING.md#what-the-layer-model-means-today) (role-based restriction vs future entitlement). **57R:** Playwright operator-journey smoke uses **deterministic mocks** (no live C# API in that suite) — see [archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md](archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md#8-e2e-tests-playwright).

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

This starts SQL Server, Azurite, and Redis in Docker (default profile — for hot-reload development). To run the full stack (API + UI in containers too): `docker compose --profile full-stack up -d --build`. For the **Docker-only evaluator path** with Contoso demo seed and simulator agents, use `.\scripts\demo-start.ps1` or see [docs/go-to-market/DEMO_QUICKSTART.md](docs/go-to-market/DEMO_QUICKSTART.md). See [docs/CONTAINERIZATION.md](docs/CONTAINERIZATION.md).

Use this connection string with the API:

```
Server=localhost,1433;Database=ArchLucid;User Id=sa;Password=ArchLucid_Dev_Pass123!;TrustServerCertificate=True;
```

## Database Setup

1. Create a database (for example `ArchLucid`, or a pilot-specific name), or use `archlucid dev up` to run SQL Server in Docker.
2. Migrations run automatically on startup via [DbUp](https://dbup.readthedocs.io/). Scripts in `ArchLucid.Persistence/Migrations/` are applied in order; add new `00x_Description.sql` files for schema changes. **Greenfield** empty catalogs replay **`001`–`050`** once (then stamp `SchemaVersions` so DbUp continues at **`051`**); see **[docs/SQL_SCRIPTS.md](docs/SQL_SCRIPTS.md)** §4.0. If the connection string is set and migration fails, the API throws and does not start (no fallback). Integration tests use **SQL Server** (per-test databases; **DbUp** runs on the test host). Full detail: **[docs/SQL_SCRIPTS.md](docs/SQL_SCRIPTS.md)** (consolidated `ArchLucid.sql`, Persistence bootstrap, two “run” tables). Governance workflow tables ship as **`017_GovernanceWorkflow.sql`**.

### Optional: Contoso trusted-baseline demo (Corrected 50R)

For a deterministic **baseline vs hardened** story (runs, manifests, governance approvals, environment activations; export history row optional), see **[docs/demo-quickstart.md](docs/demo-quickstart.md)** and the honesty boundary in **[docs/TRUSTED_BASELINE.md](docs/TRUSTED_BASELINE.md)**. Summary: set `ArchLucid:StorageProvider` to `Sql`, configure `Demo:Enabled` / `Demo:SeedOnStartup` (Development only for automatic startup seed), or call **`POST /v1.0/demo/seed`** when `Demo:Enabled` is true. Startup logs label schema bootstrap, DbUp, and demo seed in order.

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

Full **54R** tier list, copy-paste commands, SQL variables, and **`archlucid-ui`** Vitest/Playwright: **[docs/TEST_STRUCTURE.md](docs/TEST_STRUCTURE.md)**. CI job mapping: **[docs/TEST_EXECUTION_MODEL.md](docs/TEST_EXECUTION_MODEL.md)**.

**Common entry points (repo root):**

```bash
dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

```bash
dotnet test ArchLucid.sln
```

```bash
cd archlucid-ui && npm ci && npm test
```

**ArchLucid.Api.Tests** integration tests need a reachable **SQL Server**; **`ArchLucidApiFactory`** creates ephemeral databases and runs **DbUp**. See **[docs/BUILD.md](docs/BUILD.md)** for CPM, connection strings, and DecisionEngine’s Microsoft.Extensions bundle.

**Notable API behavior:** comparison replay with `replayMode: verify` returns **422** (problem+json with drift fields) when regenerated output does not match the stored comparison—not HTTP 200 with a failure flag. End-to-end run compare uses **`#run-not-found`** when a run ID is missing. See [docs/API_CONTRACTS.md](docs/API_CONTRACTS.md).

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

The ArchLucid CLI is wired to the ArchLucid API over HTTP: all of `run`, `status`, `commit`, `seed`, and `artifacts` call the API. It lets you create projects, run architecture requests, and inspect results. For a full command and config reference, see [docs/CLI_USAGE.md](docs/CLI_USAGE.md). Run commands with:

```bash
dotnet run --project ArchLucid.Cli -- <command> [options]
```

## Comparison replay

ArchLucid can persist comparison records (end-to-end run comparisons and export-record diffs) and later **replay** them to regenerate summaries or export artifacts (Markdown, HTML, DOCX, PDF). Replays can also be run in **verify** mode to detect drift between stored and regenerated comparisons, and can optionally be **persisted as new comparison records** for a full audit trail.

For details, including replay modes, supported formats, headers, and example curl commands, see [docs/COMPARISON_REPLAY.md](docs/COMPARISON_REPLAY.md).

## Decisioning: typed findings

The decisioning layer uses a **Finding envelope** plus **strongly typed payloads per category**, and the decision engine extracts those typed payloads when building manifest decisions and warnings.

See [docs/DECISIONING_TYPED_FINDINGS.md](docs/DECISIONING_TYPED_FINDINGS.md).

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

For a deeper understanding of how ArchLucid fits together:

- `docs/ARCHITECTURE_INDEX.md` – starting point; links to all other docs.
- `docs/ARCHITECTURE_CONTEXT.md` – high-level system context and qualities.
- `docs/ARCHITECTURE_CONTAINERS.md` – projects/containers and their responsibilities.
- `docs/ARCHITECTURE_COMPONENTS.md` – key components to touch when changing behavior.
- `docs/ARCHITECTURE_FLOWS.md` – run, export, and comparison/replay flows.
- `docs/DATA_MODEL.md` – core tables/records and relationships.
- `docs/COMPARISON_REPLAY.md` – comparison replay API, modes, and recipes.
- `docs/HOWTO_ADD_COMPARISON_TYPE.md` – step-by-step for introducing new comparison types.
- `docs/RUNBOOK_REPLAY_DRIFT.md` – debugging replay/drift verification issues.
