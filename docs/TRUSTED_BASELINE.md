# Trusted baseline (49R pass 2 — Corrected 50R, Corrected 51R discipline)

This document defines what the repo treats as **intentionally complete and demo-worthy** for the v1 foundation. It exists so bootstrap, demo seed, and docs stay honest when later-phase code is present but not baseline-complete.

## Corrected 51R — actor, auth, mutation audit (baseline only)

- **Actor resolution:** Application code resolves the acting principal via **`IActorContext`** (namespace **`ArchLucid.Application`**, source under `ArchLucid.Application/Common/`), backed by **`IHttpContextAccessor`**. When no identity name is present, the fallback is the non-empty string **`api-user`** (not an empty string). Baseline services should use **`IActorContext`** instead of reading **`HttpContext.User`** directly.
- **Auth on baseline controllers:** **`RunsController`**, **`GovernanceController`**, and **`RunComparisonController`** use the existing **`ReadAuthority`** / **`ExecuteAuthority`** policy split consistent with **`ArchLucidPolicies`**. Controllers outside this set may still differ until intentionally aligned.
- **Mutation audit (logging, not SQL `AuditEvents`):** Successful and failed **trusted baseline mutations** emit structured **`ILogger`** lines via **`IBaselineMutationAuditService`** / **`BaselineMutationAuditService`**. This is separate from **`ArchLucid.Core.Audit.IAuditService`**, which persists to the database for other scenarios. Event type strings live in **`ArchLucid.Core.Audit.AuditEventTypes.Baseline`** (e.g. **`Baseline.Architecture.RunStarted`**, **`Baseline.Architecture.RunCompleted`**, **`Baseline.Governance.ManifestPromoted`**). Failed or blocked operations must not log a **success** event; blocked commits and merge failures emit **`Baseline.Architecture.RunFailed`** with a short reason.

## What is trusted (Category A)

- **Startup:** `Program.cs` — config load, optional `ISchemaBootstrapper` when `ArchLucid:StorageProvider` = `Sql`, **DbUp** over embedded `ArchLucid.Persistence/Migrations/*.sql`, optional demo seed, then the HTTP pipeline.
- **DbUp:** `ArchLucid.Persistence/Data/Infrastructure/DatabaseMigrator.cs` — applies embedded **`ArchLucid.Persistence/Migrations/*.sql`** when `ConnectionStrings:ArchLucid` is set, with deterministic `NNN_Name.sql` ordering.
- **Canonical run detail:** `IRunDetailQueryService` / `RunDetailQueryService` — single aggregate for run, tasks, results, manifest, traces.
- **Compare (trusted):** Manifest compare by version (`ManifestsController`), agent result compare between runs (`RunComparisonController` + `IAgentResultDiffService`).
- **Governance workflow (trusted for demo):** Tables from `017_GovernanceWorkflow.sql`; seed creates approval, promotion, environment activations used by **governance preview** (`IGovernancePreviewService`).
- **Health:** `GET /health` — includes database connectivity when `IDbConnectionFactory` is configured.

## Optional / partial (Category B — not required for baseline success)

- **Export / consulting DOCX replay:** Some export paths and replay types are implemented; the seeded `RunExportRecords` row is a **history placeholder** (`ArchitectureAnalysis` / Markdown-style metadata) and is **not** required for export-replay baseline closure. Consulting DOCX export replay expects a different export type and audited JSON; treat as advanced, not part of the minimal demo proof.
- **End-to-end replay comparison, policy packs, alerts, retrieval, UI:** Code may exist; they are **not** part of the trusted baseline unless explicitly promoted in release notes.

## Demo seed contract

- **Service:** `IDemoSeedService` / `DemoSeedService` in `ArchLucid.Application/Bootstrap/`.
- **Config:** `Demo:Enabled`, `Demo:SeedOnStartup` (startup seed only in **Development**).
- **HTTP:** `POST /v1.0/demo/seed` when `Demo:Enabled` and Development.
- **Story:** Contoso Retail — **baseline** vs **hardened** runs: the **development default tenant** (`ScopeIds.DefaultTenant`) keeps stable GUID PKs from `ContosoRetailDemoIdentifiers` / coordinator **`N`** keys (`6e8c4a102b1f4c9a9d3e10b2a4f0c501`, `…502`). **Additional tenants** in the same catalog use `ContosoRetailDemoIds.ForTenant(tenantId)` so global PKs (`dbo.Runs`, `dbo.ArchitectureRequests`, `dbo.AgentTasks`, …) do not collide across self-service registrations. Seed still writes **`dbo.Runs`** and coordinator artifacts only (migration **049** dropped **`dbo.ArchitectureRuns`**; ADR **0012** complete).
- **Idempotent:** Re-running seed skips existing keys.

## Proof checklist (local)

1. Configure `ConnectionStrings:ArchLucid` (SQL Server) and `ArchLucid:StorageProvider` = `Sql` if using the full SQL stack.
2. Start API → logs show schema bootstrap (if applicable), DbUp, optional demo seed.
3. `GET /health` → healthy.
4. `GET /v1/architecture/run/6e8c4a102b1f4c9a9d3e10b2a4f0c501` (with auth) → 200 with manifest.
5. `GET /v1/architecture/run/compare/agents?leftRunId=6e8c4a102b1f4c9a9d3e10b2a4f0c501&rightRunId=6e8c4a102b1f4c9a9d3e10b2a4f0c502` → 200.
6. `GET /v1.0/architecture/manifest/compare?leftVersion=contoso-baseline-v1&rightVersion=contoso-hardened-v1` → 200 (adjust host/version as needed).

Later features may ship in the same repo; **this list** is the boundary for “Corrected 50R” foundation closure.
