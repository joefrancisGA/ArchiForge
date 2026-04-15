# V1 requirements ↔ tests / scripts traceability

**Audience:** Engineers and operators who need a **lightweight** map from **[`V1_SCOPE.md`](V1_SCOPE.md)** to **evidence in this repo** (docs, automated tests, scripts).

**Status:** Living document. When **`V1_SCOPE.md`** changes, update the corresponding rows here.

**Not a substitute for:** Full requirements management tooling, 100% test enumeration, or contractual compliance matrices.

---

## Traceability matrix

| V1 reference (see V1_SCOPE) | Primary docs | Representative tests / automation | Example `dotnet test --filter` | Notes |
|-----------------------------|--------------|-----------------------------------|----------------------------------|--------|
| **2.1** Run lifecycle (request → execute → commit) | [`API_CONTRACTS.md`](API_CONTRACTS.md), [`ARCHITECTURE_FLOWS.md`](ARCHITECTURE_FLOWS.md) | `ArchLucid.Api.Tests` (architecture / run controllers), `ArchLucid.Persistence.Tests` (run + manifest repositories) | `FullyQualifiedName~ArchitectureController` OR `FullyQualifiedName~RunLifecycleStatePropertyTests` | Coordinator vs authority paths in [`DUAL_PIPELINE_NAVIGATOR.md`](DUAL_PIPELINE_NAVIGATOR.md). |
| **2.2** Manifest & artifact review | [`operator-shell.md`](operator-shell.md), [`CLI_USAGE.md`](CLI_USAGE.md) | API artifact/export tests under `ArchLucid.Api.Tests`; CLI tests under `ArchLucid.Cli.Tests` | `FullyQualifiedName~ExportsController` OR `FullyQualifiedName~ManifestSummaryServiceTests` | UI: `archlucid-ui` run detail + artifacts. |
| **2.3** Compare | [`COMPARISON_REPLAY.md`](COMPARISON_REPLAY.md), [`ARCHITECTURE_FLOWS.md`](ARCHITECTURE_FLOWS.md) | `ArchLucid.Api.Tests` comparison controllers; `ArchLucid.Decisioning.Tests` comparison logic | `FullyQualifiedName~ComparisonReplay` OR `FullyQualifiedName~ArchitectureEndToEndComparison` | Persisted **`ComparisonRecords`** — see orphan probe below. |
| **2.4** Replay | [`ARCHITECTURE_FLOWS.md`](ARCHITECTURE_FLOWS.md) | `ArchLucid.Api.Tests` (replay/compare); `ArchLucid.Decisioning.Tests` (replay logic) | `FullyQualifiedName~Replay` | Operator UI replay surfaces in `operator-shell.md`. |
| **2.5** Graph | [`KNOWLEDGE_GRAPH.md`](KNOWLEDGE_GRAPH.md) | `ArchLucid.Api.Tests` graph endpoints; persistence graph snapshot tests | `FullyQualifiedName~KnowledgeGraphServiceTests` OR `FullyQualifiedName~GraphValidatorTests` | Single-run exploration in UI. |
| **2.6** Export / packages | [`ARCHITECTURE_FLOWS.md`](ARCHITECTURE_FLOWS.md) | `ArchLucid.Api.Tests` export controllers; `ArchLucid.ArtifactSynthesis.Tests` where applicable | `FullyQualifiedName~ExportsController` OR `FullyQualifiedName~ArtifactSynthesis` | Background jobs: `JobsController` + `ArchLucid.Application.Jobs`. |
| **2.7** Deployability | [`CONTAINERIZATION.md`](CONTAINERIZATION.md), [`SQL_SCRIPTS.md`](SQL_SCRIPTS.md), [`BUILD.md`](../BUILD.md) | CI workflows; `ArchLucid.Persistence.Tests` migration/DbUp-related tests | `FullyQualifiedName~DatabaseMigration` OR `FullyQualifiedName~GreenfieldSqlBoot` | Terraform under `infra/`; customer topology varies. |
| **2.8** Supportability | [`API_CONTRACTS.md`](API_CONTRACTS.md) § correlation, [`PILOT_GUIDE.md`](PILOT_GUIDE.md) | `ArchLucid.Cli.Tests` (CLI); `ArchLucid.Api.Tests` (health endpoints) | `FullyQualifiedName~SupportBundle` OR `FullyQualifiedName~HealthEndpoint` | `X-Correlation-ID` and support bundle discipline. |
| **2.9** Pilot readiness | [`PILOT_GUIDE.md`](PILOT_GUIDE.md), [`RELEASE_SMOKE.md`](RELEASE_SMOKE.md) | `release-smoke.ps1`, `v1-rc-drill.ps1`, [`V1_RC_DRILL.md`](V1_RC_DRILL.md) | *(scripts — run `.\release-smoke.ps1` from repo root)* | Playwright may use mocks — see V1_SCOPE **out of scope** table. |
| **2.10** Optional features | [`INTEGRATION_EVENTS_AND_WEBHOOKS.md`](INTEGRATION_EVENTS_AND_WEBHOOKS.md), [`PRE_COMMIT_GOVERNANCE_GATE.md`](PRE_COMMIT_GOVERNANCE_GATE.md), [`ALERTS.md`](ALERTS.md) | `ArchLucid.Decisioning.Tests` governance FsCheck/property tests; integration tests behind traits | `FullyQualifiedName~GovernanceWorkflow` OR `FullyQualifiedName~AlertEvaluator` | Enable per environment; not every pilot runs all paths. |
| **§4 Happy path** | [`ONBOARDING_HAPPY_PATH.md`](ONBOARDING_HAPPY_PATH.md), [`operator-shell.md`](operator-shell.md) | `archlucid-ui` (wizard + E2E); `ArchLucid.Api.Tests` (architecture flows) | `FullyQualifiedName~NewRunWizardClient` OR `FullyQualifiedName~ArchitectureIngestion` | First-run wizard route **`/runs/new`**. |
| **§5 Release criteria** | [`TEST_STRUCTURE.md`](TEST_STRUCTURE.md), [`RELEASE_SMOKE.md`](RELEASE_SMOKE.md) | `.github/workflows/ci.yml`; `ArchLucid.Api.Tests` (`Suite=Core` traits) | `Suite=Core` | Exact CI job names live in `.github/workflows`. |

---

## Data consistency: comparison orphans (archival / missing runs)

| Concern | Implementation | Operator / SRE evidence |
|--------|----------------|-------------------------|
| **Detection** | `ArchLucid.Host.Core.Hosted.DataConsistencyOrphanProbeHostedService` | Logs + counter **`archlucid_data_consistency_orphans_detected_total`** (labels **`table`**: **`ComparisonRecords`**, **`GoldenManifests`**, **`FindingsSnapshots`**; **`column`**: **`LeftRunId`** / **`RightRunId`** / **`RunId`**) |
| **Alerting** | [`infra/prometheus/archlucid-alerts.yml`](../infra/prometheus/archlucid-alerts.yml) § `archlucid-data-consistency` | Tune `for:` / thresholds per environment |
| **Remediation** | Admin API: `POST .../orphan-comparison-records`, `POST .../orphan-golden-manifests`, `POST .../orphan-findings-snapshots` — each supports `dryRun=true` first, then `dryRun=false` (cap 500 rows) + durable audit | [`runbooks/COMPARISON_RECORD_ORPHAN_REMEDIATION.md`](runbooks/COMPARISON_RECORD_ORPHAN_REMEDIATION.md) (comparison); golden-manifest remediation deletes `dbo.ArtifactBundles` first. |

**Soft archive note:** `ArchiveRunsCreatedBeforeAsync` sets **`ArchivedUtc`** on **`dbo.Runs`**; runs **remain** present. The orphan probe targets rows whose **GUID run id does not exist** in **`dbo.Runs`** (hard delete or inconsistency), not merely archived runs.

---

## Related documents

| Doc | Use |
|-----|-----|
| [`V1_SCOPE.md`](V1_SCOPE.md) | Source contract for rows above |
| [`QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`](QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md) | Weighted gaps and improvement targets |
| [`CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md`](CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md) | Paste-ready prompts for this workstream |
