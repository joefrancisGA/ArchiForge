> **Scope:** Operator quickstart — ArchLucid (commands only) (56R) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Operator quickstart — ArchLucid (commands only) (56R)

**Canonical action map (UI + API + CLI):** [OPERATOR_ATLAS.md](OPERATOR_ATLAS.md).

Copy-paste from the **repository root** unless noted. **Windows:** use `.cmd`; **PowerShell:** use `.ps1` where listed.

## What ArchLucid does (one paragraph)

ArchLucid is an **HTTP API** (and optional **operator UI**) that turns a structured **architecture request** into a **run**, **agent results** (after **execute**), and a versioned **golden manifest** plus **artifacts** (after **commit**). Local pilots often use **`AgentExecution:Mode=Simulator`** so you do not need cloud AI keys to complete a flow. **V1 scope and gates:** [V1_SCOPE.md](V1_SCOPE.md).

**After the demo (`archlucid try`) → your own inputs:** [SECOND_RUN.md](SECOND_RUN.md) — `archlucid second-run SECOND_RUN.toml` (or paste the same file on **New run → Starting point** in the operator UI).

---

## Environment

> **Install order moved.** See [INSTALL_ORDER.md](../INSTALL_ORDER.md). This page now only covers operator commands **after** the local or hosted environment is running.

## Local API (example)

```bash
cd ArchLucid.Api
dotnet user-secrets set "ConnectionStrings:ArchLucid" "Server=localhost,1433;Database=ArchLucid;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

```bash
cd ..
dotnet run --project ArchLucid.Api
```

```bash
curl -s http://localhost:5128/health/live
```

```bash
curl -s http://localhost:5128/health/ready
```

**Build provenance (support handoff):**

- **`GET /version`** — returns JSON with `informationalVersion`, `assemblyVersion`, `commitSha`, `runtimeFramework`, and `environment`. No authentication required.
- **Startup log** — look for **`Pilot/support configuration snapshot`** (includes `BuildInformationalVersion`, `BuildAssemblyVersion`, `BuildFileVersion`, `BuildCommitSha`, `RuntimeFramework`).
- **`/health/ready`** and **`/health`** — detailed JSON includes `version` (same value as **`GET /version`** field `informationalVersion`) and `commitSha`, plus per-check status and durations. Authenticated **`/health`** also includes **`circuit_breakers`** with **`data.gates[]`** (**`name`**, **`state`**) for Azure OpenAI completion/embedding breakers ([OBSERVABILITY.md](OBSERVABILITY.md) § Health JSON).
- Set CI or local publish with **`/p:SourceRevisionId=$(git rev-parse HEAD)`** to embed the commit SHA automatically.

```bash
curl -s http://localhost:5128/version
```

(Optional: pipe through `python -m json.tool` or `jq` if available.)

**CLI doctor (API must be running):**

```bash
dotnet run --project ArchLucid.Cli -- doctor
```

`doctor` now prints CLI build info and calls `GET /version` to display the API's build identity before running health probes.

**Support bundle (attach to support tickets):**

```bash
dotnet run --project ArchLucid.Cli -- support-bundle --zip
```

Creates a timestamped folder (and zip): open **`README.txt`** first for triage order; includes bounded OpenAPI probe — see [TROUBLESHOOTING.md](../TROUBLESHOOTING.md#support-bundle-attach-to-tickets).

---

## Pilot run (CLI, fastest)

```bash
dotnet run --project ArchLucid.Cli -- new my-pilot-project
cd my-pilot-project
dotnet run --project ../ArchLucid.Cli -- run --quick
```

---

## Pilot run (curl — replace `RUN_ID` after step 1)

```bash
export BASE=http://localhost:5128
```

```bash
curl -s -X POST "$BASE/v1/architecture/request" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: pilot-demo-1" \
  -d '{"requestId":"pilot-001","systemName":"PilotService","description":"Design a small internal API with basic security and observability.","environment":"dev","cloudProvider":"Azure","constraints":["Use managed identity where possible"],"requiredCapabilities":["HTTPS"]}'
```

```bash
curl -s -X POST "$BASE/v1/architecture/run/RUN_ID/execute" -H "X-Correlation-ID: pilot-demo-2"
```

```bash
curl -s -X POST "$BASE/v1/architecture/run/RUN_ID/commit" -H "X-Correlation-ID: pilot-demo-3"
```

**Traceability bundle (audit hand-off ZIP):** `GET /v1/architecture/run/{runId}/traceability-bundle.zip` returns `application/zip` with run summary, a scoped audit slice, and decision traces. The API applies a **size cap** and may respond with **413** when the bundle would exceed it. Use the same **ReadAuthority** auth and scope headers as other `/v1/architecture/...` reads.

```bash
curl -sS -o traceability-RUN_ID.zip "$BASE/v1/architecture/run/RUN_ID/traceability-bundle.zip"
```

---

## Operator UI

```bash
cd archlucid-ui
cp .env.example .env.local
```

Edit **`.env.local`**: `ARCHLUCID_API_BASE_URL=http://localhost:5128` (match your API).

**Optional — trace viewer deep link:** set **`NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE`** to a URL containing the literal **`{traceId}`** placeholder (replaced with the **`X-Trace-Id`** value from the API response). If unset, the UI still shows a short trace id preview and copy control when the header is present; the **View trace** link is hidden.

Examples:

- **Jaeger:** `https://jaeger.example.com/trace/{traceId}`
- **Grafana Tempo (Explore):** `https://grafana.example.com/explore?left=["now-1h","now","Tempo",{"query":"{traceId}"}]` (URL-encode the template value in real deployments as needed)
- **Application Insights:** `https://portal.azure.com/#blade/AppInsightsExtension/DetailsV2Blade/DataModel/trace/traceId/{traceId}`

```bash
npm ci
npm run dev
```

Open **http://localhost:3000** → **Runs** → open run → **Artifacts**.

**Pilot feedback (58R):** Nav **Pilot feedback** → scoped dashboard, opportunities, triage queue, and export links. Details: [PRODUCT_LEARNING.md](PRODUCT_LEARNING.md).

```bash
curl -s "http://localhost:5128/v1/product-learning/summary"
```

*(Use the same scope headers your UI uses if you override defaults.)*

---

## Readiness + tests

```bat
run-readiness-check.cmd
```

```powershell
.\run-readiness-check.ps1
```

**Full E2E release smoke** (needs `ARCHLUCID_SMOKE_SQL` — see [RELEASE_SMOKE.md](RELEASE_SMOKE.md)):

```powershell
$env:ARCHLUCID_SMOKE_SQL = 'Server=...;Database=...;...'
.\release-smoke.ps1
```

```bat
release-smoke.cmd
```

```bash
dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

```bash
dotnet test ArchLucid.sln --filter "Suite=Core"
```

```bash
cd archlucid-ui && npm ci && npm test
```

```bat
test-ui-unit.cmd
```

**Package API to `artifacts/release/api/`:**

```bat
package-release.cmd
```

Also creates **`artifacts/release/PACKAGE-HANDOFF.txt`**, **`metadata.json`**, **`release-manifest.json`**, and **`checksums-sha256.txt`** (see [RELEASE_LOCAL.md](RELEASE_LOCAL.md)).

---

## Docs

| Doc | Use |
|-----|-----|
| [archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md](../archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md) | Archived pilot narrative (first run, logs, issue template) |
| [PRODUCT_LEARNING.md](PRODUCT_LEARNING.md) | Pilot feedback dashboard, triage export (58R) |
| [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) | Common failures |
| [operator-shell.md](operator-shell.md) | UI workflow detail |
| [RELEASE_LOCAL.md](RELEASE_LOCAL.md) | RC build scripts |
