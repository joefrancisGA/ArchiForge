> **Scope:** Owner security self-assessment — procurement pack excerpt (no customer names).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

# Owner-conducted security assessment — procurement excerpt

This document is the **buyer-shareable excerpt** for procurement bundles. It summarizes the **same programme** as the in-repo draft [`../security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`](../security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md) but **must not** be edited with customer-specific names in the pack — use `PROCUREMENT_PACK_COVER.md` for deal context only.

## What this is (and is not)

- **Is:** Internal **owner / engineering** security self-assessment structured for transparency until third-party artefacts exist.
- **Is not:** A SOC 2 report, ISO certificate, or third-party penetration-test result.

## Method (summary)

1. **Automated CI gates** — SAST, dependency and container scanning, contract testing, secret scanning, and documented API abuse paths (see [`../SECURITY.md`](../SECURITY.md) and [`../AUDIT_COVERAGE_MATRIX.md`](../AUDIT_COVERAGE_MATRIX.md)).
2. **Manual checklist** — ASVS-oriented review of authentication, authorization, tenant isolation (RLS + JWT), rate limits, and LLM prompt / trace handling (see [`../security/SYSTEM_THREAT_MODEL.md`](../security/SYSTEM_THREAT_MODEL.md)).
3. **Findings register** — tracked internally with severity, owner, and remediation dates (tables in the full draft use placeholders until the assessment window completes).

## Full draft under NDA

Detailed tables, environment-specific links, and sign-off names live in the repository draft linked above. Procurement teams requiring **assessor-grade** evidence should request the **separate** pen-test and SOC 2 roadmap items referenced from [`TRUST_CENTER.md`](TRUST_CENTER.md).
