> **Scope:** ArchLucid V1 — scope contract - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid V1 — scope contract

**Audience:** Product, engineering, pilots, and operators who need a single, decisive boundary for what "V1" means in this repository.

**Status:** Contract for the current codebase and docs. It describes what is implemented and supportable today, not a roadmap of net-new capabilities.

This scope document lists in-scope capabilities, explicit out-of-scope items, the operator happy path, and minimum release checks. Naming and rename posture are summarized in **Related** below.

---

## Related

- **[README.md](../../README.md)** — repo overview and install spine
- **[GLOSSARY.md](GLOSSARY.md)** — terms and naming
- **[BREAKING_CHANGES.md](../../BREAKING_CHANGES.md)** — breaking change trail
- **[ARCHLUCID_RENAME_CHECKLIST.md](../ARCHLUCID_RENAME_CHECKLIST.md)** — remaining rename phases
- **[ARCHITECTURE_ON_ONE_PAGE.md](../ARCHITECTURE_ON_ONE_PAGE.md)** — architecture poster
- **[OPERATOR_ATLAS.md](OPERATOR_ATLAS.md)** — operator atlas

---

## 1. What this document does

- States **what is in V1** (must work for a pilot).
- States **what is out of V1** (deferred, optional, or non-goals).
- Defines the **core operator happy path** and **minimum release checks** aligned with existing scripts and guides.

For deeper flow detail, use [ONBOARDING_HAPPY_PATH.md](ONBOARDING_HAPPY_PATH.md) and [ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md).

**Deferred / exploratory inventory (doc-sourced):** [V1_DEFERRED.md](V1_DEFERRED.md) — consolidates partial stories so V1 does not read as open-ended.

---

## 2. In scope for V1 — organized by product layer

V1 capabilities map to **two** product layers (**Pilot** and **Operate**). See [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) for the full inventory, [CORE_PILOT.md](../CORE_PILOT.md) for the first-pilot walkthrough, and [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) for when to stay in **Pilot** versus expand into **Operate**.

---

### Layer 1 — Pilot

The minimum set every pilot must complete. Delivered by default; no additional configuration beyond API + SQL.

#### 2.1 Run lifecycle: request → execute → commit

- Create a **run** from a structured **architecture request** (`POST /v1/architecture/request`).
- Drive the run through **execution** so agent work completes under the configured **simulator or real** execution mode.
- **Commit** a **golden manifest** (`POST /v1/architecture/run/{runId}/commit`), with documented state and conflict behavior ([API_CONTRACTS.md](API_CONTRACTS.md)).
- End-to-end request → execute → commit behavior, including convergence on manifests and artifacts, is described in [ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md).

#### 2.2 Manifest and artifact review

- **API:** list and download manifest-scoped artifacts; export-related endpoints per OpenAPI/Swagger.
- **CLI:** `artifacts`, `status` per [CLI_USAGE.md](CLI_USAGE.md).
- **Operator UI:** runs list, run detail, manifest summary, artifact review, and download ([operator-shell.md](operator-shell.md)).

#### 2.3 Export and package generation

- **Markdown/DOCX** exports and **replay** from persisted export records ([ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md)).
- **ZIP** downloads (bundle and run-export) from run detail.

#### 2.4 Deployability and supportability

- **Container images** and **docker compose** profiles ([CONTAINERIZATION.md](CONTAINERIZATION.md)).
- **SQL Server** persistence via DbUp migrations; automatic on startup ([SQL_SCRIPTS.md](SQL_SCRIPTS.md)).
- **Health:** `/health/live`, `/health/ready`, `/health`; `GET /version` for support attribution.
- **Correlation IDs**, **CLI diagnostics** (`doctor`, `support-bundle`), and **Troubleshooting** runbooks.
- **Authentication modes:** development bypass, JWT bearer, API key ([README.md](../../README.md)).
- **Infrastructure-as-code** examples (Terraform modules under `infra/`).

---

### Layer 2 — Operate

**Operate** is the second buyer-facing layer. It includes deeper investigation and comparison tools (available once you have at least one committed run; in the operator UI, enable via **Show more links** in the sidebar) **and** governance, auditability, and compliance tooling (configuration-driven; most features require explicit enablement; full surface visible after enabling extended/advanced links in the sidebar).

#### 2.5 Compare

- **Two-run** comparison: structured golden-manifest deltas + legacy diff + optional AI explanation ([COMPARISON_REPLAY.md](COMPARISON_REPLAY.md)).
- Operator UI: **Compare runs** workflow ([operator-shell.md](operator-shell.md)).

#### 2.6 Replay

- **Comparison replay** (artifact vs regenerate vs verify modes) for persisted comparison records.
- **Run replay** (authority chain re-validation) with validation flags surfaced in the operator shell.

#### 2.7 Graph

- **Knowledge / provenance / architecture graph** for a single run in the operator UI ([KNOWLEDGE_GRAPH.md](KNOWLEDGE_GRAPH.md)).

#### 2.8 Advisory, Q&A, and pilot signals

- **Ask** — natural-language queries against architecture context.
- **Advisory scans** — architecture digests and scheduled scans.
- **Pilot feedback** — rollup and triage of product learning signals.
- **Recommendation learning** — learning profiles per run.
- **Integration events** (optional Azure Service Bus, CloudEvents envelope, webhooks) ([INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md)).

Use these surfaces when the next question is analytical: what changed, why it changed, what the architecture or provenance graph shows, or how two runs differ.

#### 2.9 Governance workflows

- **Approval workflow** with segregation of duties (self-approval blocked), SLA tracking, and webhook escalation on breach.
- **Pre-commit governance gate** — `ArchLucid:Governance:PreCommitGateEnabled` blocks manifest commit when findings exceed configured severity thresholds ([PRE_COMMIT_GOVERNANCE_GATE.md](PRE_COMMIT_GOVERNANCE_GATE.md)).
- **Policy packs** — versioned rule sets with scope assignments and effective governance resolution.
- **Governance dashboard** — cross-run pending approvals and policy change summary.

#### 2.10 Audit and compliance

- **78 typed audit events** in an append-only SQL store with CSV export ([AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md)).
- **Audit log** — filter by event type, actor, run ID, correlation ID, time window.
- **Row-level security (RLS)** — SQL `SESSION_CONTEXT` tenant isolation ([security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md)).
- **Compliance drift trend** — tracking and operator UI chart.

#### 2.11 Alerts

- **Alert rules, routing, composite rules, tuning** — configurable alert pipeline.
- **Alert inbox** — open and acknowledged alerts with correlation to runs and manifests.
- **Alert simulation** — evaluate rules against synthetic payloads.

#### 2.12 Trust and access

- **Entra ID / JWT bearer, API key, RBAC roles** (Admin / Operator / Reader / Auditor).
- **SCIM 2.0 inbound provisioning** — dedicated `ScimBearer` automation surface (`/scim/v2/*`) with per-tenant bearer tokens, group→role mapping, and enterprise seat accounting ([`docs/integrations/SCIM_PROVISIONING.md`](../integrations/SCIM_PROVISIONING.md), ADR [`0032`](../adr/0032-scim-v2-service-provider.md)).
- **Private endpoints** and WAF Terraform modules; no SMB/445 public exposure.
- **DPA template, subprocessors register, SOC 2 roadmap** ([go-to-market/TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md)).

Use these surfaces when the next question is governance or trust: approvals, policy enforcement, audit evidence, compliance drift, alerts, or operational control.

#### 2.13 First-party ITSM connectors (Jira, ServiceNow)

**In scope for V1 GA** — first-party connectors are **committed product obligations** for V1 (not deferred to V1.1). **Implementation sequencing:** **ServiceNow** before the **Atlassian** first-party surfaces. **Atlassian pair** (**Jira** here + **Confluence** in §2.15): engineer and release as **one workstream** — **Confluence** publish **before** **Jira** issue depth (**owner policy 2026-05-05**); **Jira** follows **immediately** in the **same** V1 tranche (not a separate delayed program). Historical *Resolved 2026-04-27* **ServiceNow-before-Jira** ordering is **superseded** for **Atlassian** by *Resolved 2026-05-05 (Atlassian sequencing — Confluence before Jira)* in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md). **Scope pinning** for Jira and ServiceNow was moved from V1.1 to V1 by *Resolved 2026-05-05 (Jira + ServiceNow — promoted to V1 scope)*. Atlassian Marketplace and ServiceNow Store listings **may trail** functional connectors in the same delivery window.

- **ServiceNow** — Incident creation from Authority-shaped findings (`incident` table) with correlation back-link; basic-auth patterns. **`cmdb_ci`** — when set — uses the **`cmdb_ci_appl`** class: match **`SystemName`** to CMDB **`name`**, set **`cmdb_ci`** to the matched **`sys_id`**, or leave empty when no match; optional tenant flag **`ServiceNow:AutoCreateCmdbCi`** (default **`false`**) may create an Application CI when no match exists. **Two-way status sync** (ServiceNow → ArchLucid finding state) is **committed for V1 GA** — status-only sync using a configurable per-tenant mapping (default: `New`/`In Progress` → `Open`/`InProgress`; `Resolved`/`Closed` → `Resolved`); OAuth 2.0 is a follow-on (*Resolved 2026-05-06 (ITSM bidirectional sync — both connectors)* in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md)).
- **Jira** — Issue creation from findings with correlation back-link; **bi-directional status sync** (Jira → ArchLucid finding state) is **committed for V1 GA** using a configurable per-tenant mapping (default: `To Do` → `Open`; `In Progress` → `InProgress`; `Done` → `Resolved`); OAuth 2.0 / API token auth (*Resolved 2026-05-06 (ITSM bidirectional sync — both connectors)*).

Until these connectors are enabled in a given environment, customers may still use **CloudEvents webhooks**, **REST**, and **customer-operated** recipes under [`docs/integrations/recipes/`](../integrations/recipes/README.md).

#### 2.14 Slack (first-party chat-ops)

**In scope for V1 GA** — first-party **Slack** outbound notification sink with **parity** to the shipped **Microsoft Teams** chat-ops path: same per-tenant **`EnabledTriggersJson`** opt-in matrix (and canonical trigger / event-type catalog), secret material in **Azure Key Vault** with only a **secret-name** reference persisted in SQL, and the same Authority-shaped payloads used by existing webhook delivery (`DigestSlackWebhookDeliveryChannel`, `AlertSlackWebhookDeliveryChannel`, alert routing). **Slack App Directory** listing, OAuth-based Slack app installation UX, and **in-Slack interactive actions** (acknowledge / approve) are **not** committed for V1 unless an explicit owner decision adds them.

#### 2.15 Confluence (first-party documentation publish)

**In scope for V1 GA** — first-party **Confluence Cloud** connector to **publish** architecture findings or run summaries as pages in a customer space. **Minimum viable shape** (per historical Improvement 3 design intent, now promoted from V1.1 to V1): **one-way** publish to a **single fixed `Confluence:DefaultSpaceKey`** per tenant configuration (**3a** — no multi-space or dynamic routing in the initial shipped shape unless an owner decision extends it). **Authentication** (**3b**): **Confluence API token** with **basic auth** for the V1 MVP; **OAuth 2.0** is a **follow-on** within the V1 delivery window if a buyer requires it. **Implementation sequencing:** Same as §2.13 — **ServiceNow** first; then **Confluence** **before** **Jira** inside the **paired Atlassian** workstream (*Resolved 2026-05-05 (Atlassian sequencing — Confluence before Jira)* in [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md)). Atlassian Marketplace listing **may trail** a usable connector.

---

## 3. Out of scope for V1 (explicit non-goals or V1.1+)

| Area | Rationale |
|------|-----------|
| **Advanced autonomous planning** | Agents are **orchestrated** with explicit tasks and execution modes; V1 does not promise open-ended self-directed multi-step planning beyond what the implemented pipelines already do. |
| **Broad event-bus integrations** | Optional publish/consume paths exist; V1 does **not** include a guaranteed catalog of enterprise integrations, mapping tools, or "any message bus" adapters. Custom consumers are customer-owned. |
| **VS Code (or IDE) shell integration** | No committed product surface for a VS Code–native operator experience; CLI and HTTP remain the primary integration points outside the web UI. |
| **Multi-region active/active product guarantees** | Documentation may describe **tier targets** and failover runbooks ([RTO_RPO_TARGETS.md](RTO_RPO_TARGETS.md)); V1 does not promise a fully specified multi-region SaaS topology out of the box. |
| **Speculative ecosystem** | Marketplace plugins, third-party agent stores, and similar ecosystem features are **not** V1 commitments. **MCP** is **not** V1; it is explicitly a **V1.1** membrane surface — see the MCP row at the end of this table and [V1_DEFERRED.md §6d](V1_DEFERRED.md). |
| **Full UI E2E against every live API configuration** | Playwright operator smoke may use **deterministic mocks**; passing it does not replace SQL-backed API validation ([RELEASE_SMOKE.md](RELEASE_SMOKE.md)). |
| **Net-new public HTTP routes that extend only the Coordinator repository family** | After [ADR 0021](../adr/0021-coordinator-pipeline-strangler-plan.md) acceptance, new externally-visible surfaces must converge on Authority semantics (or go through the unified read façade) — do not add coordinator-only endpoints without an explicit superseding ADR. |
| **Commerce un-hold (Stripe live keys flipped + Marketplace listing published + `signup.archlucid.net` DNS cutover)** | **V1.1 candidate** (Resolved 2026-04-23). Not in V1: no live Stripe keys in production, no `Published` Marketplace SaaS offer, no production DNS cutover for `signup.archlucid.net`. **What is in V1:** all wiring (`BillingStripeWebhookController`, `BillingMarketplaceWebhookController`, `BillingCheckoutController`, `BillingProductionSafetyRules`, `[RequiresCommercialTenantTier]` 402 filter, Marketplace alignment doc, `/pricing` page) plus the trial funnel TEST-mode end-to-end on staging (Improvement 2 in [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](../archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) §3). The V1 commercial motion is **sales-led**: `/pricing` displays numbers, `ORDER_FORM_TEMPLATE.md` drives quote-to-cash. The Stripe-live-keys flip and the Marketplace `Published` state are both **owner-only** (Partner Center seller verification, tax profile, and payout account cannot be filed by the assistant). Tracked under V1.1 in [V1_DEFERRED.md §6b](V1_DEFERRED.md). |
| **Signed design partner engagement** (first design-partner or equivalent early-adopter commercial agreement per [`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md) and [`ORDER_FORM_TEMPLATE.md`](../go-to-market/ORDER_FORM_TEMPLATE.md)) | **V1.1 candidate** (Resolved 2026-05-03). Not in V1: no signed or active **design partner** is required for V1 GA or for **`(A)` headline readiness**. **What is in V1:** pricing rows, addendum templates, reference runbooks, and sales-led pilots without a formal design-partner slot. **Independent quality assessments must not** deduct **`(A)`** for absence of a design partner, list it as a headline defect, or bulk-expand “open questions” around design-partner pipeline unless the user explicitly asked for GTM pipeline review — then **`(B)` informational** only (zero weight on **`(A)`**). Tracked under V1.1 in [V1_DEFERRED.md §6b](V1_DEFERRED.md). |
| **SOC 2 Type I / II report (CPA-issued SOC examination)** | **Post–V1.1 readiness gate / not headline-scored for V1.1 product readiness** (clarification 2026-05-03). Not in V1 or V1.1 **`(A)` assessments:** no CPA SOC 2 report is required for V1 GA or for V1.1 headline delivery readiness. **What is in V1:** SOC 2 **self-attestation**, trust-center honesty, CAIQ/SIG/DPA/process templates, and roadmap narratives ([`SOC2_SELF_ASSESSMENT_2026.md`](../security/SOC2_SELF_ASSESSMENT_2026.md), [`SOC2_ROADMAP.md`](../go-to-market/SOC2_ROADMAP.md), [`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)). **Independent quality assessments must not** deduct **`(A)`** solely for lacking CPA SOC 2; SOC buyer friction stays under **`(B)` procurement-realism** (see `.cursor/rules/Assessment-Scope-V1_1.mdc`). Timing beyond V1.1 is owner-driven — [V1_DEFERRED.md §6c](V1_DEFERRED.md). |
| **Third-party pen-test summary publication (vendor redacted summary / Trust Center row)** | **V2 candidate** (owner 2026-05-01; supersedes prior V1.1 Aeronova framing). Not in V1: no **external** third-party pen test, no assessor redacted summary row tied to a vendor engagement. **What is in V1:** owner-conducted penetration-style exercise ([`2026-Q2-OWNER-CONDUCTED.md`](../security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md)), self-assessment and CI security gates, SoW + redacted-summary **templates** at [`docs/security/pen-test-summaries/2026-Q2-SOW.md`](../security/pen-test-summaries/2026-Q2-SOW.md) and [`2026-Q2-REDACTED-SUMMARY.md`](../security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md) reserved for a future **V2** vendor cycle. **Independent quality assessments must not** penalize V1 readiness for lacking third-party pen-test publication. Tracked under **V2** in [V1_DEFERRED.md §6c](V1_DEFERRED.md). |
| **PGP key drop for `security@archlucid.net` (coordinated-disclosure key)** | **V1.1 candidate** (Resolved 2026-04-23, sixth pass). Not in V1: no public PGP key block at `archlucid-ui/public/.well-known/pgp-key.txt`, no marketing `/security` page key reference, no `SECURITY.md` key-fingerprint update. **What is in V1:** the recipe at [`docs/security/PGP_KEY_GENERATION_RECIPE.md`](../security/PGP_KEY_GENERATION_RECIPE.md) and CI guard that turns green automatically when the key file appears. Key generation, custodian naming, and the same-day single PR that drops the key + updates `SECURITY.md` + updates the marketing `/security` page are all **V1.1**, gated on `archlucid.net` domain acquisition + `security@archlucid.net` mailbox provisioning. Tracked under V1.1 in [V1_DEFERRED.md §6c](V1_DEFERRED.md). |
| **Model Context Protocol (MCP) server — tenant-scoped agent tool surface** | **V1.1 candidate** (scope documentation 2026-04-24). Not in V1: no first-party MCP host in the shipping solution, no MCP SDK as a dependency of core libraries (`ArchLucid.Application` and below). **What is in V1:** REST API, CLI, and operator UI remain the supported integration paths for humans and automation. **In scope for V1.1:** a **thin MCP membrane** (dedicated façade project) exposing **read-mostly**, **tenant-scoped** tools that map **1:1** to existing application services — same **RLS / `SESSION_CONTEXT`** guarantees as HTTP reads, typed **audit** events for MCP tool classes, **quota / circuit-breaker / observability** parity with the existing LLM completion pipeline, and **no SMB/445** transport. Product intent, tool inventory, and non-goals (e.g. outbound ArchLucid-as-MCP-client to arbitrary third-party servers deferred past V1.1 unless separately promoted) are in [`MCP_AND_AGENT_ECOSYSTEM_BACKLOG.md`](MCP_AND_AGENT_ECOSYSTEM_BACKLOG.md) and summarized in [V1_DEFERRED.md §6d](V1_DEFERRED.md). |

---

## 4. Core operator happy path (V1)

### 4.1 Pilot path — start here

The **Pilot** path is the minimum journey every pilot must complete. It maps 1:1 to the **Core Pilot checklist** on the operator UI Home page and to the four steps in [CORE_PILOT.md](../CORE_PILOT.md):

1. **Configure** storage (typically **Sql**), connection string, and auth for the environment ([PILOT_GUIDE.md](PILOT_GUIDE.md)).
2. **Start** the API; confirm **live/ready** and note **version** for any ticket.
3. **Create a run** from a structured request (`POST /v1/architecture/request`) — use the seven-step wizard in the operator UI or the CLI.
4. **Execute** the run and wait until it is ready to commit (watch the pipeline timeline in the UI or poll the API).
5. **Commit** (`POST /v1/architecture/run/{runId}/commit`) to produce a **golden manifest** and **artifacts**.
6. **Review** the manifest and artifacts in the operator UI (run detail → Artifacts table) or via API/CLI ([operator-shell.md](operator-shell.md)).

This is the complete first-pilot deliverable. Nothing beyond step 6 is required to call a pilot successful.

### 4.2 Operate (available but not required after Pilot)

**Operate** is optional until the team has a real analytical or governance question beyond the Pilot deliverable.

#### Analysis (Show more links)

Enable these once you have at least one committed run. In the operator UI, click **Show more links** in the sidebar.

- **Compare** two runs (`/compare`) — structured manifest deltas + legacy diff.
- **Replay** a run (`/replay`) — re-validate the authority chain and surface drift flags.
- **Graph** (`/graph`) — visual provenance or architecture graph for a single run ID.
- **Export** — download bundle ZIP and run-export ZIP from run detail → Artifacts.

Use these when the next question is analytical rather than operational: what changed, why it changed, or how to inspect the result more deeply.

#### Governance (extended and advanced links)

Enable extended and advanced links in the sidebar to surface governance, audit, and alerts.

- **Governance** — approval workflows, policy packs, pre-commit gate, governance dashboard.
- **Audit** — append-only audit log, CSV export, compliance drift tracking.
- **Alerts** — rules, routing, composite rules, simulation, tuning.

Use these when the next question is governance or trust: approvals, policy enforcement, audit, compliance, or operational control.

Optional: run **readiness** or **release-smoke** before a demo ([PILOT_GUIDE.md](PILOT_GUIDE.md), [RELEASE_SMOKE.md](RELEASE_SMOKE.md)).

---

## 5. Minimum release criteria (V1)

These are **practical gates** already encoded or described in-repo—not an exhaustive test matrix.

| Criterion | Evidence in repo |
|-----------|------------------|
| **Solution builds** in Release | CI and [BUILD.md](BUILD.md) |
| **Core-tier tests** pass for the agreed filter (e.g. fast core / `Suite=Core` conventions) | [TEST_STRUCTURE.md](TEST_STRUCTURE.md), [RELEASE_SMOKE.md](RELEASE_SMOKE.md) |
| **API starts** against Sql configuration; **health/live** and **health/ready** succeed when dependencies are up | [README.md](../../README.md), [PILOT_GUIDE.md](PILOT_GUIDE.md) |
| **One scripted end-to-end run** produces a committed manifest and **at least one** artifact descriptor | `release-smoke.ps1` expectations ([RELEASE_SMOKE.md](RELEASE_SMOKE.md)) |
| **Operator UI** builds when Node is in use; Vitest/build steps as per readiness scripts | [RELEASE_SMOKE.md](RELEASE_SMOKE.md), [archlucid-ui/README.md](../../archlucid-ui/README.md) |
| **Version and diagnostics** available for handoff (`GET /version`, CLI `doctor`, support bundle discipline) | [PILOT_GUIDE.md](PILOT_GUIDE.md) |

**Not required** for every internal build: Playwright E2E, full integration matrix, performance benchmarks, or full Terraform apply to a live subscription—unless your program explicitly adds them as release gates.

---

## 6. Related documents

| Doc | Use |
|-----|-----|
| [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) | **Two-layer capability inventory:** Pilot · Operate |
| [CORE_PILOT.md](../CORE_PILOT.md) | First-pilot walkthrough (4 steps) |
| [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) | Practical guide for which layer to use next and what can be ignored for now |
| [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) | Actionable pre-handoff checklist (scope freeze, deploy, health, operator flow, exports, recovery) |
| [V1_DEFERRED.md](V1_DEFERRED.md) | Doc inventory: V1.1+ candidates, audit gaps, Phase 7 rename, infra polish, maintainer backlog |
| [PILOT_GUIDE.md](PILOT_GUIDE.md) | Pilot onboarding narrative |
| [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) | Command-oriented operator entry |
| [operator-shell.md](operator-shell.md) | UI workflows and API expectations |
| [ARCHITECTURE_FLOWS.md](ARCHITECTURE_FLOWS.md) | Export, comparison, replay sequences |
| [API_CONTRACTS.md](API_CONTRACTS.md) | HTTP behavior and policy references |
| [RELEASE_SMOKE.md](RELEASE_SMOKE.md) | Scripted smoke scope and limits |
| [V1_RC_DRILL.md](V1_RC_DRILL.md) | RC drill: full operator path against a running API (`v1-rc-drill.ps1`) |
| [V1_READINESS_SUMMARY.md](V1_READINESS_SUMMARY.md) | Short honest snapshot: done, deferred, risks, pilot bar, post-V1 priorities |
| [V1_REQUIREMENTS_TEST_TRACEABILITY.md](V1_REQUIREMENTS_TEST_TRACEABILITY.md) | Lightweight map from this scope doc to tests, scripts, and data-consistency runbooks |

---

**Change control:** When V1 boundaries shift, update **this file** first, then align [PILOT_GUIDE.md](PILOT_GUIDE.md) and [README.md](../../README.md) so pilots do not see conflicting messages.
