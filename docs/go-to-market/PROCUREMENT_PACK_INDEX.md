> **Scope:** Single canonical procurement evidence index — file paths are source of truth for CI; statuses are buyer-safe labels aligned with **`PROCUREMENT_RESPONSE_ACCELERATOR.md`**, not attestations.

# Procurement evidence pack — buyer index (canonical)

**Audience:** Security, procurement, and GRC reviewers.

**How to cite:** Prefer **Evidence Artifact** titles and **`Source File`** links below rather than improvising statuses in questionnaires. Use **`trust-center.md`** for high-level posture; use this file for granular artifact inventory. **Five-minute skim (same paths as this table):** [`PROCUREMENT_FAST_LANE.md`](PROCUREMENT_FAST_LANE.md).

| Evidence Artifact | Evidence Type | Last Reviewed UTC | Source File | Buyer-safe Claim |
|---|---|---|---|---|
| Trust Center (buyer index) | Self-asserted | 2026-05-01 | [docs/trust-center.md](../trust-center.md) | Central index links only to in-repo evidence; distinguishes self-assessed vs deferred third-party artefacts. |
| Security overview | Self-asserted | 2026-05-01 | [docs/library/SECURITY.md](../library/SECURITY.md) | Describes scanning, boundaries, authentication modes documented in-repo. |
| System threat model (STRIDE) | Self-asserted | 2026-05-01 | [docs/security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md) | Internal architectural threat enumeration — not substitute for customer architecture review. |
| Multi-tenant RLS | Implemented | 2026-05-01 | [docs/security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md) | SQL `SESSION_CONTEXT` design and risk posture documented; engineering controls described in-linked implementation notes. |
| SOC 2 procurement statement | Self-asserted | 2026-04-24 | [docs/go-to-market/SOC2_STATUS_PROCUREMENT.md](SOC2_STATUS_PROCUREMENT.md) | States Type II issuance not yet claimed; directs to roadmap and self-assessment. |
| SOC 2 self-assessment narrative | Self-asserted | 2026-04-24 | [docs/security/SOC2_SELF_ASSESSMENT_2026.md](../security/SOC2_SELF_ASSESSMENT_2026.md) | Internal CC mapping narrative — explicitly not a CPA audit opinion. |
| SOC 2 roadmap | Deferred V1.1 | 2026-04-24 | [docs/go-to-market/SOC2_ROADMAP.md](SOC2_ROADMAP.md) | Planned programme timing only; confirms no SOC 2 report yet. |
| CAIQ-lite pre-fill | Self-asserted | 2026-05-01 | [docs/security/CAIQ_LITE_2026.md](../security/CAIQ_LITE_2026.md) | Questionnaire-aligned draft sourced from documented controls posture. |
| SIG Core pre-fill | Self-asserted | 2026-05-01 | [docs/security/SIG_CORE_2026.md](../security/SIG_CORE_2026.md) | Questionnaire-aligned draft referencing library evidence pointers. |
| SCIM + Entra ID provisioning recipe | Self-asserted | 2026-05-02 | [docs/integrations/SCIM_ENTRA_ID_SETUP.md](../integrations/SCIM_ENTRA_ID_SETUP.md) | Documents SCIM URLs, bearer token issuance, verification tests, parser guardrails against Entra filter literals. |
| Tenant isolation narrative | Self-asserted | 2026-05-01 | [docs/go-to-market/TENANT_ISOLATION.md](TENANT_ISOLATION.md) | Logical isolation framing for diligence — contract-specific items via MSA/DPA. |
| DPA template | Self-asserted | 2026-05-01 | [docs/go-to-market/DPA_TEMPLATE.md](DPA_TEMPLATE.md) | Template wording only until executed under customer legal review. |
| Subprocessors register | Self-asserted | 2026-05-01 | [docs/go-to-market/SUBPROCESSORS.md](SUBPROCESSORS.md) | Lists subprocessors acknowledged in-repo; customer due diligence completes against their policy. |

**Historical navigation index:** The shorter navigation-only table remains in **`PROCUREMENT_EVIDENCE_PACK_INDEX.md`** — that file intentionally defers statuses to **this** index for CI-validated freshness.
