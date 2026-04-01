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

The API must be running for `run`, `status`, `submit`, `commit`, `seed`, `artifacts`, `health`, `doctor` / `check`, and **`support-bundle`**. Use `health` for a quick ping (`GET /health`); use **`doctor`** (alias **`check`**) for liveness + readiness JSON and local project checks (`GET /health/live`, `GET /health/ready`).

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
| `doctor` / `check` | Readiness diagnostics: CLI build info, local `archiforge.json` (brief, writable outputs dir), API `GET /version` (build identity), then API `/health/live`, `/health/ready`, and `/health`. Exit 1 if readiness is not 2xx. |
| `support-bundle` | Writes a **pilot/support** folder of JSON files (and optional `--zip`): build/version, health probes, non-secret `archiforge.json` summary, safe `ARCHIFORGE_*` / `DOTNET_*` env snapshot, outputs folder stats, doc references. No connection strings or API key **values**. Default folder name `support-bundle-<utc-timestamp>Z` in the current directory. Flags: `--output <dir>`, `--zip`. |
| `comparisons list` | List/search persisted comparison records (supports paging and filters). |
| `comparisons replay <comparisonRecordId>` | Replay a saved comparison record and export it again to a file (Markdown/HTML/DOCX/PDF depending on type). |
| `comparisons replay-batch <id1,id2,...>` | Replay multiple comparison records and download a ZIP of the exported artifacts. |
| `comparisons summary <comparisonRecordId>` | Get the stored summary (or regenerated markdown) for a comparison record. |
| `comparisons drift <comparisonRecordId>` | Run drift analysis for a saved comparison record. |
| `comparisons diagnostics` | Show recent replay activity (requires replay diagnostics permission). |
| `comparisons tag <comparisonRecordId>` | Update label and tags on a comparison record. |

---

## Comparisons

The CLI can search and replay persisted comparison records.

### List comparisons

```bash
archiforge comparisons list [filters]
```

Supported flags:

- `--type <end-to-end-replay|export-record-diff>`
- `--left-run <runId>`
- `--right-run <runId>`
- `--left-export <exportRecordId>`
- `--right-export <exportRecordId>`
- `--label <label>`
- `--tag <tag>` (single tag)
- `--tags <t1,t2,...>` (multi-tag match)
- `--cursor <cursor>` to use cursor-based paging (API `cursor` query param)
- `--sort-by <createdUtc|type|label|leftRunId|rightRunId>` (defaults to `createdUtc`)
- `--sort <asc|desc>` (defaults to `desc`, maps to API `sortDir`)
- `--skip <n>` and `--limit <n>` for paging
- `--json` to output machine-readable JSON
- `--table` to output an aligned table

Examples:

```bash
# Page through end-to-end comparisons
archiforge comparisons list --type end-to-end-replay --limit 20 --skip 0 --table
archiforge comparisons list --type end-to-end-replay --limit 20 --skip 20 --table

# Filter by tag and label
archiforge comparisons list --tags incident,urgent --label incident-42 --json
```

### Replay a comparison (export to file)

```bash
archiforge comparisons replay <comparisonRecordId> [options]
```

Options:

- `--format <markdown|html|docx|pdf>` (default `markdown`)
- `--mode <artifact|regenerate|verify>` (default `artifact`)
- `--profile <default|short|detailed|executive>` (end-to-end only)
- `--persist` to persist the replay as a new comparison record (prints `PersistedReplayRecordId` when returned)
- `--out <path>` to control output location:
  - directory → saves as server-provided filename in that directory
  - file path → saves exactly to that path
- `--force` to overwrite an existing output file

Examples:

```bash
# Replay as DOCX into a directory (creates the directory if missing)
archiforge comparisons replay <id> --format docx --out outputs --force

# Verify replay and persist the replayed record
archiforge comparisons replay <id> --mode verify --persist
```

### Batch replay (download ZIP)

```bash
archiforge comparisons replay-batch <id1,id2,...> [--format docx] [--out outputs] [--force]
```

### Summary

```bash
archiforge comparisons summary <comparisonRecordId> [--json]
```

### Drift analysis

```bash
archiforge comparisons drift <comparisonRecordId> [--json]
```

### Replay diagnostics

```bash
archiforge comparisons diagnostics [--limit 50] [--json|--table]
```

This endpoint requires the API permission claim `replay:diagnostics`.

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
