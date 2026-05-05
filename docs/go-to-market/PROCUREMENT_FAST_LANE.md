> **Scope:** Five-minute procurement skim — outbound links duplicate **`PROCUREMENT_PACK_INDEX.md`** and the buyer **`docs/trust-center.md`** index only (no artefacts beyond those inventories). Claims here are labels, not attestations; **legal-only** artefacts are explicitly **template-only** until executed.

# Procurement fast lane

**Audience:** Procurement and security reviewers with a short clock.

**How to use:** Open the canonical table for full buyer-safe wording and freshness columns: **`[PROCUREMENT_PACK_INDEX.md](PROCUREMENT_PACK_INDEX.md)`**. Use **`[docs/trust-center.md](../trust-center.md)`** for web/posture summaries. Spreadsheet-aligned prompts remain in **`PROCUREMENT_RESPONSE_ACCELERATOR.md`** (still cite only paths grounded in those two indexes).

Every **Source File** cell below repeats an existing procurement-index path only (equivalent to **`[PROCUREMENT_PACK_INDEX.md](PROCUREMENT_PACK_INDEX.md)`** canonical rows).

| Starter need | Evidence type / deferral | Source File |
|---|---|---|
| Buyer-wide index | Self-asserted | [docs/trust-center.md](../trust-center.md) |
| Engineering security narrative | Self-asserted | [docs/library/SECURITY.md](../library/SECURITY.md) |
| STRIDE / boundary threat model | Self-asserted | [docs/security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md) |
| Tenant SQL RLS | Implemented | [docs/security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md) |
| SOC 2 procurement wording (status, not issuance) | Self-asserted | [SOC2_STATUS_PROCUREMENT.md](SOC2_STATUS_PROCUREMENT.md) |
| SOC 2 self-assessment (not CPA audit) | Self-asserted | [docs/security/SOC2_SELF_ASSESSMENT_2026.md](../security/SOC2_SELF_ASSESSMENT_2026.md) |
| SOC 2 roadmap / timing | Deferred **V1.1** (see linked file — not an auditor opinion) | [SOC2_ROADMAP.md](SOC2_ROADMAP.md) |
| CAIQ-style pre-fill | Self-asserted | [docs/security/CAIQ_LITE_2026.md](../security/CAIQ_LITE_2026.md) |
| SIG Core–style pre-fill | Self-asserted (questionnaire-aligned draft — not legal advice) | [docs/security/SIG_CORE_2026.md](../security/SIG_CORE_2026.md) |
| Tenant isolation (buyer narrative) | Self-asserted | [TENANT_ISOLATION.md](TENANT_ISOLATION.md) |
| DPA | Self-asserted · **Legal template-only** pending customer/legal execution | [DPA_TEMPLATE.md](DPA_TEMPLATE.md) |
| Subprocessors | Self-asserted | [SUBPROCESSORS.md](SUBPROCESSORS.md) |
| **Route ↔ tier ↔ policy ↔ nav crosswalk** | Engineering-maintained (verify against controllers) | [docs/library/ROUTE_TIER_POLICY_NAV_MATRIX.md](../library/ROUTE_TIER_POLICY_NAV_MATRIX.md) |

**Excluded from this skim (see `trust-center` / deferral docs, not fabricated here):** third-party SOC 2 Type II report (**V2** / programme timing via roadmap and status docs), awarded third-party penetration test deliverables (**V2** narrative in `trust-center` posture table — procurement index deliberately omits those rows).
