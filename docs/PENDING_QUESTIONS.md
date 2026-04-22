> **Scope:** Product and operations decisions the repo cannot resolve alone — consolidated pending list (supersedes scattered assessment §9 lists).

# Pending questions (product and operations)

**Last updated:** 2026-04-21 (interactive owner Q&A session + same-day 5-decision follow-up + bundled DDL change set — see *Resolved 2026-04-21 (owner Q&A — 19 decisions)*, *Resolved 2026-04-21 (follow-up Q&A — 5 decisions)*, and *Resolved 2026-04-21 (bundled DDL change set — Teams + RLS)* tables below; supersedes the prior `Last updated` entry)

Single place to track **decisions only a human owner** can make. When you ask what is still open, start here. Items marked **Resolved** stay for audit trail; remove them only when you intentionally shrink the file.

---

## Resolved 2026-04-21 (owner Q&A — 19 decisions)

These decisions came out of a structured 19-question owner Q&A session on 2026-04-21. Each answer also rewrites the corresponding "Still open" item below (or marks it Resolved). Where an answer creates a new mechanical work item, that item is captured in [`docs/CHANGELOG.md`](CHANGELOG.md) under the same date.

| Decision | Answer | Affects |
|----------|--------|---------|
| **PGP / security mailbox** | Canonical: **`security@archlucid.com`** (`.dev` retired). | `SECURITY.md`, `docs/go-to-market/TRUST_CENTER.md`, `docs/go-to-market/INCIDENT_COMMUNICATIONS_POLICY.md`, `archlucid-ui/public/.well-known/security.txt`. Resolves the custodian sub-bullets on items 2 / 10 / 20 / 21. |
| **Marketplace + Stripe live cutover** | **Held** — neither flips on a date yet; production-safety guards still ship. | Item 22 stays open as **"Held"** (owner has not chosen a calendar). |
| **Microsoft Teams connector scope** | **Notification-only** for v1; two-way is a V1.1 candidate (no M365 app manifest registration in v1). | Resolves item 23. |
| **Microsoft Teams trigger set** | Add **all three** of `compliance.drift.escalated`, `advisory.scan.completed`, `seat.reservation.released` to the v1 default workflow. | Resolves item 32. |
| **Golden-cohort baseline SHA lock** | **Lock today** from a single approved Simulator run (`ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED=true`). | Resolves item 33. Item 15 / 25 (real-LLM budget) **stays open** — this answer is Simulator-only. |
| **Reference discount %** | **15%** standardized — stop negotiating per deal. | Resolves item 7. `PRICING_PHILOSOPHY.md` § 5.4 "suggested" → "standard". |
| **Public-sector vertical framing** | **Both** EU/GDPR (existing) and US (FedRAMP / StateRAMP). Wizard ships a picker label. | Resolves item 17. New work: `templates/briefs/public-sector-us/` + `templates/policy-packs/public-sector-us/`. |
| **Vertical starter tiering** | All five verticals **stay in Core Pilot / trial** for v1; no paid-tier gating. | Resolves item 18. Documented in `templates/README.md`. |
| **ROI bulletin minimum N + signatory** | **N = 5** for the first issue; **owner-solo** sign-off. | Resolves item 27. |
| **`/why` competitive comparison delivery** | **Both** PDF download and inline page section, with a CI check that fails if comparison rows in `why-archlucid-comparison.ts` and the PDF builder diverge. | Resolves item 31. |
| **SOC 2 timing** | **Stays deferred.** Revisit trigger: owner-defined ARR threshold (assistant cannot set the dollar figure — captured under item 6). | Updates item 6 with a stable revisit-trigger sentence on the Trust Center. |
| **ADR 0021 Phase 3 cut-over** | **Accelerate to 2026-05-15** — product not yet released, so finish the strangler this sprint. **[ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md) drafted in this change set** (the prior Draft [ADR 0028 — completion scaffold](adr/0028-coordinator-strangler-completion.md) is marked Superseded by 0029). | Resolves item 24. Dropped the `2026-07-20` deprecation-header constant to `2026-05-15` atomically (see ADR 0029 § Component breakdown). |
| **Coordinator parity-probe write path** | **Auto-commit to `main`** is acceptable — grant `contents: write` to `coordinator-parity-daily.yml`. | Resolves item 16 sub-bullet (parity probe write path). |
| **`IMPROVEMENTS_COMPLETE.md` at repo root** | **Archive** to `docs/archive/` with a superseded note. **Done in this change set** (`git mv` to [`docs/archive/IMPROVEMENTS_COMPLETE_2026_04_21.md`](archive/IMPROVEMENTS_COMPLETE_2026_04_21.md); superseded banner prepended; canonical replacements named in the banner). | Resolves the QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60 §1.23 anchor (and item 34 in that assessment's open questions). |
| **ArchLucid rename — RLS object-name SQL migration** | **Approved.** Land in a dedicated next session (so the DDL change set is reviewable on its own). | Reaffirms `ArchLucid-Rename.mdc` rule's explicit RLS-rename note; not landed in this change set. |
| **Quality-assessment cadence** | **Weekly** going forward; next pass scheduled **2026-04-28**. | Captured in the *Related* table below. |
| **Phase 3 ADR 0022 lifecycle** | ~~After **14 contiguous green daily rows** in the parity table, ADR 0022 flips to **Superseded** by a Phase-3 deletion ADR.~~ **Superseded by the same-day follow-up** — gate (iv) was waived for pre-release, so ADR 0022 flips to Superseded **inside PR A** itself (no waiting for 14 rows that cannot accumulate pre-release). See follow-up table row "Phase 3 gate (iv) — pre-release waiver". | Updates item 16 (ADR 0022 lifecycle sub-bullet). |
| **Phase 3 legacy-wire sunset date alignment** | The same **2026-05-15** date applies to deprecation header `Sunset:` values + parity-probe doc + ADR 0029 + any client SDK release notes. | Updates item 16 (legacy `CoordinatorRun*` sunset sub-bullet). |
| **Improvements 4 (Marketplace + Stripe) production-safety guards** | Continue shipping the guards (CI alignment, `BillingProductionSafetyRules`, preflight CLI) — no live keys touched. | No item resolved; item 22 explicitly notes the guards-but-no-keys posture. |

---

## Resolved 2026-04-21 (follow-up Q&A — 5 decisions)

These decisions came out of a same-day five-question follow-up after the 19-decision batch landed. They tighten the operational details so the Phase 3 cut-over and the GTM artifacts produced in the 19-decision batch are mechanically executable.

| Decision | Answer | Affects |
|----------|--------|---------|
| **Phase 3 gate (iv) — pre-release waiver** | **Waive gate (iv) for the pre-release window** (alongside the already-waived gate (i)). Pre-release there is no customer traffic, so the daily parity probe cannot accumulate the 14 zero-write rows the gate measures; the runbook stays live. **Both** waivers expire automatically when V1 ships to a paying customer. | [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md) § Operational considerations + Lifecycle table; [ADR 0022](adr/0022-coordinator-phase3-deferred.md) Assumptions / Constraints / gate-evidence row / Architecture-overview diagram / Component-breakdown row / Follow-up; [`docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md`](runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md) § Phase 3 gate status. |
| **Phase 3 PR A authorship** | **Assistant drafts PR A end-to-end** in this repo (deletes coordinator concretes/interfaces, sweeps DI, shrinks `DualPipelineRegistrationDisciplineTests` allow-list, regenerates OpenAPI snapshot, opens PR for owner review). To be done in a **separate dedicated session** — large surgical change set, deserves its own clean turn. | New "Still open" sub-item under item **16** ("Phase 3 PR A authorship — queued for dedicated session"). |
| **Public-sector US — CJIS scope** | **FedRAMP Moderate / NIST SP 800-53 Rev. 5 only** in v1. Drop the CJIS Security Policy reference from the policy-pack metadata, brief, wizard preset, and rule descriptions. CJIS overlay is captured as a future pack rather than v1 work. | `templates/policy-packs/public-sector-us/policy-pack.json`, mirrored UI copy at `archlucid-ui/public/vertical-templates/public-sector-us/policy-pack.json`, `templates/policy-packs/public-sector-us/compliance-rules.json`, `templates/briefs/public-sector-us/brief.md`, `archlucid-ui/src/lib/vertical-wizard-presets.ts`, `templates/README.md` § Owner decisions. |
| **ROI bulletin sign-off audit format** | **Dedicated tagged section** in `docs/CHANGELOG.md` of the form `## YYYY-MM-DD — ROI bulletin signed: Q?-YYYY` — greppable with one `rg` command. The section *is* the signature; no separate signature artifact, no co-signer. | `docs/go-to-market/AGGREGATE_ROI_BULLETIN_TEMPLATE.md` § Owner-approval gate (column rename) + new § Sign-off audit format (heading shape + `rg` recipe + "no bulletin without a section" rule). |
| **Microsoft Teams — per-trigger opt-in** | **Per-trigger opt-in matrix** per connection (defaults to all-on so existing rows keep current behaviour). Costs an extra column on `dbo.TenantTeamsIncomingWebhookConnections` (`EnabledTriggersJson NVARCHAR(MAX) NOT NULL`) and a UI checkbox matrix on `/integrations/teams`; Logic Apps workflow filters server-side before fan-out. To be done in a **separate session** alongside the RLS object-name SQL migration so both DDL change sets are reviewable together. | New "Still open" sub-item under item **23** ("Per-trigger Teams opt-in matrix — queued for dedicated session"). |

---

## Resolved 2026-04-21 (bundled DDL change set — Teams + RLS)

These two work items were the dedicated-session items queued by the same-day follow-up table above. Both ship together so the two SQL DDL changes are reviewable in a single window.

| Decision | Answer | Affects |
|----------|--------|---------|
| **Microsoft Teams — per-trigger opt-in matrix (Part A)** | **Implemented.** DbUp **`107_TeamsConnectionsEnabledTriggers.sql`** + master DDL mirror, canonical six-trigger catalog, `EnabledTriggers` round-tripped through contracts + Dapper / InMemory repos, controller subset validation (400 on unknown), `/integrations/teams` UI checkbox matrix, Logic Apps `teams-notification-fanout` README updated for server-side filter, tests for round-trip + invalid-trigger + default-all-on. | Closes the new "Still open" sub-item under item **23** ("Per-trigger Teams opt-in matrix — queued for dedicated session"). See `docs/CHANGELOG.md` 2026-04-21 entry "Teams per-trigger opt-in matrix (Part A) + ArchLucid RLS object-name SQL migration (Part B)". |
| **ArchLucid rename — RLS object-name SQL migration (Part B)** — **`SESSION_CONTEXT` keys naming** | **Atomic cutover to `al_*`** (no dual-read shim). Owner answer to in-session question `q_session_context_keys` was **`rename_to_al`**. | DbUp **`108_RlsRenameToArchLucid.sql`** + rollback `R108`; master DDL substitution; `RlsSessionContextApplicator` / `RlsBypassPolicyBootstrap` / `DevelopmentDefaultScopeTenantBootstrap` / `SqlTenantHardPurgeService`; integration tests updated (CI string-concatenation workaround retired). |
| **ArchLucid rename — RLS object-name SQL migration (Part B)** — **Brownfield rollout sequencing** | **Apply migration 108 + deploy application binaries together.** No compatibility window — old binaries writing `af_*` after 108 will be denied by the new predicates. Documented in `docs/CHANGELOG.md` Part B entry. | Closes item "ArchLucid rename — RLS object-name SQL migration" in the 19-decision table. Closes RLS leftover row at `docs/ARCHLUCID_RENAME_CHECKLIST.md` § 7.9. |

---

## Resolved (2026-04-21 — owner decisions, prior session)

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

    - **Custodian mailbox (Resolved 2026-04-21):** **`security@archlucid.com`** is canonical. Trust Center, `SECURITY.md`, `INCIDENT_COMMUNICATIONS_POLICY.md`, and `security.txt` all aligned in this change set; the eventual PGP UID must use the same address.

3. **PGP for coordinated disclosure** — [`SECURITY.md`](../SECURITY.md) now points at `archlucid-ui/public/.well-known/pgp-key.txt` as **pending** until the custodian commits the public key. **Mailbox alignment (Resolved 2026-04-21): the UID is `security@archlucid.com`.** Items 10 / 21 still own the actual key generation.

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

6. **SOC 2 Type I assessor + audit period start date** — **Stays deferred (Resolved 2026-04-21).** Interim posture: self-assessment + Trust Center honesty. **Revisit trigger:** owner-defined ARR threshold — assistant cannot set the dollar figure; the Trust Center compliance-and-certifications row was rewritten in this change set to make the trigger explicit. Sub-question still open: **what ARR figure?**

7. **Reference-customer publication ownership and discount-for-reference percent** — **Discount Resolved 2026-04-21:** **15% standardized.** `PRICING_PHILOSOPHY.md` § 5.4 was promoted from "suggested" to "standard" in this change set. **Still open (item 19):** ownership of graduating the first PLG row from `Customer review` to `Published`.

8. **Marketplace publication go-live decision** — sign off on Azure Marketplace SaaS plan SKUs (aligned to PRICING_PHILOSOPHY tiers), legal entity, lead-form webhook URL. Prompt 3 pre-builds the alignment guard and the publication checklist diff; cannot create a real listing.

    - **Needed from owner:** (a) **Partner Center publisher / seller** identity (legal entity name on the commercial marketplace listing); (b) **Microsoft Partner ID / publisher id** and the transactable **offer id** to load into `Billing:AzureMarketplace:MarketplaceOfferId` for production (CI alignment: `python scripts/ci/assert_marketplace_pricing_alignment.py`); (c) **Tax profile + payout bank account** completion in Partner Center; (d) **Landing page URL** (must match `Billing:AzureMarketplace:LandingPageUrl` — public HTTPS, not localhost); (e) confirmation the **webhook** `https://<api-host>/v1/billing/webhooks/marketplace` is registered and JWT validation metadata (`OpenIdMetadataAddress`, `ValidAudiences`) matches the app registration Microsoft will call; (f) explicit **go-live date** and who records it in `CHANGELOG.md`.

9. **Stripe production go-live policy decisions** — chargeback / refund / dunning text for the order-form template; legal entity name on customer statements; live API key + webhook secret. Prompt 3 lands the production-safety guards but no live keys.

    - **Needed from owner:** (a) **Statement descriptor** / customer-facing legal name as it should appear on card statements; (b) **Chargeback, refund, and dunning** policy text for [`ORDER_FORM_TEMPLATE.md`](go-to-market/ORDER_FORM_TEMPLATE.md) and Trust Center; (c) **`sk_live_` + `whsec_` live signing secret** injected only via Key Vault / deployment secret store (never committed) and webhook endpoint URL `https://<prod-api-host>/v1/billing/webhooks/stripe` registered in Stripe **live** Dashboard; (d) who **owns** rotation and incident response if webhook delivery fails after deploy.

10. **PGP key for `security@archlucid.com`** — owner generates the key pair (or designates a custodian) and drops the public key into `archlucid-ui/public/.well-known/pgp-key.txt`. The CI guard in Prompt 4 turns green automatically the moment the file appears.

    - **Custodian mailbox (Resolved 2026-04-21):** **`security@archlucid.com`** is canonical. Generation + custodian-naming still owner-only.

11. **Workflow-integration sequencing (rescoped)** — **Prompt 5 (ServiceNow + Confluence) is deferred** — see Resolved table. When picking the next integration, sequence **Microsoft-native** options (Teams notifications, Logic Apps standard workflows, deeper ADO/GitHub) rather than Confluence/ServiceNow unless strategy changes.

12. **WCAG 2.2 AA conformance publication channel** — Trust Center page only, or also a public `/accessibility` page on the marketing site? Whether to create an `accessibility@archlucid.dev` alias or reuse `security@`.

13. **Public price list publication on marketing site** — `PRICING_PHILOSOPHY.md` is internal today. Marketplace publication (item 8) makes price public anyway; do we publish on the marketing site simultaneously or stay quote-on-request elsewhere?

    - **Repo wiring (2026-04-22):** anonymous **`POST /v1/marketing/pricing/quote-request`** + **`dbo.MarketingPricingQuoteRequests`** capture intent when live checkout is not the chosen path; CRM / Salesforce owner decisions still apply before production mail-forwarding.

14. **Cross-tenant pattern-library implementing ADR ownership** — approved per item above (`Resolved` table) but the implementing ADR has not been drafted; who owns it?

15. **Golden-cohort LLM budget approval** — Prompt 6 stands up a nightly golden-cohort drift detector. Owner approves a dedicated Azure OpenAI deployment + estimated monthly token budget for the nightly run.

    - **Shipped (simulator, no new Azure spend):** `archlucid golden-cohort lock-baseline [--cohort <path>] [--write]` captures committed-manifest SHA-256 fingerprints against a **Simulator** API host; `.github/workflows/golden-cohort-nightly.yml` can run drift assertions when repository variable `ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCKED` is set to `true` (cohort JSON must contain non-placeholder SHAs first — see item 33).
    - **Still gated on this item:** optional **real-LLM** cohort execution remains behind `ARCHLUCID_GOLDEN_COHORT_REAL_LLM` plus injected Azure OpenAI secrets on a protected GitHub Environment (the assistant does not provision deployments or spend).

16. **ADR 0021 Phase 3 — owner policy (Prompt 2 landed code + stopped at gate)** — Phase 2 catalog (`AuditEventTypes.Run.*` + dual-write), `IRunCommitOrchestrator` façade, and parity probe tooling shipped **2026-04-21**; Phase 3 **deletion** PRs remain blocked until ADR 0021 exit gates **(i)–(iv)**.
    - **Legacy `CoordinatorRun*` sunset (Resolved 2026-04-21):** **2026-05-15.** Product not yet released, so the strangler is being accelerated; the prior `Sunset: 2026-07-20` deprecation-header value drops to `Sunset: 2026-05-15` atomically across deprecation headers, parity-probe doc, [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md), and any client SDK release notes (see this change set). The earlier Draft [ADR 0028 — completion scaffold](adr/0028-coordinator-strangler-completion.md) is marked Superseded by 0029.
    - **Parity probe write path (Resolved 2026-04-21):** **Auto-commit to `main`** is acceptable. `coordinator-parity-daily.yml` was granted `contents: write` in this change set; if branch protection blocks the push, the workflow logs a marker and the operator pastes manually.
    - **ADR 0022 lifecycle (Resolved 2026-04-21, updated same-day follow-up):** Flip to **Superseded** by a Phase 3 **deletion** ADR **inside PR A itself** — gate (iv) was waived for pre-release per [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md), so there are no 14-rows to wait for; PR A merging is the trigger.
    - **Phase 3 PR A authorship (Resolved 2026-04-21 follow-up):** **Assistant drafts PR A end-to-end** in this repo (concretes + interfaces deletion, DI sweep, `DualPipelineRegistrationDisciplineTests` allow-list shrink, OpenAPI snapshot regen). **Queued for a dedicated session** — large surgical change set, deserves its own clean turn (will not be bundled with smaller items). Sequencing intent: ship the per-trigger Teams matrix + RLS object-name SQL migration session **first**, then PR A.
    - **Phase 3 gate (iv) — pre-release waiver (Resolved 2026-04-21 follow-up):** Waived alongside gate (i) for the pre-release window. Both gates restore automatically when V1 ships to a paying customer. See [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md) § Operational considerations for the rationale.

17. **Vertical starter — public-sector regulatory framing (Prompt 11)** — **Resolved 2026-04-21: ship BOTH** EU/GDPR (existing `templates/briefs/public-sector/`, `templates/policy-packs/public-sector/`) **and** US (FedRAMP / StateRAMP — new `templates/briefs/public-sector-us/`, `templates/policy-packs/public-sector-us/`). Wizard exposes a clear picker label.

    - **CJIS overlay scope (Resolved 2026-04-21 follow-up):** **FedRAMP Moderate / NIST SP 800-53 Rev. 5 only** in v1. The CJIS Security Policy reference was dropped from policy-pack metadata, brief, wizard preset, and rule descriptions in this change set. Authoring the full CJIS Security Policy v5.9.5 control mappings (~30 controls) is a future pack rather than a v1 overlay.

18. **Vertical starter templates — tiering (Prompt 11)** — **Resolved 2026-04-21: all five verticals stay in Core Pilot / trial** for v1. No paid-tier gating on industry templates. Documented in `templates/README.md`. Re-open if packaging strategy changes.

---

## Surfaced by 2026-04-21 second independent assessment (weighted **67.61%**)

These items came out of [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_67_61.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_67_61.md) §4 and the eight Cursor prompts in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_67_61.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_67_61.md). Each is **owner-only** — the assistant cannot answer them from repository state.

19. **First-paying-tenant graduation owner** — who watches the trial-to-paid event, validates the case study draft with the customer, and flips the row in `docs/go-to-market/reference-customers/README.md` from `Customer review` to `Published`? (Specific to Improvement 1 / Prompt 1.)

20. **Pen-test execution window for the awarded Aeronova SoW** — schedule the engagement, name the customer-shareable redacted-summary review owner, decide what (if anything) is published in the public Trust Center vs NDA-gated. (Improvement 2 / Prompt 2.)

    - **Custodian mailbox (Resolved 2026-04-21):** **`security@archlucid.com`**. All public surfaces aligned in this change set; assessor comms must use the same address.

21. **PGP key custodian for `security@archlucid.com`** — owner generates the key pair (or designates a custodian) and drops the public key into `archlucid-ui/public/.well-known/pgp-key.txt`. The CI guard added by Prompt 2 turns green automatically the moment the file appears.

    - **Custodian mailbox (Resolved 2026-04-21):** **`security@archlucid.com`** is the canonical UID. Generation + custodian-naming still owner-only.

22. **Marketplace + Stripe live go-live calendar — HELD (2026-04-21).** Owner has not chosen a calendar; production-safety guards (CI alignment, `BillingProductionSafetyRules`, `archlucid marketplace preflight`) continue to ship and stay green, but **no live keys are flipped**. When the owner picks a date, all four sub-items below become live decisions on that day; until then this item is intentionally parked, not abandoned.

    - **Needed from owner (when un-held):** (a) **Single cutover vs staged** — same maintenance window for Marketplace “Go live” + Stripe live keys, or Stripe first / Marketplace first (with rollback owners named per path); (b) **calendar dates** and **communication** to early customers if checkout is briefly unavailable; (c) confirmation **staging** remains on Stripe **TEST** + non-production webhook secrets until (a) is executed (see [`STRIPE_CHECKOUT.md`](go-to-market/STRIPE_CHECKOUT.md) § Staging); (d) who runs `archlucid marketplace preflight` + Partner Center certification checklist the day before either flip.

23. **Microsoft Teams connector scope** — **Resolved 2026-04-21: notification-only for v1.** Two-way (approve governance from Teams) is a V1.1 candidate; no Teams app manifest registration in v1. `MICROSOFT_TEAMS_NOTIFICATIONS.md` and the Logic Apps workflow keep their notification-only posture.

    - **Per-trigger opt-in (Resolved 2026-04-21 follow-up):** **Per-trigger opt-in matrix** per connection (defaults to all-on so existing rows keep current behaviour). Costs an extra `EnabledTriggersJson NVARCHAR(MAX) NOT NULL` column on `dbo.TenantTeamsIncomingWebhookConnections` and a UI checkbox matrix on `/integrations/teams`; Logic Apps workflow filters server-side before fan-out so tenants can't be spammed with disabled triggers. **Queued for a dedicated session** — needs a SQL migration + master DDL update + UI work + tests for coverage; will be bundled with the deferred RLS object-name SQL migration since both are SQL-shaped.

24. **ADR 0021 strangler completion target date** — **Resolved 2026-04-21: 2026-05-15** (latest-by). Product not yet released, so the strangler is accelerated. **[ADR 0029 — Coordinator strangler acceleration to 2026-05-15](adr/0029-coordinator-strangler-acceleration-2026-05-15.md)** is the operative decision record (it Supersedes the earlier Draft [ADR 0028 — completion scaffold](adr/0028-coordinator-strangler-completion.md), whose `_TODO (owner)_` placeholders this Q&A answered). Deprecation `Sunset:` headers are dropped from `2026-07-20` to `2026-05-15` atomically across `ArchLucid.Api/Filters/CoordinatorPipelineDeprecationFilter.cs`, ADR 0021 § Status note, ADR 0022 § Constraints / Components / Follow-up, and `docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md` § Phase 3 gate status. **Updated 2026-04-21 follow-up:** post-PR-A 30-day soak gate **(i)** **and** parity-rows gate **(iv)** are **both waived for the pre-release window only** (rationale in ADR 0029 § Operational considerations: no published clients to protect with a soak; no customer traffic to measure with the parity probe). Gates **(ii)** and **(iii)** remain in force; both are produced inside PR A's own CI run. **Net effect:** PR A is unblocked the moment gates (ii) and (iii) clear on the deletion branch; 2026-05-15 is a latest-by deadline, not a wait-for-evidence one.

25. **Golden-cohort dedicated Azure OpenAI deployment + monthly token budget** — needed to flip the nightly real-LLM golden-cohort run from optional to mandatory. (Improvement 8 / Prompt 8 — same shape as item 15 but specific to the cohort.)

    - **Repo wiring today:** drift + lock-baseline **refuse** when `ARCHLUCID_GOLDEN_COHORT_REAL_LLM` is truthy in the operator shell, and the placeholder `cohort-real-llm-gate` job in `golden-cohort-nightly.yml` stays disabled until this item plus secrets are in place.
    - **Needed from owner:** the same deployment/budget answers as item 15, scoped explicitly to the **20-row cohort** workload (expected longer prompts than a single interactive chat turn).

26. **VPAT publication decision** — produce a formal VPAT for accessibility published on the Trust Center, or stay with the WCAG 2.1 AA self-attestation in `ACCESSIBILITY.md`? (Adjacent to item 12 — accessibility publication channel.)

27. **Aggregate ROI bulletin publication cadence** — **Resolved 2026-04-21:** (a) **N = 5** for the first issue; (b) **owner-solo** sign-off; (c) **p50 + p90** both stay in v1 bulletins; (d) first publication window opens **once at least one PLG tenant is `Published`** (item 19). `AGGREGATE_ROI_BULLETIN_TEMPLATE.md` updated in this change set.

28. **Customer-supplied baseline soft-required at signup** — flip `baselineReviewCycleHours` from optional to soft-required (skippable but defaulted to model). Owner approves the UX change and the privacy-notice update.

    - **Needed from owner:** (a) sign-off on the shipped copy in [`docs/go-to-market/TRIAL_BASELINE_PRIVACY_NOTE.md`](go-to-market/TRIAL_BASELINE_PRIVACY_NOTE.md) (or delegate edits to legal/comms); (b) confirm the **GitHub main link** from the signup form to that note is the correct public surface vs hosting the same text on `archlucid.com`; (c) whether marketing wants **any** additional in-form disclaimer beyond the inline note + tooltip.

31. **Public `/why` comparison delivery** — **Resolved 2026-04-21: BOTH** PDF download (`GET /v1/marketing/why-archlucid-pack.pdf`) **and** inline page section, with a CI sync check that fails if comparison rows in `archlucid-ui/src/marketing/why-archlucid-comparison.ts` and the PDF builder diverge. Implementation tracked in this change set.

32. **Microsoft Teams notification triggers beyond v1 defaults** — **Resolved 2026-04-21: add ALL THREE** of `com.archlucid.compliance.drift.escalated`, `com.archlucid.advisory.scan.completed`, and `com.archlucid.seat.reservation.released` to the first production workflow alongside the existing `run.completed`, `governance.approval.submitted`, and `alert.fired`. Implementation tracked in this change set.

33. **Golden-cohort baseline SHA lock timing** — **Resolved 2026-04-21: lock today** from a single approved Simulator run. Operator runs `archlucid golden-cohort lock-baseline --write` after setting `ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED=true`. The nightly workflow flips from "contract test only" to manifest drift report once `tests/golden-cohort/cohort.json` carries non-zero SHAs. Real-LLM cohort run (item 15 / 25) **stays gated on owner budget**.

34. **Production Simmy / fault-injection game day** — The `simmy-chaos-scheduled.yml` workflow exposes a **`production`** `environment` input for documentation symmetry only. **Default remains staging-only execution.** Owner must approve any real production chaos (customer notification, SLO ownership, blast radius, rollback). See [`docs/runbooks/GAME_DAY_CHAOS_QUARTERLY.md`](runbooks/GAME_DAY_CHAOS_QUARTERLY.md).

---

## Quality-assessment cadence (Resolved 2026-04-21)

- **Cadence:** **Weekly.** Each pass produces a `QUALITY_ASSESSMENT_<date>_INDEPENDENT_<score>.md` plus a paired `CURSOR_PROMPTS_<...>.md` and updates this file.
- **Next pass:** **2026-04-28.**
- **Trigger to break cadence:** any of the three "score-moving" owner events (first PLG row `Published`, Marketplace listing live, Aeronova pen test summary published) — when one lands, run an unscheduled pass within 48 hours so the score reflects the new artefact.

---

## Related

| Doc | Use |
|-----|-----|
| [`docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) | **Latest** weighted independent assessment (68.60%) |
| [`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md) | Eight paste-ready Cursor prompts for the 68.60% assessment |
| [`docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_67_61.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_67_61.md) | Prior 2026-04-21 assessment (67.61%) |
| [`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_67_61.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_67_61.md) | Eight paste-ready Cursor prompts for the 67.61% assessment |
| [`docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md) | Earlier 2026-04-21 assessment (64.14%) |
| [`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md) | Six paste-ready Cursor prompts; #3 and #4 stop at owner gates |
| [`docs/archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md) | Prior assessment + §8 prompts |
| [`docs/go-to-market/PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md) § 5.4 | Reference-customer CI guard and discount re-rate |
