> **Scope:** Code map (where to open first) - full detail, tables, and links in the sections below.

# Code map (where to open first)

## 1. Objective

Reduce time-to-orientation for a developer or SRE by listing **high-signal paths** aligned to **interfaces → services → data → orchestration**.

## 2. Assumptions

- You build with **.NET 10** and **C#**; UI with **Next.js** under `archlucid-ui/`.

## 3. Constraints

- This map is **not** exhaustive; grep and `docs/DI_REGISTRATION_MAP.md` fill gaps.
- **Change checklist (controller → app → SQL → audit):** [GOLDEN_CHANGE_PATH.md](GOLDEN_CHANGE_PATH.md).

## 4. Architecture overview

**Flow:** `ArchLucid.Api` / `ArchLucid.Worker` → `Host.Composition` (DI) → `Application` + `Persistence` → SQL / Azure services.

## 5. Component breakdown

| Concern | Path |
|---------|------|
| API startup | `ArchLucid.Api/Program.cs`, `ArchLucid.Api/Startup/` |
| Auth + ArchLucid bridge | `ArchLucid.Api/Auth/`, `ArchLucid.Api/Configuration/ArchLucidAuthConfigurationBridge.cs` |
| Config merge (storage + auth keys) | `ArchLucid.Host.Core/Configuration/ArchLucidConfigurationBridge.cs` |
| Storage + repository registration | `ArchLucid.Host.Composition/Configuration/ArchLucidStorageServiceCollectionExtensions.cs` |
| Feature DI slices | `ArchLucid.Host.Composition/Startup/ServiceCollectionExtensions.*.cs` |
| Outbox operational metrics | `ArchLucid.Persistence/Diagnostics/DapperOutboxOperationalMetricsReader.cs`, `ArchLucid.Host.Core/Hosted/OutboxOperationalMetricsHostedService.cs` |
| OTel meters / gauges | `ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs` |
| SQL schema (master) | `ArchLucid.Persistence/Scripts/ArchLucid.sql` |
| UI API proxy | `archlucid-ui/src/app/api/proxy/[...path]/route.ts` |
| CD smoke + rollback | `.github/workflows/cd.yml`, `cd-staging-on-merge.yml` |
| ZAP baseline (blocking) | `infra/zap/baseline-pr.tsv`, `.github/workflows/ci.yml` (`security-zap-api-baseline`), `zap-baseline-strict-scheduled.yml` |
| Prometheus alerts | `infra/prometheus/archlucid-alerts.yml` |

## 6. Data flow

- **HTTP request:** Middleware → controller → application service → persistence repository → SQL.
- **Background:** Worker hosted services → outbox readers/processors → SQL / Service Bus.

## 7. Security model

- Policy: `ArchLucid.Api` authorization policies and `[Authorize]` usage; see `InfrastructureExtensions` for global auth notes.

## 8. Operational considerations

- **Health:** `ArchLucid.Host.Core/Health` and standard `MapHealthChecks` wiring.
- **Diagnostics:** admin outbox endpoints (versioned under `/v1/admin/...`) and build info `ArchLucid.Core/Diagnostics/BuildInfoResponse.cs`.
