> **Scope:** ArchLucid Trust Center - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

**Buyer posture table (single index):** [`docs/trust-center.md`](../trust-center.md) — same evidence links; rendered in-product at **`/trust`**.

# ArchLucid Trust Center

**Audience:** Security reviewers, procurement, and legal teams evaluating ArchLucid as a **vendor-operated (SaaS)** service.

**Last reviewed:** 2026-04-22

ArchLucid is built so that **security, privacy, and operational transparency** are first-class: identity-backed access, defense-in-depth on the data plane, measurable reliability targets, and documentation you can trace to the product and infrastructure code. This page is the **buyer-facing index** into policies and deep technical references maintained in the repository.

---

## Security overview at a glance

- **Identity:** Microsoft **Entra ID** (OIDC / JWT) with **app roles** (**Admin**, **Operator**, **Reader**, **Auditor**) and optional **API keys** for automation; see [../SECURITY.md](../library/SECURITY.md) and [../CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md).
- **Network:** Optional **Azure Front Door + WAF**, optional **API Management**, **TLS** to the API, and **private endpoints** for **Azure SQL** and **Blob** when the private stack is enabled; **no public SMB (port 445)** for tenant data paths (see [../CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md)).
- **Data isolation:** **Row-level security** in SQL with per-request **`SESSION_CONTEXT`** scope (tenant / workspace / project) on covered tables — defense-in-depth alongside application authorization; see [TENANT_ISOLATION.md](TENANT_ISOLATION.md) and [../security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md).
- **Secrets:** **Azure Key Vault** references for application configuration in hosted deployments (see [../CONFIGURATION_KEY_VAULT.md](../library/CONFIGURATION_KEY_VAULT.md)).
- **Auditability:** Durable **append-only** audit trail in SQL (`dbo.AuditEvents`) with a **typed event catalog** and correlation identifiers; see [../AUDIT_COVERAGE_MATRIX.md](../library/AUDIT_COVERAGE_MATRIX.md) and [../SECURITY.md](../library/SECURITY.md) (PII / exports).
- **Testing in CI:** **OWASP ZAP** baseline on the API image, **Schemathesis** contract checks, and documented rate limiting / RBAC — see [../SECURITY.md](../library/SECURITY.md).
- **SOC 2 self-assessment:** The in-repo [SOC2_SELF_ASSESSMENT_2026.md](../security/SOC2_SELF_ASSESSMENT_2026.md) is maintained under **internal CISO ownership** (interim posture until a CPA attestation is funded — see compliance table below).
- **LLM outbound hygiene:** Optional deny-list **prompt redaction** before Azure OpenAI and aligned redaction for trace persistence (`LlmPromptRedaction`); see [../runbooks/LLM_PROMPT_REDACTION.md](../runbooks/LLM_PROMPT_REDACTION.md).

For a **STRIDE-oriented** view of the whole product boundary, see [../security/SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md).

---

## Penetration testing and security assessments

**Owner-conducted (interim):** until a funded third-party engagement completes, ArchLucid may publish an **internal security self-assessment** (not a pen test, not SOC 2). Draft scaffold: [`../security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`](../security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md).

**Third-party (procurement-grade):** formal external pen tests are **scheduled per customer / release train**, not implied by CI smoke alone. Procurement-ready templates:

- [Statement of work template](../security/PEN_TEST_SOW_TEMPLATE.md)
- [Redacted customer summary template](../security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md)
- [2026 Q2 — **awarded** SoW (Aeronova Red Team LLC)](../security/pen-test-summaries/2026-Q2-SOW.md)
- [2026 Q2 — redacted summary working copy](../security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md) (assessor narrative — **not** a public artefact)

**NDA-gated pen-test summaries (decision 2026-04-22 — [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item 20):** Pen-test redacted summaries are made available **under NDA only**; the public Trust Center records the existence of the engagement and the high-level posture. To request the most recent redacted summary, contact **`security@archlucid.com`**. The discipline folder [`docs/security/pen-test-summaries/`](../security/pen-test-summaries/README.md) remains the **in-repo working surface** for procurement alignment — it does **not** imply public publication of Aeronova deliverables.

**Questionnaires (pre-filled):**

- [CAIQ Lite pre-fill (2026)](../security/CAIQ_LITE_2026.md) — map to CSA CAIQ v4 spreadsheet for STAR submissions
- [SIG Core pre-fill (2026)](../security/SIG_CORE_2026.md) — map to Shared Assessments SIG Core workbook

---

## Recent assurance activity

This table lists **engagement metadata only** — not redacted findings, not customer names. Each row records that an assurance activity occurred, what it covered, and how to obtain redacted material under NDA (where applicable). Procurement teams can cite the table as evidence that the relevant control has a current, dated review on record without ArchLucid having to lower the NDA wall.

| Engagement | Vendor | Scope | Completed (UTC) | Summary access |
|------------|--------|-------|-----------------|----------------|
| 2026-Q2 third-party penetration test | Aeronova Red Team LLC | API, operator UI, hosted SaaS data plane | Engagement awarded 2026-04-21; kickoff 2026-05-06 (in flight — see [`pen-test-summaries/2026-Q2-SOW.md`](../security/pen-test-summaries/2026-Q2-SOW.md)) | NDA-only — email `security@archlucid.com` once assessor delivers redacted summary |
| Internal owner security self-assessment | ArchLucid (internal CISO ownership) | STRIDE-aligned control review across SOC 2 Common Criteria mapping | 2026-Q2 (latest revision tracked in [`SOC2_SELF_ASSESSMENT_2026.md`](../security/SOC2_SELF_ASSESSMENT_2026.md)) | Public summary available at [`SOC2_SELF_ASSESSMENT_2026.md`](../security/SOC2_SELF_ASSESSMENT_2026.md); detailed gap register on request |
| Accessibility self-attestation review | ArchLucid (accessibility custodian, same operational team as `security@`) | WCAG 2.1 Level AA against operator UI top routes via axe-core + jsx-a11y | 2026-04-22 (annual cadence; next window 2027-04-22 — see [`ACCESSIBILITY.md`](../../ACCESSIBILITY.md) "Review cadence") | Public — see marketing route `/accessibility` and root [`ACCESSIBILITY.md`](../../ACCESSIBILITY.md) |
| Quarterly staging chaos exercise | ArchLucid Platform / on-call | Staging-only fault injection (SQL pool exhaustion 2026-04-29; subsequent runs 2026-07-29, 2026-10-28) — production chaos out-of-scope per owner decision 2026-04-22 ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item 34) | Calendar published 2026-04-22; first run 2026-04-29 (staging) — see [`docs/quality/game-day-log/`](../quality/game-day-log/README.md) | Public — closing reports linked from the game-day calendar |

> Wording note: the quarterly chaos row is intentionally conservative — it records that a calendar is published and the first run is scheduled, not that production fault injection is in scope. `PENDING_QUESTIONS.md` item 34 remains the owner gate for any production chaos.

---

## Trust documents

### Get the procurement pack

Use the **`archlucid` CLI** so field teams do not need to remember script paths:

```bash
archlucid procurement-pack --out archlucid-procurement-pack.zip
```

```powershell
archlucid procurement-pack --out .\archlucid-procurement-pack.zip
```

**Buyer-facing steps** (what to send in email templates): [`HOW_TO_REQUEST_PROCUREMENT_PACK.md`](HOW_TO_REQUEST_PROCUREMENT_PACK.md).

**Technical / CI:** the same build runs via `python scripts/build_procurement_pack.py` (optional `--dry-run`, optional `--out <path>`). Thin wrappers remain: **`scripts/build_procurement_pack.sh`** (POSIX) and **`scripts/build_procurement_pack.ps1`** (Windows). Each successful build emits **`dist/procurement-pack.zip`** containing **`manifest.json`** (per-file **bytes + SHA-256**), **`versions.txt`** (git commit + UTC timestamp + CLI version), and **`redaction_report.md`** (canonical omissions). Canonical paths are enforced in **`scripts/procurement_pack_canonical.json`** — the build **fails loud** if any required source file is missing.

**Cover letter:** complete [`PROCUREMENT_PACK_COVER.md`](PROCUREMENT_PACK_COVER.md) **outside** the committed repo before sending buyer-specific names (legal / owner gate).

| Document | Description |
|----------|-------------|
| [TENANT_ISOLATION.md](TENANT_ISOLATION.md) | Buyer-readable summary of tenant isolation (identity, app layer, SQL RLS). |
| [SUBPROCESSORS.md](SUBPROCESSORS.md) | Subprocessors used to deliver the service (Microsoft Azure, Entra ID, Azure OpenAI, etc.). |
| [DPA_TEMPLATE.md](DPA_TEMPLATE.md) | Data Processing Agreement **template** for customers (requires legal review before use). |
| [INCIDENT_COMMUNICATIONS_POLICY.md](INCIDENT_COMMUNICATIONS_POLICY.md) | How we classify incidents and communicate with customers. |
| [SOC2_ROADMAP.md](SOC2_ROADMAP.md) | SOC 2 readiness: controls in place, gaps, and milestone roadmap. |
| [../security/SOC2_SELF_ASSESSMENT_2026.md](../security/SOC2_SELF_ASSESSMENT_2026.md) | Owner-led SOC 2 **self-assessment** (not CPA attestation); includes **Type I scoping** and gap register. |
| [../security/CAIQ_LITE_2026.md](../security/CAIQ_LITE_2026.md) | CAIQ Lite–style pre-fill (align to CSA CAIQ v4 download). |
| [../security/SIG_CORE_2026.md](../security/SIG_CORE_2026.md) | SIG Core–style pre-fill (align to Shared Assessments workbook). |
| [../security/COMPLIANCE_MATRIX.md](../security/COMPLIANCE_MATRIX.md) | Maps control themes to repository evidence paths. |
| [../SECURITY.md](../library/SECURITY.md) | Engineering security overview (ZAP, Schemathesis, RBAC, rate limits, PII). |
| [../CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md) | Architecture: edge, identity, private connectivity, correlation IDs. |
| [../API_SLOS.md](../library/API_SLOS.md) | Customer-visible HTTP SLOs (e.g. availability **99.5%** / 30 days) and measurement. |
| [../security/PII_RETENTION_CONVERSATIONS.md](../security/PII_RETENTION_CONVERSATIONS.md) | PII framing and retention considerations for conversation / Ask data. |
| [SLA_SUMMARY.md](SLA_SUMMARY.md) | Buyer-facing SLO targets (availability 99.5%, latency, maintenance). |
| [BACKUP_AND_DR.md](BACKUP_AND_DR.md) | Backup schedule, disaster recovery, data lifecycle, RTO/RPO estimates. |
| [OPERATIONAL_TRANSPARENCY.md](OPERATIONAL_TRANSPARENCY.md) | Status page plan and operational transparency roadmap. |
| [INTEGRATION_CATALOG.md](INTEGRATION_CATALOG.md) | Available and planned integrations (API, CLI, webhooks, CI/CD, SIEM). |
| Public compliance journey (marketing UI: route **`/compliance-journey`**) | Plain-language map to in-repo CAIQ/SIG/DPA/subprocessor evidence — **not** a certification claim (see compliance table below). |
| [SIEM_EXPORT.md](SIEM_EXPORT.md) | Audit log export for SIEM ingestion (Splunk, Sentinel, generic). |
| [CUSTOMER_ONBOARDING_PLAYBOOK.md](CUSTOMER_ONBOARDING_PLAYBOOK.md) | Structured onboarding checklist (6-week pilot alignment). |

---

## Compliance and certifications

| Item | Status | Notes |
|------|--------|--------|
| **SOC 2** (Type I / II) | **Deferred** — interim self-assessment + Trust Center honesty until ARR materially supports CPA attestation cost | Interim: [SOC2_SELF_ASSESSMENT_2026.md](../security/SOC2_SELF_ASSESSMENT_2026.md) + [COMPLIANCE_MATRIX.md](../security/COMPLIANCE_MATRIX.md); roadmap [SOC2_ROADMAP.md](SOC2_ROADMAP.md). We will pursue SOC 2 Type 1 readiness once we cross approximately **$1M ARR**; until then, we publish a self-attested security and compliance summary. (Directional, not contractual — see `docs/PENDING_QUESTIONS.md` item 6 resolved 2026-04-22.) External readiness consultant + CPA firm selection paused until that threshold; G-001 in the self-assessment captures the resumption checklist. |
| **GDPR / DPA** | Template available | See [DPA_TEMPLATE.md](DPA_TEMPLATE.md); subprocessors in [SUBPROCESSORS.md](SUBPROCESSORS.md). |
| **ISO 27001** | Not claimed | Roadmap TBD with SOC 2 program. |

---

## Commercial terms

**Drafted 2026-04-22; pending legal sign-off before any commerce un-hold.**

**Chargebacks:** bank-initiated disputes; Vendor responds via Stripe with contract + delivery evidence; outcome follows card-network rules.

**Refunds:** no Customer early-cancellation refund unless agreed in writing (aligned with order form **§5** / **§9**).

**Dunning:** Stripe smart retries for cards by default; suspension after **15 days past due** aligns with order form **§7** after notice.

Full text: [ORDER_FORM_TEMPLATE.md § 9](ORDER_FORM_TEMPLATE.md).

---

## Contact

- **Security inquiries:** `security@archlucid.com` (canonical mailbox — confirmed 2026-04-21; aligns `SECURITY.md`, this Trust Center, and the eventual PGP UID).
- **Accessibility barriers (non-security):** `accessibility@archlucid.com` — same operational custodian as `security@archlucid.com` (decision 2026-04-22); see [../security/ACCESSIBILITY_MAILBOX.md](../security/ACCESSIBILITY_MAILBOX.md) and the public marketing route **`/accessibility`**.
- **Coordinated disclosure key:** generation procedure: [../security/PGP_KEY_GENERATION_RECIPE.md](../security/PGP_KEY_GENERATION_RECIPE.md) (key publication pending). When published, the public material is served at **`/.well-known/pgp-key.txt`** on the marketing site (see [../SECURITY.md](../library/SECURITY.md) PGP). Until that file exists, use plain email.

For support alignment during incidents, clients should include **`X-Correlation-ID`** on API requests where possible ([../CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md) §8).

---

## Related documents

| Doc | Use |
|-----|-----|
| [POSITIONING.md](POSITIONING.md) | Product positioning and messaging |
| [../MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md](../library/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md) | SaaS-only marketability assessment |
| [../V1_SCOPE.md](../library/V1_SCOPE.md) | What V1 ships (grounding for claims) |
