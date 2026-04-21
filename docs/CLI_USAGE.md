> **Scope:** ArchLucid CLI Reference - full detail, tables, and links in the sections below.

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

The API must be running for `run`, `status`, `trace`, `submit`, `commit`, `seed`, `artifacts`, `first-value-report`, `reference-evidence`, `health`, `doctor` / `check`, and **`support-bundle`**. Use `health` for a quick ping (`GET /health`); use **`doctor`** (alias **`check`**) for liveness + readiness JSON and local project checks (`GET /health/live`, `GET /health/ready`).

---

## Commands

| Command | Description |
|--------|-------------|
| `new <projectName>` | Create a new project: `archlucid.json`, `inputs/brief.md`, `outputs/`, `plugins/plugin-lock.json`, optional Terraform stubs, `docs/README.md`. |
| `dev up` | Start SQL Server, Azurite, and Redis via Docker Compose (requires `docker-compose.yml` in repo root). |
| `pilot up` | Start the **full-stack + demo** Docker Compose profile (`docker-compose.yml` + `docker-compose.demo.yml`): API on **5000**, operator UI on **3000**, SQL, Azurite, Redis; waits for **`/health/ready`**. Same effective stack as `scripts/demo-start.ps1` / `demo-start.sh` — simulator agents, demo seed on API startup. Requires Docker only. |
| `try [--no-open] [--api-base-url <url>] [--ui-base-url <url>] [--readiness-deadline <secs>] [--commit-deadline <secs>]` | One-shot first-value loop. Composes **`pilot up`** → **`POST /v1/demo/seed`** → submits a sample architecture request → polls **`GET /v1/architecture/run/{runId}`** until `ReadyForCommit` (falls back to seeding fake results once the deadline elapses) → **`commit`** → **`GET /v1/pilots/runs/{runId}/first-value-report`** (Markdown saved to cwd) → opens the saved Markdown and **`{uiBaseUrl}/runs/{runId}`** in the OS default handlers. **`--no-open`** disables the OS opens (use it inside containers / SSH / CI). See **[archlucid try](#archlucid-try)** below. |
| `trial smoke --org <name> --email <email> [--display-name <name>] [--baseline-hours <n>] [--baseline-source <text>] [--api-base-url <url>] [--skip-pilot-run-deltas]` | Pure-HTTP smoke loop for the **public trial signup funnel** against any local or staging API. Calls **`POST /v1/register`** → **`GET /v1/tenant/trial-status`** → **`GET /v1/pilots/runs/{trialWelcomeRunId}/pilot-run-deltas`** and prints **PASS / FAIL** per step with an audit-event hint on failure. **No Docker, no SQL on your laptop.** Honours the same global **`--json`** flag for machine-readable output. See **[archlucid trial smoke](#archlucid-trial-smoke)** and the funnel runbook **[`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`](runbooks/TRIAL_FUNNEL_END_TO_END.md)**. |
| `run` | Submit an architecture request. Reads `archlucid.json` and `inputs/brief.md` from current directory. |
| `run --quick` | Same as `run`, then seeds fake results and commits in one step (development only). |
| `status <runId>` | Show run status, tasks, and submitted results. |
| `trace <runId>` | Look up the persisted OpenTelemetry trace ID for the run and print the trace viewer URL (or open it in the default browser when **`ARCHLUCID_TRACE_OPEN_BROWSER`** is `1` / `true`). Set **`ARCHLUCID_TRACE_VIEWER_URL_TEMPLATE`** with a **`{traceId}`** placeholder (e.g. Grafana explore) to enable links; otherwise the CLI prints the raw trace ID and setup instructions. |
| `submit <runId> <result.json>` | Submit an agent result for a run (JSON must match `AgentResult` schema). |
| `seed <runId>` | Seed fake agent results for a run (development only). |
| `commit <runId>` | Merge results and produce a versioned manifest. |
| `artifacts <runId>` | Fetch and display the committed manifest. |
| `artifacts <runId> --save` | Same, and save manifest to `outputs/manifest-{version}.json` (requires project dir). |
| `first-value-report <runId> [--save]` | Downloads sponsor Markdown from **`GET /v1/pilots/runs/{runId}/first-value-report`** (`text/markdown`). Prints to stdout, or writes `first-value-{runId}.md` in the current directory with **`--save`**. Uses **`ARCHLUCID_API_URL`** / **`ARCHLUCID_API_KEY`** like other CLI commands. |
| `reference-evidence --run <runId> [--out <dir>] [--include-demo]` | Writes a **reference-evidence** folder: **`pilot-run-deltas.json`** (`GET /v1/pilots/runs/{runId}/pilot-run-deltas`), **`first-value-report.md`**, **`first-value-report.pdf`**, **`sponsor-one-pager.pdf`** when endpoints succeed. Refuses Contoso demo runs unless **`--include-demo`**. Default output: **`./reference-evidence/<runId>/`**. |
| `reference-evidence --tenant <tenantId> [--out <dir>] [--include-demo]` | **AdminAuthority** only: downloads **`GET /v1/admin/tenants/{tenantId}/reference-evidence`** as **`reference-evidence-{tenantId}.zip`** (default directory **`./reference-evidence/tenant-{tenantId}/`**). |
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

## archlucid try

`archlucid try` is the **adoption-friction reducer**: a single command that takes a brand-new evaluator from "I cloned the repo" to "I have a committed manifest and a sponsor-grade Markdown report on my disk" — without making them stitch together `pilot up`, `run`, `seed`, `commit`, and `first-value-report`.

### What it does, in order

1. **Pilot stack up.** Reuses `archlucid pilot up` (Docker Compose `docker-compose.yml` + `docker-compose.demo.yml`, full-stack profile) and waits for `GET http://127.0.0.1:5000/health/ready` for up to `--readiness-deadline` seconds (default **120**).
2. **Demo seed (best-effort).** `POST /v1/demo/seed` — gated to Development + `ExecuteAuthority`. Tolerates 400 / 403 / 404 because the demo overlay also runs the seed at API startup; this call exists for a re-runnable, idempotent guarantee.
3. **Sample architecture request.** Submits a deterministic Azure-retail brief to `POST /v1/architecture/request` (`SystemName=ArchLucidTryDemo`).
4. **Execute + poll.** `POST /v1/architecture/run/{runId}/execute` (best-effort kick), then polls `GET /v1/architecture/run/{runId}` every **2 s** until status is `ReadyForCommit` (or higher). If the simulator does not progress within `--commit-deadline` seconds (default **180**), falls back to `POST /v1/architecture/run/{runId}/seed` (Development-only fake results) so the loop can still complete.
5. **Commit.** `POST /v1/architecture/run/{runId}/commit` — produces a versioned golden manifest.
6. **First-value report.** `GET /v1/pilots/runs/{runId}/first-value-report` (Markdown), saved to **`first-value-{runId}.md`** in the current directory.
7. **Open artifacts.** Opens the saved Markdown in the OS default handler and the operator UI at **`{uiBaseUrl}/runs/{runId}`** (default `http://localhost:3000`). Suppress with **`--no-open`** in headless / containerized contexts.

### Flags

| Flag | Default | Purpose |
|------|---------|---------|
| `--api-base-url <url>` | `http://localhost:5000` | API base URL for steps 2 → 6. Demo overlay binds the API on 5000, **not** the dotnet-run default 5128. |
| `--ui-base-url <url>` | `http://localhost:3000` | Operator-UI base URL printed and opened in step 7. |
| `--no-open` | (open) | Skip the OS `open` calls in step 7 (recommended inside containers, SSH sessions, the bundled `.devcontainer/`, and CI). |
| `--readiness-deadline <secs>` | `120` | Pilot-stack readiness probe deadline (passed to `pilot up`). |
| `--commit-deadline <secs>` | `180` | Maximum seconds to wait for the sample run to reach `ReadyForCommit` before falling back to `seed`. |

### Exit codes

- **0** Success — manifest committed, report saved.
- **1** Usage error — unknown flag, missing value, or `docker-compose.yml` not found from the current directory upward.
- **3** API unavailable — `/health/ready` did not respond within `--readiness-deadline`.
- **4** Operation failed — sample-run create failed, commit failed, or the seed-fake-results fallback failed.

### Devcontainer

The repo ships a `.devcontainer/` (compose-based, .NET 10 SDK + Node 22, host docker socket bind-mounted as Docker-outside-of-Docker) that runs **`dotnet run --project ArchLucid.Cli -- try --no-open`** on `postCreateCommand`. Open the repo in VS Code Dev Containers (or GitHub Codespaces) and the first boot brings up the demo stack and lands you on a committed run.

---

## archlucid trial smoke

`archlucid trial smoke` is the **funnel-validation** complement to `archlucid try`. Where `try` proves the operator-shell first-value loop works on a laptop, `trial smoke` proves the **public trial signup funnel** is healthy against a target API base URL — including a remote staging environment in **Stripe TEST mode** — without standing up Docker or SQL on the developer machine.

### What it does, in order

1. **`POST /v1/register`** — creates a fresh tenant from the supplied `--org` / `--email` (anonymous endpoint, rate-limited by the `registration` policy on the API). Forwards `--baseline-hours` / `--baseline-source` when supplied so the same baseline-capture path the marketing form uses gets exercised. Expects **201 Created** with a `tenantId` body.
2. **`GET /v1/tenant/trial-status`** — using the registration scope headers (`X-Tenant-Id`, `X-Workspace-Id`, `X-Project-Id`) returned by step 1. Expects **200 OK**, with `trialWelcomeRunId` populated by the bootstrap path.
3. **`GET /v1/pilots/runs/{trialWelcomeRunId}/pilot-run-deltas`** — confirms the seeded sample run is queryable for time-to-committed-manifest and findings counts. Skipped automatically when the trial-status response has no welcome run, or explicitly with **`--skip-pilot-run-deltas`**.

Each step prints **`PASS` / `FAIL`** with the underlying HTTP detail. Failures include a forensic hint pointing at the audit-event type to grep for in `dbo.AuditEvents` (for example `TrialSignupAttempted` / `TrialSignupFailed` for step 1).

### Flags

| Flag | Default | Purpose |
|------|---------|---------|
| `--org <name>` | — (required) | Organization name for the smoke tenant. Use a timestamped value so reruns do not collide on the org slug. |
| `--email <email>` | — (required) | Administrator email for the smoke tenant. Use an `*.invalid` domain for staging to avoid sending real verification mail. |
| `--display-name <name>` | `Trial Smoke User` | Display name on the admin role assignment. |
| `--baseline-hours <n>` | (none) | When supplied, exercises the optional baseline review-cycle capture path on `POST /v1/register`. |
| `--baseline-source <text>` | (none) | Free-text provenance note for `--baseline-hours`. Requires `--baseline-hours`. |
| `--api-base-url <url>` | resolved from `archlucid.json` / `ARCHLUCID_API_URL` | Override the API base URL for this single invocation. |
| `--skip-pilot-run-deltas` | (off) | Stop after step 2. Useful when the staging tenant has not committed a run yet. |

### Exit codes

- **0** Success — every step returned the expected status.
- **1** Usage error — missing `--org` / `--email`, invalid `--baseline-hours`, or unknown flag.
- **4** Operation failed — at least one step did not return the expected status (see PASS/FAIL output for the failing step).

### Local quick-start (Stripe TEST mode against staging)

```bash
export ARCHLUCID_API_URL=https://staging.archlucid.com
dotnet run --project ArchLucid.Cli -- trial smoke \
  --org "TrialSmoke-$(date +%s)" \
  --email "trial-smoke@example.invalid" \
  --baseline-hours 16 \
  --baseline-source "team estimate"
```

PowerShell (Windows):

```powershell
$env:ARCHLUCID_API_URL = "https://staging.archlucid.com"
dotnet run --project ArchLucid.Cli -- trial smoke `
  --org "TrialSmoke-$([int][double]::Parse((Get-Date -UFormat %s)))" `
  --email "trial-smoke@example.invalid" `
  --baseline-hours 16
```

For machine-readable output (CI smoke gates) place the global `--json` flag **before** the subcommand:

```bash
dotnet run --project ArchLucid.Cli -- --json trial smoke --org Acme --email ops@example.invalid
```

The companion **end-to-end runbook** for the funnel — the full happy path, audit chain, owner-only blockers, and Playwright mock spec — lives at [`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`](runbooks/TRIAL_FUNNEL_END_TO_END.md).

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
