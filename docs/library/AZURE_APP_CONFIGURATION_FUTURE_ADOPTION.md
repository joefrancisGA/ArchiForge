> **Scope:** Azure App Configuration — future adoption plan - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Azure App Configuration — future adoption plan

**Companion to:** [ADR 0017](../adr/0017-azure-app-configuration-deferred.md)
**Status:** Plan-only (not yet adopted)
**Last reviewed:** 2026-04-18

## Objective

Provide a low-risk migration path to **Azure App Configuration** when one of the **revisit triggers** in ADR 0017 fires, without rewriting host bootstrap or breaking local development.

## Assumptions

- The team accepts the recurring spend documented in ADR 0017.
- Key Vault is already provisioned per env (it is; `appsettings.KeyVault.sample.json`, `SECRET_AND_CERT_ROTATION.md`).
- All host paths use `IConfiguration` only — no static config singletons (verified in current code).

## Constraints

- Must remain **fully runnable offline** (inner loop, CI). No mandatory Azure dependency for `dotnet test` or `dotnet run` in `Development`.
- Must respect the **port 445 / private endpoint** default. App Configuration store gets a private endpoint; `public_network_access = Disabled`.
- **Identity-only** auth (`local_auth_enabled = false`); no shared keys.
- Single source of truth for any given key — never read the same key from both JSON and App Configuration.

## Architecture overview

```
+-----------------------------+
| ArchLucid host (Api/Worker) |
+-----------------------------+
        |
        v
+--------------------------------+        +-------------------------+
| ConfigurationBuilder           |        | dotnet user-secrets     |
|                                |        | (developer machine)     |
| 1. appsettings.json            |        +-------------------------+
| 2. appsettings.{Env}.json      |
| 3. user-secrets (Dev only)     |
| 4. environment variables       |
| 5. Azure App Configuration *   |---->  Azure App Configuration  ----> Key Vault
|    (only when AppConfig:Enabled)       (private endpoint, Entra ID)   (KV references)
+--------------------------------+
```

`*` Provider 5 is **opt-in** via `AppConfig:Enabled=true`. Default is `false` so the flow degrades to today's behavior.

## Component breakdown

| Component | Purpose | Notes |
|---|---|---|
| `AppConfig:Enabled` (bool) | Master switch | Default `false`. Required `true` in Production once adopted. |
| `AppConfig:Endpoint` (Uri) | App Configuration store URI | Per-env. |
| `AppConfig:Label` (string) | Label selector | Usually equals environment name. |
| `AppConfig:SentinelKey` (string) | Sentinel for refresh | Default `"Sentinel"`. |
| `AppConfig:RefreshIntervalSeconds` (int) | Cache TTL | Default 30. |
| `AzureAppConfigurationProviderRegistrar` (new) | Wires provider into `ConfigurationBuilder` | Lives in `ArchLucid.Host.Core/Configuration/`. |
| `AppConfigurationStartupValidationRule` (new) | Fail-fast in `Production` if disabled or wrong endpoint | Mirrors `ProductionSafetyRules` pattern. |

## Data flow (boot)

1. Host reads `appsettings.json` + env-specific JSON + env vars.
2. If `AppConfig:Enabled=true`, register the App Configuration provider with:
   - Two `Select` calls: `(KeyFilter.Any, LabelFilter.Null)` for shared keys, then `(KeyFilter.Any, env)` for env-specific.
   - `ConfigureKeyVault(...)` with `DefaultAzureCredential`.
   - `ConfigureRefresh(... .Register("Sentinel", refreshAll: true) .SetCacheExpiration(30s))`.
   - `UseFeatureFlags(ff => ff.Label = env)`.
3. Subsequent `IConfiguration` reads merge App Configuration over JSON; **last provider wins**.
4. Sentinel updates fire `IOptionsMonitor.OnChange` for any `IOptionsMonitor<T>` consumer (no code changes needed in consumers).

## Security model

- **No shared keys.** RBAC roles only:
  - `App Configuration Data Reader` for runtime identities (API, Worker).
  - `App Configuration Data Owner` only for Terraform service principal.
- **Private endpoint** mandatory; public network disabled.
- **Key Vault references** for any secret material; KV access policies / RBAC unchanged from today.
- **No PII in keys or values.** Same prohibition as logs (`docs/SECURITY.md` PII section).

## Operational considerations

- **Snapshots** taken per release; release pinned to a snapshot to prevent drift during blue/green.
- **Event Grid → audit log** for change events; route into the same sink as governance audit.
- **Disaster recovery:** geo-replicated store in paired region; provider auto-fails over.
- **Local cache:** SDK writes a last-known-good cache; survives short App Configuration outages.

## Migration sequence (when triggered)

1. **Provision** dev-only App Configuration store via Terraform (no production impact).
2. **Mirror 3–5 keys** behind `AppConfig:Enabled=true` in Development; verify `IOptionsMonitor.OnChange`.
3. **Move feature flags first.** All `FeatureManagement:*` keys move atomically per env. Delete from JSON in the same PR.
4. **Move resilience knobs** (`CircuitBreakerGate*`, retry/timeout settings).
5. **Convert secrets to KV references** in App Configuration; retire `appsettings.KeyVault.sample.json`.
6. **Collapse `appsettings.{Env}.json`** to a thin bootstrap (endpoint, environment name, log level).
7. **Add `AppConfigurationStartupValidationRule`** to `ProductionSafetyRules` so missing/disabled config in Production fails fast.
8. **Add Event Grid → audit** wiring and runbook updates.

## Local development behavior after adoption

| Mode | When | How |
|---|---|---|
| Pure local (default) | Inner loop, offline, CI unit tests | `AppConfig:Enabled=false`. Reads JSON + user-secrets exactly as today. |
| Local + shared dev store | Engineer wants to test a flag toggle | `AppConfig:Enabled=true`, dev endpoint, `az login` for `DefaultAzureCredential`. |
| Container emulation | Integration tests in offline CI | Use the App Configuration emulator container if it is GA at adoption time; otherwise stay in pure-local mode for tests. |

## Rollback

Set `AppConfig:Enabled=false` and redeploy. JSON + env vars remain authoritative; nothing has been deleted from the deployment artifacts. This is the explicit reason migration retires keys from JSON only **after** the corresponding App Configuration keys are verified in production.
