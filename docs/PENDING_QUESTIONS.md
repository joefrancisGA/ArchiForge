> **Scope:** Product and operations decisions the repo cannot resolve alone — consolidated pending list (supersedes scattered assessment §9 lists).
> **Updated 2026-05-05:** **Jira** and **ServiceNow** first-party connectors promoted from V1.1 to **V1 GA** scope — [`V1_SCOPE.md`](library/V1_SCOPE.md) §2.13; *Resolved 2026-05-05* below. **Confluence** first-party connector remains **V1.1** ([`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6).
> **Updated 2026-05-03 (commercial entity migration):** Phased playbook to move seller-of-record and related posture to **Francis Architecture, LLC** — [`runbooks/FRANCIS_ARCHITECTURE_LLC_V1_CUTOVER.md`](runbooks/FRANCIS_ARCHITECTURE_LLC_V1_CUTOVER.md). Until that runbook is executed and **`CHANGELOG.md`** records completion, **Joseph Francis (Sole Proprietorship)** (Partner Center) and **Joseph Francis** (Stripe webhook operational owner) resolutions below remain in force.
> **Updated 2026-05-03:** **Design partner** (signed commercial engagement) → **V1.1** commercial motion, **not** a V1 GA gate; **`(A)` headline assessments must not** penalize or foreground absence — see [`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6b and *Resolved 2026-05-03* below.
> **Updated 2026-05-01:** External **third-party** pen test release window → **V2**; **V1** = owner-conducted pen test; **no** vendor committed; V1 quality assessments **must not** penalize lack of third-party pen test — see [`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6c.
> **Updated 2026-04-27:** Resolved two long-standing deferred items:
> 1. Authentication Strategy for SaaS/On-Prem default: Require Entra ID or explicit API keys (Resolved).
> 2. Unified Error Responses for Hidden UI Features: 404 Not Found (Resolved).

# Pending questions (product and operations)

**Last updated:** 2026-05-05 — Jira + ServiceNow first-party connectors → **V1** scope (**[`V1_SCOPE.md`](library/V1_SCOPE.md)** §2.13); Confluence stays **V1.1**. Prior 2026-05-03 — design partner → **V1.1** commercial motion only; **`(A)` assessments must not** score absence (**`V1_DEFERRED.md`** §6b). Prior 2026-05-01 — third-party pen test → **V2**; V1 = **owner-conducted** (**`V1_DEFERRED.md`** §6c). **API key lifecycle** Q3 resolved: Terraform create; semiannual rotation; secret-channel distribution; **no API keys in production**.

**Earlier owner batches (2026-04-21 → 2026-04-24):** 2026-04-24 (independent §8 ten-improvement owner Q&A — 14 decisions), sixth pass (17 decisions), assessment §4 (11), commerce + connector + SaaS scope tables, 2026-04-22 assessment + ADR 0030 sub-tables, 2026-04-21 (19 + follow-up 5 + Teams/RLS bundle + Phase 3 re-scope). Older verbatim tables moved to **[`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md)** so this spine file stays within CI line budget; summaries and **Still open** items remain here.

Single place to track **decisions only a human owner** can make. When you ask what is still open, start here. Items marked **Resolved** stay for audit trail; remove them only when you intentionally shrink the file.

---

## Resolved 2026-05-05 (Jira + ServiceNow — promoted to V1 scope)

| Sub-decision | Decision | Affects |
|---|---|---|
| **First-party Jira connector** | **In scope for V1 GA** — committed product obligation per [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) §2.13 (issue create + correlation back-link; bi-directional status sync in V1 window, may fast-follow). **Supersedes** prior V1.1-only pinning from 2026-04-23 / 2026-04-24 for **Jira** only. | [`V1_SCOPE.md`](library/V1_SCOPE.md), [`INTEGRATION_CATALOG.md`](go-to-market/INTEGRATION_CATALOG.md), recipe index cross-links |
| **First-party ServiceNow connector** | **In scope for V1 GA** — same §2.13 (finding → `incident`; optional `cmdb_ci` planning unchanged; two-way SNOW→ArchLucid status sync **not** committed unless owner adds). **Supersedes** prior V1.1-only pinning for **ServiceNow** only. | Same |
| **Engineering sequencing** | Unchanged: **ServiceNow before Jira** (see **Resolved 2026-04-27 (ITSM V1.1 first-party implementation priority)** — that resolution’s **release window** label for ServiceNow/Jira is **obsolete**; build order remains valid). **Confluence** remains **V1.1** ([`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6). | ITSM ADRs, implementation plan |
| **Customer-owned recipes** | **Optional** bridges (Power Automate / Logic Apps / templates) remain valid for tenants that prefer them or need coverage before connector enablement — they do **not** replace the V1 first-party commitment. | [`docs/integrations/recipes/README.md`](integrations/recipes/README.md) |

---

## Resolved 2026-05-03 (Design partner vs V1 headline assessments)

| Sub-decision | Decision | Affects |
|---|---|---|
| **Design partner as product/pilot gate** | **Not a V1 gate.** Closing a **signed design partner** (per [`docs/go-to-market/PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md)) is a **V1.1** commercial motion alongside reference-customer publication — see [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) §6b. | V1 GA and Core Pilot remain defined by [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) without a design-partner prerequisite. |
| **Assessment scoring and narrative** | **`(A)` must not drop** for lacking a design partner. **Do not** list “no design partner yet” as a headline defect, recurring open question, or scored pillar gap unless the user explicitly asked for **GTM pipeline** depth — then **`(B)` informational** only (zero weight on **`(A)`**). | `.cursor/rules/Assessment-Scope-V1_1.mdc`; future independent assessments. |

---

## Resolved 2026-04-27 (Post-Assessment Q&A)

| Sub-decision | Decision | Affects |
|---|---|---|
| **Default Authentication Strategy for SaaS vs. On-Prem** | **Require Entra ID configuration or a static API key.** The open `DevelopmentBypassAll` default must not be the production posture. | `ArchLucid.Api/Auth/Models/ArchLucidAuthOptions.cs`, `docs/library/` |
| **Unify Error Responses for Hidden UI Features** | **404 Not Found.** Restricted API routes will return 404 instead of 403 to prevent feature and resource enumeration by unauthorized tiers/roles. | `ArchLucid.Api/Filters/CommercialTenantTierFilter.cs`, `ArchLucid.Api.Tests/`, Operator UI interpretation logic. |

**Refined 2026-04-28 (Assessor B):** for **tenant-scoped** run/manifest APIs, owner prefers **403** with clear Problem Details for **debuggability**; **404** remains preferred for **admin** surfaces. See **Resolved 2026-04-28** — implementation work reconciles this split with the filter above on a per-route basis.

### Resolved 2026-04-27 (ITSM V1.1 first-party implementation priority)

**Superseded in part (2026-05-05):** **ServiceNow** and **Jira** are **V1 GA** commitments ([`V1_SCOPE.md`](library/V1_SCOPE.md) §2.13); this resolution’s **build order** (**ServiceNow before Jira**) remains authoritative. **Confluence** stays **V1.1** ([`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6).

| Sub-decision | Decision | Affects |
|---|---|---|
| **First-party ITSM / Atlassian connector build order** | **ServiceNow first**, then **Jira** for **V1** ITSM connectors; **Confluence** follows in **V1.1** unless a superseding owner entry reorders. Does **not** alter Microsoft-native preference (Teams, Logic Apps, ADO, GitHub) as the primary integration anchor. **Scope pinning:** see **Resolved 2026-05-05** — ServiceNow/Jira are **not** V1.1-deferred. | [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) §2.13, [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) §6, [`docs/go-to-market/INTEGRATION_CATALOG.md`](go-to-market/INTEGRATION_CATALOG.md), ITSM planning ADRs |

---

## Resolved 2026-04-28 (Assessor B follow-up — `docs/library/QUALITY_ASSESSMENT_2026_04_27_INDEPENDENT_B_64_63.md` §9)

| Sub-decision | Decision | Affects / notes |
|---|---|---|
| **Auth modes (first pilot)** | **Both** in spirit: people use **JWT** (Entra / OIDC); automation uses **API keys** where the environment still allows. | With **`ArchLucidAuth:RequireJwtBearerInProduction = true`**, **Production** must use **`ArchLucidAuth:Mode = JwtBearer`** — **not** `ApiKey` at the host. Automate against prod with **client-credentials JWT** (or a non-prod host that accepts API keys). |
| **Require JWT in production** | **Yes** — `ArchLucidAuth:RequireJwtBearerInProduction` **true** for production. | `ArchLucid.Host.Core/Startup/Validation/Rules/AuthenticationRules.cs`, `docs/library/SECURITY.md` |
| **403 vs 404 (enumeration vs debuggability)** | **Split:** **404** when denying access to **admin**-style surfaces; **403** (clear Problem Details) for **tenant-scoped** run/manifest and main product APIs. | Align handlers and filters in a follow-up; refines 2026-04-27 row for tenant paths. |
| **Azure spend (staging + production)** | **Target:** combined **no more than ~USD $400 / month** until there are paying customers. | FinOps; buyer-facing `ROI_MODEL.md` infrastructure bands are often higher — **your** COGS is leaner pre-customer. |
| **Third-party pen test / “shareable” date (Q8)** | **No** third-party pen-test **report** or **customer-shareable** redacted summary **yet**. Security work in progress: **internal / founder-led** penetration testing plus in-repo and CI controls (ZAP, Schemathesis, runbooks, Trust Center). **When** a third-party firm is engaged, shareable vs NDA-only is set with that SoW. | Procurement narrative; do **not** imply independent attestation. Trust Center honest labels unchanged. |
| **Uptime / 30-day rollup (Q9)** | **No 30-day measured rollup** for `archlucid.net` / `staging` **yet** — not publishing an **achieved** availability %. **Stated SLO in docs** remains **99.9%** availability over a **30-day** rolling window (e.g. `docs/library/API_SLOS.md`, Prometheus rules in-repo). | Buyer narrative: **target** from docs, not a verified operational score until monitoring exports a 30-day number. |

### Resolved 2026-04-29 (2026-04-21 assessment backlog — item 12 — accessibility publication channel)

| Sub-decision | Decision | Notes / surfaces |
|---|---|---|
| **WCAG conformance publication surface** | **Public marketing route** (`/accessibility`) **is canonical** — not Trust Center-only. Buyer-facing narrative and tooling live on **`archlucid-ui/src/app/(marketing)/accessibility/page.tsx`** (pipeline from root **`ACCESSIBILITY.md`** per **`CHANGELOG.md` 2026-04-22**). **`docs/go-to-market/TRUST_CENTER.md`** may cross-link. | Aligns backlog item **12** with shipped UI; WCAG clause level in policy text follows root **`ACCESSIBILITY.md`**. |
| **Mailbox** | **`accessibility@archlucid.net`** — **use this alias** for accessibility/WCAG reports; **do not** advertise **`security@`** as the sole contact for accessibility-only topics. Custodian routing is documented in [`docs/security/ACCESSIBILITY_MAILBOX.md`](security/ACCESSIBILITY_MAILBOX.md). | Reconfirmed by owner **2026-04-29**; UI mailto on [`AccessibilityMarketingPublicView`](../archlucid-ui/src/components/marketing/AccessibilityMarketingPublicView.tsx); backlog question’s `accessibility@archlucid.dev` hypothetical **superseded** by **`.net`**. |

### Resolved 2026-04-29 (Governance SoD — dual display identity vs Entra `oid`)

| Sub-decision | Decision | Affects / notes |
|---|---|---|
| **Schema for canonical SoD keys** | **Option B (additive columns).** **`RequestedByActorKey`** and **`ReviewedByActorKey`** on **`dbo.GovernanceApprovalRequests`** store canonical keys; **`RequestedBy`** / review display fields stay human-readable. | [`ArchLucid.Persistence/Migrations/130_GovernanceApprovalRequests_ActorKeys.sql`](../ArchLucid.Persistence/Migrations/130_GovernanceApprovalRequests_ActorKeys.sql); [`ArchLucid.Application/Common/ActorContext.cs`](../ArchLucid.Application/Common/ActorContext.cs); [**ADR 0034**](adr/0034-segregation-of-duties-entra-oid-actor-keys.md) |
| **Residual risk (organization-level)** | **Accepted with documentation.** A single natural person who holds **two Entra principals** (e.g. **user** + **separate** service principal) still has **two `oid` values** — SoD cannot merge those without org policy (e.g. privileged-access reviews) or future product work. **API-key-only** hosts keep **display-string** SoD only (no `oid`). | Compensating controls and scope called out in ADR 0034. |

### Resolved 2026-05-01 (Assessor B §9 — Q3 API key lifecycle)

| Sub-decision | Decision | Notes |
|---|---|---|
| **Creation** | **Terraform** | Keys used in **non-production** (and similar) are created/managed via IaC, not ad-hoc portal minting. |
| **Rotation** | **Twice per year** | Calendar cadence (~six months). |
| **Distribution** | **Secret channel only** | No plaintext email/Slack; approved secret stores / channels only. |
| **Break-glass (production)** | **No API keys in production** | **Production** must **not** issue or rely on ArchLucid API keys; operator and automation access uses **JWT** (Entra / OIDC), including client-credentials for automation, consistent with **Resolved 2026-04-28** in this file. Emergency access does **not** bypass this via a prod API key—use identity/session recovery and infra controls. |

**Resolved 2026-04-29 (performance regression sentinel approach)** — **Named-query allowlist (Option A).** SaaS product — no customer DBAs, team owns the full stack. SQL text snapshots produce high CI noise (false positives on every whitespace / ORM change) and erode gate trust; allowlist keeps the gate high-signal. A query name that crosses its p95 threshold fails CI; everything not in the allowlist is invisible until deliberately added. Allowlist grows organically as new critical paths are identified. Implementation deferred to **TB-003** in `docs/library/TECH_BACKLOG.md`.

**Resolved 2026-04-29 (production config warnings → OTel)** — **OTel counter + log.** Startup validation warnings emit both a structured `LogWarning` line and increment `archlucid_startup_config_warnings_total` (label `rule_name`; bounded cardinality — rule names are code constants). A Terraform alert rule fires when the counter is non-zero on a Production-classified host. Implementation deferred to **TB-002** in `docs/library/TECH_BACKLOG.md`.

**Resolved 2026-04-29 (audit coverage on async paths)** — **Best-effort, never block.** Audit write failures on async / fire-and-forget paths must **not** surface to the user or degrade their experience. Log the failure as a structured warning (include correlation ID and the missed event type), increment a counter metric (`archlucid_audit_write_failures_total`), but **continue processing** regardless. Fail-closed behaviour is reserved for **synchronous, user-visible** paths where the audit record is part of the response contract (e.g. governance approval submission). Affects `ArchLucid.Provenance/`, dual-write design, and `docs/library/AUDIT_COVERAGE_MATRIX.md` wording.

**Resolved 2026-04-28 (Q7 — agent eval / real LLM signal)** — **Manual gate preferred by owner** until a CI rollup is a habit. Use **`docs/quality/MANUAL_QA_CHECKLIST.md` §8.3** (*Real-LLM / agent output quality*) to perform a **staging real-LLM** run, subjective sponsor-readiness check, and a one-line private note. **Optional later:** add “last time I ran §8.3” / link to a **green** `agent-eval-datasets-nightly` run when you start tracking it. Automated nightly does **not** replace §8.3; it **complements** it.

---

## Resolved 2026-04-24 (independent assessment §8 ten-improvement owner Q&A — 14 decisions)

These decisions came out of a structured owner Q&A on the ten improvement opportunities surfaced during the 2026-04-24 independent re-assessment session. The improvement numbering (**1**–**10**) is the one used in that session's chat-emitted improvement list (SCIM Service Provider, real-Azure-OpenAI first-value path, Confluence Cloud connector, brand-rebrand PR-2, opt-in tour copy, days-since-first-commit badge, single-page contributor index, audit-count CI reconciliation, privacy notice, operator cost-preview). They are recorded here as the single source of truth; downstream files (`AUDIT_COVERAGE_MATRIX.md`, `SCIM_OPERATOR_RUNBOOK.md`, `FIRST_REAL_VALUE.md`, `CONTRIBUTOR_ON_ONE_PAGE.md`, `SPONSOR_BANNER_FIRST_COMMIT_BADGE.md`, ADR 0032 / 0033 stubs, `appsettings.json` defaults, the rebrand workstream tracker) update against this table in the implementation PRs that follow. **No production code touched in this entry** — this is a decision snapshot.

### Improvement 1 — SCIM 2.0 Service Provider (V1.1 commit)

| Sub-decision | Decision | Affects |
|---|---|---|
| **1a — group-to-role override semantics** | **Override enabled.** SCIM group membership wins over manual `dbo.UserRoles` entries. The role mapper writes resolved roles with `Source = "Scim"`; manual rows carry `Source = "Manual"` and lose on conflict. Adds a new `RoleOverriddenByScim` typed audit event (now **8** new SCIM constants instead of 7; matrix marker bumps accordingly). | `ArchLucid.Application/Scim/RoleMapping/GroupToRoleMapper.cs`, `dbo.UserRoles.Source` column, `AuditEventTypes.cs`, `AUDIT_COVERAGE_MATRIX.md`. |
| **1b — token rotation cadence** | **Six-month reminder cadence.** New option `Scim:TokenRotationReminderDays = 180`. A daily `ScimTokenRotationReminderJob` hosted service emits `archlucid.scim.token.rotation_due` warning logs and writes a `dbo.AdminNotifications` row when token age exceeds the threshold. **No automatic revocation.** Owner can disable by setting the option to `0`. | `ArchLucid.Application/Scim/Tokens/`, `appsettings.json`, `dbo.AdminNotifications`. |
| **1c — Entra app gallery listing** | **Owner-undecided. Stay as TODO.** No stub PR. The SCIM operator runbook gets a clearly-marked `## Future: Entra app gallery listing` section with a one-paragraph explainer of the Microsoft Partner Center "Application Gallery" submission process (publisher verification, SCIM compliance demo, partner team review window). When the owner is ready, the gallery listing PR is its own ~100-line change against that runbook + a screenshots folder. | `docs/integrations/SCIM_OPERATOR_RUNBOOK.md` (Future section only). |

### Improvement 2 — Real Azure OpenAI "first real value" path

| Sub-decision | Decision | Affects |
|---|---|---|
| **2a — `--strict-real` posture for golden-cohort CI** | **Opt-in to `--strict-real`.** The nightly golden-cohort gate workflow passes `--strict-real`, which means real-mode failures fail the gate instead of silently falling back to the simulator. Developer-laptop `archlucid try --real` keeps the friendly fallback (more useful for evaluators than a hard failure). | `.github/workflows/golden-cohort-real-llm-scheduled.yml`, `ArchLucid.Cli/Commands/TryCommandOptions.cs`. |
| **2b — default `MaxCompletionTokens`** | **16,384.** Matches the `gpt-4o` / `gpt-4.1` family model-enforced ceiling — setting higher (e.g., 32K) has no functional effect on those deployments. Worst-case cost per `try --real` ≈ $1.25 across a 5-call pipeline at gpt-4o pricing. Operators on `o1` / `o3` reasoning deployments override per-host with `AZURE_OPENAI_MAX_COMPLETION_TOKENS=32768` (or higher); the env var override is in the design. **Cap is per-LLM-call, not per-run, and is a CEILING, not a TARGET** — most calls land at 1–4K. | `appsettings.json` (`AzureOpenAI:MaxCompletionTokens`), `docker-compose.real-aoai.yml`, `docs/library/FIRST_REAL_VALUE.md` cost-explainer section. |

### Improvement 3 — Confluence Cloud connector

> **DEFERRED TO V1.1 — owner decision 2026-04-24** (**updated 2026-05-05**). **Confluence** first-party connector remains **out of scope for V1** and **in scope for V1.1**. **Jira** first-party connector was **promoted to V1 GA** ([`V1_SCOPE.md`](library/V1_SCOPE.md) §2.13; **Resolved 2026-05-05**) — do **not** block Confluence implementation decisions on Jira anymore. Sub-decisions below are **Confluence-only** V1.1 design intent. See [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) §6 and [`docs/go-to-market/INTEGRATION_CATALOG.md`](go-to-market/INTEGRATION_CATALOG.md). For **Confluence during V1**, operators integrate via **CloudEvents webhooks** or **REST API**.

| Sub-decision | Decision | Affects (V1.1) |
|---|---|---|
| **3a — space targeting** | **Single fixed `Confluence:DefaultSpaceKey`.** No multi-space or per-tenant routing in v1.1 initial shape. | `ArchLucid.Integrations.Confluence/ConfluenceIntegrationOptions.cs`. |
| **3b — auth scheme** | **API token (basic auth) for v1.1.** OAuth deferred to a follow-on PR if/when a buyer asks. Documented in `docs/integrations/CONFLUENCE.md` § "v1.1 limits". | `ArchLucid.Integrations.Confluence/ConfluencePagePublisher.cs`, `docs/integrations/CONFLUENCE.md`. |

### Improvement 4 — Brand rebrand workstream PR-2

| Sub-decision | Decision | Affects |
|---|---|---|
| **4a — screenshot gallery refresh shape** | **Keep separate as PR-2.5.** The screenshot gallery refresh runs as its own PR after PR-2 merges and before PR-3, so PR-2's wording-swap diff stays small and reviewable. | `docs/architecture/REBRAND_WORKSTREAM_2026_04_23.md` (PR sequence table). |

### Improvement 5 — RESOLVED: opt-in tour copy (2026-04-24)

> **Resolved 2026-04-24 (tour copy approved).** Owner approved all five step copies. `TourStepPendingApproval` wrappers removed in a single batch PR per option B. No mixed-state UI.

| Sub-decision | Decision | Affects |
|---|---|---|
| **5a — wrapper-removal rollout shape** | **Option B — batch all five in one PR.** All five `<TourStepPendingApproval>` → `<>` fragment swaps + the five copy bodies land in a single PR after the owner approves all five copies. Avoids mixed-state UI ("step 2 polished, step 3 says pending approval") which reads worse than uniform "all five pending → all five live". One PR review instead of five. The marker-rendering pattern stays per-step (Q8 stop-and-ask), so no risk of accidental disclosure during the wait. | `archlucid-ui/src/components/OptInTour.tsx`, the five tour-step fragments, `OptInTour.test.tsx`. |

### Improvement 6 — Days-since-first-commit badge

| Sub-decision | Decision | Affects |
|---|---|---|
| **6a — operator-local timezone toggle** | **Defaults — UTC only in v1.** Operator-local timezone toggle is a follow-on if a pilot asks; UTC keeps the badge unambiguous. | `archlucid-ui/src/components/EmailRunToSponsorBanner.tsx`, `docs/library/SPONSOR_BANNER_FIRST_COMMIT_BADGE.md`. |

### Improvement 7 — Single-page contributor index

| Sub-decision | Decision | Affects |
|---|---|---|
| **7a — line cap** | **100 lines (hard cap).** CI guard `scripts/ci/assert_contributor_on_one_page_size.py` enforces ≤ 100 lines on `docs/CONTRIBUTOR_ON_ONE_PAGE.md`; CI fails on overflow. | `docs/CONTRIBUTOR_ON_ONE_PAGE.md`, `scripts/ci/assert_contributor_on_one_page_size.py`, `.github/workflows/ci.yml`. |

### Improvement 8 — Audit-count documentation / CI guard reconciliation

| Sub-decision | Decision | Affects |
|---|---|---|
| **8a — drift resolution direction** | **Default: prefer adding the missing constant** (treat `AuditEventTypes.cs` as the source of truth and refresh `AUDIT_COVERAGE_MATRIX.md` to match). The new `scripts/ci/assert_audit_const_count.py` does both a count check **and** a name-set check so silent drift in either direction fails CI. | `scripts/ci/assert_audit_const_count.py`, `docs/library/AUDIT_COVERAGE_MATRIX.md`, `.github/workflows/ci.yml`. |

### Improvement 9 — RESOLVED: public privacy policy (2026-05-03)

| Sub-decision | Decision | Affects |
|---|---|---|
| **9a — canonical public privacy policy** | **Owner-approved as-shipped.** The marketing **`/privacy`** route renders **`docs/go-to-market/PRIVACY_POLICY.md`** (see `archlucid-ui/src/lib/privacy-policy-marketing.ts`). There is **no** separate `docs/security/PRIVACY_NOTICE_DRAFT.md` file; superseded scaffold references pointed at that obsolete path — **GDPR/CCPA prose for visitors and product users** lives **only** in **`PRIVACY_POLICY.md`**; operator-facing **Article 30** records remain **`docs/security/PRIVACY_NOTE.md`**. Effective date unchanged unless a future editorial pass makes material substantive edits (see **`PRIVACY_POLICY.md`** §12). | `docs/go-to-market/PRIVACY_POLICY.md`, `archlucid-ui/src/app/(marketing)/privacy/page.tsx`, `docs/security/PRIVACY_NOTE.md` |

### Improvement 10 — Operator UI in-product cost preview

| Sub-decision | Decision | Affects |
|---|---|---|
| **10a — endpoint auth posture** | **`[AllowAnonymous]` on `GET /v1/agent-execution/cost-preview`.** Endpoint returns host-level configuration only — no tenant data, no PII, no run data. Marketing surface (`/pricing`, `/see-it`) can read it without auth. Documented in the controller XML doc and in `docs/library/COST_PREVIEW_ENDPOINT.md` (new). | `ArchLucid.Api/Controllers/AgentExecution/CostPreviewController.cs`, `archlucid-ui/src/app/(operator)/runs/new/NewRunWizardClient.tsx`, marketing pages. |

### Cross-cutting notes (2026-04-24)

| Item | Note |
|---|---|
| **Improvement 2 — `MaxCompletionTokens` clarification** | The cap is per-LLM-call (each agent in the pipeline), not per-run; and a CEILING, not a TARGET. Most calls in the demo seed land at 1–4K tokens. The `gpt-4o` family caps at 16,384 model-side regardless of the client setting; only `o1` / `o3` / `gpt-5` reasoning deployments benefit from a higher value. The 16K default + per-host env override gives operators a safe starting point with a clean upgrade path. |
| **Improvement 5 — wrapper-removal pattern reused** | The Improvement 7 PR (single-page contributor index) documents the inline marker-removal protocol used by the tour-copy batch PR. Same pattern, same test discipline. |
| **Improvement 8 — audit-count guard hardening** | The new Python guard supersedes the simple count-only guard. CI will now fail with a diff (added vs. removed constants) instead of just a count mismatch, eliminating the "drift the matrix to match the code" temptation that the count-only guard accidentally permitted. |
| **No new pending items** | This Q&A round closed all sub-decisions surfaced in the 2026-04-24 chat-emitted improvement list. Improvements 4 and 9 remain DEFERRED (rebrand sequencing already gated by PR-2 merge, privacy notice still owner-blocked). |

---

## Resolved history (2026-04-21 — 2026-04-23)

Verbatim owner decision tables for **2026-04-22** (assessment + ADR 0030), **2026-04-21** (19 + follow-up + Phase 3 + Teams/RLS + prior session), and **2026-04-23** (SaaS follow-on, Jira/ServiceNow/Slack, reference customer, commerce un-hold, sixth pass, assessment §4) live in **[`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md)** so this spine file stays within the CI line budget. *Still open* items below retain links — where they used to say "see table above" for those batches, read the matching section in the archive file.

---

## Still open (needs your input later)

1. **Design-partner row (`DESIGN_PARTNER_NEXT`)** — When a **named** design partner (not PLG) is authorized, replace `<<CUSTOMER_NAME>>` in [`DESIGN_PARTNER_NEXT_CASE_STUDY.md`](go-to-market/reference-customers/DESIGN_PARTNER_NEXT_CASE_STUDY.md) and move the table row through **Drafting → Customer review → Published** per [`reference-customers/README.md`](go-to-market/reference-customers/README.md).

2. **External pen-test vendor (third-party)** — **V2** — when funded, select vendor, award SoW, fill `<<vendor>>` / `<<TBD>>` in [`docs/security/pen-test-summaries/2026-Q2-SOW.md`](security/pen-test-summaries/2026-Q2-SOW.md), and replace placeholders in [`2026-Q2-REDACTED-SUMMARY.md`](security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md) after delivery. **Owner 2026-05-01:** there is **no** commitment to Aeronova or any other third-party vendor; **V1** pen testing is **owner-conducted** ([`2026-Q2-OWNER-CONDUCTED.md`](security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md)). **Independent quality assessments must not** penalize V1 for lacking a third-party pen test.

    - **Custodian mailbox (Resolved 2026-04-21):** **`security@archlucid.net`** is canonical. Trust Center, `SECURITY.md`, `INCIDENT_COMMUNICATIONS_POLICY.md`, and `security.txt` all aligned in this change set; the eventual PGP UID must use the same address.
    - **Release window (owner 2026-05-01):** **V2** for third-party engagement + assessor deliverables. Prior "V1.1" pen-test publication framing is **superseded** for external scope — see [`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6c and [`V1_SCOPE.md`](library/V1_SCOPE.md) §3.

3. **PGP for coordinated disclosure** — [`SECURITY.md`](../SECURITY.md) now points at `archlucid-ui/public/.well-known/pgp-key.txt` as **pending** until the custodian commits the public key. **Mailbox alignment (Resolved 2026-04-21): the UID is `security@archlucid.net`.** Items 10 / 21 still own the actual key generation.

    - **Release window (Resolved 2026-04-23, sixth pass):** **V1.1.** Key generation, drop, and `SECURITY.md` / marketing `/security` updates are no longer V1 obligations — see Q12 / Q13 / Q14 in *Resolved 2026-04-23 (sixth pass — fresh independent assessment §10 owner Q&A — 17 decisions)* in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part B) and [`V1_DEFERRED.md`](library/V1_DEFERRED.md) § 6c. UID is gated on `archlucid.net` domain acquisition.

4. **Next Microsoft-aligned workflow integration** — GitHub manifest-delta and Azure DevOps pipeline tasks are shipped ([`GITHUB_ACTION_MANIFEST_DELTA.md`](integrations/GITHUB_ACTION_MANIFEST_DELTA.md), [`AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md`](integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md)). **First-party ServiceNow + Jira are V1 GA commitments** ([`V1_SCOPE.md`](library/V1_SCOPE.md) §2.13; **Resolved 2026-05-05**). **Confluence** first-party connector stays **V1.1** ([`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6; Improvement 3). Next anchor for **additional** workflow breadth remains a **product** call among remaining Microsoft-native surfaces (e.g. Teams / Logic Apps fan-out per ADR 0019), optional versus completing ship-ready ITSM connectors.

---

## Six quality prompts (2026-04-20 independent assessment) — execution status

| Prompt | Intent | Repo status (2026-04-21) |
|--------|--------|--------------------------|
| **8.1** Reference customer + CI guard | Case study assets, table row, merge-blocking when `Published` | **Done** (auto-flip in `ci.yml`); **extended** with PLG case study + table row in this change set. |
| **8.2** `archlucid pilot up` | One-command Docker pilot | **Done** — [`ArchLucid.Cli/Commands/PilotUpCommand.cs`](../ArchLucid.Cli/Commands/PilotUpCommand.cs). *Note:* `POST /v1.0/demo/seed` is **Development-only** and needs **ExecuteAuthority**; the Docker path relies on **demo seed on startup** instead. |
| **8.3** First-value report | CLI + `GET /v1/pilots/runs/{id}/first-value-report` | **Done** — see CHANGELOG 2026-04-20. |
| **8.4** GitHub Action manifest delta | Composite action + docs + example workflow | **Done** — `integrations/github-action-manifest-delta/`, [`docs/integrations/GITHUB_ACTION_MANIFEST_DELTA.md`](integrations/GITHUB_ACTION_MANIFEST_DELTA.md). |
| **8.5** Persistence consolidation | Proposal doc only | **Done** — [`docs/PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md`](library/PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md). |
| **8.6** Pen-test publication path | Templates + Trust Center | **Done** — `docs/security/pen-test-summaries/`; **extended** with owner-assessment draft + Trust Center wording in this change set. |

---

## Still open — surfaced by 2026-04-21 independent assessment

These came out of [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md`](archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md) § 9 and the six Cursor prompts in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md`](archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md). Each is **owner-only** — the assistant cannot answer them from repository state.

5. **External (third-party) pen-test scope and budget** — **V2** — vendor selection, scope (web app only / web + infra / web + infra + LLM threat model), test window, funding. Picks up where item 2 above leaves off. **Does not** gate V1; V1 uses **owner-conducted** pen testing per [`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6c.

6. **SOC 2 Type I assessor + audit period start date** — **Stays deferred (Resolved 2026-04-21).** Interim posture: self-assessment + Trust Center honesty. **Revisit trigger:** owner-defined ARR threshold — assistant cannot set the dollar figure; the Trust Center compliance-and-certifications row was rewritten in this change set to make the trigger explicit. Sub-question still open: **what ARR figure?**

7. **Reference-customer publication ownership and discount-for-reference percent** — **Discount Resolved 2026-04-21:** **15% standardized.** `PRICING_PHILOSOPHY.md` § 5.4 was promoted from "suggested" to "standard" in this change set. **Still open (item 19):** ownership of graduating the first PLG row from `Customer review` to `Published`.

8. **Marketplace publication go-live decision** — sign off on Azure Marketplace SaaS plan SKUs (aligned to PRICING_PHILOSOPHY tiers), legal entity, lead-form webhook URL. Prompt 3 pre-builds the alignment guard and the publication checklist diff; cannot create a real listing.

    - **Needed from owner:** (a) **Partner Center publisher / seller** identity (**Resolved 2026-04-27:** Joseph Francis (Sole Proprietorship) — **planned successor:** Francis Architecture, LLC per [`runbooks/FRANCIS_ARCHITECTURE_LLC_V1_CUTOVER.md`](runbooks/FRANCIS_ARCHITECTURE_LLC_V1_CUTOVER.md), which supersedes this sub-row only after execution + `CHANGELOG.md`); (b) **Microsoft Partner ID / publisher id** and the transactable **offer id** to load into `Billing:AzureMarketplace:MarketplaceOfferId` for production (CI alignment: `python scripts/ci/assert_marketplace_pricing_alignment.py`); (c) **Tax profile + payout bank account** completion in Partner Center; (d) **Landing page URL** (**Resolved 2026-04-27:** `https://archlucid.net/signup`); (e) confirmation the **webhook** `https://<api-host>/v1/billing/webhooks/marketplace` is registered and JWT validation metadata (`OpenIdMetadataAddress`, `ValidAudiences`) matches the app registration Microsoft will call; (f) explicit **go-live date** and who records it in `CHANGELOG.md`.

9. **Stripe production go-live policy decisions** — chargeback / refund / dunning text for the order-form template; legal entity name on customer statements; live API key + webhook secret. Prompt 3 lands the production-safety guards but no live keys.

    - **Needed from owner:** (a) **Statement descriptor** / customer-facing legal name as it should appear on card statements (**Resolved 2026-04-27:** `ARCHLUCID PLATFORM`); (b) **Chargeback, refund, and dunning** policy text for [`ORDER_FORM_TEMPLATE.md`](go-to-market/ORDER_FORM_TEMPLATE.md) and Trust Center (**Resolved 2026-04-27:** text reviewed and approved by owner); (c) **`sk_live_` + `whsec_` live signing secret** injected only via Key Vault / deployment secret store (never committed) and webhook endpoint URL `https://<prod-api-host>/v1/billing/webhooks/stripe` registered in Stripe **live** Dashboard; (d) who **owns** rotation and incident response if webhook delivery fails after deploy (**Resolved 2026-04-27:** Joseph Francis — after [`runbooks/FRANCIS_ARCHITECTURE_LLC_V1_CUTOVER.md`](runbooks/FRANCIS_ARCHITECTURE_LLC_V1_CUTOVER.md), update runbooks to the LLC **officer role** / named delegate as counsel directs).

10. **PGP key for `security@archlucid.net`** — owner generates the key pair (or designates a custodian) and drops the public key into `archlucid-ui/public/.well-known/pgp-key.txt`. The CI guard in Prompt 4 turns green automatically the moment the file appears.

    - **Custodian mailbox (Resolved 2026-04-21):** **`security@archlucid.net`** is canonical. Generation + custodian-naming still owner-only.

11. **Workflow-integration sequencing — Resolved 2026-05-05 (scope update).** **ServiceNow** and **Jira** first-party connectors are **V1 GA** commitments ([`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) §2.13). **Engineering order:** **ServiceNow before Jira** (historical **Resolved 2026-04-27** build-order decision — release-window label superseded by **Resolved 2026-05-05**). **Confluence** first-party connector remains **V1.1** ([`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) §6; owner decision 2026-04-24 — Improvement 3). **Slack** remains **V2** ([`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) Part B: *Resolved 2026-04-23 (ServiceNow + Slack connector scope)*). Prefer **Microsoft-native** options (Teams — shipped in V1, Logic Apps, ADO, GitHub) where they suffice; customer-owned recipes remain optional bridges ([`docs/integrations/recipes/README.md`](integrations/recipes/README.md)).

12. **WCAG conformance publication channel — Resolved 2026-04-22 (reconfirmed 2026-04-29).** **Public `/accessibility`** on the marketing site is **canonical** (not Trust Center-only). Use **`accessibility@archlucid.net`** for accessibility reports — **not** `security@` as the advertised channel for WCAG-only follow-up. See **Resolved 2026-04-29** above, [`CHANGELOG.md`](CHANGELOG.md) (2026-04-22), and [`docs/security/ACCESSIBILITY_MAILBOX.md`](security/ACCESSIBILITY_MAILBOX.md).

13. **Public price list publication on marketing site** — `PRICING_PHILOSOPHY.md` is internal today. Marketplace publication (item 8) makes price public anyway; do we publish on the marketing site simultaneously or stay quote-on-request elsewhere?

    - **Repo wiring (2026-04-22):** anonymous **`POST /v1/marketing/pricing/quote-request`** + **`dbo.MarketingPricingQuoteRequests`** capture intent when live checkout is not the chosen path; **`Email:PricingQuoteSalesInbox`** (default **`sales@archlucid.net`**) receives a transactional notification after SQL persist when **`Email:Provider`** is not **`Noop`** ([`docs/runbooks/MARKETING_PRICING_QUOTE_NOTIFICATIONS.md`](runbooks/MARKETING_PRICING_QUOTE_NOTIFICATIONS.md)). CRM / Salesforce owner decisions still apply for lead routing beyond inbox mail.

14. **Cross-tenant pattern library — Accepted 2026-05-03.** **ADR 0031** is **Accepted** ([`docs/adr/0031-cross-tenant-pattern-library.md`](adr/0031-cross-tenant-pattern-library.md)). Implementation PRs (SQL aggregates, nightly ETL, PatternInsights API, operator UI slice) **may merge** when they conform to the ADR — **RLS untouched** on tenant primaries; **dedicated MI/SP**; **k ≥ 5**; **opt-in OFF** default; **nightly projection** — not interactive elastic fan-out (**§Constraints**/**§Architecture Overview**). **Reminder:** **`DPA_TEMPLATE.md`** §10 stubs still need completion before **GA**-facing marketing claims.

15. **Golden-cohort LLM budget approval** — Prompt 6 stands up a nightly golden-cohort drift detector. Owner approves a dedicated Azure OpenAI deployment + estimated monthly token budget for the nightly run.

    - **Shipped (simulator, no new Azure spend):** `archlucid golden-cohort lock-baseline [--cohort <path>] [--write]` captures committed-manifest SHA-256 fingerprints against a **Simulator** API host; `.github/workflows/golden-cohort-nightly.yml` can run drift assertions when repository variable `ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCKED` is set to `true` (cohort JSON must contain non-placeholder SHAs first — see item 33).
    - **Still gated on this item:** optional **real-LLM** cohort execution remains behind `ARCHLUCID_GOLDEN_COHORT_REAL_LLM` plus injected Azure OpenAI secrets on a protected GitHub Environment (the assistant does not provision deployments or spend).
    - **Budget (Resolved 2026-04-23, sixth pass):** **$50/month approved** at the same ceiling as the prior 2026-04-22 resolution. New **Improvement 11** in [`QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md`](archive/root-superseded-2026-05-01/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md) §3 covers the cost-and-latency dashboard + nightly kill-switch. Azure OpenAI deployment provisioning + secret injection on the protected GitHub Environment **remain owner-only operational tasks**.

16. **ADR 0021 Phase 3 — owner policy (Prompt 2 landed code + stopped at gate)** — Phase 2 catalog (`AuditEventTypes.Run.*` + dual-write), `IRunCommitOrchestrator` façade, and parity probe tooling shipped **2026-04-21**; Phase 3 **deletion** PRs remain blocked until ADR 0021 exit gates **(i)–(iv)**.
    - **Legacy `CoordinatorRun*` sunset (Resolved 2026-04-21):** **2026-05-15.** Product not yet released, so the strangler is being accelerated; the prior `Sunset: 2026-07-20` deprecation-header value drops to `Sunset: 2026-05-15` atomically across deprecation headers, parity-probe doc, [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md), and any client SDK release notes (see this change set). The earlier Draft [ADR 0028 — completion scaffold](adr/0028-coordinator-strangler-completion.md) is marked Superseded by 0029.
    - **Parity probe write path (Resolved 2026-04-21; workflow retired 2026-05-05 — PR B):** **Auto-commit to `main`** was acceptable when **`coordinator-parity-daily.yml`** existed (**`contents: write`** granted). **Phase 3 PR B** ([ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md)) removed that workflow — historical record only.
    - **ADR 0022 lifecycle (Resolved 2026-04-21, updated same-day follow-up):** Flip to **Superseded** by a Phase 3 **deletion** ADR **inside PR A itself** — gate (iv) was waived for pre-release per [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md), so there are no 14-rows to wait for; PR A merging is the trigger.
    - **Phase 3 PR A authorship (Resolved 2026-04-21 follow-up):** **Assistant drafts PR A end-to-end** in this repo (concretes + interfaces deletion, DI sweep, `DualPipelineRegistrationDisciplineTests` allow-list shrink, OpenAPI snapshot regen). **Queued for a dedicated session** — large surgical change set, deserves its own clean turn (will not be bundled with smaller items). Sequencing intent: ship the per-trigger Teams matrix + RLS object-name SQL migration session **first**, then PR A.
    - **Phase 3 gate (iv) — pre-release waiver (Resolved 2026-04-21 follow-up):** Waived alongside gate (i) for the pre-release window. Both gates restore automatically when V1 ships to a paying customer. See [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md) § Operational considerations for the rationale.

17. **Vertical starter — public-sector regulatory framing (Prompt 11)** — **Resolved 2026-04-21: ship BOTH** EU/GDPR (existing `templates/briefs/public-sector/`, `templates/policy-packs/public-sector/`) **and** US (FedRAMP / StateRAMP — new `templates/briefs/public-sector-us/`, `templates/policy-packs/public-sector-us/`). Wizard exposes a clear picker label.

    - **CJIS overlay scope (Resolved 2026-04-21 follow-up):** **FedRAMP Moderate / NIST SP 800-53 Rev. 5 only** in v1. The CJIS Security Policy reference was dropped from policy-pack metadata, brief, wizard preset, and rule descriptions in this change set. Authoring the full CJIS Security Policy v5.9.5 control mappings (~30 controls) is a future pack rather than a v1 overlay.

18. **Vertical starter templates — tiering (Prompt 11)** — **Resolved 2026-04-21: all five verticals stay in Core Pilot / trial** for v1. No paid-tier gating on industry templates. Documented in `templates/README.md`. Re-open if packaging strategy changes.

---

## Surfaced by 2026-04-21 second independent assessment (weighted **67.61%**)

These items came out of [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_67_61.md`](archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_67_61.md) §4 and the eight Cursor prompts in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_67_61.md`](archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_67_61.md). Each is **owner-only** — the assistant cannot answer them from repository state.

19. **First-paying-tenant graduation owner** — who watches the trial-to-paid event, validates the case study draft with the customer, and flips the row in `docs/go-to-market/reference-customers/README.md` from `Customer review` to `Published`? (Specific to Improvement 1 / Prompt 1.)

20. **Third-party pen-test execution window (V2)** — when a vendor is selected under item **2**, schedule the engagement, name the customer-shareable redacted-summary review owner, decide what (if anything) is published in the public Trust Center vs NDA-gated. **Owner 2026-05-01:** no Aeronova or other vendor awarded; this item applies only to a **future V2** third-party cycle.

    - **Custodian mailbox (Resolved 2026-04-21):** **`security@archlucid.net`**. All public surfaces aligned in this change set; assessor comms must use the same address.
    - **Release window (owner 2026-05-01):** **V2** — see [`V1_DEFERRED.md`](library/V1_DEFERRED.md) §6c. Historical Q10 / Q11 text in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part B) reflected an earlier posture; **external** pen test is **not** a V1.1 scoring obligation.

21. **PGP key custodian for `security@archlucid.net`** — owner generates the key pair (or designates a custodian) and drops the public key into `archlucid-ui/public/.well-known/pgp-key.txt`. The CI guard added by Prompt 2 turns green automatically the moment the file appears.

    - **Custodian mailbox (Resolved 2026-04-21):** **`security@archlucid.net`** is the canonical UID. Generation + custodian-naming still owner-only.
    - **Release window (Resolved 2026-04-23, sixth pass):** **V1.1.** Key generation + drop are no longer V1 obligations — see Q12 / Q13 / Q14 in *Resolved 2026-04-23 (sixth pass)* in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part B). UID gated on `archlucid.net` domain acquisition.

22. **Marketplace + Stripe live go-live calendar — HELD (2026-04-21); V1 trial-funnel TEST-mode end-to-end shipped 2026-04-23.** Owner has not chosen a live-keys calendar; production-safety guards (CI alignment, `BillingProductionSafetyRules`, `archlucid marketplace preflight`) continue to ship and stay green, but **no live keys are flipped**. The **V1 deliverable that makes a future V1.1 commerce un-hold safe** landed 2026-04-23 (Improvement 2): the trial signup funnel now runs end-to-end on staging in **Stripe TEST mode** for sales-engineer-led product evaluation — `archlucid trial smoke --staging` (one-line PASS|FAIL + correlation id), [`archlucid-ui/e2e/trial-funnel-test-mode.spec.ts`](../archlucid-ui/e2e/trial-funnel-test-mode.spec.ts) (UI smoke, self-skips when `STRIPE_TEST_KEY` unset), nightly [`.github/workflows/trial-funnel-test-mode.yml`](../.github/workflows/trial-funnel-test-mode.yml), merge-blocking CI guard [`scripts/ci/assert_billing_safety_rules_shipped.py`](../scripts/ci/assert_billing_safety_rules_shipped.py), and a sales-engineer playbook in [`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`](runbooks/TRIAL_FUNNEL_END_TO_END.md) § 9.1. See the 2026-04-23 entry "Trial funnel TEST-mode end-to-end on staging" in [`docs/CHANGELOG.md`](CHANGELOG.md). When the owner picks a live-keys date, all four sub-items below become live decisions on that day; until then this item is intentionally parked, not abandoned.

    - **Needed from owner (when un-held):** (a) **Single cutover vs staged** — same maintenance window for Marketplace “Go live” + Stripe live keys, or Stripe first / Marketplace first (with rollback owners named per path); (b) **calendar dates** and **communication** to early customers if checkout is briefly unavailable; (c) confirmation **staging** remains on Stripe **TEST** + non-production webhook secrets until (a) is executed (see [`STRIPE_CHECKOUT.md`](go-to-market/STRIPE_CHECKOUT.md) § Staging); (d) who runs `archlucid marketplace preflight` + Partner Center certification checklist the day before either flip; (e) the real `STAGING_ONCALL_WEBHOOK_URL` for the nightly trial-funnel workflow (currently a placeholder secret that soft no-ops when unset). **Operational strike list:** [`runbooks/STRIPE_OPERATOR_CHECKLIST.md`](runbooks/STRIPE_OPERATOR_CHECKLIST.md) (pricing § 3.2 amount, **`PriceIdTeam`**, webhook **`checkout.session.completed`**, DB verification).

23. **Microsoft Teams connector scope** — **Resolved 2026-04-21: notification-only for v1.** Two-way (approve governance from Teams) is a V1.1 candidate; no Teams app manifest registration in v1. `MICROSOFT_TEAMS_NOTIFICATIONS.md` and the Logic Apps workflow keep their notification-only posture.

    - **Per-trigger opt-in (Resolved 2026-04-21 follow-up):** **Per-trigger opt-in matrix** per connection (defaults to all-on so existing rows keep current behaviour). Costs an extra `EnabledTriggersJson NVARCHAR(MAX) NOT NULL` column on `dbo.TenantTeamsIncomingWebhookConnections` and a UI checkbox matrix on `/integrations/teams`; Logic Apps workflow filters server-side before fan-out so tenants can't be spammed with disabled triggers. **Queued for a dedicated session** — needs a SQL migration + master DDL update + UI work + tests for coverage; will be bundled with the deferred RLS object-name SQL migration since both are SQL-shaped.

24. **ADR 0021 strangler completion target date** — **Resolved 2026-04-21: 2026-05-15** (latest-by). Product not yet released, so the strangler is accelerated. **[ADR 0029 — Coordinator strangler acceleration to 2026-05-15](adr/0029-coordinator-strangler-acceleration-2026-05-15.md)** is the operative decision record (it Supersedes the earlier Draft [ADR 0028 — completion scaffold](adr/0028-coordinator-strangler-completion.md), whose `_TODO (owner)_` placeholders this Q&A answered). Deprecation `Sunset:` headers are dropped from `2026-07-20` to `2026-05-15` atomically across `ArchLucid.Api/Filters/CoordinatorPipelineDeprecationFilter.cs`, ADR 0021 § Status note, ADR 0022 § Constraints / Components / Follow-up, and `docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md` § Phase 3 gate status. **Updated 2026-04-21 follow-up:** post-PR-A 30-day soak gate **(i)** **and** parity-rows gate **(iv)** are **both waived for the pre-release window only** (rationale in ADR 0029 § Operational considerations: no published clients to protect with a soak; no customer traffic to measure with the parity probe). Gates **(ii)** and **(iii)** remain in force; both are produced inside PR A's own CI run. **Net effect:** PR A is unblocked the moment gates (ii) and (iii) clear on the deletion branch; 2026-05-15 is a latest-by deadline, not a wait-for-evidence one.

25. **Golden-cohort dedicated Azure OpenAI deployment + monthly token budget** — needed to flip the nightly real-LLM golden-cohort run from optional to mandatory. (Improvement 8 / Prompt 8 — same shape as item 15 but specific to the cohort.)

    - **Repo wiring today:** drift + lock-baseline **refuse** when `ARCHLUCID_GOLDEN_COHORT_REAL_LLM` is truthy in the operator shell, and the placeholder `cohort-real-llm-gate` job in `golden-cohort-nightly.yml` stays disabled until this item plus secrets are in place.
    - **Needed from owner:** the same deployment/budget answers as item 15, scoped explicitly to the **20-row cohort** workload (expected longer prompts than a single interactive chat turn).
    - **Budget (Resolved 2026-04-23, sixth pass):** **$50/month approved** at the same ceiling as item 15. New **Improvement 11** adds the cost-and-latency dashboard + nightly kill-switch. Azure OpenAI deployment provisioning + secret injection on the protected GitHub Environment **remain owner-only operational tasks**.
    - **Budget portion fully Resolved 2026-04-24 (Prompt 11 / Improvement 11 shipped):** the kill-switch is wired (warn at 80% / kill at 95% of cap — Q15-conditional rule), the Azure Monitor Workbook Terraform module exists at [`infra/modules/golden-cohort-cost-dashboard/`](../infra/modules/golden-cohort-cost-dashboard/README.md), and the merge-blocking guard at [`scripts/ci/assert_golden_cohort_kill_switch_present.py`](../scripts/ci/assert_golden_cohort_kill_switch_present.py) prevents any future PR from weakening those ratios. Operator runbook: [`docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md`](runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md). Azure OpenAI deployment provisioning + secret injection still owner-only; flipping `cohort-real-llm-gate` from `if:` to no-`if:` is an owner-only one-line PR after the deployment exists.

26. **VPAT publication decision** — produce a formal VPAT for accessibility published on the Trust Center, or stay with the WCAG 2.1 AA self-attestation in `ACCESSIBILITY.md`? (Adjacent to item 12 — accessibility publication channel.)

27. **Aggregate ROI bulletin publication cadence** — **Resolved 2026-04-21:** (a) **N = 5** for the first issue; (b) **owner-solo** sign-off; (c) **p50 + p90** both stay in v1 bulletins; (d) first publication window opens **once at least one PLG tenant is `Published`** (item 19). `AGGREGATE_ROI_BULLETIN_TEMPLATE.md` updated in this change set.

28. **Customer-supplied baseline soft-required at signup.** **Resolved 2026-05-03 (owner).** **`baselineReviewCycleHours`** SHOULD present as **soft-required** in onboarding (pre-filled sensible default + clear skip affordance)—implementation tracks product UX backlog; owner approves aligning copy with **`TRIAL_AND_SIGNUP.md`** / trial wizard. **`TRIAL_BASELINE_PRIVACY_NOTE.md`** copy + link treatment: **canonical public surface is `https://archlucid.net` signup/trial UX** embedding or linking repo-authored markdown per existing pattern (GitHub **`main`** remains **inspectable**, not mandatory buyer-facing). **No extra in-form disclaimers beyond** tooltip + **`TRIAL_BASELINE_PRIVACY_NOTE.md`** linkage **unless legal later requests.**

31. **Public `/why` comparison delivery** — **Resolved 2026-04-21: BOTH** PDF download (`GET /v1/marketing/why-archlucid-pack.pdf`) **and** inline page section, with a CI sync check that fails if comparison rows in `archlucid-ui/src/marketing/why-archlucid-comparison.ts` and the PDF builder diverge. Implementation tracked in this change set.

32. **Microsoft Teams notification triggers beyond v1 defaults** — **Resolved 2026-04-21: add ALL THREE** of `com.archlucid.compliance.drift.escalated`, `com.archlucid.advisory.scan.completed`, and `com.archlucid.seat.reservation.released` to the first production workflow alongside the existing `run.completed`, `governance.approval.submitted`, and `alert.fired`. Implementation tracked in this change set.

33. **Golden-cohort baseline SHA lock timing** — **Resolved 2026-04-21: lock today** from a single approved Simulator run. Operator runs `archlucid golden-cohort lock-baseline --write` after setting `ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED=true`. The nightly workflow flips from "contract test only" to manifest drift report once `tests/golden-cohort/cohort.json` carries non-zero SHAs. Real-LLM cohort run (item 15 / 25) **stays gated on owner budget**.

34. **Production Simmy / fault-injection game day** — The `simmy-chaos-scheduled.yml` workflow is **staging-only** for `environment` and rejects a non-empty optional workflow_dispatch **`production`** string (fail-fast guard). **Default remains staging-only execution.** Owner must approve any real production chaos (customer notification, SLO ownership, blast radius, rollback) before any future widening of that gate. See [`docs/runbooks/GAME_DAY_CHAOS_QUARTERLY.md`](runbooks/GAME_DAY_CHAOS_QUARTERLY.md) and the calendar in [`docs/quality/game-day-log/README.md`](quality/game-day-log/README.md).

35. **Coordinator → Authority pipeline unification — sequenced multi-PR plan ([ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md))** — Phase 3 PR A's grounding read (2026-04-21) found three structural mismatches that block a single-session deletion. The ADR splits the work into PRs **A0 → A4**; the items below are the **per-sub-PR owner decisions** that have to land before the corresponding sub-PR can merge. Each is **owner-only** — the assistant cannot answer them from repository state.

    - **a. PR A0 — Authority engine projection shape. (Resolved 2026-04-22 — see `Resolved 2026-04-22 (ADR 0030 owner sub-decisions — 35a + 35b)` in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part A).)** Owner picked **(ii) new mapper class** (`AuthorityCommitProjectionBuilder`) consumed by `RunCommitOrchestratorFacade` — Authority engine stays pure. Plus four field-level sub-decisions resolved the same day: 35a.1 = `sibling-row` for `SystemName`; 35a.2 = `empty-with-guard` for typed `Services` + `Datastores` (populated later in new PR A0.5); 35a.3 = `empty-with-guard` for `Relationships` (deferred until PR A2 planning); 35a.4 = `yes` to the JSON allow-list + CI guard mechanism. **PR A0 drafting unblocked.**

    - **b. PR A1 — `IGoldenManifestRepository` overload return shape. (Resolved 2026-04-22 — see `Resolved 2026-04-22 (ADR 0030 owner sub-decisions — 35a + 35b)` in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part A).)** Owner expanded the original `Task` vs `Task<Guid>` framing to a third option and chose it: **`Task<Decisioning.Models.GoldenManifest>`** (return the produced Authority-shape manifest so the caller keeps idempotency-key reasoning). **PR A1 drafting unblocked.**

    - **c. PR A2 — feature-flag scope for facade target swap. (Resolved 2026-04-22 — see *Resolved 2026-04-22 (35c + 35f — ADR 0030)* in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part A); mechanical wiring follow-on.)** **(c.1) = (ii) global** `Coordinator:LegacyRunCommitPath` (`LegacyRunCommitPathOptions` in `ArchLucid.Core`). **(c.2) = (B)** long-term default **`false`**; **interim** shipped `appsettings` stays **`true`** until `RunCommitPathSelector` + `AuthorityDrivenArchitectureRunCommitOrchestrator` merge. Next small PR: register the selector, implement the authority orchestrator (idempotency + UoW persistence parity with the pipeline), flip default to `false`, and update test hosts.

    - **d. PR A4 — `dbo.GoldenManifestVersions` table drop — backfill / archival policy. (Resolved 2026-04-22 — see *Resolved 2026-04-22 (assessment owner Q&A — 16 decisions)* → **ADR 0030 sub-decisions (items 35d / 35e)** and [ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md) § Component breakdown / PR A4 + § Owner sub-decisions row **35d**).** Owner chose **(i) hard drop** — no historical Coordinator-shape rows preserved; backfill / archival branch removed from ADR 0030. Merge-time gate is no-rollback sign-off only.

    - **e. Phase 3 PR B placeholder tracker (`docs/architecture/PHASE_3_PR_B_TODO.md`). (Resolved 2026-04-22 — see *Resolved 2026-04-22 (assessment owner Q&A — 16 decisions)* → **ADR 0030 sub-decisions (items 35d / 35e)** row **35e**, [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md) § Lifecycle § **PR B — audit-constant retirement checklist**.)** Owner chose **both**: authoritative inline checklist on ADR 0029 plus a standalone working-surface file; **PR B merge 2026-05-05** retired the file and `scripts/ci/assert_pr_b_tracker_in_sync.py` after checklist closure.

    - **f. PR A0.5 — typed-services source for `ManifestService.ServiceType` / `RuntimePlatform`. (Resolved 2026-04-22 — see *Resolved 2026-04-22 (35c + 35f — ADR 0030)* in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part A).)** **(i) graph `Properties` metadata** — `GraphNode.Properties` keys `serviceType` and `runtimePlatform` (and `datastoreType` for storage-category nodes) hold enum names. `DefaultGoldenManifestBuilder` populates `Decisioning.Models.GoldenManifest.Services` / `Datastores` from `TopologyResource` nodes; `AuthorityCommitProjectionBuilder` maps them onto the coordinator-shaped `Contracts.Manifest.GoldenManifest`. **PR A0.5 implementation in progress in the same change set as 35c.**

---

## Surfaced by 2026-04-23 owner Q&A on assessment §4

39. **"AI Architecture Review Board" rebrand workstream — schedule.** *(Schedule sub-decision **Resolved 2026-04-23 sixth pass — Q6 / Q7** in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part B) — V1 schedule confirmed, replacement string `AI Architecture Review Board` confirmed. Brand-neutral content seam + `/why` flip + WARN-mode CI guard **shipped** 2026-04-23 as PR-1 of the rebrand workstream — see [`docs/architecture/REBRAND_WORKSTREAM_2026_04_23.md`](architecture/REBRAND_WORKSTREAM_2026_04_23.md) for the seven-PR sequence.)* Assessment §4 cross-cutting q11 was resolved 2026-04-23 as "open to repositioning" toward "AI Architecture Review Board"; Q6 / Q7 then scheduled the workstream to V1 and named the replacement string. The rebrand is a **multi-doc + multi-route** change (marketing site `/why`, `/pricing`, `/get-started`; sponsor brief; competitive landscape; per-vertical briefs; Trust Center; in-product copy on the operator-shell governance pages). **Owner-only follow-on:** any final brand approval and any trademark / domain check before PR-7 (the closing PR that flips the CI guard from WARN to FAIL) merges. Assistant continues PR-2..PR-6 in separate sessions per the workstream tracker.

---

## Surfaced by 2026-04-23 SaaS-framing reconciliation

These came out of the 2026-04-23 owner clarification — *"the user will never have to install Docker or SQL because this is a SaaS product"* — applied against the latest assessment ([`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) §0.1) and the canonical entry doc ([`START_HERE.md`](START_HERE.md) "Audience split").

36. **Buyer-facing first-30-minutes doc — copy approval.** *(Resolved 2026-04-23 sixth pass for the wiring; **owner-blocked only on Q3 screenshot capture** — see "Resolved 2026-04-23 (sixth pass — fresh independent assessment §10 owner Q&A — 17 decisions)" Q1–Q5 in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part B) and the 2026-04-23 entry "Buyer-facing first-30-minutes path: repo stub + marketing /get-started route" in `docs/CHANGELOG.md`.)* Both surfaces now ship: [`docs/BUYER_FIRST_30_MINUTES.md`](BUYER_FIRST_30_MINUTES.md) (consultative voice per Q1, q35 placeholders per Q4) and the marketing route at [`archlucid-ui/src/app/(marketing)/get-started/page.tsx`](../archlucid-ui/src/app/%28marketing%29/get-started/page.tsx) (placeholder image slots per Q3, no "talk to a human" CTA per Q5, vertical-picker labels mirror the `templates/briefs/` folder slugs per Q2 via `get-started-verticals.ts`). Merge-blocking CI guard `scripts/ci/assert_buyer_first_30_minutes_in_sync.py` enforces picker-vs-folder sync and the q35-or-allow-list rule on every prose paragraph in the buyer files. **Still owner-blocked (Q3 follow-on, not a deferred decision):** real anonymized-tenant screenshots — owner picks `tenantId` and `runId`, capture replaces the five `step-{n}-placeholder.png` slots in a follow-on PR. **Q2 wording note:** the owner answer enumerates `manufacturing` rather than the on-disk `retail` and `saas`; the picker ships the actual six on-disk slugs and the discrepancy is flagged for the owner's next pass on Q2 (this assistant treated the on-disk folders as the firmer source of truth because the CI guard checks against them).

40. **First-tenant funnel — per-tenant emission consent (owner-only).** *(Surfaced 2026-04-24 as part of Improvement 12 — first-tenant onboarding telemetry funnel — see the 2026-04-24 entry in [`docs/CHANGELOG.md`](CHANGELOG.md).)* The funnel ships in **aggregated-only** mode by default: the counter `archlucid_first_tenant_funnel_events_total` is emitted with the `event` label only — no `tenant_id`, no `userId`, no IP. The owner-only feature flag `Telemetry:FirstTenantFunnel:PerTenantEmission` (bound to `FirstTenantFunnelOptions`, default `false`) gates two privacy-sensitive behaviours: (a) adding the `tenant_id` tag to the App Insights metric, and (b) inserting per-tenant rows into `dbo.FirstTenantFunnelEvents`. The processing activity is recorded in [`docs/security/PRIVACY_NOTE.md`](security/PRIVACY_NOTE.md) §3.A under GDPR Art. 6(1)(f) (legitimate interest). **`(ii) Owner direction (2026-05-03):`** Defer `dbo.FirstTenantFunnelEvents` retention, purge, and aggregate semantics to **V1.1**. **V1** implements **no** automatic deletion of these SQL rows and **no** prune job. **`(i)(iii)(iv) Resolved 2026-05-03 (owner):`** **(i)** Legitimate-interest / balancing framing in **`PRIVACY_NOTE.md`** §3.A agreed for **future** gated per-tenant emission. **(iii)** Use **notice-only** posture (no separate mandatory tenant-admin **opt-in** gate before `tenantId`-tagged metrics / funnel SQL rows)—document in Trust Center/DPA narratives when the flag is flipped. **(iv)** **`60%`** first-finding-within-thirty-minute **dashboard target** is **approved until pilot data warrants revision** (`infra/modules/first-tenant-funnel-dashboard/variables.tf`). **Operational:** default stays **`false`** until you consciously set **`Telemetry:FirstTenantFunnel:PerTenantEmission = true`** per host (staging/prod)—prerequisites are documented; flipping is not automatic.

37. **In-product support-bundle download.** *(Parts (a) + (b) **Shipped 2026-04-24** per decisions F + G in [`docs/archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md`](archive/PENDING_QUESTIONS_RESOLVED_HISTORY.md) (Part B) — see the 2026-04-24 entry "In-product opt-in tour + `/admin/support` support-bundle download UI" in [`docs/CHANGELOG.md`](CHANGELOG.md). **Part (c) Resolved 2026-05-03 (owner)** — **`A`:** adopt the **documented shipped defaults** plus **minimal additional rule for third-party forwarding** — bearer tokens / `X-Api-Key` / password-shaped pairs redacted **at assembly**; secret-shaped env vars show **`(set)`/`(not set)` only**; keep config snapshot + run summaries + bounded audit tails as assembled today — **tenant-identifying or contact PII MUST NOT cross to external support/recipients unless** the downloader holds **`ExecuteAuthority`** and **explicitly chooses** to attach that detail (manual review mandatory every time).* UI: `/admin/support` (`ExecuteAuthority`; `POST /v1/admin/support-bundle`). Assembler: `ArchLucid.Application/Support/SupportBundleAssembler.cs`; redaction seam: `SupportBundleSensitivePatternRedactor`.

---

## Quality-assessment cadence (Resolved 2026-04-21)

- **Cadence:** **Weekly.** Each pass produces a `QUALITY_ASSESSMENT_<date>_INDEPENDENT_<score>.md` plus a paired `CURSOR_PROMPTS_<...>.md` and updates this file.
- **Next pass:** **2026-04-28.**
- **Trigger to break cadence:** any of these "score-moving" owner events — first PLG row `Published`, Marketplace listing live, or **V2** third-party pen-test summary published (when that programme completes) — when one lands, run an unscheduled pass within 48 hours so the score reflects the new artefact. **V1** does **not** require a third-party pen-test summary for scoring; owner-conducted V1 testing does **not** trigger this bullet by itself.
- **Documentation layout (Resolved 2026-04-23):** Buyer-facing canonical entry is **[`docs/START_HERE.md`](START_HERE.md)**. CI caps markdown files directly under `docs/` (see `scripts/ci/assert_docs_root_size.py`). Most former root reference pages moved to **[`docs/library/`](library/)** with markdown links rewritten; superseded Cursor/quality packs (except the latest **68.60** pair at repo root) live under **[`docs/archive/quality/2026-04-23-doc-depth-reorg/`](archive/quality/2026-04-23-doc-depth-reorg/)**. Full path listing: **[`docs/library/DOC_INVENTORY_2026_04_23.md`](library/DOC_INVENTORY_2026_04_23.md)**.

---

## Related

| Doc | Use |
|-----|-----|
| [`docs/library/DOC_INVENTORY_2026_04_23.md`](library/DOC_INVENTORY_2026_04_23.md) | Every active `docs/**/*.md` (excluding `docs/archive/`) with last-modified + audience heuristics |
| [`docs/archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) | **Latest** weighted independent assessment (68.60%) |
| [`docs/archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md) | Eight paste-ready Cursor prompts for the 68.60% assessment |
| [`docs/archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_67_61.md`](archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_67_61.md) | Prior 2026-04-21 assessment (67.61%) — **archived** |
| [`docs/archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_67_61.md`](archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_67_61.md) | Eight paste-ready Cursor prompts for the 67.61% assessment — **archived** |
| [`docs/archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md`](archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md) | Earlier 2026-04-21 assessment (64.14%) — **archived** |
| [`docs/archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md`](archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21.md) | Six paste-ready Cursor prompts; #3 and #4 stop at owner gates — **archived** |
| [`docs/archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md) | Prior assessment + §8 prompts |
| [`docs/go-to-market/PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md) § 5.4 | Reference-customer CI guard and discount re-rate |
