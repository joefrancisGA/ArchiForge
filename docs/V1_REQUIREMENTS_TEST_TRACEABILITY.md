> **Scope:** V1 requirements ↔ tests / scripts traceability - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# V1 requirements ↔ tests / scripts traceability

**Audience:** Engineers and operators who need a **lightweight** map from **[`V1_SCOPE.md`](library/V1_SCOPE.md)** to **evidence in this repo** (docs, automated tests, scripts).

**Status:** Living document. When **`V1_SCOPE.md`** changes, update the corresponding rows here.

**Not a substitute for:** Full requirements management tooling, 100% test enumeration, or contractual compliance matrices.

**Last reviewed:** 2026-04-17 (integration outbox NFR row).

---

## Traceability matrix

| V1 reference (see V1_SCOPE) | Primary docs | Representative tests / automation | Example `dotnet test --filter` | Notes |
|-----------------------------|--------------|-----------------------------------|----------------------------------|--------|
| **2.1** Run lifecycle (request → execute → commit) | [`API_CONTRACTS.md`](library/API_CONTRACTS.md), [`ARCHITECTURE_FLOWS.md`](library/ARCHITECTURE_FLOWS.md) | `ArchLucid.Api.Tests` (architecture / run controllers), `ArchLucid.Persistence.Tests` (run + manifest repositories) | `FullyQualifiedName~ArchitectureController` OR `FullyQualifiedName~RunLifecycleStatePropertyTests` | Operator pipeline overview in [`CANONICAL_PIPELINE.md`](library/CANONICAL_PIPELINE.md). |
| **2.2** Manifest & artifact review | [`operator-shell.md`](library/operator-shell.md), [`CLI_USAGE.md`](library/CLI_USAGE.md) | API artifact/export tests under `ArchLucid.Api.Tests`; CLI tests under `ArchLucid.Cli.Tests` | `FullyQualifiedName~ExportsController` OR `FullyQualifiedName~ManifestSummaryServiceTests` | UI: `archlucid-ui` run detail + artifacts. |
| **2.3** Compare | [`COMPARISON_REPLAY.md`](library/COMPARISON_REPLAY.md), [`ARCHITECTURE_FLOWS.md`](library/ARCHITECTURE_FLOWS.md) | `ArchLucid.Api.Tests` comparison controllers; `ArchLucid.Decisioning.Tests` comparison logic | `FullyQualifiedName~ComparisonReplay` OR `FullyQualifiedName~ArchitectureEndToEndComparison` | Persisted **`ComparisonRecords`** — see orphan probe below. |
| **2.4** Replay | [`ARCHITECTURE_FLOWS.md`](library/ARCHITECTURE_FLOWS.md) | `ArchLucid.Api.Tests` (replay/compare); `ArchLucid.Decisioning.Tests` (replay logic) | `FullyQualifiedName~Replay` | Operator UI replay surfaces in `operator-shell.md`. |
| **2.5** Graph | [`KNOWLEDGE_GRAPH.md`](library/KNOWLEDGE_GRAPH.md) | `ArchLucid.Api.Tests` graph endpoints; persistence graph snapshot tests | `FullyQualifiedName~KnowledgeGraphServiceTests` OR `FullyQualifiedName~GraphValidatorTests` | Single-run exploration in UI. |
| **2.6** Export / packages | [`ARCHITECTURE_FLOWS.md`](library/ARCHITECTURE_FLOWS.md) | `ArchLucid.Api.Tests` export controllers; `ArchLucid.ArtifactSynthesis.Tests` where applicable | `FullyQualifiedName~ExportsController` OR `FullyQualifiedName~ArtifactSynthesis` | Background jobs: `JobsController` + `ArchLucid.Application.Jobs`. |
| **2.7** Deployability | [`CONTAINERIZATION.md`](library/CONTAINERIZATION.md), [`SQL_SCRIPTS.md`](library/SQL_SCRIPTS.md), [`BUILD.md`](library/BUILD.md) | CI workflows; `ArchLucid.Persistence.Tests` migration/DbUp-related tests | `FullyQualifiedName~DatabaseMigration` OR `FullyQualifiedName~GreenfieldSqlBoot` | Terraform under `infra/`; customer topology varies. |
| **2.8** Supportability | [`API_CONTRACTS.md`](library/API_CONTRACTS.md) § correlation, [`PILOT_GUIDE.md`](library/PILOT_GUIDE.md) | `ArchLucid.Cli.Tests` (CLI); `ArchLucid.Api.Tests` (health endpoints) | `FullyQualifiedName~SupportBundle` OR `FullyQualifiedName~HealthEndpoint` | `X-Correlation-ID` and support bundle discipline. |
| **2.9** Pilot readiness | [`PILOT_GUIDE.md`](library/PILOT_GUIDE.md), [`RELEASE_SMOKE.md`](library/RELEASE_SMOKE.md) | `release-smoke.ps1`, `v1-rc-drill.ps1`, [`V1_RC_DRILL.md`](library/V1_RC_DRILL.md) | *(scripts — run `.\release-smoke.ps1` from repo root)* | Playwright may use mocks — see V1_SCOPE **out of scope** table. |
| **2.10** Optional features | [`INTEGRATION_EVENTS_AND_WEBHOOKS.md`](library/INTEGRATION_EVENTS_AND_WEBHOOKS.md), [`PRE_COMMIT_GOVERNANCE_GATE.md`](library/PRE_COMMIT_GOVERNANCE_GATE.md), [`ALERTS.md`](library/ALERTS.md) | `ArchLucid.Decisioning.Tests` governance FsCheck/property tests; integration tests behind traits | `FullyQualifiedName~GovernanceWorkflow` OR `FullyQualifiedName~AlertEvaluator` | Enable per environment; not every pilot runs all paths. |
| **§4 Happy path** | [`ONBOARDING_HAPPY_PATH.md`](library/ONBOARDING_HAPPY_PATH.md), [`operator-shell.md`](library/operator-shell.md) | `archlucid-ui` (wizard + E2E); `ArchLucid.Api.Tests` (architecture flows) | `FullyQualifiedName~NewRunWizardClient` OR `FullyQualifiedName~ArchitectureIngestion` | First-run wizard route **`/runs/new`**. |
| **§5 Release criteria** | [`TEST_STRUCTURE.md`](library/TEST_STRUCTURE.md), [`RELEASE_SMOKE.md`](library/RELEASE_SMOKE.md) | `.github/workflows/ci.yml`; `ArchLucid.Api.Tests` (`Suite=Core` traits) | `Suite=Core` | Exact CI job names live in `.github/workflows`. |

---

## Non-functional traceability (implicit V1 gates)

V1_SCOPE emphasizes functional clauses **2.1–2.10**; the rows below map **cross-cutting** NFR themes to **existing** docs and tests so reliability/security/performance are not only implied by **2.7–2.9**.

| NFR theme | Primary docs | Representative tests / automation | Notes |
|-----------|----------------|-------------------------------------|--------|
| **Reliability** | [`RESILIENCE_CONFIGURATION.md`](library/RESILIENCE_CONFIGURATION.md), [`DEGRADED_MODE.md`](library/DEGRADED_MODE.md), [`OBSERVABILITY.md`](library/OBSERVABILITY.md) | `Suite=Core` persistence + outbox tests; `FullyQualifiedName~CircuitBreaker`; `FullyQualifiedName~IntegrationEventOutbox`; `FullyQualifiedName~DualPersistenceRowReconciliation` | Circuit breakers, SQL open retries, outbox convergence, dual-write reconciliation — not a formal SRE error budget in V1 RTM. |
| **Integration event outbox (async delivery)** | [`API_SLOS.md`](library/API_SLOS.md) § Outbox convergence, [`INTEGRATION_EVENT_CATALOG.md`](library/INTEGRATION_EVENT_CATALOG.md) | `FullyQualifiedName~IntegrationEventOutboxProcessorTests`; `FullyQualifiedName~IntegrationEventOutbox`; hosted **`IntegrationEventOutboxHostedService`** | **SLI:** recording series **`archlucid:slo:integration_event_outbox_oldest_age_seconds`** (see **`infra/prometheus/archlucid-slo-rules.yml`**); **alert:** `ArchLucidIntegrationEventOutboxConvergenceSlow` in **`infra/prometheus/archlucid-alerts.yml`**. **Terraform (optional):** managed Prometheus rules mirror depth in **`infra/terraform-monitoring/prometheus_slo_rules.tf`** (`ArchLucidSloOutboxDepthCriticalTf`). |
| **Security** | [`SECURITY.md`](library/SECURITY.md), [`SYSTEM_THREAT_MODEL.md`](security/SYSTEM_THREAT_MODEL.md), [`MULTI_TENANT_RLS.md`](security/MULTI_TENANT_RLS.md) | `ArchLucid.Host.Composition.Tests` (`AuthSafetyGuardTests`, `ArchLucidAuthorizationPoliciesRegistrationTests`); `FullyQualifiedName~RlsArchLucidScope` | Auth defaults, RBAC, RLS — pilot auth modes in **2.9** overlap but do not replace threat-model review. |
| **Performance / capacity** | [`PERFORMANCE_TESTING.md`](library/PERFORMANCE_TESTING.md), [`LOAD_TEST_BASELINE.md`](library/LOAD_TEST_BASELINE.md), [`CAPACITY_AND_COST_PLAYBOOK.md`](library/CAPACITY_AND_COST_PLAYBOOK.md) | `.github/workflows/ci.yml` (`k6-smoke-api`, `k6-ci-smoke`); `tests/load/*.js` | Merge-blocking k6 thresholds are environment-tuned; V1_SCOPE does not mandate universal perf benchmarks. |

---

## Data consistency: comparison orphans (archival / missing runs)

| Concern | Implementation | Operator / SRE evidence |
|--------|----------------|-------------------------|
| **Detection** | `ArchLucid.Host.Core.Hosted.DataConsistencyOrphanProbeHostedService` | Logs + counter **`archlucid_data_consistency_orphans_detected_total`** (labels **`table`**: **`ComparisonRecords`**, **`GoldenManifests`**, **`FindingsSnapshots`**; **`column`**: **`LeftRunId`** / **`RightRunId`** / **`RunId`**). Optional **`DataConsistency:OrphanProbeRemediationDryRunLogMaxRows`** (1–500): same **`SELECT`** as admin dry-run, **Information** log of sample ids only — no **`DELETE`**. |
| **Alerting** | [`infra/prometheus/archlucid-alerts.yml`](../infra/prometheus/archlucid-alerts.yml) § `archlucid-data-consistency` | Tune `for:` / thresholds per environment |
| **Remediation** | Admin API: `POST .../orphan-comparison-records`, `POST .../orphan-golden-manifests`, `POST .../orphan-findings-snapshots` — each supports `dryRun=true` first, then `dryRun=false` (cap 500 rows) + durable audit | [`runbooks/COMPARISON_RECORD_ORPHAN_REMEDIATION.md`](runbooks/COMPARISON_RECORD_ORPHAN_REMEDIATION.md) (comparison); golden-manifest remediation deletes `dbo.ArtifactBundles` first. |

**Soft archive note:** `ArchiveRunsCreatedBeforeAsync` sets **`ArchivedUtc`** on **`dbo.Runs`**; runs **remain** present. The orphan probe targets rows whose **GUID run id does not exist** in **`dbo.Runs`** (hard delete or inconsistency), not merely archived runs.

---

## Related documents

| Doc | Use |
|-----|-----|
| [`V1_SCOPE.md`](library/V1_SCOPE.md) | Source contract for rows above |
| [`QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`](archive/quality/2026-04-23-doc-depth-reorg/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md) | Weighted gaps and improvement targets |
| [`CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md`](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md) | Paste-ready prompts for this workstream |
