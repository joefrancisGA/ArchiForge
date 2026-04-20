> **Scope:** Decision log + delivery status for the six improvements identified in [`QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_75_37.md`](QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_75_37.md) § 4. Tracks which work shipped on **2026-04-20**, which is **gated on a product-owner answer**, and the answers received so far.

# Quality-improvement decisions — 2026-04-20

## Section A — Decisions captured from the product owner (this session)

| # | Topic | Owner answer |
|---|---|---|
| A1 | Top priority for this improvement set | **Public marketing surface + brand domain.** All other work is sequenced behind this. |
| A2 | DNS, Front Door, marketing analytics ownership | **Owned by the product owner.** Engineering does not provision, rotate, or terminate these. |
| A3 | Coverage / quality gate timeline | **Hold the current schedule.** No sliding gates to fit improvement work. |
| A4 | ADR 0021 — Coordinator strangler | **Implement as soon as possible.** Treat as an unblocked architectural priority. |
| A5 | Pen-test scope | **Staging only for now.** Production-targeted pen tests are not in scope until the marketing/billing GA path is live. |
| A6 | Cost / product-spend dashboard visibility + retention | **Visible to ops and admin roles. Retain 365 days.** |
| A7 | Documentation site | **Tentative pick: Docusaurus** — owner has no prior experience. Engineering ships a one-page primer + scaffold PR before any content migration. |
| A8 | Daily audit-Merkle job (Improvement 6) — schedule | **00:30 UTC daily.** |

> Use this table as the canonical source if anyone asks "what did we agree on for X?" — it is updated as further answers arrive in chat.

---

## Section B — What shipped this session (no input required)

| Improvement | Slice shipped | Where | Status |
|---|---|---|---|
| **2a — Compress cognitive load** | Single-front-door **`docs/FIRST_30_MINUTES.md`** (operator persona, Docker-only, 10 numbered commands, demo-start path). | [`docs/FIRST_30_MINUTES.md`](FIRST_30_MINUTES.md) | Done |
| **2c — Compress cognitive load** | README "Key documentation" replaced with a **6-row persona table** that puts FIRST_30_MINUTES first; the long deeper-doc list collapsed to one tail paragraph. | [`README.md`](../README.md) (Getting started section) | Done |
| **5a — Migration numbering ratchet** | Wired the existing **`scripts/ci/check_migration_numbering.py`** into CI as `continue-on-error: true` (warn-only) with a comment naming the gating question (B-pending **5.1**) for the flip to blocking. | [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) (Guard — migration numbering) | Done (warn-only) |
| **1 — Reference-customer auto-flip** | Added a **second** CI step that re-runs the guard **without** `continue-on-error` once the first step succeeds (i.e. once a `Status: Published` row exists). No manual edit needed at re-rate time. | [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) (Guard — reference-customer status auto-flip) | Done |
| **3a — Architecture boundary tests** | Added **`ContractsFrameworkIsolationTests`** (2 tests, both green) pinning that **Contracts** and **Contracts.Abstractions** must not depend on `Microsoft.AspNetCore`, `Microsoft.Data.SqlClient`, `System.Data.SqlClient`, `Dapper`, or `DbUp`. Existing `DependencyConstraintTests` already covered 14 other layer rules — this fills the framework-leak gap. | [`ArchLucid.Architecture.Tests/ContractsFrameworkIsolationTests.cs`](../ArchLucid.Architecture.Tests/ContractsFrameworkIsolationTests.cs) | Done |
| **QA pointer hygiene** | Updated stale `docs/QUALITY_ASSESSMENT.md` redirect to point at the canonical 75.37 assessment + this decisions log. | [`docs/QUALITY_ASSESSMENT.md`](QUALITY_ASSESSMENT.md) | Done |
| **2d — Doc scope header** | Added **`scripts/ci/check_doc_scope_header.py`** (requires first non-empty line `> **Scope:**` under `docs/**` except `docs/archive/`; README may use `<!-- **Scope:** ... -->`). **`scripts/ci/backfill_doc_scope_headers.py`** back-filled **300** active docs; **`test_check_doc_scope_header.py`** + **`test_backfill_doc_scope_headers.py`**; CI step after `check_doc_links` is **merge-blocking** (no `continue-on-error`). Root **`README.md`** uses the HTML scope line. | [`scripts/ci/check_doc_scope_header.py`](../scripts/ci/check_doc_scope_header.py), [`scripts/ci/backfill_doc_scope_headers.py`](../scripts/ci/backfill_doc_scope_headers.py), [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) | Done |

### Pre-existing assets re-confirmed (no change needed)

| Asset | Why it was on the list | Outcome |
|---|---|---|
| `docs/runbooks/SECRET_HISTORY_REWRITE.md` | Improvement 1 called for an instructions-only runbook. | **Already in repo.** No-op. |
| `docs/runbooks/STRIPE_WEBHOOK_INCIDENT.md` | Improvement 1 called for a Stripe webhook runbook. | **Already in repo.** No-op. |
| `scripts/ci/check_migration_numbering.py` | Improvement 5 called for a CI guard for duplicate `###_` prefixes. | **Already in repo.** Wiring (CI step) is the only delta — see B-row 3 above. |
| `ArchLucid.Architecture.Tests` project + `DependencyConstraintTests` | Improvement 3 called for a NetArchTest project. | **Already in repo** with 14 layer assertions. Added the missing Contracts/framework-isolation slice. |

---

## Section C — What is **deferred this session** with explicit rationale

| Slice | Why deferred | What unblocks it |
|---|---|---|
| **2b — Bulk archive of stale assessments** to `docs/archive/quality/` | Older quality and marketability assessments are referenced **by path** from heavily-active docs: `docs/CHANGELOG.md` (3 narrative entries on 80.72), `docs/adr/0021-coordinator-pipeline-strangler-plan.md`, four `docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20*.md` files, and the `docs/go-to-market/*` set. A bulk move would require ~25 link rewrites across active narrative docs in one PR — high risk of breaking `scripts/ci/check_doc_links.py`. | A focused archive PR that moves files in **one family at a time** with synchronized link rewrites. Tracked under question **C-DEFERRED-1** below. |
| **3 — First consolidation merge** + ADR 0022 | Awaiting answer **B-pending 3.2** (which two sibling Persistence projects to collapse first) and **B-pending 3.1** (any external integrators on the Coordinator interface family that would be broken by ADR 0021 / strangler). | Owner answers below. |
| **4 — Greenfield SaaS workflow + OTEL collector module** | Workflow file already exists at `.github/workflows/cd-saas-greenfield.yml` (skeleton). Cannot run end-to-end until **B-pending 4.1** (dedicated greenfield Azure subscription) and **B-pending 4.2** (OIDC federation) are answered. OTEL collector module can be built ahead, but is a 200-line Terraform stack — better landed in its own focused PR. | Owner answers + a focused infra PR. |
| **5 — Stateful Schemathesis on PR + typed-client replay tests** | Both are non-trivial pipeline additions; Schemathesis stateful needs a latency budget answer (**B-pending 5.2**), and the duplicate `035_*` and `096_*` migrations (visible in the new warn-only guard) cannot be renumbered until **B-pending 5.1** confirms no production catalog has applied them. | Owner answers + a focused PR each. |
| **6 — Evidence pack endpoint + daily Merkle job** | Daily Merkle job schedule is **A8 = 00:30 UTC** (answered). Endpoint authorization scope (**B-pending 6.1**), per-tenant scoping (**B-pending 6.2**), and immutable-blob retention (**B-pending 6.3**) still need answers before code can land safely. | Owner answers. |

---

## Section D — Open questions still gating progress

> Numbering matches the original list given in chat. Re-pasted here so this doc stands on its own.

### D.1 — Improvement 1 (Marketing live + Stripe + reference customer + secret history)

| # | Question | Why it gates progress |
|---|---|---|
| **1.1** | Exact brand hostname(s) to bind in `infra/terraform-edge/frontdoor-marketing.tf` (apex / www / both / redirect direction)? | Required for `marketing_brand_hostname` Terraform variable + Front Door custom-domain resource. |
| **1.2** | TLS strategy — Front Door **managed certificate** or **BYO from Key Vault**? | Two different `azurerm_cdn_frontdoor_custom_domain` resource shapes. |
| **1.3** | Analytics provider — **GA4 / Plausible / PostHog / none**? | Drives `NEXT_PUBLIC_ANALYTICS_PROVIDER` + cookie-consent text. |
| **1.4** | Stripe account state — live keys + webhook secret today, or ship `Billing:Stripe:GaEnabled=false` and wire keys later? | Determines whether GA flips in this PR or in a follow-up. |
| **1.5** | Reference customer — one **named, permissioned** publishable today, or placeholder + design-partner row? | Triggers the auto-flip (already wired in CI) the moment the first row reaches `Status: Published`. |
| **1.6** | Secret-history rewrite — OK with coordinated `git filter-repo` + force-push to `main`? | Without consent the `SECRET_HISTORY_REWRITE.md` runbook is documentation only. |
| **1.7** | Marketing audience — any users in **EU / UK / California**? | Determines whether a cookie banner + analytics opt-out are required from day 1. |

### D.2 — Improvement 3 (Architecture boundary tests + first consolidation merge)

| # | Question | Why it gates progress |
|---|---|---|
| **3.1** | External integrators on the Coordinator interface family — anyone (pilot, partner, `ArchLucid.Api.Client` consumer) relying on `coordinator/*` types we shouldn't break in the strangler PR series? | Decides if the deletion is a single sweep or needs a deprecation window + 410 Gone surface. |
| **3.2** | First consolidation candidate — proposed: merge **`ArchLucid.Persistence.Alerts`** + **`ArchLucid.Persistence.Advisory`** (smallest siblings, heaviest mutual coupling). OK to change assembly identity? | Anyone consuming the `.Alerts` or `.Advisory` assembly by name needs to know. |
| **3.3** | OK to mark single existing offenders the new architecture tests find with `[Trait("Category","Quarantine")]` + tracking issue, instead of fixing every violation in the same PR? | Otherwise the architecture-tests PR is unbounded in scope. |

### D.3 — Improvement 4 (Greenfield SaaS workflow + OTEL collector)

| # | Question | Why it gates progress |
|---|---|---|
| **4.1** | Dedicated **greenfield-test Azure subscription** available where monthly `terraform destroy` is allowed? | Workflow cannot apply without one. |
| **4.2** | OIDC federation for GitHub Actions to that subscription — already set up, or do you want a one-time bootstrap Terraform under `infra/terraform-entra/`? | Drives whether we use OIDC or a service-principal secret. |
| **4.3** | Tail-sampling policy — **always keep error traces, root-span > 2 s, and 100% of `ArchLucid.AuthorityRun`**. Acceptable, or different thresholds (cost vs visibility tradeoff)? | Locks the OTEL collector configuration. |
| **4.4** | Greenfield workflow cadence — **monthly** (1st @ 06:00 UTC), or weekly? | Cost vs signal tradeoff. |

### D.4 — Improvement 5 (Schemathesis stateful + replay + migration renumbering)

| # | Question | Why it gates progress |
|---|---|---|
| **5.1** | **Critical:** have any production catalogs applied the duplicate-numbered migrations (`035_AuditProvenanceConversationTables.sql` + `035_HostLeaderLeases.sql`; `096_CheckJson_CorePayloadColumns.sql` + `096_RlsTenantIdOnlyTables.sql`)? | If **yes**, we cannot renumber — DbUp tracks filename in `SchemaVersions`. We must keep the duplicates and document the historical exception. If **no**, we renumber to fix the bug and flip the warn-only CI guard to blocking. |
| **5.2** | PR-time stateful Schemathesis budget — max **4 minutes** acceptable (with `--max-examples=10 --hypothesis-seed=fixed`)? | Higher = more bugs caught; lower = faster PRs. |
| **5.3** | Acceptable to ship `api-schemathesis-stateful` initially as `continue-on-error: true`, flip to blocking after one green nightly on `main`? | Avoids surprise blocking on first merge. |

### D.5 — Improvement 6 (Evidence pack endpoint)

| # | Question | Why it gates progress |
|---|---|---|
| **6.1** | Authorization scope for `GET /v1/support/evidence-pack.zip` — **Admin + Auditor only** (default), or also **Operator (read-only)**? | The "ops + admin" answer in **A6** was about the cost dashboard; evidence pack is a separate decision. |
| **6.2** | Per-tenant scoping — **calling tenant only** by default, or **cross-tenant** when the caller is an internal Admin? | Two endpoint shapes: tenant-scoped vs `?tenantId=` admin override. |
| **6.3** | Audit-Merkle blob container retention — **7 years immutable** (typical SOC 2 / SOX), or another floor (e.g. 3 / 10 years)? | Sets the immutability policy on the new `audit-merkle` blob container. |
| **6.4** | Daily Merkle job time | **Answered: 00:30 UTC.** |

### D.6 — Cross-cutting

| # | Question | Why it gates progress |
|---|---|---|
| **C-DEFERRED-1** | OK to ship the bulk archive of older quality / marketability assessments in a **focused PR** with synchronized link rewrites across CHANGELOG / ADR 0021 / CURSOR_PROMPTS_*? | Without this, those files stay at the top level and continue to compete for attention with the canonical assessment. |
| **D-1** | Docusaurus primer — do you want a one-page "what you get / what it costs / migration shape" doc landed before any content move, or proceed straight to a scaffold PR? | Keeps the rename / migration honest. |

---

## Section E — How to use this document

1. When a question gets answered in chat, **append the answer to Section A** (or the matching D-row) and update the `Status` column in Section B if the work is now unblocked.
2. When a slice ships, move its row from **Section C → Section B** and link the PR.
3. When all six improvements have shipped, link this doc from the next quality assessment so the audit trail is preserved.

> **Why a separate decisions log instead of editing the assessment in-place:** the assessment is a point-in-time snapshot of *what was scored*; this log is the operational record of *how the gap was closed*. Keeping them separate matches the **`docs/archive/quality/README.md`** policy (assessments are immutable history; decision logs are living).
