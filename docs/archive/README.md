# docs/archive — historical design-session logs

This folder contains **historical implementation records** and superseded notes. Files here are **not maintained** for accuracy against the current codebase (class names, tables, and flows may be outdated). They are kept for archaeological reference.

**Current documentation** starts at the **canonical Day-1 docs** — **[`../onboarding/day-one-developer.md`](../onboarding/day-one-developer.md)** (developer), **[`../onboarding/day-one-sre.md`](../onboarding/day-one-sre.md)** (SRE), **[`../onboarding/day-one-security.md`](../onboarding/day-one-security.md)** (security), **[`../OPERATOR_QUICKSTART.md`](../library/OPERATOR_QUICKSTART.md)** (operator commands) — plus a short hub at **[`../START_HERE.md`](../START_HERE.md)**.

These change-set files are **immutable historical records** where noted. They capture incremental prompt logs, deferred-backlog decisions, and exact delivery scope for each change set as it was produced.

Do **not** edit archived change-set bodies. If a decision changes, write a new ADR or add a new CHANGELOG entry.

| File | What it covers |
|------|---------------|
| [CHANGE_SET_55R_SUMMARY.md](CHANGE_SET_55R_SUMMARY.md) | Operator shell coherence — nav, breadcrumbs, compare, artifact review |
| [CHANGE_SET_56R.md](CHANGE_SET_56R.md) | Release-candidate hardening — config validation, health, `/version`, pilot docs, support bundle, local scripts |
| [CHANGE_SET_57R.md](CHANGE_SET_57R.md) | Operator-journey E2E — Playwright harness, loopback mock server, `-RunPlaywright` flag |
| [CHANGE_SET_58R.md](CHANGE_SET_58R.md) | Product learning dashboard — pilot signals, aggregation, triage API, operator UI |
| [CHANGE_SET_59R.md](CHANGE_SET_59R.md) | Learning-to-planning bridge — themes, plans, SQL + contract foundation |
| [QUALITY_ASSESSMENT.md](QUALITY_ASSESSMENT.md) | Historical product quality snapshot (superseded; canonical: `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`) |
| [QUALITY_ASSESSMENT_2026_04.md](QUALITY_ASSESSMENT_2026_04.md) | Historical quality snapshot — April 2026 |
| [QUALITY_ASSESSMENT_2026_04_14.md](QUALITY_ASSESSMENT_2026_04_14.md) | Historical quality snapshot — 2026-04-14 |
| [ONBOARDING_START_HERE_2026_04_17.md](ONBOARDING_START_HERE_2026_04_17.md) | Superseded long-form **START_HERE** (pre–onboarding consolidation) |
| [ONBOARDING_GOLDEN_PATH_2026_04_17.md](ONBOARDING_GOLDEN_PATH_2026_04_17.md) | Superseded **GOLDEN_PATH** (environment sequencing) |
| [ONBOARDING_GOLDEN_CHANGE_PATH_2026_04_17.md](ONBOARDING_GOLDEN_CHANGE_PATH_2026_04_17.md) | Superseded **GOLDEN_CHANGE_PATH** |
| [ONBOARDING_HAPPY_PATH_2026_04_17.md](ONBOARDING_HAPPY_PATH_2026_04_17.md) | Superseded **ONBOARDING_HAPPY_PATH** (single-request walkthrough) |
| [ONBOARDING_PILOT_GUIDE_2026_04_17.md](ONBOARDING_PILOT_GUIDE_2026_04_17.md) | Superseded **PILOT_GUIDE** narrative |
| [ONBOARDING_CONTRIBUTOR_ONBOARDING_2026_04_17.md](ONBOARDING_CONTRIBUTOR_ONBOARDING_2026_04_17.md) | Superseded **CONTRIBUTOR_ONBOARDING** |

For a summarised, navigable view of all releases use **[docs/CHANGELOG.md](../CHANGELOG.md)**.
