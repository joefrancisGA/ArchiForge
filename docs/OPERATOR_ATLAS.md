> **Scope:** Canonical operator action map — UI routes, APIs, CLI, and authority hints in one place.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Operator atlas

**Audience:** Operators, reviewers, and engineers who need a **single map** from product intent → **shell route** → **HTTP surface** → **CLI** without opening ten onboarding files.

**Source of truth for nav:** `archlucid-ui/src/lib/nav-config.ts` (labels, `tier`, `requiredAuthority`) composed with `nav-shell-visibility.ts`. **Authoritative authorization** remains **`[Authorize(Policy = …)]`** on `ArchLucid.Api` — the UI only shapes disclosure.

**Related:** [CORE_PILOT.md](CORE_PILOT.md) · [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) · [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) · [operator-shell.md](operator-shell.md) · [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) §3 · [API_CONTRACTS.md](API_CONTRACTS.md)

---

## Core Pilot — essential (default sidebar)

| Action | CLI (examples) | Primary API | Operator UI | Authority (nav hint) | Runbook / doc |
|--------|----------------|-------------|-------------|------------------------|---------------|
| Health / readiness | `dotnet run --project ArchLucid.Cli -- health` | `GET /health/live`, `GET /health/ready` | — | Anonymous | [BUILD.md](BUILD.md) |
| Version | `dotnet run --project ArchLucid.Cli -- doctor` | `GET /version` | — | Read (doctor) | [README.md](../README.md) |
| Create architecture request | `dotnet run --project ArchLucid.Cli -- run` | `POST /v1/architecture/request` | `/runs/new` | Execute (wizard submit) | [CORE_PILOT.md §3](CORE_PILOT.md#3-step-by-step-walkthrough) |
| Poll run / pipeline | `… status <runId>` | `GET /v1/architecture/run/{runId}` | `/runs/{runId}` | Read | [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) |
| Commit manifest | `… commit <runId>` | `POST /v1/architecture/run/{runId}/commit` | Run detail | Execute | [CORE_PILOT.md §3](CORE_PILOT.md#3-step-by-step-walkthrough) |
| Manifest + artifacts | `… artifacts <runId> [--save]` | `GET /v1/architecture/manifest/{version}`, artifact routes | Run detail | Read | [CORE_PILOT.md §4](CORE_PILOT.md#4-review-manifest-and-artifacts) |
| Home / pilot checklist | `… try`, `… pilot up` | tenant + health reads | `/` | Read | [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) |
| Guided onboarding | — | bootstrap reads | `/onboarding` | Read | [PILOT_GUIDE.md](PILOT_GUIDE.md) |
| First-session handoff | `… try` | same as create + demo | `/onboard` | Execute | [FIRST_30_MINUTES.md](FIRST_30_MINUTES.md) |
| Sponsor PDF (post-commit) | `… sponsor-one-pager <runId> [--save]` | export endpoints on run | Run detail → exports | Read / Execute per op | [CORE_PILOT.md](CORE_PILOT.md), [CLI_USAGE.md](CLI_USAGE.md) |
| First-value Markdown | `… first-value-report <runId> [--save]` | value report API | Run detail | Read / Execute | [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) |

---

## Core Pilot — extended (Show more links)

| Action | CLI | Primary API | Operator UI | Authority | Runbook / doc |
|--------|-----|-------------|-------------|-----------|---------------|
| Graph / provenance | — | graph + run payloads | `/graph` | Read | [ARCHITECTURE_COMPONENTS.md](ARCHITECTURE_COMPONENTS.md) |
| Compare two runs | `… comparisons …` | compare controllers | `/compare` | Read | [ARCHITECTURE_FLOWS.md § Flow C](ARCHITECTURE_FLOWS.md#flow-c-comparison-lifecycle-compare--persist-record--replayexport--verify-drift) |
| Replay authority chain | `… trace <runId>` | replay endpoints | `/replay` | Execute | [CANONICAL_PIPELINE.md](CANONICAL_PIPELINE.md) |
| Demo telemetry / sponsor story | — | pilot + demo reads | `/why-archlucid` | Read | [DEMO_QUICKSTART.md](go-to-market/DEMO_QUICKSTART.md) |

---

## Advanced Analysis

| Action | CLI | Primary API | Operator UI | Authority | Runbook / doc |
|--------|-----|-------------|-------------|-----------|---------------|
| Ask (RAG Q&A) | — | Ask / retrieval routes | `/ask` | Read | [operator-shell.md](operator-shell.md) |
| Search indexed content | — | search APIs | `/search` | Read | [API_CONTRACTS.md](API_CONTRACTS.md) |
| Advisory scans & digests | — | `/v1/advisory…`, digest reads | `/advisory`, `/digests` | Read | [runbooks/ADVISORY_SCAN_FAILURES.md](runbooks/ADVISORY_SCAN_FAILURES.md), [ARCHITECTURE_COMPONENTS.md](ARCHITECTURE_COMPONENTS.md) |
| Recommendation learning | — | learning APIs | `/recommendation-learning` | Read | [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) |
| Pilot feedback | — | feedback APIs | `/product-learning` | Read | [PILOT_GUIDE.md](PILOT_GUIDE.md) |
| Planning themes | — | planning writes | `/planning` | Execute | [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) |
| Evolution candidates | — | evolution APIs | `/evolution-review` | Execute | [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) |
| Advisory schedules | — | schedule CRUD | `/advisory-scheduling` | Execute | [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) |
| Digest subscriptions | — | `/v1/digest-subscriptions…` | `/digest-subscriptions` | Execute | [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md) |
| Exec digest email prefs | — | `/v1/tenant/exec-digest-preferences` | `/settings/exec-digest` | Read | [CHANGELOG.md](CHANGELOG.md) (weekly digest entry) |

---

## Enterprise Controls

| Action | CLI | Primary API | Operator UI | Authority | Runbook / doc |
|--------|-----|-------------|-------------|-----------|---------------|
| Alerts inbox | — | `/v1/alerts…` | `/alerts` | Read | [support/TIER_1_RUNBOOK.md](support/TIER_1_RUNBOOK.md) |
| Alert rules | — | `/v1/alert-rules…` | `/alert-rules` | Read | [API_CONTRACTS.md](API_CONTRACTS.md) |
| Alert routing | — | routing subscriptions | `/alert-routing` | Read | [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md) |
| Composite alert rules | — | `/v1/composite-alert-rules…` | `/composite-alert-rules` | Read | [API_CONTRACTS.md](API_CONTRACTS.md) |
| Alert simulation / tuning | — | simulation + tuning | `/alert-simulation`, `/alert-tuning` | Read | [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) |
| Policy packs | — | `/v1/policy-packs…` | `/policy-packs` | Read / Admin on writes | [ARCHITECTURE_COMPONENTS.md](ARCHITECTURE_COMPONENTS.md) |
| Governance resolution (read) | — | effective governance | `/governance-resolution` | Read | [PRE_COMMIT_GOVERNANCE_GATE.md](PRE_COMMIT_GOVERNANCE_GATE.md) |
| Governance dashboard | — | dashboard aggregates | `/governance/dashboard` | Read | [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) |
| Governance workflow (mutations) | — | workflow POSTs | `/governance` | Execute | [COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md) |
| Audit log | — | `/v1/audit…` | `/audit` | Read (+ Auditor role for CSV where documented) | [support/TIER_1_RUNBOOK.md](support/TIER_1_RUNBOOK.md) |
| Security & trust center | — | static + trust payloads | `/security-trust` | Read | [SECURITY.md](../SECURITY.md) |
| Value report DOCX | — | value report generation | `/value-report` | Execute | [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) |

---

## Cross-cutting CLI (not tied to one page)

| Action | CLI | Notes | Doc |
|--------|-----|-------|-----|
| New project scaffold | `… new <name>` | Creates client folder layout | [CLI_USAGE.md](CLI_USAGE.md) |
| Local dependencies | `… dev up` | SQL / Azurite / Redis profile | [CONTAINERIZATION.md](CONTAINERIZATION.md) |
| Pilot stack | `… pilot up` | Demo-oriented compose | [FIRST_30_MINUTES.md](FIRST_30_MINUTES.md) |
| One-shot try | `… try` | Seed + sample + open UI | [CLI_USAGE.md](CLI_USAGE.md#archlucid-try) |
| Comparisons library | `… comparisons …` | list / replay / drift | [COMPARISON_REPLAY.md](COMPARISON_REPLAY.md) |
| Support bundle | `… support-bundle …` | Sanitize before sharing | [README.md](../README.md) |
| Reference evidence ZIP | `… reference-evidence …` | tenant or admin | [go-to-market/reference-customers/README.md](go-to-market/reference-customers/README.md) |

---

**Day-one role files** (`docs/onboarding/day-one-*.md`) stay for week-one checklists — use **this atlas** when you need the **canonical action map** (route × API × CLI) without narrative.
