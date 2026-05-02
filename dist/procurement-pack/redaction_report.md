# Redaction / omission report

The canonical procurement ZIP (see `scripts/procurement_pack_canonical.json`) **includes only** the reviewer checklist. The following repository paths are **not** copied into this pack and are listed here so owners can audit gaps.

| Repository path | Reason |
|-----------------|--------|
| `docs/security/CAIQ_LITE_2026.md` | CSA CAIQ Lite pre-fill — not on the canonical procurement reviewer checklist; provide on request for STAR / enterprise questionnaires. |
| `docs/security/SIG_CORE_2026.md` | Shared Assessments SIG Core pre-fill — not on the canonical checklist; provide on request. |
| `docs/go-to-market/SLA_SUMMARY.md` | SLA summary — referenced from TRUST_CENTER; omitted from minimal canonical ZIP to avoid duplicate policy threads (add to pack if buyer requests SLA excerpt explicitly). |

**Do not** add unredacted customer names or deal-specific cover letter text without owner sign-off.
