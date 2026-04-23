> Archived 2026-04-23 — superseded by [docs/START_HERE.md](../START_HERE.md) and the current assessment pair under ``docs/``. Kept for audit trail.

> **Scope:** Cursor prompts — canonical index (weighted improvements) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Cursor prompts — canonical index (weighted improvements)

Use this file as a **stable entry point** for paste-ready improvement prompts. Detailed assessment: **[QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md](archive/quality/2026-04-23-doc-depth-reorg/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md)** § *Six Best Improvements*.

## All prompt packs (single index)

| Pack | Purpose |
|------|---------|
| **This file** | Index + cross-links; start here. |
| **[QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md](archive/quality/2026-04-23-doc-depth-reorg/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md)** § *Cursor prompts for Improvements 1–6* | Improvements **1–2** (coverage, governance FsCheck, security guards, RBAC) — inline fenced blocks. |
| **[CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md)** | Rename, single `.sln`, legacy sunset, archive quality docs. |
| **[CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md)** | Rename verify bundle, traceability, orphan probe, finding narrative, wizard parity, RFC 9457 / Problem Details sweep. |
| **[CURSOR_PROMPTS_SIX_QUALITY_IMPROVEMENTS.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_SIX_QUALITY_IMPROVEMENTS.md)** | Six-quality execution prompts (historical bundle). |
| **[CURSOR_PROMPTS_QUALITY_IMPROVEMENT_3.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_QUALITY_IMPROVEMENT_3.md)** | k6 / performance CI prompts. |
| **[QUALITY_IMPROVEMENT_PROMPTS.md](library/QUALITY_IMPROVEMENT_PROMPTS.md)** | Older quality prompt inventory (superseded in part by weighted doc). |
| **[CURSOR_PROMPTS_BACKGROUND_TO_CONTAINER_JOBS.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_BACKGROUND_TO_CONTAINER_JOBS.md)** | Move background hosted services (advisory scan, archival, trial lifecycle, Cosmos change feed, Service Bus consumer, etc.) to **Azure Container Apps Jobs** (chosen over Functions on cost + private-endpoint grounds). Includes shared CLI runner, Terraform module, KEDA scalers, observability, rollback drill. |
| **[CURSOR_PROMPTS_LOGIC_APPS.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_LOGIC_APPS.md)** | Azure Logic Apps (Standard): governance approval routing, trial lifecycle email, marketplace fulfillment hand-off, incident ChatOps (**`LOGIC_APPS_INCIDENT_CHATOPS.md`** runbook), customer promotion notifications — plus **repo execution** notes (ADR 0019, Terraform root, Marketplace integration event). |
| **[CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md)** | **68.60%** assessment — primary **eight** improvements (reference customer, trial funnel, ROI bulletin, Marketplace/Stripe guards, `/why` PDF pack, pen-test/PGP trust, Teams, golden cohort). |
| **[CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60_ADDITIONAL.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60_ADDITIONAL.md)** | Same assessment — **eight follow-on** prompts **A–H** (strangler inventory + CI ceiling, board-pack PDF, task-success telemetry, pricing quote request, compliance journey page, procurement ZIP, per-run traceability ZIP, quarterly chaos game day). |

| Area | Document |
|------|----------|
| Improvements **1–2** (coverage, governance FsCheck, security guards, RBAC) | Inline fenced blocks in **[QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md](archive/quality/2026-04-23-doc-depth-reorg/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md)** § *Cursor prompts for Improvements 1–6* |
| Improvement **3** (rename, single `.sln`, legacy sunset, archive quality docs) | **[CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md)** |
| Improvements **3–6** (rename verify bundle, traceability, orphan probe, finding narrative, wizard parity, RFC 9457 / Problem Details sweep) | **[CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md)** |
| Logic Apps Standard (edge orchestration, human-in-the-loop) | **[CURSOR_PROMPTS_LOGIC_APPS.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_LOGIC_APPS.md)** |

**Related runbooks**

- **[DATABASE_MIGRATION_ROLLBACK.md](library/DATABASE_MIGRATION_ROLLBACK.md)** — manual SQL rollbacks (`ArchLucid.Persistence/Migrations/Rollback/`), greenfield baseline notes.
- **[API_ERROR_CONTRACT.md](library/API_ERROR_CONTRACT.md)** — Problem+JSON expectations for clients.
- **[CONTROLLER_AREA_MAP.md](library/CONTROLLER_AREA_MAP.md)** — logical API controller grouping + bulk endpoints.
