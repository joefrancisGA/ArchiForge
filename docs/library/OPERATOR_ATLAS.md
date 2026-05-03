> **Scope:** Canonical operator action map — UI routes, APIs, CLI, and authority hints in one place.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Operator atlas

**Audience:** Operators, reviewers, and engineers who need a **single map** from product intent → **shell route** → **HTTP surface** → **CLI** without opening ten onboarding files.

**Source of truth for nav:** `archlucid-ui/src/lib/nav-config.ts` (labels, `tier`, `requiredAuthority`) composed with `nav-shell-visibility.ts`. **Authoritative authorization** remains **`[Authorize(Policy = …)]`** on `ArchLucid.Api` — the UI only shapes disclosure.

**Related:** [CORE_PILOT.md](../CORE_PILOT.md) · [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) · [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) · [operator-shell.md](operator-shell.md) · [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) §3 · [API_CONTRACTS.md](API_CONTRACTS.md)

---

## Core Pilot — essential (default sidebar)

| Action | CLI (examples) | Primary API | Operator UI | Authority (nav hint) | Runbook / doc |
|--------|----------------|-------------|-------------|------------------------|---------------|
| Health / readiness | `dotnet run --project ArchLucid.Cli -- health` | `GET /health/live`, `GET /health/ready` | — | Anonymous | [BUILD.md](BUILD.md) |
| Version | `dotnet run --project ArchLucid.Cli -- doctor` | `GET /version` | — | Read (doctor) | [README.md](../../README.md) |
| Create architecture request | `dotnet run --project ArchLucid.Cli -- run` | `POST /v1/architecture/request` | `/runs/new` | Execute (wizard submit) | [CORE_PILOT.md §3](../CORE_PILOT.md#3-step-by-step-walkthrough) |
| Poll run / pipeline | `… status <runId>` | `GET /v1/architecture/run/{runId}` | `/runs/{runId}` | Read | [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) |
| Commit manifest | `… commit <runId>` | `POST /v1/architecture/run/{runId}/commit` | Run detail | Execute | [CORE_PILOT.md §3](../CORE_PILOT.md#3-step-by-step-walkthrough) |
| Manifest + artifacts | `… artifacts <runId> [--save]` | `GET /v1/architecture/manifest/{version}`, artifact routes | Run detail | Read | [CORE_PILOT.md §4](../CORE_PILOT.md#4-review-manifest-and-artifacts) |
| Home / pilot checklist | `… try`, `… pilot up` | tenant + health reads | `/` | Read | [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) |
| Getting started / trial checklist | — | `GET /v1/tenant/trial-status`, registration session, same checklist as Home | `/getting-started` | Read | [TRIAL_SIGNUP_UI.md](../../archlucid-ui/docs/TRIAL_SIGNUP_UI.md), [PILOT_GUIDE.md](PILOT_GUIDE.md) |
| Sponsor PDF (post-commit) | `… sponsor-one-pager <runId> [--save]` | export endpoints on run | Run detail → exports | Read / Execute per op | [CORE_PILOT.md](../CORE_PILOT.md), [CLI_USAGE.md](CLI_USAGE.md) |
| First-value Markdown | `… first-value-report <runId> [--save]` | value report API | Run detail | Read / Execute | [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) |
| Recent committed-run delta panel | — | `GET /v1/pilots/runs/recent-deltas?count=N` | Top of `/runs`, sidebar "Recent activity" card, inline on `/runs/{runId}` | Read | [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) (`BeforeAfterDeltaPanel`) |

---

## Core Pilot — extended (Show more links)

| Action | CLI | Primary API | Operator UI | Authority | Runbook / doc |
|--------|-----|-------------|-------------|-----------|---------------|
| Graph / provenance | — | graph + run payloads | `/graph` | Read | [ARCHITECTURE_COMPONENTS.md](ARCHITECTURE_COMPONENTS.md) |
| Compare two runs | `… comparisons …` | compare controllers | `/compare` | Read | [ARCHITECTURE_FLOWS.md § Flow C](ARCHITECTURE_FLOWS.md#flow-c-comparison-lifecycle-compare--persist-record--replayexport--verify-drift) |
| Replay authority chain | `… trace <runId>` | replay endpoints | `/replay` | Execute | [CANONICAL_PIPELINE.md](CANONICAL_PIPELINE.md) |
| Demo telemetry / sponsor story | — | pilot + demo reads | `/why-archlucid` | Read | [DEMO_QUICKSTART.md](../go-to-market/DEMO_QUICKSTART.md) |

---

## Operate (analysis workloads)

| Action | CLI | Primary API | Operator UI | Authority | Runbook / doc |
|--------|-----|-------------|-------------|-----------|---------------|
| Ask (RAG Q&A) | — | Ask / retrieval routes | `/ask` | Read | [operator-shell.md](operator-shell.md) |
| Search indexed content | — | search APIs | `/search` | Read | [API_CONTRACTS.md](API_CONTRACTS.md) |
| Advisory hub (scans + schedules) | — | `/v1/advisory…`; `/v1/advisory-scheduling…` (CRUD) | `/advisory` (default **Scans**; **Schedules** `?tab=schedules`; legacy `/advisory-scheduling` → redirect) | Read (scans); schedules tab lists GET at Read, mutations Execute | [runbooks/ADVISORY_SCAN_FAILURES.md](../runbooks/ADVISORY_SCAN_FAILURES.md), [ARCHITECTURE_COMPONENTS.md](ARCHITECTURE_COMPONENTS.md) |
| Digests hub (browse + subs + schedule) | — | digest list reads; `/v1/digest-subscriptions…` (mutations); `/v1/tenant/exec-digest-preferences` (save) | `/digests` (default **Browse**; **Subscriptions** `?tab=subscriptions`; **Schedule** `?tab=schedule`; legacy `/digest-subscriptions` and `/settings/exec-digest` → redirect) | Read nav; subscription CRUD Execute; exec schedule GET Read, save Execute | [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md), [CHANGELOG.md](../CHANGELOG.md) |
| Recommendation learning | — | learning APIs | `/recommendation-learning` | Read | [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) |
| Pilot feedback | — | feedback APIs | `/product-learning` | Read | [PILOT_GUIDE.md](PILOT_GUIDE.md) |
| Planning themes | — | planning writes | `/planning` | Execute | [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) |
| Evolution candidates | — | evolution APIs | `/evolution-review` | Execute | [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) |

---

## Operate (governance and trust)

| Action | CLI | Primary API | Operator UI | Authority | Runbook / doc |
|--------|-----|-------------|-------------|-----------|---------------|
| Alerts (hub) | — | `/v1/alerts…` and related alert APIs | `/alerts` — **Inbox**; **Rules** `?tab=rules`; **Routing** `?tab=routing`; **Composite** `?tab=composite`; **Simulation & Tuning** `?tab=simulation` (legacy paths redirect) | Read | [support/TIER_1_RUNBOOK.md](../support/TIER_1_RUNBOOK.md), [API_CONTRACTS.md](API_CONTRACTS.md) |
| Policy packs | — | `/v1/policy-packs…` | `/policy-packs` | Read / Admin on writes | [ARCHITECTURE_COMPONENTS.md](ARCHITECTURE_COMPONENTS.md) |
| Governance resolution (read) | — | effective governance | `/governance-resolution` | Read | [PRE_COMMIT_GOVERNANCE_GATE.md](PRE_COMMIT_GOVERNANCE_GATE.md) |
| Governance dashboard | — | dashboard aggregates | `/governance/dashboard` | Read | [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) |
| Governance workflow (mutations) | — | workflow POSTs | `/governance` | Execute | [COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md) |
| Audit log | — | `/v1/audit…` | `/audit` | Read (+ Auditor role for CSV where documented) | [support/TIER_1_RUNBOOK.md](../support/TIER_1_RUNBOOK.md) |
| Security & trust center | — | static + trust payloads | `/workspace/security-trust` (public table: `/security-trust`) | Read | [SECURITY.md](../../SECURITY.md) |
| Trust Center evidence pack (ZIP) | — | `GET /v1/marketing/trust-center/evidence-pack.zip` | `/trust` (marketing — Download evidence pack button) | Anonymous | [trust-center.md](../trust-center.md) (one ZIP: DPA, subprocessors, SLA, `security.txt`, CAIQ Lite, SIG Core, owner sec assessment, 2026-Q2 SoW, audit matrix; SHA-256 ETag, 1h cache) |
| In-product support bundle (ZIP) | `archlucid support-bundle` | `POST /v1/admin/support-bundle` | `/admin/support` (Download support bundle button) | Execute (per owner decision F, item 37) | [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) item **37(c) Resolved 2026-05-03** — shipped secret redaction + manual forward review; disclose tenant-identifying/contact PII to external support **only when** downloader (`ExecuteAuthority`) **explicitly intends** it |
| Operator opt-in tour | — | — | `/` (operator home — "Show me around" button) | Authenticated | [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) item 38 (5 steps; assistant draft copy wrapped in pending-approval markers; never auto-launches per owner Q9) |
| Value report DOCX | — | value report generation | `/value-report` | Execute | [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) |

---

## Cross-cutting CLI (not tied to one page)

| Action | CLI | Notes | Doc |
|--------|-----|-------|-----|
| New project scaffold | `… new <name>` | Creates client folder layout | [CLI_USAGE.md](CLI_USAGE.md) |
| Local dependencies | `… dev up` | SQL / Azurite / Redis profile | [CONTAINERIZATION.md](CONTAINERIZATION.md) |
| Pilot stack | `… pilot up` | Demo-oriented compose | [FIRST_30_MINUTES.md](../FIRST_30_MINUTES.md) |
| One-shot try | `… try` | Seed + sample + open UI | [CLI_USAGE.md](CLI_USAGE.md#archlucid-try) |
| Comparisons library | `… comparisons …` | list / replay / drift | [COMPARISON_REPLAY.md](COMPARISON_REPLAY.md) |
| Support bundle | `… support-bundle …` | Sanitize before sharing | [README.md](../../README.md) |
| Reference evidence ZIP | `… reference-evidence …` | tenant or admin | [go-to-market/reference-customers/README.md](../go-to-market/reference-customers/README.md) |

---

**Day-one role files** (`docs/onboarding/day-one-*.md`) stay for week-one checklists — use **this atlas** when you need the **canonical action map** (route × API × CLI) without narrative.
