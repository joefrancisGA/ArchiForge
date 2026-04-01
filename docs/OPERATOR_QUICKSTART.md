# Operator quickstart — commands only (56R)

Copy-paste from the **repository root** unless noted. **Windows:** use `.cmd`; **PowerShell:** use `.ps1` where listed.

---

## Environment

```bash
cd ArchiForge.Api
dotnet user-secrets set "ConnectionStrings:ArchiForge" "Server=localhost,1433;Database=ArchiForge;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

```bash
cd ..
dotnet run --project ArchiForge.Api
```

```bash
curl -s http://localhost:5128/health/live
```

```bash
curl -s http://localhost:5128/health/ready
```

**Build provenance (support handoff):**

- **`GET /version`** — returns JSON with `informationalVersion`, `assemblyVersion`, `commitSha`, `runtimeFramework`, and `environment`. No authentication required.
- **Startup log** — look for **`Pilot/support configuration snapshot`** (includes `BuildInformationalVersion`, `BuildAssemblyVersion`, `BuildFileVersion`, `RuntimeFramework`).
- **`/health/ready`** and **`/health`** — detailed JSON now includes `version` and `commitSha` alongside per-check status and durations.
- Set CI or local publish with **`/p:SourceRevisionId=$(git rev-parse HEAD)`** to embed the commit SHA automatically.

```bash
curl -s http://localhost:5128/version | python -m json.tool
```

**CLI doctor (API must be running):**

```bash
dotnet run --project ArchiForge.Cli -- doctor
```

`doctor` now prints CLI build info and calls `GET /version` to display the API's build identity before running health probes.

**Support bundle (attach to support tickets):**

```bash
dotnet run --project ArchiForge.Cli -- support-bundle --zip
```

Creates a timestamped folder (and zip) of JSON diagnostics without secrets — see [TROUBLESHOOTING.md](TROUBLESHOOTING.md#support-bundle-attach-to-tickets).

---

## Pilot run (CLI, fastest)

```bash
dotnet run --project ArchiForge.Cli -- new my-pilot-project
cd my-pilot-project
dotnet run --project ../ArchiForge.Cli -- run --quick
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

---

## Operator UI

```bash
cd archiforge-ui
cp .env.example .env.local
```

Edit **`.env.local`**: `ARCHIFORGE_API_BASE_URL=http://localhost:5128` (match your API).

```bash
npm ci
npm run dev
```

Open **http://localhost:3000** → **Runs** → open run → **Artifacts**.

---

## Readiness + tests

```bat
run-readiness-check.cmd
```

```powershell
.\run-readiness-check.ps1
```

**Full E2E release smoke** (needs `ARCHIFORGE_SMOKE_SQL` — see [RELEASE_SMOKE.md](RELEASE_SMOKE.md)):

```powershell
$env:ARCHIFORGE_SMOKE_SQL = 'Server=...;Database=...;...'
.\release-smoke.ps1
```

```bat
release-smoke.cmd
```

```bash
dotnet test ArchiForge.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

```bash
dotnet test ArchiForge.sln --filter "Suite=Core"
```

```bash
cd archiforge-ui && npm ci && npm test
```

```bat
test-ui-unit.cmd
```

**Package API to `artifacts/release/api/`:**

```bat
package-release.cmd
```

---

## Docs

| Doc | Use |
|-----|-----|
| [PILOT_GUIDE.md](PILOT_GUIDE.md) | Context, first run, where logs live |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | Common failures |
| [operator-shell.md](operator-shell.md) | UI workflow detail |
| [RELEASE_LOCAL.md](RELEASE_LOCAL.md) | RC build scripts |
