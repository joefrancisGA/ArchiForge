> **Scope:** ADR 0017 — Azure App Configuration: deferred for v1 on cost grounds - full detail, tables, and links in the sections below.

# ADR 0017 — Azure App Configuration: deferred for v1 on cost grounds

**Status:** Accepted (deferred adoption)
**Date:** 2026-04-18

## Context

ArchLucid centralizes runtime configuration through `IConfiguration` (`appsettings.{Env}.json` + environment variables + `dotnet user-secrets` for local) and feature flags through `Microsoft.FeatureManagement` (`FeatureManagementFeatureFlags`, `FeatureManagementAuthorityPipelineModeResolver`). Secrets are intended to live in **Azure Key Vault** (`appsettings.KeyVault.sample.json`, `docs/runbooks/SECRET_AND_CERT_ROTATION.md`). Live-reload of resilience knobs already uses `IOptionsMonitor<T>` (`CircuitBreakerGate`, `CircuitBreakerGateOptionsMonitorTests`).

**Azure App Configuration** is a strong architectural fit for this stack:

- Native sink for `Microsoft.FeatureManagement` (targeting filters, percentage rollouts, time-windows).
- Native trigger for `IOptionsMonitor.OnChange` via sentinel-key refresh.
- Single keyspace with **labels** to collapse `appsettings.{Env}.json` sprawl.
- **Key Vault references** keep secret material in KV while exposing a single resolver.
- Built-in **revision history**, **snapshots**, and **Event Grid** change events that align with the audit posture in `GovernanceWorkflowService`.

We evaluated adoption in v1 and chose to **defer** it.

## Decision

**Do not adopt Azure App Configuration in v1.** Continue with:

- `appsettings.{Env}.json` + environment variables (`ARCHLUCID_*`) + `dotnet user-secrets` for local.
- `Microsoft.FeatureManagement` flags sourced from JSON / env (today's path).
- **Azure Key Vault** as the only managed secret store, accessed directly by host identities (no App Configuration intermediation).
- Resilience knobs reloaded through `IOptionsMonitor<T>` over the existing JSON + env providers.

## Reasoning

The blocker is **total cost of ownership for v1**, not technical fit:

| Cost element                                                | v1 estimate (USD) |
|-------------------------------------------------------------|-------------------|
| Standard SKU per store (~$1.20 / day)                       | ~$36 / month / store |
| **3 envs × 2 regions** (geo-replication for multi-region parity per `docs/REDIS_AND_MULTI_REGION.md`) | ~$216 / month |
| Private endpoint hours (per security default — port 445 / private endpoints rule) | added per region |
| Engineering time: provider wiring, identity, RBAC, Terraform module, runbook, rollback, tests | ~1–2 engineer-weeks |
| Operational learning + on-call training                     | ongoing |

For v1, the **same outcomes** (env-scoped config, secret indirection, flag toggles) are reachable through facilities **already in the budget**: JSON files, env vars, KV, and `IOptionsMonitor`. The Free SKU is **disqualified** because it omits **labels** and **geo-replication** — the two features that justify adoption in the first place.

## Alternatives considered

- **App Configuration Free SKU.** Rejected: no labels, no geo-replication, request quotas insufficient for fleet refresh patterns. Adopting Free now and migrating to Standard later doubles the integration work.
- **Replace JSON entirely with environment variables only.** Rejected: loses structure and complicates local dev for nested settings; current JSON layering is already understood by the team.
- **Self-hosted alternative** (e.g., `etcd`, Consul). Rejected: introduces a new operational primitive for marginal benefit and contradicts Azure-native default.
- **Use Key Vault as a config store** (`KeyVaultConfigurationProvider` for non-secret keys). Rejected: muddies the secret/non-secret boundary, makes audit noisy, and KV throttles aren't designed for routine config reads.

## Consequences

- **Positive — cost.** No new monthly Azure spend for config; secret cost stays in KV (already budgeted).
- **Positive — simplicity.** Local development continues to work fully offline. Inner loop is unaffected.
- **Negative — feature flag toggles still require a redeploy** (or an env-var change + restart). Percentage rollouts and per-tenant targeting are unavailable until adoption.
- **Negative — `appsettings.{Env}.json` sprawl persists.** Five files in `ArchLucid.Api` today (`Development`, `Staging`, `Production`, `Advanced`, `KeyVault.sample`, `Entra.sample`); discipline is required to keep them aligned.
- **Negative — config audit is partial.** Changes are auditable through git on the JSON files and through Terraform on env vars, but there is no real-time change-event stream the way App Configuration would emit via Event Grid.
- **Risk — dual-source flag drift if adopted later.** When App Configuration is adopted, all `Microsoft.FeatureManagement` keys must move at once. A staged migration creates "which provider wins" ambiguity.

## Revisit triggers

Re-open this decision when **any** of the following becomes true:

1. We need **per-tenant** or **percentage** rollouts of a feature flag (e.g., a new `IFindingEngine` plugin, a new pipeline mode, a UI experiment).
2. We need to change resilience or quota knobs (`CircuitBreakerGate*`, retry budgets) **without a redeploy** during an incident.
3. We adopt **multi-region active/active** beyond what `REDIS_AND_MULTI_REGION.md` covers and need geo-replicated config.
4. Compliance requires an **immutable, queryable change feed** of config mutations beyond git history.
5. The number of `appsettings.{Env}.json` keys per env exceeds ~50 with non-trivial drift between envs.
6. We onboard **enterprise customers** whose contracts require centralized config governance.

When the trigger fires, follow the migration plan in **`docs/AZURE_APP_CONFIGURATION_FUTURE_ADOPTION.md`** (created with this ADR).

## Compliance / security notes

- Deferral does **not** weaken the security posture. KV remains the only secret store; identities remain managed; `ProductionSafetyRules` still gates `DevelopmentBypass*`.
- When adopted, App Configuration **must** use private endpoints (per the workspace default port-445 / private-endpoint rule), `local_auth_enabled = false`, and **App Configuration Data Reader** role for runtime identities. No shared keys.

## Related

- ADR 0001 (Hosting roles) — config provider wiring is identical across `Api`, `Worker`, `Combined`.
- ADR 0011 (`ArchLucid:StorageProvider` — InMemory vs Sql) — same "single switch, simple defaults" philosophy.
- `docs/AZURE_APP_CONFIGURATION_FUTURE_ADOPTION.md` — migration plan when a revisit trigger fires.
- `docs/SECURITY.md` — secret handling and PII retention.
- `docs/runbooks/SECRET_AND_CERT_ROTATION.md` — Key Vault rotation.
