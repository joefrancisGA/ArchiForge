# Pilot guide (Change Set 56R)

**Audience:** Design partners and early pilots who need to run ArchiForge locally or in a test environment **without** walking through internal design docs.

**Support:** When something fails, capture **approximate time**, **what you ran**, **`X-Correlation-ID`** from the API response (if any), and the **first error line** from logs. See [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

---

## What ArchiForge does (short)

ArchiForge is an **HTTP API** that turns a structured **architecture request** (system name, description, constraints, and similar fields) into:

1. A **run** with tasks for specialized agents (topology, cost, compliance, critique).
2. **Agent results** (after **execute**).
3. A versioned **golden manifest** and related **artifacts** (after **commit**) — diagrams, narratives, matrices, and other generated files you can open in the **operator UI** or download.

Default local setups often use a **simulator** for agents so you do not need cloud AI keys to complete a run.

---

## Minimum setup

| Need | Notes |
|------|--------|
| **.NET 10 SDK** | [Download](https://dotnet.microsoft.com/download). |
| **SQL Server** | LocalDB, Express, Docker (`archiforge dev up`), or an existing instance. |
| **Connection string** | Set `ConnectionStrings:ArchiForge` (User Secrets in Development, or environment variables in production). See [README.md](../README.md#secrets-development). |
| **Storage mode** | For a normal pilot, use **`ArchiForge:StorageProvider`** = **`Sql`** (typical default in appsettings). |
| **Node.js 22+** | Optional; only for the **operator UI** in `archiforge-ui/`. |

Clone or unpack the repo, then from `ArchiForge.Api`:

```bash
cd ArchiForge.Api
dotnet user-secrets set "ConnectionStrings:ArchiForge" "Server=localhost,1433;Database=ArchiForge;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

*(Adjust the connection string for your SQL instance.)*

Start the API from the **repository root**:

```bash
dotnet run --project ArchiForge.Api
```

Default base URL is often **`http://localhost:5128`** (see `ArchiForge.Api/Properties/launchSettings.json`).

**Sanity check:**

```bash
curl -s http://localhost:5128/health/live
```

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
dotnet run --project ArchiForge.Cli -- new my-pilot-project
cd my-pilot-project
dotnet run --project ../ArchiForge.Cli -- run --quick
```

This creates a project folder, submits from `inputs/brief.md`, seeds simulated results, and commits. Use **`status`** / **`artifacts`** as in [CLI_USAGE.md](CLI_USAGE.md).

---

## How to review artifacts

| Where | What to do |
|-------|------------|
| **Operator UI** | Start API, then in `archiforge-ui/`: `npm ci`, copy `.env.example` → `.env.local`, set **`ARCHIFORGE_API_BASE_URL`**, run **`npm run dev`**. Open **Runs** → your run → **Artifacts** → **Review** / **Download**. Details: [operator-shell.md](operator-shell.md), [archiforge-ui/README.md](../archiforge-ui/README.md). |
| **API** | List/download via artifact endpoints (see Swagger under artifacts/manifests). Empty list `[]` means “no files for this manifest,” not always an error. |
| **CLI** | `archiforge artifacts <runId>` (and `--save` to write manifest JSON under `outputs/`). |

Authoritative artifact content lives in the **database** (and streams through the API); local `outputs/` from the CLI is a **cache**, not the source of truth.

---

## Readiness checks (before a demo or handoff)

From the **repository root** (Windows):

```bat
run-readiness-check.cmd
```

Or PowerShell:

```powershell
.\run-readiness-check.ps1
```

This builds **Release**, runs **fast core** .NET tests, and runs **Vitest** if Node is installed. Details: [RELEASE_LOCAL.md](RELEASE_LOCAL.md).

**Deeper single-path smoke** (starts a temporary API, runs **`archiforge run --quick`**, checks synthesized artifacts): set **`ARCHIFORGE_SMOKE_SQL`** and run **`release-smoke.cmd`** — [RELEASE_SMOKE.md](RELEASE_SMOKE.md).

---

## Core tests (deeper regression)

**Fast core** (quick, no full HTTP integration suite):

```bash
dotnet test ArchiForge.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

**Full Core** trait:

```bash
dotnet test ArchiForge.sln --filter "Suite=Core"
```

Scripts: `test-fast-core.cmd`, `test-core.cmd` (and `.ps1`). Full tier list: [TEST_STRUCTURE.md](TEST_STRUCTURE.md).

---

## Where logs and “artifacts” live

| Item | Where |
|------|--------|
| **API logs** | **Console / host stdout** (Serilog). Search for **`RunId=`**, **`RequestId=`**, **`GraphResolutionMode=`** (authority path), and errors after failed requests. |
| **Published API** | If you used **`package-release`**, the DLLs are under **`artifacts/release/api/`** (gitignored). |
| **Synthesized architecture artifacts** | Stored **in the database**; exposed through the API and UI (not a shared folder on disk by default). |
| **CLI `outputs/`** | Optional local copies when you use **`archiforge artifacts --save`**. |
| **UI proxy diagnostics** | Next.js server logs may include JSON lines from **`archiforge-ui-proxy`** when the upstream API returns errors (see [TROUBLESHOOTING.md](TROUBLESHOOTING.md)). |

---

## Next steps

- **Operator cheat sheet (commands only):** [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md)  
- **Problems and fixes:** [TROUBLESHOOTING.md](TROUBLESHOOTING.md)  
- **Packaging an RC build:** [RELEASE_LOCAL.md](RELEASE_LOCAL.md)  
- **Demo seed (optional):** [demo-quickstart.md](demo-quickstart.md)
