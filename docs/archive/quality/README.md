# docs/archive/quality/ — historical quality and marketability assessments

> **Scope:** Demotion target for quality / marketability / correctness assessments older than the **current quarter**. This folder exists so the active `docs/` set stays small enough for a human to scan.

## Demotion policy

A `docs/*ASSESSMENT*.md` file moves here when **all** of the following are true:

1. A newer file in the **same family** (Marketability, Quality, Correctness) exists at the top level of `docs/`.
2. The file is **referenced for context only** by the newer file (i.e., it is not load-bearing for any operational runbook, ADR, or shipped CI script).
3. The file is older than 90 days **or** has been superseded by a release tagged after it.

When a file moves here:

- Update any internal links via `rg <old-path> -l` and rewrite to the new location.
- Add a one-line entry to this README naming the file and the date it was archived.
- Do **not** delete; keep the historical record so deltas can be reconstructed.

## What is **not** archived here

- ADRs (`docs/adr/`) — those have their own status field (`Accepted`, `Superseded by`).
- Runbooks (`docs/runbooks/`) — runbooks are operational; if one becomes stale, fix it or delete it deliberately.
- The most recent file in each assessment family — keep it on the top level.

## Archive log

| Date archived | File | Reason |
|---|---|---|
| 2026-04-21 | `QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md` | Superseded snapshots + link hygiene (Improvement 10); canonical narrative stays in active `docs/` entry points. |
| 2026-04-21 | `QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_75_37.md` | Same. |
| 2026-04-21 | `QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md` | Same. |
| 2026-04-21 | `QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_69_33.md` | Same. |
| 2026-04-21 | `QUALITY_IMPROVEMENT_DECISIONS_2026_04_20.md` | Same (decision log retained; inbound links rewritten). |

## Companion CI guards

- **Doc scope header (shipped 2026-04-20):** `scripts/ci/check_doc_scope_header.py` runs in `.github/workflows/ci.yml` after `check_doc_links.py` as a **merge-blocking** step. Active docs under `docs/` (excluding `docs/archive/`) must open with `> **Scope:**` (see `scripts/ci/backfill_doc_scope_headers.py` for the mechanical prepend). README HTML exception is documented in the checker script.
- **Stale assessment index (planned):** a non-blocking job named `docs-stale-assessment-warn` may later warn when more than one file from the same assessment family lives at the top level of `docs/`. Until that ships, demotion remains a manual hygiene step at the end of each release.
