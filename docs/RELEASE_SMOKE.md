# Release smoke path (Change Set 56R)

One **deterministic** end-to-end check for **pilot / commercial confidence** (not full coverage). Implemented as **`release-smoke.ps1`** / **`release-smoke.cmd`** at the repo root.

**What it verifies**

1. **Release build** — whole solution (`build-release`).
2. **Core-tier tests** — **fast core** (`Suite=Core`, excluding Slow + Integration), in **Release**, matching the usual first gate.
3. **Optional: full Core** — `-FullCore` adds `Suite=Core` (may require SQL for integration tests).
4. **Operator UI** — when Node is on `PATH`: `npm ci`, **Vitest**, **`npm run build`** (production bundle). Skip with **`-SkipUi`**.
5. **API readiness** — starts **`ArchiForge.Api`** (Release, **http** profile, port **5128**), waits for **`GET /health/ready`** and **`GET /health/live`**.
6. **Sample run** — CLI **`new ArchiForgeSmokeRc`** in a temp folder, then **`run --quick`** (Development seed + commit).
7. **Artifacts** — **`GET /v1/architecture/run/{runId}`** must show **`goldenManifestId`**; **`GET /api/artifacts/manifests/{manifestId}`** must return **≥ 1** descriptor.

**Not included:** Playwright, SQL container contract tests, multi-tenant matrix, performance — by design.

---

## Prerequisites (full smoke)

- **.NET 10 SDK**
- **SQL Server** and a valid **`ConnectionStrings:ArchiForge`**-style string for the **E2E** block
- **Node.js 22+** (optional; UI steps skipped if `node` is missing unless **`-SkipUi`**)
- **Port 5128** free (or override **`-ApiBaseUrl`** and ensure the API profile matches — default script assumes **5128**)

---

## Environment variables

| Variable | Purpose |
|----------|---------|
| **`ARCHIFORGE_SMOKE_SQL`** | Preferred: ADO.NET connection string for the temporary API process |
| **`ConnectionStrings__ArchiForge`** | Alternative if already set in the shell |

You can also pass **`-SqlConnectionString '...'`** (quote for special characters).

---

## Commands (repo root)

**Full smoke (E2E + UI when Node present):**

```powershell
$env:ARCHIFORGE_SMOKE_SQL = 'Server=localhost,1433;Database=ArchiForge;User Id=sa;Password=...;TrustServerCertificate=True;'
.\release-smoke.ps1
```

```bat
set ARCHIFORGE_SMOKE_SQL=Server=localhost,1433;Database=ArchiForge;User Id=sa;Password=...;TrustServerCertificate=True;
release-smoke.cmd
```

**CI-style: include full Core suite (after fast core):**

```powershell
.\release-smoke.ps1 -FullCore
```

**Without E2E (no SQL / no API start):**

```powershell
.\release-smoke.ps1 -SkipE2E
```

**Without UI (faster agent/headless):**

```powershell
.\release-smoke.ps1 -SkipUi
```

**Combine:**

```powershell
.\release-smoke.ps1 -SkipE2E -SkipUi
```

---

## Parameters

| Switch / param | Effect |
|----------------|--------|
| **`-SqlConnectionString`** | SQL for E2E API process |
| **`-ApiBaseUrl`** | Default `http://localhost:5128` |
| **`-SkipE2E`** | Build + tests (+ UI) only; no API/CLI/artifact checks |
| **`-SkipUi`** | No `npm ci` / Vitest / `next build` |
| **`-FullCore`** | After fast core, run **`dotnet test` —filter `Suite=Core`** |

---

## Relation to other scripts

| Script | Role |
|--------|------|
| **`run-readiness-check`** | Release build + fast core + Vitest only (no E2E API, no artifact assertion) |
| **`package-release`** | Publish API to `artifacts/release/api/` |
| **`release-smoke`** | **This doc** — deepest single-path confidence |

---

## Troubleshooting

- **API exits before ready:** wrong SQL string, migrations failing, or port in use — watch for a separate console if you run API manually; the smoke script starts a **hidden** `dotnet run` (stdout not shown). Re-run with **`-SkipE2E`** and start the API yourself to read logs, or temporarily change the script to use a visible window for debugging.
- **`run --quick` seed fails:** API must be **`Development`** (the script sets **`ASPNETCORE_ENVIRONMENT=Development`** for the child process).
- **Zero artifacts:** synthesis or persistence regression — check API logs for the smoke **`RunId`**.

More: [TROUBLESHOOTING.md](TROUBLESHOOTING.md), [PILOT_GUIDE.md](PILOT_GUIDE.md).
