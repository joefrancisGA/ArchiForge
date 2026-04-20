> **Scope:** Local release candidate packaging (Change Set 56R) - full detail, tables, and links in the sections below.

# Local release candidate packaging (Change Set 56R)

Practical steps to produce a **Release**-configuration build, run a **lightweight readiness gate**, and **publish** the **ArchLucid** API for handoff to a design partner or pilot (framework-dependent deployment; no Docker requirement in this doc). **Pilot narrative:** [PILOT_GUIDE.md](PILOT_GUIDE.md).

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download), SQL Server when using `ArchLucid:StorageProvider=Sql`, and optionally **Node.js 22+** for operator UI build/tests. See [BUILD.md](BUILD.md) and [TEST_STRUCTURE.md](TEST_STRUCTURE.md).

---

## Scripts (repo root)

| Script | Purpose |
|--------|---------|
| `build-release.cmd` / `build-release.ps1` | `dotnet restore` + `dotnet build ArchLucid.sln -c Release` |
| `package-release.cmd` / `package-release.ps1` | Runs release build, then **`dotnet publish`** API to `artifacts/release/api/`; if **Node** is on `PATH`, also runs `npm ci` + `npm run build` in `archlucid-ui/`. Emits **handoff metadata** next to `api/` (see below). |
| `run-readiness-check.cmd` / `run-readiness-check.ps1` | Release build → **fast core** tests (`-c Release --no-build`) → **Vitest** in `archlucid-ui/` when Node is available. Failures print a **triage** block (stage, category, **Next:** hints) via `scripts/OperatorDiagnostics.ps1`. |
| `release-smoke.cmd` / `release-smoke.ps1` | **E2E smoke:** build + fast core (+ optional `-FullCore`) + optional UI build + temporary API + CLI **`run --quick`** + artifact API check — see [RELEASE_SMOKE.md](RELEASE_SMOKE.md) |
| `test-core.cmd` / `test-core.ps1` | Full Core suite (default configuration, usually Debug). See [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md) |
| `test-fast-core.cmd` / `test-fast-core.ps1` | Core excluding Slow + Integration (default configuration) |
| `test-ui-unit.cmd` / `test-ui-unit.ps1` | `npm ci` + `npm run test` (Vitest) |

**PowerShell extras**

- `package-release.ps1 -SkipUiBuild` — publish API only; no Next.js production build.
- `package-release.ps1 -SkipChecksums` — skip per-file **SHA-256** generation (faster on huge trees; leaves a placeholder `checksums-sha256.txt` and sets `checksumsSha256Generated: false` in `release-manifest.json`).
- `run-readiness-check.ps1 -SkipUi` — skip UI unit tests even if Node is installed.

**Output folder:** `artifacts/release/` is **gitignored**. The **API** publish output is under `artifacts/release/api/`. The same folder’s parent contains **operator handoff files** produced by `scripts/Write-ReleasePackageArtifacts.ps1`:

| File | Purpose |
|------|---------|
| **`metadata.json`** | Build identity: `schemaVersion`, `packageKind`, informational / assembly / file version, `commitSha`, UTC timestamp, SDK, host, whether UI production build ran |
| **`release-manifest.json`** | Inventory: file count / total bytes, every **`api/...`** path with `sizeBytes`, layout notes, `companionFiles` list |
| **`checksums-sha256.txt`** | One line per published file: `<lowercase-hex>  api/relative/path` (same order as `apiPublishFiles` in the manifest) |
| **`PACKAGE-HANDOFF.txt`** | Short human-readable summary for design partners (what each file is, how to run `dotnet ArchLucid.Api.dll`) |

The operator UI remains developed from `archlucid-ui/` in the repo (or deploy via your host’s Node/Next workflow); it is **not** copied into `artifacts/release/` except as noted in the manifest.

---

## Typical pilot workflow

1. **Verify:** `run-readiness-check.cmd` (or `.ps1`) from repo root.
2. **Package:** `package-release.cmd` (or `.ps1`).
3. **Hand off:** Share the **repository** (or archive) plus **`artifacts/release/`** (include **`PACKAGE-HANDOFF.txt`** and **`metadata.json`** for support). Verify file integrity with **`checksums-sha256.txt`** after copy when present.

**After deploy:** For a **full V1 path** on the running API (health, two runs, compare, replay, export, support bundle), use [V1_RC_DRILL.md](V1_RC_DRILL.md) and **`v1-rc-drill.ps1`** from the repo root.

### Support-friendly handoff

- **`metadata.json`** — paste **`informationalVersion`** and **`commitSha`** into support tickets (matches **`GET /version`** when the same bits are running).
- After deploy, pilots should confirm **`GET /version`** or run **`dotnet run --project ArchLucid.Cli -- doctor`** and attach **`support-bundle --zip`** if something fails — see [PILOT_GUIDE.md](PILOT_GUIDE.md) and [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

---

## Run the published API locally

From `artifacts/release/api/` (after `package-release`):

```powershell
# Requires .NET 10 runtime (ASP.NET Core hosting bundle on Windows servers if needed).
$env:ASPNETCORE_ENVIRONMENT = 'Production'
# Example: SQL (adjust for your server; use User Secrets or env vars — do not commit secrets)
$env:ConnectionStrings__ArchLucid = 'Server=localhost,1433;Database=ArchLucid;User Id=sa;Password=...;TrustServerCertificate=True;'
dotnet .\ArchLucid.Api.dll
```

Defaults for URLs are in `ArchLucid.Api/Properties/launchSettings.json` when developing; for published runs, set `ASPNETCORE_URLS` (e.g. `http://localhost:5128`) if you need a fixed binding.

**Health:** `GET /health/live`, `GET /health/ready`, `GET /health` (see root [README.md](../README.md)).

**Configuration:** [README.md](../README.md) (secrets, auth modes), [demo-quickstart.md](demo-quickstart.md) for optional demo seed.

---

## Run the operator UI locally

From repo `archlucid-ui/`:

```bash
npm ci
npm run dev
```

For a **production-style** check (matches `package-release` UI step):

```bash
npm run build
npm start
```

Point the UI at the API using `archlucid-ui` env conventions (e.g. upstream base URL for the proxy — see [archlucid-ui/README.md](../archlucid-ui/README.md) and [operator-shell.md](operator-shell.md)).

---

## Repository hygiene

**`artifacts/release/`** is the only packaging output this doc requires; it is **gitignored**. For a full map of **committed vs generated** paths (including `ArchLucid.Api.Client/Generated/` and common scratch files), see **[REPO_HYGIENE.md](REPO_HYGIENE.md)**.

---

## CI alignment

GitHub Actions uses **`-c Release`** for .NET jobs. Daily `test-fast-core` scripts use the **default** test configuration unless you use **`run-readiness-check`**, which runs fast core in **Release** after a Release build.

---

## Scope limits (56R)

- **No** self-contained RID packaging in these scripts (keeps scripts small). To publish self-contained for a specific OS, add `-r win-x64` (or your RID) to `dotnet publish` in a local one-off or fork of `package-release.ps1`.
- **No** SBOM, container build, or signing in this change set.
