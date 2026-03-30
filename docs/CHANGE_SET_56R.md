# Change Set 56R — release candidate hardening & pilot readiness

## Objective

Harden configuration, startup, logging/observability, packaging, and operator-facing readiness **without** broad feature work. Prefer explicit, production-grade C#; preserve deterministic behavior and policy controls.

## This change set (incremental)

### Prompt 1 (current) — configuration surface & startup diagnostics

- **Startup snapshot:** One structured `Information` log after host build with **non-secret** effective flags (environment, SQL connection presence, storage provider, retrieval mode, agent mode, auth mode, API key flags, CORS count, rate limit, Prometheus, demo flags, schema validation detail). Toggle via **`Hosting:LogStartupConfigurationSummary`** (default `true` when unset).
- **Validation:** `ArchiForge:StorageProvider` must be `InMemory` or `Sql` when set. In **Production**, reject `DevelopmentBypass`; require **Authority** for `JwtBearer`; for `ApiKey` mode require `Authentication:ApiKey:Enabled` and at least one of **AdminKey** / **ReadOnlyKey**.
- **Config alignment:** `appsettings.json` and Key Vault sample use **AdminKey** / **ReadOnlyKey** (matching `ApiKeyAuthenticationHandler`). Key Vault doc updated.
- **Tests:** Facts reader mapping; `ConfigurationValidator` production rules (and Development bypass happy path).

### Deferred to later prompts (56R backlog)

- Health checks / readiness vs liveness split, dependency probes.
- Structured logging enrichers (version, deployment slot) and log level profiles per environment.
- Packaging: Dockerfile polish, version stamping, optional SBOM.
- **Design-partner readiness workflow:** checklist doc, support bundle export, or operator runbook (pick one minimal slice per prompt).

## Related files

- `ArchiForge.Api/Startup/Diagnostics/*`
- `ArchiForge.Api/Startup/Validation/ConfigurationValidator.cs`
- `ArchiForge.Api/Program.cs`
- `ArchiForge.Api/appsettings.json`, `appsettings.KeyVault.sample.json`
- `docs/CONFIGURATION_KEY_VAULT.md`
