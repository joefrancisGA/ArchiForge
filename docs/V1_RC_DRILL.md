> **Scope:** V1 release-candidate (RC) drill - full detail, tables, and links in the sections below.

# V1 release-candidate (RC) drill

**Audience:** Release owners, SRE, and pilot leads validating a **candidate build** or **fresh environment** before sign-off.

**Purpose:** One **ordered** end-to-end path through the **V1 operator surface**—aligned with [V1_SCOPE.md](V1_SCOPE.md) §4, [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md), and the actual HTTP routes in OpenAPI (`/openapi/v1.json`).

**What this is not:** Full regression (see [TEST_STRUCTURE.md](TEST_STRUCTURE.md)), Terraform apply validation (see [DEPLOYMENT_TERRAFORM.md](DEPLOYMENT_TERRAFORM.md)), or Playwright UI proof against a live API (see [RELEASE_SMOKE.md](RELEASE_SMOKE.md) §57R).

---

## Prerequisites

| Requirement | Notes |
|-------------|--------|
| **API running** | Target URL reachable (default `http://localhost:5128`). |
| **SQL + DbUp** | `ArchLucid:StorageProvider=Sql` with a **migrated** database (fresh DB is fine—migrations run on startup). |
| **Auth** | Examples assume **`DevelopmentBypass`** (no JWT/API key on curl). For **JwtBearer** / **ApiKey**, add headers per [README.md](../README.md) and [API_CONTRACTS.md](API_CONTRACTS.md). |
| **Tooling** | `curl` or PowerShell `Invoke-RestMethod`; .NET SDK for CLI steps; optional Node for UI steps. |

**Automated HTTP steps (no UI):** from repo root, with the API already up:

```powershell
.\v1-rc-drill.ps1 -ApiBaseUrl 'http://localhost:5128'
```

**Switches:** **`-SkipDoctor`** / **`-SkipSupportBundle`** if you already validated CLI diagnostics separately.

**Windows CMD:** `v1-rc-drill.cmd` (same parameters).

The script does **not** deploy infrastructure, build the solution, or start the API. Use [RELEASE_LOCAL.md](RELEASE_LOCAL.md), [CONTAINERIZATION.md](CONTAINERIZATION.md), or your CD pipeline for **deploy fresh**.

---

## Phase 0 — Deploy fresh (environment)

**Goal:** A clean or upgraded database and a single API instance running the **RC bits**.

1. Provision or reset a database (empty is OK if migrations apply on startup).
2. Set **`ConnectionStrings:ArchLucid`**, **`ArchLucid:StorageProvider`**, **`ArchLucidAuth`**, and **`AgentExecution:Mode`** (typically **Simulator** for labs) per [README.md](../README.md) and [PILOT_GUIDE.md](PILOT_GUIDE.md).
3. Start **`ArchLucid.Api`** (host, container, or `dotnet run --project ArchLucid.Api`).
4. Record **image tag / package path / commit** for the release notes.

**Pass criteria:** API process stays up; next phase reaches **HTTP 200** on readiness.

---

## Phase 1 — Health and version

| Step | Action | Pass criteria |
|------|--------|----------------|
| 1.1 | `GET /health/live` | **200** |
| 1.2 | `GET /health/ready` | **200**; dependency checks match environment (e.g. SQL when using Sql storage) |
| 1.3 | `GET /health` (optional) | Review JSON for any **Degraded** entries you accept for this RC |
| 1.4 | `GET /version` | **200**; `commitSha` / `informationalVersion` match expected build |
| 1.5 | CLI **`doctor`** | From repo root: `dotnet run --project ArchLucid.Cli -- doctor` with **`ARCHLUCID_API_URL`** (or `archlucid.json`) pointing at this API — exits **0** |

**Example (bash-style):**

```bash
export BASE=http://localhost:5128
curl -s -o /dev/null -w "%{http_code}\n" "$BASE/health/live"
curl -s -o /dev/null -w "%{http_code}\n" "$BASE/health/ready"
curl -s "$BASE/version"
```

---

## Phase 2 — Create request → execute → commit (two runs)

You need **two committed runs** for compare and for authority run-level diff. Use **distinct** `requestId` values.

**Goal:** Two runs, each **Created → executed → committed** with a **golden manifest** and **artifacts**.

### Run A

| Step | HTTP | Notes |
|------|------|--------|
| 2A.1 | `POST /v1/architecture/request` | Body: structured [ArchitectureRequest](API_CONTRACTS.md) (see [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md)); capture **`run.runId`** from JSON |
| 2A.2 | `POST /v1/architecture/run/{runId}/execute` | Waits until agent work completes for this request/executor configuration |
| 2A.3 | `POST /v1/architecture/run/{runId}/commit` | Produces golden manifest; **409** if wrong state ([API_CONTRACTS.md](API_CONTRACTS.md)) |
| 2A.4 | `GET /v1/architecture/run/{runId}` | Confirm **`run.goldenManifestId`** is non-null |

### Run B

Repeat **2A.1–2A.4** with a **different** `requestId` and **`systemName`** (or description) so compare has meaningful deltas.

**CLI alternative (Development, one run):** [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) **`new`** + **`run --quick`** creates one committed run quickly; still create a **second** run (HTTP or second CLI project) for compare.

**Pass criteria:** Both runs show **`goldenManifestId`**; no unexpected **5xx**; commit returns **200** (a second commit on an already-committed run is **200** idempotent per **`API_CONTRACTS.md`**).

---

## Phase 3 — Inspect manifests and artifacts

| Step | Action | Pass criteria |
|------|--------|----------------|
| 3.1 | `GET /v1/artifacts/manifests/{goldenManifestId}` | **200**; JSON **array** with **≥ 1** descriptor for each run you care about (empty array is valid only if you explicitly expect no synthesized rows—usually **not** for RC) |
| 3.2 | Open **operator UI** (optional): **Runs** → run → **Manifest** / **Artifacts** | List matches API; **Review** / download works ([operator-shell.md](operator-shell.md)) |
| 3.3 | `GET /v1/artifacts/manifests/{goldenManifestId}/bundle` (optional) | **200** ZIP, or **404** with documented problem type when no bundle (distinct from unknown manifest—[API_CONTRACTS.md](API_CONTRACTS.md)) |

---

## Phase 4 — Compare two runs

| Step | Action | Pass criteria |
|------|--------|----------------|
| 4.1 | `GET /v1/architecture/run/compare/end-to-end?leftRunId={A}&rightRunId={B}` | **200**; response contains comparison payload (structured + legacy surfaces per implementation) |
| 4.2 | (Optional) `GET /v1/authority/compare/runs?leftRunId={A}&rightRunId={B}` | **200**; run-level + manifest comparison block as expected |
| 4.3 | (Optional) **UI:** **Compare runs** workflow with same IDs ([operator-shell.md](operator-shell.md)) | Prefill and outcome match API |

**Persist + CLI replay (optional, deeper):** `POST /v1/architecture/run/compare/end-to-end/summary` with **`persist: true`** records a comparison; then **`archlucid comparisons replay`** ([CLI_USAGE.md](CLI_USAGE.md)).

---

## Phase 5 — Replay one run

V1 includes **two** replay concepts—exercise at least **one** for RC; **authority** replay below is the safest default (**read-mostly validation**).

| Path | HTTP | Pass criteria |
|------|------|----------------|
| **Authority replay (recommended)** | `POST /v1/authority/replay` | Body: `{ "runId": "<guid>", "mode": "ReconstructOnly" }` — **200**; validation flags in response. Modes: **`ReconstructOnly`** (validate only), **`RebuildManifest`**, **`RebuildArtifacts`** (see `ReplayMode` in source: `ArchLucid.Persistence.Coordination/Replay/ReplayMode.cs`). |
| **Coordinator replay (optional)** | `POST /v1/architecture/run/{runId}/replay` | Optional JSON body (`executionMode`, `commitReplay`, …); can **mutate** data — use only when you intend to re-execute agents ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md)). |

**Auth:** Authority replay requires **`ExecuteAuthority`** (same policy family as execute/commit).

---

## Phase 6 — Export package (run ZIP)

| Step | Action | Pass criteria |
|------|--------|----------------|
| 6.1 | `GET /v1/artifacts/runs/{runId}/export` | **200**; **`application/zip`**; non-zero size |
| 6.2 | Open ZIP | Contains manifest, trace (when present), README, and artifact entries per `IArtifactPackagingService` / OpenAPI (`ArtifactExport` tag). |

**Example:**

```bash
curl -sL -o rc-run-export.zip "$BASE/v1/artifacts/runs/RUN_ID/export"
```

---

## Phase 7 — Support bundle

| Step | Action | Pass criteria |
|------|--------|----------------|
| 7.1 | Set **`ARCHLUCID_API_URL`** to the RC API | Same base URL as drill |
| 7.2 | `dotnet run --project ArchLucid.Cli -- support-bundle --zip` | Completes; review **`README.txt`** inside before any external share ([TROUBLESHOOTING.md](TROUBLESHOOTING.md), [CLI_USAGE.md](CLI_USAGE.md)) |

---

## Relation to existing automation

| Artifact | Role |
|----------|------|
| **`run-readiness-check.ps1`** | Release build + **fast core** tests + UI unit/build—**no** live API drill ([RELEASE_LOCAL.md](RELEASE_LOCAL.md)). |
| **`release-smoke.ps1`** | Deeper **build + tests + temporary API + CLI `run --quick` + artifact assertion** ([RELEASE_SMOKE.md](RELEASE_SMOKE.md)). |
| **`v1-rc-drill.ps1`** | Assumes API already running; walks **two-run** HTTP path: health/version, dual request/execute/commit, artifacts list, end-to-end compare, authority **ReconstructOnly** replay, run export ZIP, **`doctor`** + **`support-bundle`**. |

**Suggested order for a hard RC gate:** `run-readiness-check` → deploy RC → **`v1-rc-drill.ps1`** (or manual steps above) → optional **`release-smoke.ps1`** on a build agent with SQL.

---

## Evidence checklist (copy into release notes)

- [ ] Build identity: `/version` + `metadata.json` (if packaged—[RELEASE_LOCAL.md](RELEASE_LOCAL.md))
- [ ] Run IDs **A** / **B** and **goldenManifestId** values recorded
- [ ] Screenshot or JSON snippet: compare **end-to-end** response
- [ ] Authority replay **ReconstructOnly** response (or documented alternative)
- [ ] Run export ZIP checksum or size
- [ ] Support bundle generated (internal path only until redacted)

---

## Related documents

| Doc | Use |
|-----|-----|
| [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) | Checkbox release gates |
| [V1_SCOPE.md](V1_SCOPE.md) | In/out of scope |
| [PILOT_GUIDE.md](PILOT_GUIDE.md) | Narrative onboarding |
| [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) | Copy-paste commands |
| [RELEASE_SMOKE.md](RELEASE_SMOKE.md) | Scripted smoke scope |
| [COMPARISON_REPLAY.md](COMPARISON_REPLAY.md) | Comparison / replay concepts |
| [REPO_HYGIENE.md](REPO_HYGIENE.md) | What to commit vs generated |
