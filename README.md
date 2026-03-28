# ArchiForge

ArchiForge is an API for orchestrating AI-driven architecture design. It coordinates topology, cost, and compliance agents to produce architecture manifests from high-level requests.

## Key documentation

| Doc | Purpose |
|-----|---------|
| [docs/BUILD.md](docs/BUILD.md) | Build, CPM, project references, DecisionEngine bundle |
| [docs/FORMATTING.md](docs/FORMATTING.md) | C# layout / blank lines (`dotnet format`, `.editorconfig`) |
| [docs/METHOD_DOCUMENTATION.md](docs/METHOD_DOCUMENTATION.md) | XML doc conventions; piece-by-piece API commentary |
| [docs/ALERTS.md](docs/ALERTS.md) | Alerts, routing, simulation/tuning, advisory schedules (links to API contracts & doc tracker) |
| [docs/TEST_STRUCTURE.md](docs/TEST_STRUCTURE.md) | Test categories (`Integration` / `Unit`) and filter examples |
| [docs/API_CONTRACTS.md](docs/API_CONTRACTS.md) | HTTP behaviors (422 verify, 404 run-not-found, 409 commit, validation, **policy packs** / effective governance) |
| [docs/CLI_USAGE.md](docs/CLI_USAGE.md) | CLI reference |
| [docs/COMPARISON_REPLAY.md](docs/COMPARISON_REPLAY.md) | Comparison replay concepts |
| [docs/ARCHITECTURE_INDEX.md](docs/ARCHITECTURE_INDEX.md) | Architecture overview and cross-links |
| [docs/KNOWLEDGE_GRAPH.md](docs/KNOWLEDGE_GRAPH.md) | Typed graph from `ContextSnapshot`, edge inference, validation, manifest hooks |
| [docs/DATA_MODEL.md](docs/DATA_MODEL.md) | Persisted tables & domains (migrations + authority DDL overview) |
| [docs/SQL_SCRIPTS.md](docs/SQL_SCRIPTS.md) | **SQL reference:** DbUp migrations, consolidated scripts, bootstrap paths, troubleshooting, change checklist |
| [docs/demo-quickstart.md](docs/demo-quickstart.md) | **Corrected 50R demo:** DbUp + Contoso trusted-baseline seed, `Demo:*` config, `POST /v1.0/demo/seed`, verification endpoints |
| [docs/TRUSTED_BASELINE.md](docs/TRUSTED_BASELINE.md) | **49R pass 2 boundary + Corrected 51R:** baseline-trusted surface, optional features, centralized actor (`IActorContext`), log-only baseline mutation audit (`IBaselineMutationAuditService`) vs SQL audit |

## Operator quick start

- **Health:** `GET /health` runs registered checks (including the database when a connection string is configured). See [docs/BUILD.md](docs/BUILD.md) for startup vs migration failure behavior.
- **Versioned API:** Routes are under `/v1/...`. Send optional **`X-Correlation-ID`** on requests for support correlation (see [docs/API_CONTRACTS.md](docs/API_CONTRACTS.md)).
- **Auth:** Configure **`ArchiForgeAuth`** (`DevelopmentBypass` locally, `JwtBearer` in production). Policies map to `ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` (see **API authentication** below).
- **SMB / storage:** Do not expose file shares (SMB, port 445) on the public internet; use private endpoints and controlled boundaries for any Azure storage or hybrid file access.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full) with a database for ArchiForge
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional; for `archiforge dev up`)
- Node.js 20+ (optional; for the operator UI in `archiforge-ui/`)

## Operator UI (`archiforge-ui`)

A thin Next.js shell for runs, manifest summary, artifacts, compare, replay, and ZIP downloads. See [archiforge-ui/README.md](archiforge-ui/README.md).

## API authentication (`ArchiForgeAuth`)

Configure in `appsettings.*` under **`ArchiForgeAuth`**:

| Mode | Purpose |
|------|---------|
| **`DevelopmentBypass`** (default) | Local/dev: every request is authenticated as a configurable dev user with role `DevRole` (`Admin` by default). |
| **`JwtBearer`** | Production-style JWT validation using `Authority` and optional `Audience`. Map app roles to `Admin` / `Operator` / `Reader` in your IdP. |

Role claims are mapped to legacy **`permission`** claims via `ArchiForgeRoleClaimsTransformation` so existing policies (`CanCommitRuns`, etc.) keep working. Policies: **`ReadAuthority`** (Reader+), **`ExecuteAuthority`** (Operator+), **`AdminAuthority`** (Admin only). Debug principal: **`GET /api/auth/me`**.

The older **`Authentication:ApiKey`** block is no longer wired in `Program.cs` (handler remains in the repo for reference). Use JWT or DevelopmentBypass instead.

## Development environment (`archiforge dev up`)

From the ArchiForge repo directory (or any directory containing `docker-compose.yml`), run:

```bash
dotnet run --project ArchiForge.Cli -- dev up
```

This starts SQL Server, Azurite, and Redis in Docker. Use this connection string with the API:

```
Server=localhost,1433;Database=ArchiForge;User Id=sa;Password=ArchiForge_Dev_Pass123!;TrustServerCertificate=True;
```

## Database Setup

1. Create a database (e.g. `ArchiForge2`), or use `archiforge dev up` to run SQL Server in Docker.
2. Migrations run automatically on startup via [DbUp](https://dbup.readthedocs.io/). Scripts in `ArchiForge.Data/Migrations/` are applied in order; add new `00x_Description.sql` files for schema changes. If the connection string is set and migration fails, the API throws and does not start (no fallback). Integration tests use **SQL Server** (per-test databases; **DbUp** runs on the test host). Full detail: **[docs/SQL_SCRIPTS.md](docs/SQL_SCRIPTS.md)** (consolidated `ArchiForge.sql`, Persistence bootstrap, two “run” tables). Governance workflow tables ship as **`017_GovernanceWorkflow.sql`**.

### Optional: Contoso trusted-baseline demo (Corrected 50R)

For a deterministic **baseline vs hardened** story (runs, manifests, governance approvals, environment activations; export history row optional), see **[docs/demo-quickstart.md](docs/demo-quickstart.md)** and the honesty boundary in **[docs/TRUSTED_BASELINE.md](docs/TRUSTED_BASELINE.md)**. Summary: set `ArchiForge:StorageProvider` to `Sql`, configure `Demo:Enabled` / `Demo:SeedOnStartup` (Development only for automatic startup seed), or call **`POST /v1.0/demo/seed`** when `Demo:Enabled` is true. Startup logs label schema bootstrap, DbUp, and demo seed in order.

## Secrets (development)

**Do not commit connection strings or API keys.** The API project has [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) enabled. In Development, configuration is loaded from User Secrets after `appsettings.*.json`.

From the repo root:

```bash
cd ArchiForge.Api

# Required: database connection (use your own connection string or the dev Docker one below)
dotnet user-secrets set "ConnectionStrings:ArchiForge" "Server=localhost,1433;Database=ArchiForge;User Id=sa;Password=ArchiForge_Dev_Pass123!;TrustServerCertificate=True;"

# Optional: only if using real agents (AgentExecution:Mode != Simulator) with Azure OpenAI
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o"
```

**Production:** Use environment variables or your hosting provider’s secret store (e.g. Azure Key Vault, AWS Secrets Manager). Do not use User Secrets in production.

## Running the API

```bash
dotnet run --project ArchiForge.Api
```

The API listens on the URLs configured for the project (default `http://localhost:5128`; see `ArchiForge.Api/Properties/launchSettings.json`).

**API versioning:** Routes use a URL path segment **`v1`** (e.g. `/v1/architecture/...`). The default API version is **1.0** when unspecified; **`api-supported-versions`** (and related) response headers report supported versions ([Asp.Versioning.Mvc](https://github.com/dotnet/aspnet-api-versioning)).

**Correlation ID:** Send **`X-Correlation-ID`** on requests to tie logs and tracing to a client trace. If omitted, the server assigns a value (from the request trace id) and returns it on the response. Middleware applies the same id across the pipeline.

In Development:

- **Swagger UI**: `/swagger`
- **Health check**: `GET /health` — includes a database check; returns 200 when healthy and 503 (Unhealthy) when the DB check fails. Use for load balancers and runbooks.

**Rate limiting:** Applied to `/v1/architecture/*` endpoints. When a policy limit is exceeded, the API returns **429 Too Many Requests**.

| Policy     | Use case              | Default limit (per client/window) | Config keys |
|-----------|------------------------|-----------------------------------|-------------|
| `fixed`   | General endpoints     | 100/min                           | `RateLimiting:FixedWindow:PermitLimit`, `WindowMinutes`, `QueueLimit` |
| `expensive` | Execute, commit, replay | 20/min                         | `RateLimiting:Expensive:*` |
| `replay`  | Comparison replay     | Light (markdown/html) 60/min; heavy (docx/pdf) 15/min | `RateLimiting:Replay:Light:*`, `RateLimiting:Replay:Heavy:*` |

Override in `appsettings.json` or via environment variables.

**CORS:** Configure allowed origins with **`Cors:AllowedOrigins`** (array of origins). The API uses the policy name **`ArchiForge`** (`UseCors("ArchiForge")`). If the array is empty or missing, no origins are allowed (`SetIsOriginAllowed(_ => false)`). Use this for SPA or cross-origin API clients.

**Authentication:** Send the **`X-Api-Key`** header with every request to protected endpoints. Config: **`Authentication:ApiKey:Enabled`** (default `false`; when `false`, all requests are treated as authenticated with full permissions for local dev). When enabled, set **`Authentication:ApiKey:AdminKey`** and optionally **`Authentication:ApiKey:ReadOnlyKey`** (e.g. in User Secrets or environment). Authorization policies require these permission claims: **`commit:run`**, **`seed:results`**, **`export:consulting-docx`**, **`replay:comparisons`**, **`replay:diagnostics`**. Admin key receives all; read-only key receives a subset.

## Running Tests

```bash
dotnet test
```

See **[docs/BUILD.md](docs/BUILD.md)** for CPM, project-reference audits, and DecisionEngine’s Microsoft.Extensions bundle.

See **[docs/TEST_STRUCTURE.md](docs/TEST_STRUCTURE.md)** for test categories (Integration vs Unit) and filter examples (`Category=Integration`, `Category=Unit`). **ArchiForge.Api.Tests** integration tests require a reachable **SQL Server** instance (e.g. `localhost`); **`ArchiForgeApiFactory`** creates a temporary database per factory and runs **DbUp** on startup.

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

## CLI (ArchiForge.Cli)

The ArchiForge CLI is wired to the ArchiForge API over HTTP: all of `run`, `status`, `commit`, `seed`, and `artifacts` call the API. It lets you create projects, run architecture requests, and inspect results. For a full command and config reference, see [docs/CLI_USAGE.md](docs/CLI_USAGE.md). Run commands with:

```bash
dotnet run --project ArchiForge.Cli -- <command> [options]
```

## Comparison replay

ArchiForge can persist comparison records (end-to-end run comparisons and export-record diffs) and later **replay** them to regenerate summaries or export artifacts (Markdown, HTML, DOCX, PDF). Replays can also be run in **verify** mode to detect drift between stored and regenerated comparisons, and can optionally be **persisted as new comparison records** for a full audit trail.

For details, including replay modes, supported formats, headers, and example curl commands, see [docs/COMPARISON_REPLAY.md](docs/COMPARISON_REPLAY.md).

## Decisioning: typed findings

The decisioning layer uses a **Finding envelope** plus **strongly typed payloads per category**, and the decision engine extracts those typed payloads when building manifest decisions and warnings.

See [docs/DECISIONING_TYPED_FINDINGS.md](docs/DECISIONING_TYPED_FINDINGS.md).

### Prerequisites

- .NET 10 SDK
- ArchiForge API running (e.g. `dotnet run --project ArchiForge.Api`)
- For `run`, `status`, `commit`, `seed`, `artifacts`: a project directory with `archiforge.json` and `inputs/brief.md`

### Commands

| Command | Description |
|---------|-------------|
| `new <projectName>` | Create a new project skeleton with `archiforge.json`, `inputs/brief.md`, `outputs/`, and Terraform stubs |
| `dev up` | Start SQL Server, Azurite, and Redis via Docker Compose (requires `docker-compose.yml` in repo root) |
| `run` | Submit an architecture request to the API. Reads `archiforge.json` and `inputs/brief.md` |
| `run --quick` | Same as `run`, then seeds fake results and commits in one step (Development only) |
| `status <runId>` | Show run status, tasks, and submitted results |
| `submit <runId> <result.json>` | Submit an agent result for a run (JSON file must match AgentResult schema) |
| `seed <runId>` | Seed fake agent results for a run (Development only; for smoke testing) |
| `commit <runId>` | Merge results and produce a versioned manifest |
| `artifacts <runId>` | Fetch and display the committed manifest for a run |
| `artifacts <runId> --save` | Same, and save the manifest to `outputs/manifest-{version}.json` (requires project dir) |
| `health` | Check connectivity to the ArchiForge API (GET /health). Use to verify the API is running before run/status/commit/seed/artifacts. |

### Typical workflow

```bash
# 1. Create a new project
dotnet run --project ArchiForge.Cli -- new MyProject
cd MyProject

# 2. Edit inputs/brief.md with your architecture brief (min 10 chars)

# 3. Start the API (in another terminal)
cd .. && dotnet run --project ArchiForge.Api

# 4a. Full flow: create run, submit agent results, then commit
dotnet run --project ArchiForge.Cli -- run
dotnet run --project ArchiForge.Cli -- status <runId>
dotnet run --project ArchiForge.Cli -- submit <runId> topology-result.json
# ... submit more results (cost, compliance) as needed ...
dotnet run --project ArchiForge.Cli -- commit <runId>
dotnet run --project ArchiForge.Cli -- artifacts <runId>

# 4b. Quick dev flow: create run, seed fake results, and commit in one step
dotnet run --project ArchiForge.Cli -- run --quick
dotnet run --project ArchiForge.Cli -- artifacts <runId>
```

### Configuration

- **API URL**: Set `apiUrl` in `archiforge.json` or the `ARCHIFORGE_API_URL` environment variable. Default: `http://localhost:5128`.

### Installing as a global .NET tool

Package and install the CLI locally:

```bash
# From the solution root
dotnet pack ArchiForge.Cli/ArchiForge.Cli.csproj -c Release -o nupkg

# Install globally
dotnet tool install -g ArchiForge.Cli --add-source ./nupkg

# Run (no need for dotnet run)
archiforge new MyProject
archiforge run
archiforge status <runId>
```

To update: `dotnet tool update -g ArchiForge.Cli --add-source ./nupkg`

## Project Structure

| Project | Description |
|---------|-------------|
| ArchiForge.Api | ASP.NET Core Web API, controllers, health checks |
| ArchiForge.Application | Analysis report building, DOCX/Markdown export, diagram image rendering (Mermaid → PNG) |
| ArchiForge.Contracts | DTOs, request/response types, manifest models |
| ArchiForge.Coordinator | Run creation, task generation |
| ArchiForge.DecisionEngine | Merges agent results into manifests |
| ArchiForge.Data | Repositories, SQL persistence |
| ArchiForge.Cli | ArchiForge CLI: `new`, `run`, `status`, `commit`, `seed`, `artifacts`, `dev up` |

## Architecture docs (internal)

For a deeper understanding of how ArchiForge fits together:

- `docs/ARCHITECTURE_INDEX.md` – starting point; links to all other docs.
- `docs/ARCHITECTURE_CONTEXT.md` – high-level system context and qualities.
- `docs/ARCHITECTURE_CONTAINERS.md` – projects/containers and their responsibilities.
- `docs/ARCHITECTURE_COMPONENTS.md` – key components to touch when changing behavior.
- `docs/ARCHITECTURE_FLOWS.md` – run, export, and comparison/replay flows.
- `docs/DATA_MODEL.md` – core tables/records and relationships.
- `docs/COMPARISON_REPLAY.md` – comparison replay API, modes, and recipes.
- `docs/HOWTO_ADD_COMPARISON_TYPE.md` – step-by-step for introducing new comparison types.
- `docs/RUNBOOK_REPLAY_DRIFT.md` – debugging replay/drift verification issues.
