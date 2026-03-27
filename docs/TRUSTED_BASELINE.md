# Trusted baseline (49R pass 2 — Corrected 50R)

This document defines what the repo treats as **intentionally complete and demo-worthy** for the v1 foundation. It exists so bootstrap, demo seed, and docs stay honest when later-phase code is present but not baseline-complete.

## What is trusted (Category A)

- **Startup:** `Program.cs` — config load, optional `ISchemaBootstrapper` when `ArchiForge:StorageProvider` = `Sql`, **DbUp** over embedded `ArchiForge.Data/Migrations/*.sql`, optional demo seed, then the HTTP pipeline.
- **DbUp:** `ArchiForge.Data/Infrastructure/DatabaseMigrator.cs` — SQL Server only; SQLite connection strings skip DbUp (tests / local SQLite). Scripts are embedded from `Migrations\` with deterministic `NNN_Name.sql` ordering.
- **Canonical run detail:** `IRunDetailQueryService` / `RunDetailQueryService` — single aggregate for run, tasks, results, manifest, traces.
- **Compare (trusted):** Manifest compare by version (`ManifestsController`), agent result compare between runs (`RunComparisonController` + `IAgentResultDiffService`).
- **Governance workflow (trusted for demo):** Tables from `017_GovernanceWorkflow.sql`; seed creates approval, promotion, environment activations used by **governance preview** (`IGovernancePreviewService`).
- **Health:** `GET /health` — includes database connectivity when `IDbConnectionFactory` is configured.

## Optional / partial (Category B — not required for baseline success)

- **Export / consulting DOCX replay:** Some export paths and replay types are implemented; the seeded `RunExportRecords` row is a **history placeholder** (`ArchitectureAnalysis` / Markdown-style metadata) and is **not** required for export-replay baseline closure. Consulting DOCX export replay expects a different export type and audited JSON; treat as advanced, not part of the minimal demo proof.
- **End-to-end replay comparison, policy packs, alerts, retrieval, UI:** Code may exist; they are **not** part of the trusted baseline unless explicitly promoted in release notes.

## Demo seed contract

- **Service:** `IDemoSeedService` / `DemoSeedService` in `ArchiForge.Application/Bootstrap/`.
- **Config:** `Demo:Enabled`, `Demo:SeedOnStartup` (startup seed only in **Development**).
- **HTTP:** `POST /v1.0/demo/seed` when `Demo:Enabled` and Development.
- **Story:** Contoso Retail — **baseline** run `run-baseline-demo` vs **hardened** run `run-hardened-demo`, deterministic IDs in `ContosoRetailDemoIdentifiers`.
- **Idempotent:** Re-running seed skips existing keys.

## Proof checklist (local)

1. Configure `ConnectionStrings:ArchiForge` (SQL Server) and `ArchiForge:StorageProvider` = `Sql` if using the full SQL stack.
2. Start API → logs show schema bootstrap (if applicable), DbUp, optional demo seed.
3. `GET /health` → healthy.
4. `GET /v1/architecture/run/run-baseline-demo` (with auth) → 200 with manifest.
5. `GET /v1/architecture/run/compare/agents?leftRunId=run-baseline-demo&rightRunId=run-hardened-demo` → 200.
6. `GET /v1.0/architecture/manifest/compare?leftVersion=contoso-baseline-v1&rightVersion=contoso-hardened-v1` → 200 (adjust host/version as needed).

Later features may ship in the same repo; **this list** is the boundary for “Corrected 50R” foundation closure.
