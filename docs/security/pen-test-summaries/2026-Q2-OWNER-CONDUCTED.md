> **Scope:** Owner-conducted 2026-Q2 penetration-style exercise — empty findings tracker, methodology, and Trust Center cross-links only; not Aeronova deliverables, exploit reproductions, or attestations.

> **Publication:** Owner-conducted security exercise — summary structure only. **Detailed findings and reproducible exploitation steps are intentionally omitted from the public repo**; redacted artefacts follow NDA posture in [`docs/go-to-market/TRUST_CENTER.md`](../../go-to-market/TRUST_CENTER.md).
>
> **Spine:** [Five-document onboarding spine](../../FIRST_5_DOCS.md)

# 2026-Q2 Owner-conducted penetration-style assessment

## Findings tracker

| ID | Category | Severity | Status | Date Found (UTC) | Date Resolved (UTC) |
|----|----------|----------|--------|------------------|---------------------|
| _Owner to populate — do not invent findings in automation._ |

## Engagement

| Field | Value |
|-------|-------|
| Assessor | ArchLucid engineering / security liaison (internal) |
| Trigger | Quarterly assurance cadence aligned with [`PENTEST_EXTERNAL_UI_CHECKLIST.md`](../PENTEST_EXTERNAL_UI_CHECKLIST.md) and Trust Center disclosures |
| Environments covered | Hosted SaaS **operator UI**, **HTTPS API**, and **data-plane** behaviours observable from external posture (non-production-first where stipulated in runbooks) |

## Scope summary

Assessment scope targets **authenticated and unauthenticated** surfaces that materially affect confidentiality, integrity, and availability:

- REST API (**RBAC boundaries**, injection classes, predictable resource IDs)
- Operator web shell (**session / CSRF-relevant behaviours**, UX-only auth affordances vs server enforcement)
- Multi-tenant **RLS-aligned** behaviours at the documented application boundary (**not** infra pentest inside Azure tenancy)

Explicit **out-of-scope** items remain vendor platform pen-test ownership (Azure control plane compromise, certificate lifecycle on Microsoft infra) unless negotiated separately.

## Methodology

1. **CI-assisted baseline** — regressions guarded by **OWASP ZAP** and **Schemathesis** in continuous integration (**see [`SECURITY.md`](../../library/SECURITY.md)**).
2. **Manual adversarial probing** guided by **`docs/security/SYSTEM_THREAT_MODEL.md` STRIDE`** themes.
3. **SQL injection and ORM-parameter hygiene** probes on parameterized API endpoints (negative cases; no destructive bulk operations).
4. **RBAC / policy boundary fuzzing** — attempt cross-role access with representative JWT + API-key principals.
5. **RLS bypass attempts** — confirm fail-closed scope application on tenant-scoped tables per documented session context rules.

## Tools

| Tool | Role |
|------|------|
| OWASP ZAP | Automated baseline scans (API image) |
| Schemathesis | Contract-guided fuzzing against OpenAPI |
| Browser DevTools | UI session / routing / storage inspection |
| `curl` / HTTP replay | Scripted negative tests against API |

## Pen test findings remediation (links placeholder)

Track fixes as **engineering change records** rather than dumping exploit detail here:

- **Remediation link pattern:** `[PR #___](https://github.com/joefrancisGA/ArchLucid/pull/___)` *(owner fills once merged)*
- **Verify:** rerun relevant **ZAP**/contract suite and annotate above tracker **Status**.

## Overall posture assessment (stub)

_Posture narrative is written **after** the engagement window completes._ Until then:

- Assume **material unknowns remain** comparable to [`OWNER_SECURITY_ASSESSMENT_2026_Q2.md`](../OWNER_SECURITY_ASSESSMENT_2026_Q2.md).
- Third-party Aeronova-delivered summaries remain **distinct** (**NDA**) — [`2026-Q2-SOW.md`](2026-Q2-SOW.md).
