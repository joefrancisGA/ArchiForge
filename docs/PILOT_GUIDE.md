# Pilot guide (Change Set 56R)

**Audience:** Design partners and early pilots who need to run **ArchLucid** locally or in a test environment **without** walking through internal design docs.

**Scope:** For a decisive **V1 boundary** (in scope, out of scope, happy path, release gates), read **[V1_SCOPE.md](V1_SCOPE.md)** first; this guide is the practical onboarding narrative.

**What you ship in V1:** An HTTP API plus optional **operator UI** for reviewing runs, manifests, and artifacts; SQL-backed storage and health/version endpoints for operations. Narrative scope and gates: **[V1_SCOPE.md](V1_SCOPE.md)** / **[V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md)**. **Release notes:** summarized in **[CHANGELOG.md](CHANGELOG.md)**; breaking operational changes in **[BREAKING_CHANGES.md](../BREAKING_CHANGES.md)**.

**CLI naming:** Docs sometimes show the global tool form `archlucid …`. From a **clone without** `dotnet tool install`, use **`dotnet run --project ArchLucid.Cli -- <command>`** from the repo root (same as [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) and the **`release-smoke.ps1`** script).

**Support:** See **[When you report an issue](#when-you-report-an-issue)** below and [TROUBLESHOOTING.md](TROUBLESHOOTING.md). Prefer a **support bundle** (sanitized JSON) plus **build/version** identity so we can reproduce quickly.

---

## What ArchLucid does (short)

ArchLucid is an **HTTP API** that turns a structured **architecture request** (system name, description, constraints, and similar fields) into:

1. A **run** with tasks for specialized agents (topology, cost, compliance, critique).
2. **Agent results** (after **execute**).
3. A versioned **golden manifest** and related **artifacts** (after **commit**) — diagrams, narratives, matrices, and other generated files you can open in the **operator UI** or download.

Default local setups often use a **simulator** for agents so you do not need cloud AI keys to complete a run.

---

## Minimum setup

| Need | Notes |
|------|--------|
| **.NET 10 SDK** | [Download](https://dotnet.microsoft.com/download). |
| **SQL Server** | LocalDB, Express, Docker (`dotnet run --project ArchLucid.Cli -- dev up`), or an existing instance. |
| **Connection string** | Set `ConnectionStrings:ArchLucid` (User Secrets in Development, or environment variables in production). See [README.md](../README.md#secrets-development). |
| **Storage mode** | For a normal pilot, use **`ArchLucid:StorageProvider`** = **`Sql`** (typical default in appsettings). |
| **Node.js 22+** | Optional; only for the **operator UI** in `archlucid-ui/`. |

Clone or unpack the repo, then from `ArchLucid.Api`:

```bash
cd ArchLucid.Api
dotnet user-secrets set "ConnectionStrings:ArchLucid" "Server=localhost,1433;Database=ArchLucid;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

*(Adjust the connection string for your SQL instance.)*

Start the API from the **repository root**:

```bash
dotnet run --project ArchLucid.Api
```

Default base URL is often **`http://localhost:5128`** (see `ArchLucid.Api/Properties/launchSettings.json`).

**Sanity check:**

```bash
curl -s http://localhost:5128/health/live
```

**Identify the running API build** (for tickets and handoff):

```bash
curl -s http://localhost:5128/version
```

Returns JSON: informational version, commit suffix (when the build was stamped), environment, runtime. Readiness responses (`/health/ready`, `/health`) also include **`version`** (same value as **`GET /version`** `informationalVersion`) and **`commitSha`** in the JSON body.

**CLI** (from repo root, API reachable):

```bash
dotnet run --project ArchLucid.Cli -- doctor
```

Prints local CLI build info, **`GET /version`** from the API, then **`/health/live`**, **`/health/ready`**, and **`/health`**.

---

## First successful run path

### Option A — Swagger (good for learning)

1. Open **`http://localhost:5128/swagger`** (Development).
2. Authorize if your environment requires it (default **DevelopmentBypass** usually does not need extra headers).
3. Call **`POST /v1/architecture/request`** with a JSON body (example below).
4. Copy **`runId`** from the response.
5. **`POST /v1/architecture/run/{runId}/execute`**
6. **`POST /v1/architecture/run/{runId}/commit`**

Example body for **request** (minimal):

```json
{
  "requestId": "pilot-001",
  "systemName": "PilotService",
  "description": "Design a small internal API with basic security and observability.",
  "environment": "dev",
  "cloudProvider": "Azure",
  "constraints": ["Use managed identity where possible"],
  "requiredCapabilities": ["HTTPS"]
}
```

**Scope headers (optional):** If you use non-default tenant/workspace/project, send the same GUIDs your UI uses, e.g. `x-tenant-id`, `x-workspace-id`, `x-project-id`. If you omit them, the API uses built-in development defaults.

**Correlation:** Add header **`X-Correlation-ID: my-trace-001`** on requests so support can match your calls to server logs.

### Option B — CLI quick path (one command after scaffold)

From the **repo root**:

```bash
dotnet run --project ArchLucid.Cli -- new my-pilot-project
cd my-pilot-project
dotnet run --project ../ArchLucid.Cli -- run --quick
```

This creates a project folder, submits from `inputs/brief.md`, seeds simulated results, and commits. Use **`status`** / **`artifacts`** as in [CLI_USAGE.md](CLI_USAGE.md).

---

## How to review artifacts

| Where | What to do |
|-------|------------|
| **Operator UI** | Start API, then in `archlucid-ui/`: `npm ci`, copy `.env.example` → `.env.local`, set **`ARCHLUCID_API_BASE_URL`**, run **`npm run dev`**. Open **Runs** → your run → **Artifacts** → **Review** / **Download**. Details: [operator-shell.md](operator-shell.md), [archlucid-ui/README.md](../archlucid-ui/README.md). |
| **API** | List/download via artifact endpoints (see Swagger under artifacts/manifests). Empty list `[]` means “no files for this manifest,” not always an error. |
| **CLI** | `dotnet run --project ArchLucid.Cli -- artifacts <runId>` (add `--save` to write manifest JSON under `outputs/`). Same via global tool: `archlucid artifacts …`. |

Authoritative artifact content lives in the **database** (and streams through the API); local `outputs/` from the CLI is a **cache**, not the source of truth.

---

## Pilot feedback (product learning, 58R)

If your program records **human judgments** on outputs (trusted / rejected / revised, etc.), you can **review rollups** in the operator UI and export a short **triage summary** for product/architecture discussions. This is separate from **Recommendation learning** (advisory weights).

**Practical guide:** [PRODUCT_LEARNING.md](PRODUCT_LEARNING.md) (dashboard, opportunities, exports, how owners should use the data).

---

## Readiness checks (before a demo or handoff)

For a **full V1-style release gate** (scope freeze, recovery drill, export checks, naming, deferrals), use **[V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md)** in addition to the scripts below.

| Goal | Command (repo root) | Notes |
|------|---------------------|--------|
| **Quick gate** — Release build + fast core tests + Vitest (if Node installed) | `run-readiness-check.cmd` or `.\run-readiness-check.ps1` | On failure, the script prints a **triage** block (**Stage**, **Category**, **Next:** hints). [RELEASE_LOCAL.md](RELEASE_LOCAL.md) |
| **Skip UI tests** | `.\run-readiness-check.ps1 -SkipUi` | |
| **Deep smoke** — above + temporary API + CLI **`run --quick`** + artifact check | Set `ARCHLUCID_SMOKE_SQL`, then `release-smoke.cmd` or `.\release-smoke.ps1` | Needs SQL and port **5128** (or `-ApiBaseUrl`). [RELEASE_SMOKE.md](RELEASE_SMOKE.md) |
| **Smoke without E2E** (no SQL for the script) | `.\release-smoke.ps1 -SkipE2E` | Build + tests (+ UI if Node present) only |

---

## Support bundle (for support tickets)

From a machine where the **API is reachable** (set **`ARCHLUCID_API_URL`** if not `http://localhost:5128`):

```bash
dotnet run --project ArchLucid.Cli -- support-bundle --zip
```

Creates a UTC-stamped folder and a **zip**: start with **`README.txt`** inside the folder for triage order; **`manifest.json`** lists the same order as **`triageReadOrder`**. Includes **`api-contract.json`** (bounded **`GET /openapi/v1.json`**) plus build/version, health, config summary, filtered environment, workspace, references, logs. **No secrets** in normal use — still **review** before sending externally. Optional: `--output <dir>` for a fixed folder name.

Details: [TROUBLESHOOTING.md](TROUBLESHOOTING.md), [CLI_USAGE.md](CLI_USAGE.md).

---

## When you report an issue

Send as much of this as you can (plain text is fine):

1. **Build / version** — output of **`GET /version`** (or paste **`informationalVersion`** / **`commitSha`** from **`metadata.json`** if you used `package-release`).
2. **What you ran** — exact command or Swagger operation; **approximate time** (UTC if possible).
3. **Correlation** — **`X-Correlation-ID`** response header (or the value you sent on the request).
4. **First error** — first meaningful line from API **console** logs or CLI **stderr** (not a full stack dump unless asked).
5. **Health** — if the API is up: paste the **`status`** and any **unhealthy** `entries[]` from **`GET /health/ready`** JSON, or attach CLI **`doctor`** output.
6. **Support bundle** — zip from **`support-bundle --zip`** after redacting anything your policy forbids.

First steps before escalating: [TROUBLESHOOTING.md](TROUBLESHOOTING.md) (**Quick matrix** + **Still stuck?**).

---

## Core tests (deeper regression)

**Fast core** (quick, no full HTTP integration suite):

```bash
dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

**Full Core** trait:

```bash
dotnet test ArchLucid.sln --filter "Suite=Core"
```

Scripts: `test-fast-core.cmd`, `test-core.cmd` (and `.ps1`). Full tier list: [TEST_STRUCTURE.md](TEST_STRUCTURE.md).

---

## Where logs and “artifacts” live

| Item | Where |
|------|--------|
| **API logs** | **Console / host stdout** (Serilog). Search for **`RunId=`**, **`RequestId=`**, **`GraphResolutionMode=`** (authority path), and errors after failed requests. |
| **Published API** | If you used **`package-release`**, the DLLs are under **`artifacts/release/api/`** (gitignored). The parent folder also has **`PACKAGE-HANDOFF.txt`**, **`metadata.json`**, **`release-manifest.json`**, and **`checksums-sha256.txt`** for support and integrity checks — see [RELEASE_LOCAL.md](RELEASE_LOCAL.md). |
| **Synthesized architecture artifacts** | Stored **in the database**; exposed through the API and UI (not a shared folder on disk by default). |
| **CLI `outputs/`** | Optional local copies when you use **`dotnet run --project ArchLucid.Cli -- artifacts <runId> --save`** (or `archlucid artifacts --save` if the tool is installed). |
| **UI proxy diagnostics** | Next.js server logs may include JSON lines from **`archlucid-ui-proxy`** when the upstream API returns errors (see [TROUBLESHOOTING.md](TROUBLESHOOTING.md)). |

---

## Next steps

- **Operator cheat sheet (commands only):** [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md)  
- **Pilot feedback dashboard (58R):** [PRODUCT_LEARNING.md](PRODUCT_LEARNING.md)  
- **Problems and fixes:** [TROUBLESHOOTING.md](TROUBLESHOOTING.md)  
- **Packaging an RC build:** [RELEASE_LOCAL.md](RELEASE_LOCAL.md)  
- **Demo seed (optional):** [demo-quickstart.md](demo-quickstart.md)
