# Change Set 56R — release candidate hardening & pilot readiness

## Objective

Harden configuration, startup, logging/observability, packaging, and operator-facing readiness **without** broad feature work. Prefer explicit, production-grade C#; preserve deterministic behavior and policy controls.

## This change set (incremental)

### Prompt 1 — configuration surface & startup diagnostics

- **Startup snapshot:** One structured `Information` log after host build with **non-secret** effective flags. Toggle via **`Hosting:LogStartupConfigurationSummary`** (default `true` when unset).
- **Config alignment:** `appsettings.json` and Key Vault sample use **AdminKey** / **ReadOnlyKey**. Key Vault doc updated.

### Prompt 2 — configuration & environment validation (current)

- **API fail-fast:** `ArchiForgeConfigurationRules.CollectErrors` runs **immediately after** `WebApplication.Build()` and **before** schema bootstrap / DbUp. Any error → log each line and **`InvalidOperationException`** (process exit). Replaces the late **`IHostedService`** validator so misconfiguration is not masked in Development.
- **SQL vs InMemory:** `ConnectionStrings:ArchiForge` is **required** only when **`ArchiForge:StorageProvider`** is **Sql** (including default `Sql` when the section is absent). **InMemory** allows no SQL connection string.
- **Policy/schema files:** Validates **SchemaValidation** JSON schema paths are **relative**, stay **under** `AppContext.BaseDirectory`, and **exist on disk** at startup (matches `SchemaValidationService` load semantics).
- **CLI:** `ArchiForgeApiClient.GetInvalidApiBaseUrlReason` + constructor guard; `EnsureApiConnectedAsync` and **`health`** print stderr guidance for bad URLs.
- **UI:** `resolveUpstreamApiBaseUrlForProxy()` returns **503** JSON problem from `/api/proxy/*` when the upstream base URL is empty, malformed, or non-http(s).
- **Artifacts:** No separate on-disk artifact root in API config (exports are streams/DB-backed); **CLI** `archiforge run` already validates brief path and creates `outputs` from `archiforge.json` — unchanged.

### Prompt 3 — startup readiness checks

- **HTTP:** `GET /health/live` — process liveness only. `GET /health/ready` — database (skipped when `StorageProvider=InMemory`), JSON schema files, bundled compliance rule pack, writable temp directory. `GET /health` — all registered checks (live + ready).
- **CLI:** `archiforge doctor` or `archiforge check` — local project checks + calls the three endpoints and prints JSON (truncated) with clear section headers.
- **Tags:** `ArchiForge.Api.Health.ReadinessTags` (`live` / `ready`); no extra framework beyond `IHealthCheck`.

### Deferred to later prompts (56R backlog)

- Structured log enrichers, version/commit in logs, OTLP defaults.
- Structured logging enrichers (version, deployment slot) and log level profiles per environment.
- Packaging: Dockerfile polish, version stamping, optional SBOM.
- **Design-partner readiness workflow:** checklist doc, support bundle export, or operator runbook (pick one minimal slice per prompt).

## Related files

- `ArchiForge.Api/Startup/Diagnostics/*`
- `ArchiForge.Api/Startup/Validation/ArchiForgeConfigurationRules.cs`
- `ArchiForge.Api/Program.cs`
- `ArchiForge.Api/appsettings.json`, `appsettings.KeyVault.sample.json`
- `ArchiForge.Cli/ArchiForgeApiClient.cs`, `ArchiForge.Cli/Program.cs`, `ArchiForge.Cli/DoctorCommand.cs`
- `ArchiForge.Api/Health/*` (readiness tags, schema/compliance/temp checks, SQL check behavior)
- `ArchiForge.Api/Startup/PipelineExtensions.cs` (`/health/live`, `/health/ready`)
- `archiforge-ui/src/lib/config.ts`, `archiforge-ui/src/app/api/proxy/[...path]/route.ts`
- `docs/CONFIGURATION_KEY_VAULT.md`
