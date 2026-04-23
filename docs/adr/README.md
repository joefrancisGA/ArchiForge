> **Scope:** Architecture Decision Records (ADR) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Architecture Decision Records (ADR)

**Last reviewed:** 2026-04-22

Short, durable decisions for ArchLucid. Each file is **immutable** once accepted; supersede with a new ADR rather than rewriting history.

| ADR | Title |
|-----|--------|
| [0001](0001-hosting-roles-api-worker-combined.md) | Hosting roles: Api, Worker, Combined |
| [0002](0002-dual-persistence-architecture-runs-and-runs.md) | Dual persistence (historical — **Superseded** by 0012) |
| [0003](0003-sql-rls-session-context.md) | SQL RLS and SESSION_CONTEXT |
| [0004](0004-transactional-outbox-retrieval-indexing.md) | Transactional outbox for retrieval indexing |
| [0005](0005-llm-completion-pipeline.md) | LLM completion pipeline, cache, quota, metrics |
| [0006](0006-url-path-api-versioning.md) | URL-path API versioning (`/v1`) |
| [0007](0007-effective-governance-merge.md) | Effective governance merge (policy pack resolution) |
| [0008](0008-alert-dedupe-scopes.md) | Alert deduplication scopes |
| [0009](0009-digest-delivery-failure-semantics.md) | Digest delivery failure semantics |
| [0010](0010-dual-manifest-trace-repository-contracts.md) | Dual manifest and decision-trace repository contracts |
| [0011](0011-inmemory-vs-sql-storage-provider.md) | `ArchLucid:StorageProvider` — InMemory vs Sql |
| [0012](0012-runs-authority-convergence-write-freeze.md) | Runs convergence — legacy table removal (**Completed** 2026-04-12) |
| [0013](0013-api-versioning-and-json-schema-versioning.md) | API versioning (Asp.Versioning) + JSON **`schemaVersion`** on aggregates |
| [0014](0014-trial-enforcement-boundary.md) | Trial enforcement — server-side gate, run UoW increment, idempotent seats |
| [0015](0015-trial-tier-authentication-model.md) | Trial-tier authentication — External ID (MSA/Google) + optional local email/password |
| [0016](0016-billing-provider-abstraction.md) | Billing provider abstraction — Stripe + Azure Marketplace + SQL idempotency |
| [0017](0017-azure-app-configuration-deferred.md) | Azure App Configuration — **deferred** for v1 on cost grounds (companion: [`AZURE_APP_CONFIGURATION_FUTURE_ADOPTION.md`](../library/AZURE_APP_CONFIGURATION_FUTURE_ADOPTION.md)) |
| [0018](0018-background-workloads-container-apps-jobs.md) | Background workloads — **Container Apps Jobs** + `ArchLucid.Jobs.Cli` (not Functions); offload manifest + first job `advisory-scan` |
| [0019](0019-logic-apps-standard-edge-orchestration.md) | Azure Logic Apps (Standard) — narrow edge orchestration + human-in-the-loop; complements ADR 0016 / 0018 |
| [0020](0020-azure-primary-platform-permanent.md) | **Azure** as primary and permanent platform — narrative + ops alignment (not multi-cloud hedge) |
| [0021](0021-coordinator-pipeline-strangler-plan.md) | Coordinator pipeline strangler plan — phased retirement of the Coordinator interface family (**Status: Accepted**); Phase 3 code deletion merge-blocked until exit gates in ADR 0021 § Phase 3 |
| [0022](0022-coordinator-phase3-deferred.md) | Phase 3 coordinator retirement **blocked** (2026-04-21) — failed gates: (iv) parity TBD; Phase 2 `AuditEventTypes.Run` catalog not found — see ADR |
| [0024](0024-azure-devops-pipeline-task-parity-with-github-action.md) | Azure DevOps pipeline YAML parity with GitHub Actions — manifest delta job summary + sticky PR thread + PR status (**Status: Accepted**) |
| [0027](0027-demo-preview-cached-anonymous-commit-page.md) | Cached anonymous marketing **`GET /v1/demo/preview`** + **`/demo/preview`** page (**Status: Accepted**) |
| [0030](0030-coordinator-authority-pipeline-unification.md) | Coordinator → Authority pipeline unification (golden manifest / PR sequencing) |
| [0031](0031-cross-tenant-pattern-library.md) | Cross-tenant pattern library — anonymised vertical guidance (**Status: Proposed** — owner sign-off pending) |

**When to add an ADR:** Cross-cutting choice affecting security, data, or ops; multiple valid alternatives; cost of reversal is high.

**Numbering rule:** Next ADR gets the next sequential number. Never reuse a number; never share a number between two files.
