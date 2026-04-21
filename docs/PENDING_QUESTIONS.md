> **Scope:** Product and operations decisions the repo cannot resolve alone — consolidated pending list (supersedes scattered assessment §9 lists).

# Pending questions (product and operations)

**Last updated:** 2026-04-21 (items 5–16 from independent assessment; **2026-04-21** ServiceNow/Confluence + **customer-shipped containers** scope in Resolved)

Single place to track **decisions only a human owner** can make. When you ask what is still open, start here. Items marked **Resolved** stay for audit trail; remove them only when you intentionally shrink the file.

---

## Resolved (2026-04-21 — owner decisions)

| Topic | Decision |
|-------|----------|
| AWS agents / multi-cloud | **Deferred to V1.1** — Azure-first for V1. |
| Terraform `state mv` (Phase 7.5–7.8) | **Waived** — no maintenance window; resource addresses may retain historical tokens per ADR / rename checklist. |
| Commercial rails | **Stripe + Azure Marketplace** acceptable when each path is justified; ship Stripe before Marketplace unless a MACC buyer forces procurement path first. |
| Penetration testing | **Owner-conducted** security assessment (OWASP ASVS–style) until budget for **external** assessor; see [`docs/security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`](security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md). |
| Cross-tenant pattern library | **Approved** (opt-in, k-anonymity, DPA carve-out) — requires ADR before implementation lands. |
| Azure subscriptions | **Staging:** existing subscription. **Production:** **second subscription** dedicated to prod (create empty; wire Terraform/CD after staging is green). |
| Production Azure subscription ID | **`aab65184-5005-4b0d-a884-9e28328630b1`** — recorded in [`AZURE_SUBSCRIPTIONS.md`](AZURE_SUBSCRIPTIONS.md) as the single source of truth. Operator action: set GitHub Environment secret `AZURE_SUBSCRIPTION_ID` on the **`production`** environment to this value (and confirm sibling `AZURE_TENANT_ID` / `AZURE_CLIENT_ID` are populated for OIDC). Default region: **`centralus`**. |
| DNS / TLS | Owner **approves** DNS and TLS cutover for production hostnames. |
| Domain | **archlucid.com** — registration fee paid; confirm WHOIS when registrar completes. |
| Reference customer (GTM) | **Ship self-serve trial first** — first **paying** tenant becomes the first publishable reference (`TRIAL_FIRST_REFERENCE_CASE_STUDY.md`). |
| SOC 2 Type I/II | **Deferred** — interim posture is self-assessment + Trust Center honesty; revisit when ARR justifies CPA attestation. |
| ServiceNow + Confluence as **first-party** workflow integrations | **Out of scope for now (2026-04-21)** — **ServiceNow** is operational ITSM / CMDB-centric; ArchLucid is intentionally **upstream** (design-time architecture, governance, manifests). **Confluence** is deferred because the integration posture is **Microsoft-first** (Entra, Azure DevOps, Teams, Logic Apps per [`docs/adr/0019-logic-apps-standard-edge-orchestration.md`](adr/0019-logic-apps-standard-edge-orchestration.md); GitHub + ADO manifest-delta already shipped). Revisit only if product strategy changes. |
| **Customer-shipped Docker / container production bundles** | **Out of scope (2026-04-21)** — ArchLucid is a **vendor-operated SaaS** product. We do **not** treat shipping **production** Docker images, Helm charts, or customer-operable full-stack compose bundles as a standard customer deliverable. **Customer-facing artifacts** are the **CLI**, **published API client libraries** (for example `ArchLucid.Api.Client`), **OpenAPI / REST contracts**, and **documentation**. **`docker compose` / `archlucid pilot up`** remain **optional local evaluation and engineering** paths in the repo, not a committed “bring your own container” product track unless a future ADR reopens it. |

---

## Still open (needs your input later)

1. **Design-partner row (`DESIGN_PARTNER_NEXT`)** — When a **named** design partner (not PLG) is authorized, replace `<<CUSTOMER_NAME>>` in [`DESIGN_PARTNER_NEXT_CASE_STUDY.md`](go-to-market/reference-customers/DESIGN_PARTNER_NEXT_CASE_STUDY.md) and move the table row through **Drafting → Customer review → Published** per [`reference-customers/README.md`](go-to-market/reference-customers/README.md).

2. **External pen-test vendor** — When funded, award SoW, fill `<<vendor>>` / `<<TBD>>` in [`docs/security/pen-test-summaries/2026-Q2-SOW.md`](security/pen-test-summaries/2026-Q2-SOW.md), and replace placeholders in [`2026-Q2-REDACTED-SUMMARY.md`](security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md) after delivery.

3. **PGP for `security@archlucid.dev`** — [`SECURITY.md`](../SECURITY.md) still has a TODO: generate key pair, publish public key, link from Trust Center.

4. **Next Microsoft-aligned workflow integration** — GitHub manifest-delta and Azure DevOps pipeline tasks are shipped ([`GITHUB_ACTION_MANIFEST_DELTA.md`](integrations/GITHUB_ACTION_MANIFEST_DELTA.md), [`AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md`](integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md)). **ServiceNow and Confluence are explicitly out of scope for now** (see Resolved table). Next anchor is a **product** call among remaining Microsoft surfaces (e.g. Teams / Logic Apps fan-out per ADR 0019), not Atlassian/ITSM.

---

## Six quality prompts (2026-04-20 independent assessment) — execution status

| Prompt | Intent | Repo status (2026-04-21) |
|--------|--------|--------------------------|
| **8.1** Reference customer + CI guard | Case study assets, table row, merge-blocking when `Published` | **Done** (auto-flip in `ci.yml`); **extended** with PLG case study + table row in this change set. |
| **8.2** `archlucid pilot up` | One-command Docker pilot | **Done** — [`ArchLucid.Cli/Commands/PilotUpCommand.cs`](../ArchLucid.Cli/Commands/PilotUpCommand.cs). *Note:* `POST /v1.0/demo/seed` is **Development-only** and needs **ExecuteAuthority**; the Docker path relies on **demo seed on startup** instead. |
| **8.3** First-value report | CLI + `GET /v1/pilots/runs/{id}/first-value-report` | **Done** — see CHANGELOG 2026-04-20. |
| **8.4** GitHub Action manifest delta | Composite action + docs + example workflow | **Done** — `integrations/github-action-manifest-delta/`, [`docs/integrations/GITHUB_ACTION_MANIFEST_DELTA.md`](integrations/GITHUB_ACTION_MANIFEST_DELTA.md). |
| **8.5** Persistence consolidation | Proposal doc only | **Done** — [`docs/PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md`](PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md). |
| **8.6** Pen-test publication path | Templates + Trust Center | **Done** — `docs/security/pen-test-summaries/`; **extended** with owner-assessment draft + Trust Center wording in this change set. |

---

## Still open — surfaced by 2026-04-21 independent assessment

These came out of [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md) § 9 and the six Cursor prompts in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md). Each is **owner-only** — the assistant cannot answer them from repository state.

5. **External pen-test scope and budget** — vendor selection, scope (web app only / web + infra / web + infra + LLM threat model), test window. Picks up where item 2 above leaves off.

6. **SOC 2 Type I assessor + audit period start date** — readiness gap report scaffolds in Prompt 4 only land if an assessor is named.

7. **Reference-customer publication ownership and discount-for-reference percent** — Prompt 1 will pre-build the publication runbook + evidence-pack template + CLI tool but stops at "who graduates the first row in `reference-customers/README.md`?" Suggest 15% per [`PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md) § 5.4 if you don't want to negotiate per deal.

8. **Marketplace publication go-live decision** — sign off on Azure Marketplace SaaS plan SKUs (aligned to PRICING_PHILOSOPHY tiers), legal entity, lead-form webhook URL. Prompt 3 pre-builds the alignment guard and the publication checklist diff; cannot create a real listing.

9. **Stripe production go-live policy decisions** — chargeback / refund / dunning text for the order-form template; legal entity name on customer statements; live API key + webhook secret. Prompt 3 lands the production-safety guards but no live keys.

10. **PGP key for `security@archlucid.dev`** — owner generates the key pair (or designates a custodian) and drops the public key into `archlucid-ui/public/.well-known/pgp-key.txt`. The CI guard in Prompt 4 turns green automatically the moment the file appears.

11. **Workflow-integration sequencing (rescoped)** — **Prompt 5 (ServiceNow + Confluence) is deferred** — see Resolved table. When picking the next integration, sequence **Microsoft-native** options (Teams notifications, Logic Apps standard workflows, deeper ADO/GitHub) rather than Confluence/ServiceNow unless strategy changes.

12. **WCAG 2.2 AA conformance publication channel** — Trust Center page only, or also a public `/accessibility` page on the marketing site? Whether to create an `accessibility@archlucid.dev` alias or reuse `security@`.

13. **Public price list publication on marketing site** — `PRICING_PHILOSOPHY.md` is internal today. Marketplace publication (item 8) makes price public anyway; do we publish on the marketing site simultaneously or stay quote-on-request elsewhere?

14. **Cross-tenant pattern-library implementing ADR ownership** — approved per item above (`Resolved` table) but the implementing ADR has not been drafted; who owns it?

15. **Golden-cohort LLM budget approval** — Prompt 6 stands up a nightly golden-cohort drift detector. Owner approves a dedicated Azure OpenAI deployment + estimated monthly token budget for the nightly run.

16. **ADR 0021 Phase 3 — owner policy (Prompt 2 landed code + stopped at gate)** — Phase 2 catalog (`AuditEventTypes.Run.*` + dual-write), `IRunCommitOrchestrator` façade, and parity probe tooling shipped **2026-04-21**; Phase 3 **deletion** PRs remain blocked until ADR 0021 exit gates **(i)–(iv)**. Owner must decide:
    - **Legacy `CoordinatorRun*` sunset:** Confirm the fixed calendar date **2026-07-20** (already published in API deprecation headers) is still the authoritative cut-over for **removing** legacy wire values after dashboards/exports migrate, or pick a different date with platform sign-off.
    - **Parity probe write path:** Nightly **`.github/workflows/coordinator-parity-daily.yml`** can `git push` marker upserts into `docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md` on `main` when `contents: write` is permitted. Confirm **auto-commit to docs on default branch** is acceptable, or require a **bot PR** / **manual paste** instead (branch protection may block pushes — confirm token policy).
    - **ADR 0022 lifecycle:** After **14 contiguous** green daily rows in the automated table, should **ADR 0022** flip to **Superseded** immediately, or stay until an actual Phase 3 **deletion** ADR ships?

---

## Related

| Doc | Use |
|-----|-----|
| [`docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md) | Latest weighted independent assessment (64.14%) |
| [`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md) | Six paste-ready Cursor prompts; #3 and #4 stop at owner gates |
| [`docs/archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md) | Prior assessment + §8 prompts |
| [`docs/go-to-market/PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md) § 5.4 | Reference-customer CI guard and discount re-rate |
