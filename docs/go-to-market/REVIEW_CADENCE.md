> **Scope:** Review cadence and role ownership for buyer-facing procurement documentation, including stale-document escalation expectations.

# Procurement Documentation Review Cadence

**Audience:** Maintainers of procurement/trust documents and release managers.

**Last reviewed:** 2026-05-01

---

## Cadence matrix

| Document | Owner role | Review frequency | Escalation when stale |
|---|---|---|---|
| `TRUST_CENTER.md` | Security lead | Every 30 days | Raise in release checklist and update before procurement pack release |
| `CURRENT_ASSURANCE_POSTURE.md` | Security lead | Every 30 days | Block procurement deal-ready mode until refreshed |
| `SLA_SUMMARY.md` | Platform lead | Every 45 days | Escalate to product + ops owner for confirmation |
| `INCIDENT_COMMUNICATIONS_POLICY.md` | Incident manager role | Every 45 days | Escalate to on-call manager; confirm channels and timelines |
| `SUBPROCESSORS.md` | Privacy/legal operations role | Every 90 days | Escalate to legal review queue and update changelog note |

---

## Process

1. Update `Last reviewed` in each document when substantive validation is done.
2. Keep status wording aligned with `ASSURANCE_STATUS_CANONICAL.md`.
3. Run CI checks before shipping buyer packs.

---

## CI linkage

- Claim consistency: `scripts/ci/check_procurement_claim_coherence.py`
- Freshness check: `scripts/ci/check_procurement_doc_freshness.py`

