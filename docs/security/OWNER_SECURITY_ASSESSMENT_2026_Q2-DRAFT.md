> **Scope:** Owner-conducted security assessment (Q2 2026) — full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Owner-conducted security assessment — Q2 2026 (draft)

**This is not a third-party penetration test and is not a SOC 2 attestation.** It is an **internal security self-assessment** performed by the product owner / engineering team, structured for buyer transparency until a separately funded external assessor delivers a redacted summary under [`pen-test-summaries/`](pen-test-summaries/README.md).

**Assessment window (planned):** `2026-04-28` — `2026-04-28`

**Scope in / out:** ArchLucid API surface (ASP.NET Core), operator UI (Next.js), SQL Server persistence layer, Docker container images, Terraform IaC modules, CI pipeline security gates.

**Related templates:** [`PEN_TEST_SOW_TEMPLATE.md`](PEN_TEST_SOW_TEMPLATE.md) (borrow structure for scope), [`../go-to-market/TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md) (buyer index)

---

## Method

1. **Automated gates already in CI** — OWASP ZAP (baseline / scheduled), Schemathesis, CodeQL, Gitleaks, Trivy, Simmy, k6 smoke paths — re-run against the assessment environment and attach run links: GitHub Actions CI workflow (gitleaks, CodeQL, Trivy image scan, Trivy IaC scan, OWASP ZAP baseline, Schemathesis API contract, Simmy chaos) — latest main branch runs.
2. **Manual checklist** — OWASP ASVS Level 2–oriented review of authentication, authorization, tenant isolation (RLS + JWT), rate limits, SSRF surfaces, and LLM prompt / trace handling per [`SYSTEM_THREAT_MODEL.md`](SYSTEM_THREAT_MODEL.md)
3. **Findings register** — severity, component, remediation, owner, target date

---

## Findings summary

| ID | Severity | Title | Status |
|----|----------|-------|--------|
| SEC-001 | Info | OWASP ZAP informational alerts (CSP header opportunities) | Accepted |
| SEC-002 | Low | Trivy IaC: optional encryption-at-rest configuration in non-production Terraform modules | Accepted (non-production) |
| SEC-003 | Info | Legacy ArchiForge naming in SQL DDL headers and some RLS object names | Accepted (Phase 7 deferred) |
| SEC-004 | Info | CodeQL runs against the codebase in CI; actionable findings are triaged and remediated per normal development workflow | Accepted |
| SEC-005 | Info | Trivy scans container images (and IaC scope per workflow) in CI; advisory output is reviewed per release process | Accepted |
| SEC-006 | Info | Gitleaks secret scanning runs on CI with repository-configured rules | Accepted |
| SEC-007 | Info | Schemathesis exercises API surfaces where wired in CI workflow | Accepted |

---

## Sign-off (internal)

| Role | Name | Date |
|------|------|------|
| Owner | Product Owner | 2026-04-28 |

When this document is ready for external readers, move a **sanitized** copy (or this file, fully de-scrubbed) to a buyer-visible path linked from the Trust Center and archive the working notes separately if they contain internal-only detail.

This self-assessment will be superseded by the Aeronova third-party penetration test (SoW awarded 2026-04-21, V1.1 scope). Until then, this document represents the product team's internal security review based on automated CI gates and manual ASVS-oriented checklist review.
