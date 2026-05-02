> **Scope:** Buyer-safe pilot evidence capture — anonymized fields, no fabricated numerics, cross-links to PMF tracker and ROI model; internal and NDA channels only unless policy says otherwise.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md).

# Pilot evidence — buyer-safe capture template

**Audience:** Pilot leads filling **Pilot A / B / …** rows in [PMF_VALIDATION_TRACKER.md](PMF_VALIDATION_TRACKER.md) and sponsor packs in [PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md) / [PROOF_OF_VALUE_SNAPSHOT.md](../library/PROOF_OF_VALUE_SNAPSHOT.md).

**Rules:**

- Use **Pilot A**, **Pilot B** identifiers in shared docs — map to real programs only in restricted systems (CRM, charter).
- **Do not** invent **Baseline** or **Result** numerics. Use **TBD** until measured, **See scorecard** for qualitative capture per [PMF_VALIDATION_TRACKER.md](PMF_VALIDATION_TRACKER.md) §2.2, or **Unknown** when deliberately not measured (note why).
- Redact tenant, people, and identifiable quotes before anything leaves trusted channels. Demo-tenant figures are **not** customer outcomes ([PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md) §4.1.1).

## Evidence row (copy per hypothesis / pilot)

| Field | Value |
|-------|--------|
| **Pilot ID** | Pilot A / B / … |
| **Hypothesis ID** | H1–H5 (from PMF tracker) |
| **Evidence date (UTC)** | |
| **Environment** | e.g. staging URL pattern only — no secrets |
| **Baseline** | Measured pre-pilot or **TBD** / **See scorecard** / **Unknown** |
| **Result** | Measured post-window or qualitative pointer — never placeholder numerics |
| **Artifacts** | Links or paths: scorecard, benchmark JSON name, internal ticket IDs |
| **Owner sign** | Initials + date when row is **Captured** in PMF tracker |

## Afterward

Within **5 business days** of a formal scorecard review, update every in-scope row in [PMF_VALIDATION_TRACKER.md](PMF_VALIDATION_TRACKER.md) for that **Pilot ID**.
