# ArchiForge CLI Reference

Reference for the ArchiForge CLI: commands, configuration, and API URL behavior.

## Running the CLI

From the solution root:

```bash
dotnet run --project ArchiForge.Cli -- <command> [options]
```

Or install as a global .NET tool (after `dotnet pack`):

```bash
archiforge <command> [options]
```

---

## API URL

The CLI talks to the ArchiForge API over HTTP. Resolution order:

1. **`apiUrl`** in `archiforge.json` (if set)
2. **`ARCHIFORGE_API_URL`** environment variable
3. **Default:** `http://localhost:5128` (matches `ArchiForge.Api` launchSettings)

A trailing slash is trimmed (e.g. `http://localhost:5128/` → `http://localhost:5128`).

The API must be running for `run`, `status`, `submit`, `commit`, `seed`, `artifacts`, and `health`. Use `health` to verify connectivity.

---

## Commands

| Command | Description |
|--------|-------------|
| `new <projectName>` | Create a new project: `archiforge.json`, `inputs/brief.md`, `outputs/`, `plugins/plugin-lock.json`, optional Terraform stubs, `docs/README.md`. |
| `dev up` | Start SQL Server, Azurite, and Redis via Docker Compose (requires `docker-compose.yml` in repo root). |
| `run` | Submit an architecture request. Reads `archiforge.json` and `inputs/brief.md` from current directory. |
| `run --quick` | Same as `run`, then seeds fake results and commits in one step (development only). |
| `status <runId>` | Show run status, tasks, and submitted results. |
| `submit <runId> <result.json>` | Submit an agent result for a run (JSON must match `AgentResult` schema). |
| `seed <runId>` | Seed fake agent results for a run (development only). |
| `commit <runId>` | Merge results and produce a versioned manifest. |
| `artifacts <runId>` | Fetch and display the committed manifest. |
| `artifacts <runId> --save` | Same, and save manifest to `outputs/manifest-{version}.json` (requires project dir). |
| `health` | Check API connectivity (`GET /health`). Exit 0 if OK, 1 if unreachable. |

---

## archiforge.json

Single source of truth for project configuration. Required for `run`, `status`, `submit`, `commit`, `seed`, `artifacts`.

| Field | Description |
|-------|-------------|
| `schemaVersion` | Config schema version (e.g. `"1.0"`). Required. |
| `projectName` | Project name. Required. |
| `apiUrl` | Optional. Overrides default API base URL. |
| `inputs.brief` | Path to brief file (e.g. `"inputs/brief.md"`). Required; file must exist. |
| `outputs.localCacheDir` | Local cache directory for artifacts (e.g. `"outputs"`). Required. |
| `plugins.lockFile` | Path to plugin lock file (e.g. `"plugins/plugin-lock.json"`). Required; file must exist. |
| `infra.terraform` | Object with `enabled` and `path`. Required. If `enabled` is true, `path` must point to an existing directory. |
| `architecture` | Optional. `environment`, `cloudProvider`, `constraints`, `requiredCapabilities`, `assumptions`, `priorManifestVersion`. |

Example (minimal valid):

```json
{
  "schemaVersion": "1.0",
  "projectName": "MyApp",
  "inputs": { "brief": "inputs/brief.md" },
  "outputs": { "localCacheDir": "outputs" },
  "plugins": { "lockFile": "plugins/plugin-lock.json" },
  "infra": { "terraform": { "enabled": false, "path": "infra/terraform" } }
}
```

---

## Environment

| Variable | Description |
|----------|-------------|
| `ARCHIFORGE_API_URL` | API base URL when not set in `archiforge.json`. Default: `http://localhost:5128`. |

---

## Exit codes

- **0** — Success.
- **1** — Usage error, unknown command, API unreachable, or request failure.
