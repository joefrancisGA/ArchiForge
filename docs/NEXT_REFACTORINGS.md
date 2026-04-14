# Next refactorings

**Last updated:** 14 April 2026.

**Where to start:** [START_HERE.md — What to open first](START_HERE.md#what-to-open-first-contributor-decision-tree) (Mermaid + table).

## Archive (full historical backlog)

The **complete** numbered backlog (§8–§342, batch checklists, and completed batch logs through 2026-04-14) is preserved verbatim in:

**[`docs/archive/NEXT_REFACTORINGS_ARCHIVE_2026_04_15.md`](archive/NEXT_REFACTORINGS_ARCHIVE_2026_04_15.md)**

Use that file when you need the original write-ups for items already marked done in checklists, or for copy-paste context when reviving a deferred idea. **This page** stays short so new contributors are not confronted with 2k+ lines at the front door.

## Active items (prioritized)

| # | Topic | Status / next step |
|---|--------|---------------------|
| **341** | Connection factory alignment (`ISqlConnectionFactory` vs sync `IDbConnectionFactory`) | Open — see archive §“Data vs Persistence consolidation (339–341)”. |
| **7.5–7.8** | Rename / Terraform / repo path | Deferred with program approval — runbook: [`docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md`](runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md); rationale: [`RENAME_DEFERRED_RATIONALE.md`](RENAME_DEFERRED_RATIONALE.md). |

## Contracts note (unchanged)

Move heavy **service interfaces** out of **`ArchLucid.Contracts`** into owning assemblies when team boundaries justify churn; keep DTOs in **Contracts**. See **ADR 0013** (`docs/adr/0013-api-versioning-and-json-schema-versioning.md`).

## How to add a new backlog item

1. Add a short row to **Active items** above **or** a focused new section with problem / change / outcome.
2. If the write-up is long, add the full text under **`docs/archive/`** and link it here.
3. Update **`docs/ARCHLUCID_RENAME_CHECKLIST.md`** progress log if the work touches rename or Terraform.
