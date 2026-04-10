# Change Set 56R — release candidate hardening & pilot readiness

## Objective

Harden configuration, startup, logging/observability, packaging, and operator-facing readiness **without** broad feature work. Prefer explicit, production-grade C#; preserve deterministic behavior and policy controls.

## This change set (incremental)

### Prompt 1 — configuration surface & startup diagnostics

- **Startup snapshot:** One structured `Information` log after host build with **non-secret** effective flags. Toggle via **`Hosting:LogStartupConfigurationSummary`** (default `true` when unset).
- **Config alignment:** `appsettings.json` and Key Vault sample use **AdminKey** / **ReadOnlyKey**. Key Vault doc updated.

### Prompt 2 — configuration & environment validation (current)

- **API fail-fast:** `ArchLucidConfigurationRules.CollectErrors` runs **immediately after** `WebApplication.Build()` and **before** schema bootstrap / DbUp. Any error → log each line and **`InvalidOperationException`** (process exit). Replaces the late **`IHostedService`** validator so misconfiguration is not masked in Development.
- **SQL vs InMemory:** `ConnectionStrings:ArchLucid` is **required** only when **`ArchLucid:StorageProvider`** is **Sql** (including default `Sql` when the section is absent). **InMemory** allows no SQL connection string.
- **Policy/schema files:** Validates **SchemaValidation** JSON schema paths are **relative**, stay **under** `AppContext.BaseDirectory`, and **exist on disk** at startup (matches `SchemaValidationService` load semantics).
- **CLI:** `ArchLucidApiClient.GetInvalidApiBaseUrlReason` + constructor guard; `EnsureApiConnectedAsync` and **`health`** print stderr guidance for bad URLs.
- **UI:** `resolveUpstreamApiBaseUrlForProxy()` returns **503** JSON problem from `/api/proxy/*` when the upstream base URL is empty, malformed, or non-http(s).
- **Artifacts:** No separate on-disk artifact root in API config (exports are streams/DB-backed); **CLI** `archlucid run` already validates brief path and creates `outputs` from `archlucid.json` — unchanged.

### Prompt 3 — startup readiness checks

- **HTTP:** `GET /health/live` — process liveness only. `GET /health/ready` — database (skipped when `StorageProvider=InMemory`), JSON schema files, bundled compliance rule pack, writable temp directory. `GET /health` — all registered checks (live + ready).
- **CLI:** `archlucid doctor` or `archlucid check` — local project checks + calls the three endpoints and prints JSON (truncated) with clear section headers.
- **Tags:** `ArchLucid.Api.Health.ReadinessTags` (`live` / `ready`); no extra framework beyond `IHealthCheck`.

### Prompt 5 — packaging and local release scripts

- **Scripts (repo root):** `build-release`, `package-release`, `run-readiness-check` (`.cmd` + `.ps1`) — Release build, `dotnet publish` to `artifacts/release/api/`, optional Next.js production build when Node is available, RC-style gate (Release + fast core + Vitest).
- **Doc:** [RELEASE_LOCAL.md](RELEASE_LOCAL.md) — handoff workflow, run published API, UI dev/build, CI notes, scope limits (no SBOM/container in-script).

### Prompt 6 — pilot onboarding and operator docs

- **New:** [PILOT_GUIDE.md](PILOT_GUIDE.md) — what the product does, minimum setup, first run (Swagger + CLI), artifact review, readiness/core tests, logs vs DB artifacts, support hints.
- **New:** [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) — copy-paste command blocks only.
- **New:** [TROUBLESHOOTING.md](TROUBLESHOOTING.md) — common failure modes, triage table, log search tokens, UI proxy notes.
- **Updated:** [README.md](../README.md) — pilot onboarding section + doc table rows.

### Prompt 7 — end-to-end release readiness smoke

- **New:** `release-smoke.ps1`, `release-smoke.cmd` — Release build, fast core (+ optional `-FullCore`), optional UI Vitest + `next build`, temporary **ArchLucid.Api** process, **`GET /health/ready`** + **`/health/live`**, CLI **`new` + `run --quick`**, assert **≥ 1** artifact via **`GET /api/artifacts/manifests/{goldenManifestId}`**.
- **New:** [RELEASE_SMOKE.md](RELEASE_SMOKE.md) — prerequisites, env vars, switches, relation to `run-readiness-check` / `package-release`.

### Prompt 8 — error presentation and supportability

- **API:** `ProblemSupportHints` adds optional **`extensions.supportHint`** on problem+json for known `ProblemTypes` (controllers + `ApplicationProblemMapper` + global 500 handler).
- **CLI:** `CliOperatorHints` — stderr **`Next:`** lines after API failures, health unreachable, readiness failure, brief/manifest/run issues; `ArchLucidApiClient` records **HTTP status** on failed commit/submit/seed responses for hint selection.
- **UI:** Proxy returns **502** with **`supportHint`** when fetch to the C# API fails; **503** config errors include **`supportHint`** for `.env.local`.
- **Docs:** [TROUBLESHOOTING.md](TROUBLESHOOTING.md) — `supportHint` / CLI `Next:` / UI proxy errors.

### Prompt 9 — focused tests for 56R hardening

- **API:** `ProblemSupportHintsTests`, extended **`ArchLucidConfigurationRulesTests`** (storage/mode/Azure/schema paths), **`ApiProblemDetailsExceptionFilterTests`** assert **`supportHint`** on mapped problems.
- **CLI:** `InternalsVisibleTo` for **`ArchLucid.Cli.Tests`**; **`CliOperatorHintsTests`**; **`ArchLucidApiClientHttpTests`** — commit failure preserves **HTTP status code**.

### Prompt 10 — release-candidate coherence (final pass)

- **Docs:** README **`ArchLucidAuth`** table aligned with **`ApiKey`** mode; pilot guide uses **`dotnet run --project ArchLucid.Cli`** consistently with scripts; **RELEASE_SMOKE** CMD/`;` caveat.
- **Logging:** Single startup **configuration snapshot** log now includes **`ContentRoot`**; removed redundant “host built” **Information** line before validation.

### Deferred to later prompts (56R backlog)

- Structured log enrichers (deployment slot) and log level profiles per environment; OTLP defaults beyond current wiring.
- Packaging: Dockerfile polish, optional SBOM/signing, self-contained RID publish recipes in scripts.
- **Further design-partner workflow:** API-hosted support bundle, dedicated checklist doc beyond pilot guide — pick per later prompt.

## Release candidate verdict (Prompt 10 — original scope)

- **Adds (56R overall):** Fail-fast config validation before DbUp; `/health/live` + `/health/ready` + tagged checks; startup **non-secret** configuration snapshot (toggle `Hosting:LogStartupConfigurationSummary`); local **build/package/readiness/smoke** scripts; pilot/operator/troubleshooting docs; API **`supportHint`**, CLI **`Next:`** hints, UI proxy **502/503** hints; focused unit tests for rules, hints, and CLI behavior.
- **Deliberately not in original 56R:** Self-contained RID publish in scripts, SBOM/signing/container polish, rich OTLP/log-enricher profiles, Playwright in default `release-smoke`, full multi-tenant/perf matrices. (**Regenerated 56R** later added CLI **`support-bundle`**, **`GET /version`**, enriched health JSON, packaging **`metadata.json`**, and optional **`-RunPlaywright`** — see **Regenerated 56R** sections below.)
- **Pilot readiness:** **Yes** for a first design-partner run **if** they have .NET 10, a working SQL (or explicit **InMemory** dev path), and follow **PILOT_GUIDE** / **OPERATOR_QUICKSTART**. Recommend **`run-readiness-check`** before handoff and **`release-smoke`** (with **`ARCHIFORGE_SMOKE_SQL`**) when SQL and port **5128** are available.
- **Small follow-ups before “commercial” hardening:** optional design-partner checklist doc; visible API window or log capture flag for failed **`release-smoke`** E2E; self-contained publish recipe if pilots lack SDK.

## Regenerated 56R — incremental closing gaps

### Prompt 1 (regen) — build / version provenance

- **Core:** `ArchLucid.Core.Diagnostics.BuildProvenance` — single resolver for informational, assembly, and file version + runtime framework description.
- **API:** Startup `Pilot/support configuration snapshot` log extended with build fields; Serilog enricher adds `AssemblyFileVersion` when present; OpenTelemetry `service.version` uses informational version (matches logs).
- **Tests:** `BuildProvenanceTests`, extended `StartupConfigurationFactsReaderTests`.
- **Docs:** `docs/OPERATOR_QUICKSTART.md` — where to find provenance in logs; optional `/p:InformationalVersion` for CI.

### Prompt 2 (regen) — build/version/commit provenance HTTP surface, CLI, health, and release metadata

- **Core:** `BuildProvenance` extended with `CommitSha` — parsed from the `+{sha}` suffix of `AssemblyInformationalVersion` (populated automatically when `SourceRevisionId` is set at build time).
- **Core:** `BuildInfoResponse` DTO — lightweight, non-secret build identity payload for HTTP and CLI consumption.
- **API:** `GET /version` endpoint (`VersionController`, `[AllowAnonymous]`) — returns `application`, `informationalVersion`, `assemblyVersion`, `fileVersion`, `commitSha`, `runtimeFramework`, `environment`.
- **API:** `/health/ready` and `/health` now use `DetailedHealthCheckResponseWriter` — enriched JSON with per-check `name`, `status`, `durationMs`, `description`, `error`, plus top-level `version`, `commitSha`, and `totalDurationMs`. `/health/live` stays minimal for orchestrator probes.
- **CI:** Both `dotnet-fast-core` and `dotnet-full-regression` build steps now pass `/p:SourceRevisionId=$(git rev-parse HEAD)` so the commit SHA is embedded in the informational version automatically.
- **CLI:** `doctor` now prints a **CLI build info** section (version, assembly, runtime) and calls **`GET /version`** to display the API's build identity before running health probes.
- **CLI:** `ArchLucidApiClient.GetVersionJsonAsync` — new method for retrieving `/version` JSON.
- **Release:** `package-release.ps1` / `.cmd` now emit `artifacts/release/metadata.json` with `application`, `informationalVersion`, `commitSha`, `buildTimestampUtc`, `dotnetSdkVersion`, `packagerHost`.
- **Tests:** `BuildProvenanceTests` — `ParseCommitSha` theory tests, `BuildInfoResponse.FromProvenance` mapping/null tests. `VersionControllerTests` — controller returns expected fields and JSON shape. `DetailedHealthCheckResponseWriterTests` — healthy/unhealthy reports produce correct JSON payload.
- **Docs:** `OPERATOR_QUICKSTART.md` updated with `/version`, `/health/ready` enrichment, `SourceRevisionId` guidance. `CLI_USAGE.md` — `doctor` description updated.

### Prompt 3 (regen) — CLI support bundle export

- **CLI:** `archlucid support-bundle` — writes a UTC-stamped folder (default `support-bundle-<yyyyMMdd-HHmmss>Z`) with explicit JSON sections; **`--output <dir>`** and **`--zip`** supported.
- **Modules (reviewable):** `SupportBundleRedactor`, `SupportBundleCollector`, `SupportBundleArchiveWriter`, `SupportBundleCommand`, and one file per bundle DTO under `ArchLucid.Cli/Support/`.
- **Contents:** `manifest.json`, `build.json` (CLI build + raw `GET /version` JSON), `health.json` (`/health/live`, `/health/ready`, `/health` with truncated bodies), `config-summary.json` (non-secret `archlucid.json` fields + redacted API base URL), `environment.json` (machine/OS/runtime + filtered env: `ARCHIFORGE_*` / `DOTNET_*` only; secrets as `(set)`; SQL-related ArchLucid keys never show values; `ARCHIFORGE_API_URL` userinfo stripped), `workspace.json` (outputs dir file count/size + sample names), `references.json` (endpoint/doc hints), `logs.json` (guidance + optional small `outputs/last-run.log` excerpt).
- **Tests:** `ArchLucid.Cli.Tests/SupportBundleTests.cs` — redactor, mock HTTP collect, directory and zip writers.
- **Docs:** `CLI_USAGE.md`, `TROUBLESHOOTING.md`.

### Prompt 4 (regen) — readiness and smoke diagnostics for failure triage

- **Shared:** `scripts/OperatorDiagnostics.ps1` — phase headers, **`--- FAILURE (triage) ---`** blocks (**Stage**, **Category**, **Next:** hints), HTTP probe helper, readiness JSON parser (**first unhealthy check** among `entries[]`, then others sorted by **name** for deterministic output).
- **`run-readiness-check.ps1`:** Numbered phases (`[1/n]`…`[3/n]` when UI runs); triage on build, fast core, `npm ci`, Vitest failures; dynamic `n` when UI skipped or Node missing.
- **`release-smoke.ps1`:** Triage on each gate (build, core, optional full core, UI, SQL misconfig, API start/early exit, readiness **timeout** + post-timeout `/health/ready` + `/health` snapshot, liveness, CLI `new` / `run --quick`, artifacts API, Playwright); readiness wait uses **`Get-ArchLucidHttpProbe`** (captures non-200 bodies without throwing away JSON).
- **Docs:** [RELEASE_SMOKE.md](RELEASE_SMOKE.md) — “Failure triage (script output)”; [RELEASE_LOCAL.md](RELEASE_LOCAL.md) — readiness script triage note.

**Still for later regen prompts:** further pilot supportability (e.g. API-hosted bundle).

### Prompt 5 (regen) — release packaging metadata and handoff artifacts

- **`scripts/Write-ReleasePackageArtifacts.ps1`** — single writer invoked from **`package-release.ps1`** / **`package-release.cmd`** after publish (and optional UI build).
- **`metadata.json`** (extended): `schemaVersion` **1.1**, `packageKind`, `assemblyVersion`, `fileVersion` (Win32 file info), `apiPublishPathRelative`, `uiProductionBuildIncluded`; retains informational version, commit, UTC timestamp, SDK, packager host.
- **`release-manifest.json`**: `packageKind` **ArchLucid.ReleaseHandoff**, summary counts/bytes, full **`apiPublishFiles`** list with sizes, operator UI note, `companionFiles`, `checksumsSha256Generated`.
- **`checksums-sha256.txt`**: SHA-256 per file under `api/` (deterministic path order aligned with manifest); optional **`-SkipChecksums`** on **`.ps1`** only.
- **`PACKAGE-HANDOFF.txt`**: concise pilot-facing blurb and pointers to docs.
- **`docs/RELEASE_LOCAL.md`** — handoff table and **`-SkipChecksums`** note.

### Prompt 6 (regen) — docs for supportability and handoff

- **PILOT_GUIDE.md** — `GET /version`, **`doctor`**, readiness/smoke table, **support bundle** commands, **When you report an issue** checklist.
- **TROUBLESHOOTING.md** — **First-line steps** (health, version, doctor, bundle, readiness/smoke); expanded support bundle (`--output`, contents); link to pilot reporting section.
- **RELEASE_LOCAL.md** — **Support-friendly handoff** (`metadata.json` vs `/version`, bundle + doc pointers).
- **RELEASE_SMOKE.md** — Pilot note: readiness vs smoke, what to paste from triage output.
- **README.md** — Pilot onboarding tightened: version, doctor, support-bundle, reporting anchor.

### Prompt 8 (regen) — final coherence pass (supportability only)

- **Startup log:** `Pilot/support configuration snapshot` now includes **`BuildCommitSha`** (or **`(not stamped)`**), aligned with **`GET /version`** / enriched **`/health/ready`** `commitSha`.
- **Health JSON:** Inline comment in `DetailedHealthCheckResponseWriter` documents that **`version`** matches **`GET /version`** `informationalVersion`.
- **CLI:** `doctor` success line and class summary mention combined **`/health`**; **`Next:`** after readiness failure mentions **`GET /version`** for tickets.
- **Docs:** `CHANGE_SET_56R.md` verdict no longer contradicts regen deliverables; `OPERATOR_QUICKSTART` / `TROUBLESHOOTING` clarify `version` vs `informationalVersion`, optional JSON pretty-print, and **`.\release-smoke.ps1 -SkipE2E`**; `CLI_USAGE` doctor exit criteria clarified.
- **Tests:** `CliOperatorHintsTests` asserts readiness hint includes **`/version`**.

---

## Related files

- `ArchLucid.Core/Diagnostics/BuildProvenance.cs`, `ArchLucid.Core/Diagnostics/BuildInfoResponse.cs`
- `ArchLucid.Api/Controllers/VersionController.cs`
- `ArchLucid.Api/Health/DetailedHealthCheckResponseWriter.cs`
- `ArchLucid.Api/Startup/Diagnostics/*`
- `ArchLucid.Api/Startup/Validation/ArchLucidConfigurationRules.cs`
- `ArchLucid.Api/Startup/PipelineExtensions.cs` (`/health/live`, `/health/ready`, `/health`)
- `ArchLucid.Api/Program.cs`
- `ArchLucid.Api/appsettings.json`, `appsettings.KeyVault.sample.json`
- `ArchLucid.Cli/ArchLucidApiClient.cs`, `ArchLucid.Cli/Program.cs`, `ArchLucid.Cli/DoctorCommand.cs`, `ArchLucid.Cli/Support/*` (support bundle)
- `ArchLucid.Api/Health/*` (readiness tags, schema/compliance/temp checks, SQL check behavior)
- `archlucid-ui/src/lib/config.ts`, `archlucid-ui/src/app/api/proxy/[...path]/route.ts`
- `docs/CONFIGURATION_KEY_VAULT.md`
- `scripts/OperatorDiagnostics.ps1`, `scripts/Write-ReleasePackageArtifacts.ps1`, `build-release.cmd`, `build-release.ps1`, `package-release.cmd`, `package-release.ps1`, `run-readiness-check.cmd`, `run-readiness-check.ps1`
- `docs/RELEASE_LOCAL.md`
- `docs/PILOT_GUIDE.md`, `docs/OPERATOR_QUICKSTART.md`, `docs/TROUBLESHOOTING.md`, `docs/CLI_USAGE.md`
- `release-smoke.ps1`, `release-smoke.cmd`, `docs/RELEASE_SMOKE.md`
- `ArchLucid.Api/ProblemDetails/ProblemSupportHints.cs`, `ArchLucid.Api/ProblemDetails/*` (extensions wiring)
- `ArchLucid.Cli/CliOperatorHints.cs`
- `archlucid-ui/src/app/api/proxy/[...path]/route.ts`, `docs/API_CONTRACTS.md` (problem extensions)
- `.github/workflows/ci.yml` (SourceRevisionId stamping)
