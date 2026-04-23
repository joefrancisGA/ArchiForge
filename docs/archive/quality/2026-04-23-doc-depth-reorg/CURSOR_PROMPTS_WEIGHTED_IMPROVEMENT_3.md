> Archived 2026-04-23 — superseded by [docs/START_HERE.md](../START_HERE.md) and the current assessment pair under ``docs/``. Kept for audit trail.

> **Scope:** Cursor prompts — Weighted quality assessment Improvement 3 - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Cursor prompts — Weighted quality assessment Improvement 3

Improvement 3 in **`docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`** targets **cognitive load** and **evolvability**: one product solution file, no orphan rename debris, aligned legacy-config sunset messaging, and **no competing quality-assessment documents** in `docs/` root.

**Improvements 4–6** (and a session **verification bundle** for Improvement 3): see **[`CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md`](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md)**.

> **Note:** **`docs/CURSOR_PROMPTS_QUALITY_IMPROVEMENT_3.md`** is a different topic (k6 / CI load gates). Use this file for **rename + doc consolidation** prompts only.

---

## Prompt `rename-artifacts-single-sln`

Keep a single solution entry point:

1. Repo root must contain exactly one product solution: **`ArchLucid.sln`** (no **`ArchiForge.sln`**).
2. CI, scripts, and docs that reference a solution must use **`ArchLucid.sln`** — `grep -r "\.sln"` from repo root if a duplicate appears.
3. Run: `dotnet build ArchLucid.sln -c Release --nologo`

If a second `.sln` reappears, delete the duplicate or merge project entries, then re-run the build.

---

## Prompt `rename-artifacts-no-stub-archiforge-src`

Remove contributor confusion from orphan product paths:

1. There must be no **`ArchiForge/`** directory tree holding **`.cs`** sources under the repo root (stale copies of **`ArchLucid.*`**).
2. API REST client scratch file should be **`ArchLucid.Api/ArchLucid.Api.http`** (not **`ArchiForge.Api.http`**).
3. **Expected intentional** literals: **`scripts/ci/archiforge-rename-allowlist*.txt`**, historical docs, Terraform resource addresses (**`*.archiforge`**), RLS SQL object names — do not delete those.

If orphan sources exist, delete them after confirming equivalent types and tests live under **`ArchLucid.*`**; run `dotnet build ArchLucid.sln -c Release`.

---

## Prompt `legacy-config-sunset-constant`

Align legacy key warnings with a published sunset target:

1. Open **`ArchLucid.Host.Core/Configuration/ArchLucidLegacyConfigurationWarnings.cs`** — confirm **`LegacyConfigurationKeysHardEnforcementNoEarlierThan`** and the warning template reference **`docs/CONFIG_BRIDGE_SUNSET.md`**.
2. Open **`docs/CONFIG_BRIDGE_SUNSET.md`** — § Sunset timeline must match the same calendar date as the code constant.
3. When changing the date, update both files in one change set and add release-note intent if enforcement is planned.

Optional: grep **`ArchLucid.Api/Program.cs`** and **`ArchLucid.Worker/Program.cs`** for **`LogIfLegacyKeysPresent`** to ensure warnings still run after host build.

---

## Prompt `archive-superseded-quality-assessments`

Reduce doc sprawl and make the weighted assessment canonical:

1. **Canonical** living doc stays at **`docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`**.
2. Move superseded snapshots to **`docs/archive/`** (preserve git history with **`git mv`** where possible):
   - **`docs/QUALITY_ASSESSMENT.md`**
   - **`docs/QUALITY_ASSESSMENT_2026_04.md`**
   - **`docs/QUALITY_ASSESSMENT_2026_04_14.md`**
3. Replace each former path with a **short stub** (5–15 lines) that links to the matching archive file and to **`QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`**.
4. Update references in:
   - **`docs/QUALITY_IMPROVEMENT_PROMPTS.md`**
   - **`docs/QUALITY_IMPROVEMENT_PROMPTS_2026_04_14.md`**
   - **`docs/LIVE_E2E_AUTH_PARITY.md`**
   - **`docs/CURSOR_PROMPTS_SIX_QUALITY_IMPROVEMENTS.md`**
   - **`docs/ARCHLUCID_RENAME_CHECKLIST.md`** (session log row if your process uses it)
5. Add a table row in **`docs/archive/README.md`** listing the three archived quality files.
6. In **`docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`** § Cognitive Load, remove the “multiple quality assessment files” gap or mark it **mitigated** with a pointer to this prompt file.

---

## Objective / assumptions / constraints

| | |
|--|--|
| **Objective** | One `.sln`, clear rename boundaries, one canonical quality narrative in `docs/` root. |
| **Assumptions** | **`ArchLucid.sln`** is complete; CI already uses it. |
| **Constraints** | Do not modify historical SQL migrations **001–028**; Terraform **`state mv`** for resource renames stays deferred per **`docs/ARCHLUCID_RENAME_CHECKLIST.md`** **7.5–7.8**. |
