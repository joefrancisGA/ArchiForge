# ArchLucid CLI Reference

Reference for the ArchLucid CLI: commands, configuration, and API URL behavior.

## Running the CLI

From the solution root:

```bash
dotnet run --project ArchLucid.Cli -- <command> [options]
```

Or install as a global .NET tool (after `dotnet pack`):

```bash
archlucid <command> [options]
```

### Global `--json`

Place **one or more** `--json` flags **before** the subcommand for machine-readable errors on stderr and (for `health`) a JSON success line on stdout:

```bash
archlucid --json health
```

Subcommands that define their own `--json` (for example `archlucid comparisons list --json`) are unchanged — only **leading** `--json` tokens set global JSON mode.

---

## API URL

The CLI talks to the ArchLucid API over HTTP. Resolution order:

1. **`apiUrl`** in `archlucid.json` (if set)
2. **`ARCHLUCID_API_URL`** environment variable
3. **Default:** `http://localhost:5128` (matches `ArchLucid.Api` launchSettings)

A trailing slash is trimmed (e.g. `http://localhost:5128/` → `http://localhost:5128`).

The API must be running for `run`, `status`, `trace`, `submit`, `commit`, `seed`, `artifacts`, `health`, `doctor` / `check`, and **`support-bundle`**. Use `health` for a quick ping (`GET /health`); use **`doctor`** (alias **`check`**) for liveness + readiness JSON and local project checks (`GET /health/live`, `GET /health/ready`).

---

## Commands

| Command | Description |
|--------|-------------|
| `new <projectName>` | Create a new project: `archlucid.json`, `inputs/brief.md`, `outputs/`, `plugins/plugin-lock.json`, optional Terraform stubs, `docs/README.md`. |
| `dev up` | Start SQL Server, Azurite, and Redis via Docker Compose (requires `docker-compose.yml` in repo root). |
| `run` | Submit an architecture request. Reads `archlucid.json` and `inputs/brief.md` from current directory. |
| `run --quick` | Same as `run`, then seeds fake results and commits in one step (development only). |
| `status <runId>` | Show run status, tasks, and submitted results. |
| `trace <runId>` | Look up the persisted OpenTelemetry trace ID for the run and print the trace viewer URL (or open it in the default browser when **`ARCHLUCID_TRACE_OPEN_BROWSER`** is `1` / `true`). Set **`ARCHLUCID_TRACE_VIEWER_URL_TEMPLATE`** with a **`{traceId}`** placeholder (e.g. Grafana explore) to enable links; otherwise the CLI prints the raw trace ID and setup instructions. |
| `submit <runId> <result.json>` | Submit an agent result for a run (JSON must match `AgentResult` schema). |
| `seed <runId>` | Seed fake agent results for a run (development only). |
| `commit <runId>` | Merge results and produce a versioned manifest. |
| `artifacts <runId>` | Fetch and display the committed manifest. |
| `artifacts <runId> --save` | Same, and save manifest to `outputs/manifest-{version}.json` (requires project dir). |
| `health` | Check API connectivity (`GET /health`). Exit **0** if OK; **3** if unreachable; **2** if the API base URL is invalid. With global `--json`, prints one JSON object per line (stderr on failure, stdout on success). |
| `doctor` / `check` | Readiness diagnostics: CLI build info, local `archlucid.json` (brief, writable outputs dir), API `GET /version` (build identity), then API `/health/live`, `/health/ready`, and `/health`. Exit 1 if readiness or combined `/health` is not 2xx. |
| `support-bundle` | Writes a **pilot/support** folder (and optional `--zip`): **`README.txt`** (triage order), **`manifest.json`** (format **1.1**, `triageReadOrder`), **`build.json`**, **`health.json`**, **`api-contract.json`** (bounded **`GET /openapi/v1.json`**), **`config-summary.json`**, **`environment.json`**, **`workspace.json`**, **`references.json`**, **`logs.json`**. No connection strings or API key **values**. Default folder `support-bundle-<utc-timestamp>Z`. Flags: `--output <dir>`, `--zip`. |
| `comparisons list` | List/search persisted comparison records (supports paging and filters). |
| `comparisons replay <comparisonRecordId>` | Replay a saved comparison record and export it again to a file (Markdown/HTML/DOCX/PDF depending on type). |
| `comparisons replay-batch <id1,id2,...>` | Replay multiple comparison records and download a ZIP of the exported artifacts. |
| `comparisons summary <comparisonRecordId>` | Get the stored summary (or regenerated markdown) for a comparison record. |
| `comparisons drift <comparisonRecordId>` | Run drift analysis for a saved comparison record. |
| `comparisons diagnostics` | Show recent replay activity (requires replay diagnostics permission). |
| `comparisons tag <comparisonRecordId>` | Update label and tags on a comparison record. |
| `completions bash` \| `zsh` \| `powershell` | Print a shell completion script to stdout (source from your profile). |

---

## Shell completion

Install once per machine (examples):

```bash
# Bash — append to ~/.bashrc
dotnet run --project ArchLucid.Cli -- completions bash >> ~/.bash_completion_archlucid
echo 'source ~/.bash_completion_archlucid' >> ~/.bashrc
```

```bash
# zsh — save under a path on your fpath or source directly
dotnet run --project ArchLucid.Cli -- completions zsh > ~/.archlucid-completions.zsh
echo 'source ~/.archlucid-completions.zsh' >> ~/.zshrc
```

```powershell
# PowerShell — add to your profile
dotnet run --project ArchLucid.Cli -- completions powershell | Out-File -Encoding utf8 $PROFILE.CurrentUserAllHosts -Append
```

After `dotnet tool install -g ArchLucid.Cli`, use `archlucid completions …` instead of `dotnet run --project …`.

---

## Comparisons

The CLI can search and replay persisted comparison records.

### List comparisons

```bash
archlucid comparisons list [filters]
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
archlucid comparisons list --type end-to-end-replay --limit 20 --skip 0 --table
archlucid comparisons list --type end-to-end-replay --limit 20 --skip 20 --table

# Filter by tag and label
archlucid comparisons list --tags incident,urgent --label incident-42 --json
```

### Replay a comparison (export to file)

```bash
archlucid comparisons replay <comparisonRecordId> [options]
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
archlucid comparisons replay <id> --format docx --out outputs --force

# Verify replay and persist the replayed record
archlucid comparisons replay <id> --mode verify --persist
```

### Batch replay (download ZIP)

```bash
archlucid comparisons replay-batch <id1,id2,...> [--format docx] [--out outputs] [--force]
```

### Summary

```bash
archlucid comparisons summary <comparisonRecordId> [--json]
```

### Drift analysis

```bash
archlucid comparisons drift <comparisonRecordId> [--json]
```

### Replay diagnostics

```bash
archlucid comparisons diagnostics [--limit 50] [--json|--table]
```

This endpoint requires the API permission claim `replay:diagnostics`.

## archlucid.json

Single source of truth for project configuration. Required for `run`, `status`, `trace`, `submit`, `commit`, `seed`, `artifacts`.

| Field | Description |
|-------|-------------|
| `schemaVersion` | Config schema version (e.g. `"1.0"`). Required. |
| `projectName` | Project name. Required. |
| `apiUrl` | Optional. Overrides default API base URL. |
| `inputs.brief` | Path to brief file (e.g. `"inputs/brief.md"`). Required; file must exist. |
| `outputs.localCacheDir` | Local cache directory for artifacts (e.g. `"outputs"`). Required. |
| `plugins.lockFile` | Optional. When set, the file must exist. When `plugins` is omitted, plugin lock validation is skipped. |
| `infra.terraform` | Optional. When omitted, Terraform is treated as disabled. If `enabled` is true, `path` must point to an existing directory. |
| `architecture` | Optional. `environment`, `cloudProvider`, `constraints`, `requiredCapabilities`, `assumptions`, `priorManifestVersion`. |

Example (minimal valid — no plugins / infra):

```json
{
  "schemaVersion": "1.0",
  "projectName": "MyApp",
  "inputs": { "brief": "inputs/brief.md" },
  "outputs": { "localCacheDir": "outputs" }
}
```

Example (with plugin lock and Terraform path, common for `archlucid new`):

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
| `ARCHLUCID_API_URL` | API base URL when not set in `archlucid.json`. Default: `http://localhost:5128`. |

---

## Exit codes

| Code | Meaning |
|------|---------|
| **0** | Success. |
| **1** | Usage error: missing/invalid invocation or unknown top-level command. |
| **2** | Configuration error (invalid API base URL / resolution). |
| **3** | API unavailable: host unreachable or health probe failed. |
| **4** | Operation failed: HTTP/API error after connect, local validation, filesystem error, or readiness failure after connect (`doctor`). |

Automation can combine exit codes with leading **`--json`** for structured stderr lines: `{"ok":false,"exitCode":3,"error":"api_unreachable","message":"..."}`.
