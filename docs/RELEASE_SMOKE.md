# Release smoke path (Change Set 56R)

One **deterministic** end-to-end check for **pilot / commercial confidence** on **ArchLucid** (not full coverage). Implemented as **`release-smoke.ps1`** / **`release-smoke.cmd`** at the repo root.

**For pilots:** Use **`run-readiness-check`** first (faster — no temporary API). Use **`release-smoke`** when you have **SQL** and want one scripted path that also runs **CLI `run --quick`** and checks **artifacts**. If the script fails, copy the **`--- FAILURE (triage) ---`** block (**Stage**, **Category**, **Next:**) into your report — see [PILOT_GUIDE.md](PILOT_GUIDE.md#when-you-report-an-issue).

**What it verifies**

1. **Release build** — whole solution (`build-release`).
2. **Core-tier tests** — **fast core** (`Suite=Core`, excluding Slow + Integration), in **Release**, matching the usual first gate.
3. **Optional: full Core** — `-FullCore` adds `Suite=Core` (may require SQL for integration tests).
4. **Operator UI** — when Node is on `PATH`: `npm ci`, **Vitest**, **`npm run build`** (production bundle). Skip with **`-SkipUi`**.
5. **API readiness** — starts the **`ArchLucid.Api`** project (Release, **http** profile, port **5128**), waits for **`GET /health/ready`** and **`GET /health/live`**.
6. **Sample run** — CLI **`new ArchLucidSmokeRc`** in a temp folder, then **`run --quick`** (Development seed + commit).
7. **Artifacts** — **`GET /v1/architecture/run/{runId}`** must show **`goldenManifestId`**; **`GET /v1/artifacts/manifests/{manifestId}`** must return **≥ 1** descriptor.
8. **Optional: Playwright** — **`-RunPlaywright`** runs **`archlucid-ui`** **`npm run test:e2e`** (with **`CI=1`**) **after** the steps above. Not run by default.

**Not included (unless opted in):** Playwright (use **`-RunPlaywright`**), SQL container contract tests, multi-tenant matrix, performance — by design.

### What `-RunPlaywright` actually exercises (57R)

The Playwright suite is **operator-journey smoke** for the Next shell: **home**, **run → manifest → back**, **manifest with empty artifact list**, **compare** (prefill, structured/legacy outcomes, stale-input warning), and **compare + Explain (AI)** via mocked **`/api/proxy`** — all with **deterministic fixtures** and a **loopback TypeScript mock** (`archlucid-ui/e2e/`), **not** the live **`ArchLucid.Api`** started in steps 5–6. Passing it does **not** imply the UI was validated against the same SQL-backed API instance used for the CLI smoke.

It is **not** a full browser regression suite. Authoritative detail: **[archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md](../archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md)** — **section 8 (E2E tests / Playwright)**.

---

## Prerequisites (full smoke)

- **.NET 10 SDK**
- **SQL Server** and a valid **`ConnectionStrings:ArchLucid`**-style string for the **E2E** block
- **Node.js 22+** (optional; UI steps skipped if `node` is missing unless **`-SkipUi`**)
- **Port 5128** free (or override **`-ApiBaseUrl`** and ensure the API profile matches — default script assumes **5128**)

---

## Environment variables

| Variable | Purpose |
|----------|---------|
| **`ARCHLUCID_SMOKE_SQL`** | Preferred: ADO.NET connection string for the temporary API process |
| **`ConnectionStrings__ArchLucid`** | Alternative if already set in the shell |

You can also pass **`-SqlConnectionString '...'`** (quote for special characters).

---

## Commands (repo root)

**Full smoke (E2E + UI when Node present):**

```powershell
$env:ARCHLUCID_SMOKE_SQL = 'Server=localhost,1433;Database=ArchLucid;User Id=sa;Password=...;TrustServerCertificate=True;'
.\release-smoke.ps1
```

**Windows CMD:** connection strings contain `;` — avoid inline `set` (it breaks at the first semicolon). Prefer PowerShell above, or run **`release-smoke.cmd`** after setting the variable in PowerShell / System Properties. The **`.cmd`** wrapper invokes **`release-smoke.ps1`** with `%*`; you can pass **`-SqlConnectionString '...'`** from CMD if quoted carefully.

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

**Include Playwright E2E after UI (+ API smoke when not skipped):**

```powershell
.\release-smoke.ps1 -RunPlaywright
```

With **`-SkipE2E`**, Playwright still runs **after** the UI block (if any); use **`npm ci`** automatically when **`-SkipUi`** left dependencies uninstalled.

---

## Parameters

| Switch / param | Effect |
|----------------|--------|
| **`-SqlConnectionString`** | SQL for E2E API process |
| **`-ApiBaseUrl`** | Default `http://localhost:5128` |
| **`-SkipE2E`** | Build + tests (+ UI) only; no API/CLI/artifact checks |
| **`-SkipUi`** | No `npm ci` / Vitest / `next build` |
| **`-RunPlaywright`** | After other steps: **`archlucid-ui`** Playwright E2E (**`CI=1`**); see [What `-RunPlaywright` actually exercises](#what--runplaywright-actually-exercises-57r) |
| **`-FullCore`** | After fast core, run **`dotnet test` —filter `Suite=Core`** |

---

## Relation to other scripts

| Script | Role |
|--------|------|
| **`run-readiness-check`** | Release build + fast core + Vitest only (no E2E API, no artifact assertion) |
| **`package-release`** | Publish API to `artifacts/release/api/` |
| **`release-smoke`** | **This doc** — deepest single-path confidence |

---

## CD synthetic smoke (GitHub → Azure Container Apps)

GitHub Actions workflows **`.github/workflows/cd.yml`** and **`cd-staging-on-merge.yml`** run **post-deploy validation** when **`SMOKE_TEST_BASE_URL`** is set on the environment. The implementation is **`scripts/ci/cd-post-deploy-verify.sh`** (documented in **`docs/DEPLOYMENT_CD_PIPELINE.md`**):

1. **`GET …/health/live`** — HTTP **200**; response excerpt logged on failure.
2. **`GET …/health/ready`** — HTTP **200** and JSON **`.status` must be `"Healthy"`** (not merely “reachable”); per-check **`entries[].status`** lines are printed when the overall status is not Healthy.
3. **`GET …/openapi/v1.json`** — HTTP **200** (fails closed if the host does not expose OpenAPI in that environment).
4. **`GET …/version`** — HTTP **200**; full version JSON logged in compact form for traceability.
5. **`GET …{SMOKE_SYNTHETIC_PATH}`** when that path is not **`/version`** — HTTP **200**.

Optional retries: repository variables **`CD_POST_DEPLOY_MAX_ATTEMPTS`** and **`CD_POST_DEPLOY_RETRY_WAIT_SECONDS`**.

If validation fails and **`CD_ROLLBACK_ON_SMOKE_FAILURE`** is **true**, the workflow attempts to **deactivate the new API revision** and, when **`CONTAINER_APP_WORKER_NAME`** is configured, the **worker** revision updated in the same deploy. This is **not** a substitute for full **release-smoke** above — it is an **automated gate** after deploy with logs aimed at first-line diagnosis.

---

## Failure triage (script output)

Both **`release-smoke.ps1`** and **`run-readiness-check.ps1`** use shared helpers in **`scripts/OperatorDiagnostics.ps1`**.

- **Phases** are labeled **`[step/total]`** in the log (e.g. **`[5/6]`**) so you can see **which gate failed first**.
- On failure, a **`--- FAILURE (triage) ---`** block prints **`Stage`**, **`Category`** (e.g. `ReadinessTimeout`, `Misconfiguration`, `TestFailure`), and **`Next:`** bullet hints.
- **`ReadinessTimeout`:** after the triage block, a **readiness probe snapshot** runs: **`GET /health/ready`** and **`GET /health`** with HTTP status and the **first unhealthy check** (alphabetically among failing entries), matching the API’s detailed health JSON (`entries[].name`, `status`, `description`, `error`).

Deterministic behavior is unchanged: same step order, same filters, same timeouts; diagnostics are **additive**.

---

## Troubleshooting

- **API exits before ready:** wrong SQL string, migrations failing, or port in use — watch for a separate console if you run API manually; the smoke script starts a **hidden** `dotnet run` (stdout not shown). Re-run with **`-SkipE2E`** and start the API yourself to read logs, or temporarily change the script to use a visible window for debugging.
- **`run --quick` seed fails:** API must be **`Development`** (the script sets **`ASPNETCORE_ENVIRONMENT=Development`** for the child process).
- **Zero artifacts:** synthesis or persistence regression — check API logs for the smoke **`RunId`**.
- **Playwright fails:** ensure browsers are installed (**`npx playwright install`** in **`archlucid-ui`**) and port **3000** is free for the test **`webServer`**.

More: [TROUBLESHOOTING.md](TROUBLESHOOTING.md), [PILOT_GUIDE.md](PILOT_GUIDE.md).
