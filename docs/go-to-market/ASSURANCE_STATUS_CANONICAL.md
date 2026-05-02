> **Scope:** Canonical assurance status source for procurement-facing language; defines current status, deferred windows, allowed wording, and evidence links to prevent cross-document drift.

# Assurance Status Canonical

**Audience:** Procurement, security reviewers, and internal authors updating buyer-facing artifacts.

**Last reviewed:** 2026-05-01

This document is the single source of truth for assurance status wording used by:

- `TRUST_CENTER.md`
- `CURRENT_ASSURANCE_POSTURE.md`
- `PROCUREMENT_FAQ.md`
- `PROCUREMENT_RESPONSE_ACCELERATOR.md`
- `SOC2_STATUS_PROCUREMENT.md`

---

## Canonical status table

| Assurance item | Current status | Deferred window | Allowed buyer wording | Evidence |
|---|---|---|---|---|
| SOC 2 Type II attestation | Not issued | Deferred (funding-gated) | "SOC 2 Type II is not currently issued. ArchLucid provides a self-assessment and evidence pack while attestation is deferred." | [SOC2_STATUS_PROCUREMENT.md](SOC2_STATUS_PROCUREMENT.md), [../security/SOC2_SELF_ASSESSMENT_2026.md](../security/SOC2_SELF_ASSESSMENT_2026.md), [TRUST_CENTER.md](TRUST_CENTER.md) |
| SOC 2 Type I engagement | Not started | Deferred (funding-gated) | "Type I scoping is deferred until funded assessor engagement." | [SOC2_STATUS_PROCUREMENT.md](SOC2_STATUS_PROCUREMENT.md), [TRUST_CENTER.md](TRUST_CENTER.md) |
| Owner-conducted penetration-style assessment | Active V1 control | Not deferred | "V1 uses owner-conducted penetration-style testing documented in-repo." | [../security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md](../security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md), [TRUST_CENTER.md](TRUST_CENTER.md) |
| Third-party penetration test | Not executed for V1 | V2 | "External third-party penetration testing is V2-scoped; no external vendor engagement is claimed for V1." | [../library/V1_DEFERRED.md](../library/V1_DEFERRED.md), [TRUST_CENTER.md](TRUST_CENTER.md), [../security/pen-test-summaries/2026-Q2-SOW.md](../security/pen-test-summaries/2026-Q2-SOW.md) |
| Redacted third-party assessor summary | Not available for V1 | V2 | "Redacted third-party assessor summaries are NDA-gated and only available after a V2 external engagement completes." | [TRUST_CENTER.md](TRUST_CENTER.md), [../library/V1_DEFERRED.md](../library/V1_DEFERRED.md) |

---

## Authoring rules

- Do not use "in flight" for third-party pen-test or SOC2 attestation items while status remains deferred.
- Do not imply issuance of external attestations when evidence is self-assessment or template-only.
- If status changes, update this file first, then update all listed downstream docs in the same change.

