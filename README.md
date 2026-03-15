# ArchiForge

ArchiForge is an API for orchestrating AI-driven architecture design. It coordinates topology, cost, and compliance agents to produce architecture manifests from high-level requests.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full) with a database for ArchiForge
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional; for `archiforge dev up`)

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
2. Configure the connection string. Migrations run automatically on startup via [DbUp](https://dbup.readthedocs.io/). Scripts in `ArchiForge.Data/Migrations/` are applied in order; add new `00x_Description.sql` files for schema changes.
3. Store the connection string in User Secrets (development):

   ```bash
   cd ArchiForge.Api
   dotnet user-secrets set "ConnectionStrings:ArchiForge" "Server=localhost;Database=ArchiForge2;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

   For production, use environment variables or your hosting provider's secret store.

## Running the API

```bash
dotnet run --project ArchiForge.Api
```

The API listens on the URLs configured for the project (default `http://localhost:5128`; see `ArchiForge.Api/Properties/launchSettings.json`).

In Development:

- **Swagger UI**: `/swagger`
- **Health check**: `GET /health`

## Running Tests

```bash
dotnet test
```

Integration tests use in-memory SQLite by default—no SQL Server required. The schema is created automatically.

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

## CLI (ArchiForge.Cli)

The ArchiForge CLI is wired to the ArchiForge API over HTTP: all of `run`, `status`, `commit`, `seed`, and `artifacts` call the API. It lets you create projects, run architecture requests, and inspect results. For a full command and config reference, see [docs/CLI_USAGE.md](docs/CLI_USAGE.md). Run commands with:

```bash
dotnet run --project ArchiForge.Cli -- <command> [options]
```

### Prerequisites

- .NET 9 SDK
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
| ArchiForge.Contracts | DTOs, request/response types, manifest models |
| ArchiForge.Coordinator | Run creation, task generation |
| ArchiForge.DecisionEngine | Merges agent results into manifests |
| ArchiForge.Data | Repositories, SQL persistence |
| ArchiForge.Cli | ArchiForge CLI: `new`, `run`, `status`, `commit`, `seed`, `artifacts`, `dev up` |
