> Archived 2026-04-23 — superseded by [docs/START_HERE.md](../START_HERE.md) and the current assessment pair under ``docs/``. Kept for audit trail.

> **Scope:** Eight **additional** paste-ready Cursor prompts for improvements called out in [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) §§1.9–1.18 and §2 — **beyond** the primary eight in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md). Each prompt is self-contained for a fresh agent session.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Cursor prompts — eight follow-on improvements (68.60% assessment)

**How to use.** One prompt per session. Paste the whole fenced block. Honor **Stop and ask** boundaries. Update [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) when a prompt surfaces a new owner-only decision.

---

## Prompt A — ADR 0021 strangler: inventory + regression CI + ADR 0028 scaffold

**Owner gate.** Naming the Phase 3 **completion calendar date**, flipping ADR 0022 to **Superseded**, and approving **auto-commit vs bot PR** for parity markers remain owner-only ([`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) item **16**).

```
Goal: make coordinator→authority convergence measurable in CI so the dual
interface-family tax cannot silently grow back.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md §1.9, §2.1 item 7, §2.3 item 3, §2.4 item 3
- docs/adr/0021-*.md (coordinator strangler) and docs/adr/0022-*.md (Phase 3 deferral / exit gates)
- docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md (if present) and .github/workflows/coordinator-parity-daily.yml
- ArchLucid.Api/Controllers/ (grep for Coordinator namespace / legacy route families)
- ArchLucid.Coordinator/ and ArchLucid.Application/ (grep for IGoldenManifestRepository / IDecisionTraceRepository dual registrations)
- ArchLucid.Host.Composition.Tests/ or ArchLucid.Api.Tests/ for DualPipelineRegistrationDisciplineTests (existing guard)

Do this:
1. Produce docs/architecture/COORDINATOR_STRANGLER_INVENTORY.md with three tables:
   migrate / keep / delete — each row: symbol or route family, owning assembly, last touched PR link placeholder, risk note.
2. Add scripts/ci/assert_coordinator_reference_ceiling.py that counts non-test references
   to a checked-in allowlist JSON (start from current baseline). CI step: fail when count increases.
   Paired unit tests under scripts/ci/tests/.
3. Add docs/adr/0028-coordinator-strangler-completion.md as **Draft** — sections only:
   Objective, Assumptions, Constraints, Decision, Consequences, Exit gates — with TODO for completion date (owner).
4. Cross-link from docs/DUAL_PIPELINE_NAVIGATOR.md (or ARCHITECTURE_COMPONENTS.md) to the inventory + script.
5. Append PENDING_QUESTIONS.md item 16 sub-bullets if any new mechanical unblockers appear (do not resolve owner gates).

Stop and ask the user before:
- Picking the authoritative cut-over calendar date
- Deleting coordinator interfaces or changing ADR 0022 state in git
- Enabling auto-push to main if branch protection forbids it

Exit criteria: inventory doc committed; CI ceiling script green on main;
ADR 0028 draft scaffold; navigator cross-link; no behavior change to
runtime routing without explicit owner approval.
```

---

## Prompt B — Quarterly board-pack PDF for sponsors

**Owner gate.** Legal/comms approval of **forward-looking** language in a board-facing PDF; CFO attribution of any **financial** claims.

```
Goal: ship one downloadable artifact that consolidates sponsor-facing
signals for a UTC quarter — beyond the weekly exec digest.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md §1.10
- ArchLucid.Application/ (ExecDigestComposer, FirstValueReportBuilder, MarkdownPdfRenderer / sponsor PDF paths)
- ArchLucid.Api/Controllers/ (pilots, value-report, exec-digest routes)
- docs/go-to-market/ROI_MODEL.md and docs/PILOT_ROI_MODEL.md (bounded claims)
- templates/email/exec-digest.* (tone reference)

Do this:
1. Add POST /v1/pilots/board-pack.pdf (ExecuteAuthority + commercial tier
   guard consistent with value-report) accepting quarter (Q1–Q4 + year) + optional tenant UTC window.
2. Reuse existing composers/renderers; do not duplicate ROI math — call shared services.
3. Add ArchLucid.Api.Tests integration test with Jwt or factory pattern matching other pilot PDF tests.
4. Document curl example in docs/CLI_USAGE.md and a short runbook section in docs/runboards or docs/OPERATOR_QUICKSTART.md.
5. Wire archlucid-ui link from /value-report or /settings/exec-digest (single CTA) behind useEnterpriseMutationCapability.

Stop and ask the user before:
- Adding revenue projections, customer logos, or unaudited financial totals
- Sending the endpoint output directly to external recipients without review

Exit criteria: endpoint + test + docs; PDF builds deterministically in CI;
no new hardcoded dollar claims beyond ROI_MODEL anchors.
```

---

## Prompt C — Task-success telemetry + operator dashboard tile

**Owner gate.** Production **PII** in event payloads; **sampling** policy; dashboard copy signed off by product.

```
Goal: measure first-session and first-commit success rates in-product so
usability is evidence-based (assessment §1.11).

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md §1.11
- docs/OBSERVABILITY.md (meter naming conventions)
- archlucid-ui/src/app/(operator)/ (home / runs index — where a small tile fits)
- ArchLucid.Api/Controllers/Diagnostics/ (existing telemetry endpoints, if any)
- grep archlucid_first_session or sponsor-banner telemetry patterns

Do this:
1. Define a minimal event model: archlucid_operator_task_success_total
   with labels {task="first_run_committed|first_session_completed"} — increment only server-side on verified state transitions.
2. Expose counters on the existing Prometheus scrape path (follow current meter registration patterns).
3. Add a read-only operator dashboard tile (ReadAuthority) showing last-7-day rates from GET /v1/diagnostics/... or reuse metrics snapshot API if one exists; if not, add a narrow read endpoint.
4. Vitest for the tile component; no PII in labels.
5. Document the metric in docs/OBSERVABILITY.md and link from docs/FIRST_30_MINUTES.md.

Stop and ask the user before:
- Enabling high-cardinality labels (tenant name, email domain)
- Storing client-side only metrics that security review would reject

Exit criteria: metric registered + dashboard tile + tests + docs;
Prometheus text parse smoke if repo pattern exists.
```

---

## Prompt D — Public /pricing quote-on-request flow

**Owner gate.** **To:** address for quote mail; **Salesforce/HubSpot** integration; **auto-provisioning** — owner decisions.

```
Goal: remove a calendar round-trip for buyers who see prices but cannot
click live checkout yet (assessment §1.12 / §1.15).

Read first:
- archlucid-ui/src/app/(marketing)/pricing/page.tsx
- docs/go-to-market/ORDER_FORM_TEMPLATE.md
- docs/PENDING_QUESTIONS.md items 8, 9, 13
- ArchLucid.Api/Controllers/ (patterns for anonymous POST with rate limit + honeypot)

Do this:
1. Add a minimal POST /v1/marketing/pricing/quote-request (anonymous,
   rate-limited) body: work email, company, tier interest, message max N chars.
2. Persist to an append-only table OR forward to configured webhook/email —
   choose the smallest path consistent with repo patterns (prefer existing outbound mail abstraction if present).
3. Add server-side validation + bot friction (honeypot field + fixed window rate limit).
4. Marketing page form with axe-clean markup; Playwright spec for submit happy path (mock transport).
5. Update docs/go-to-market/PRICING_PHILOSOPHY.md cross-link: quote path vs live checkout path.

Stop and ask the user before:
- Wiring to a production CRM without DPA review
- Auto-creating tenants or trials from quote requests

Exit criteria: form ships; API tested; spam-safe defaults; docs updated;
PENDING_QUESTIONS item 13 annotated if quote path changes marketing posture.
```

---

## Prompt E — Marketing site “compliance journey” page

**Owner gate.** **Legal** sign-off on every sentence that sounds like a certification; **SOC 2** calendar claims.

```
Goal: one honest public page: where ArchLucid is today, what is in scope,
what is explicitly not attested (assessment §1.13).

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md §1.13, §2.3
- docs/go-to-market/TRUST_CENTER.md
- docs/security/CAIQ_LITE_2026.md, docs/security/COMPLIANCE_MATRIX.md
- archlucid-ui/src/app/(marketing)/ (layout patterns, trust links)
- docs/PENDING_QUESTIONS.md items 6, 12, 17

Do this:
1. Add archlucid-ui route /compliance-journey (or /trust/compliance-journey) with content sourced only from existing docs — no new certifications claimed.
2. Link to Trust Center, CAIQ/SIG, DPA, subprocessors; include explicit "not SOC 2 attested" language matching PENDING_QUESTIONS Resolved table.
3. axe Playwright gate for the new route if live-api a11y suite pattern exists; else Vitest a11y smoke.
4. Add footer/nav cross-link from TRUST_CENTER.md to the new page.
5. Add PENDING_QUESTIONS.md note under item 12 if publication channel choice shifts.

Stop and ask the user before:
- Implied FedRAMP/StateRAMP readiness without item 17 owner decision
- Publishing a "roadmap date" for SOC 2 without finance/legal alignment

Exit criteria: page live in repo; links verified; no over-claim vs COMPLIANCE_MATRIX;
CHANGELOG one-liner.
```

---

## Prompt F — One-click procurement pack (ZIP)

**Owner gate.** **Versioning** of bundled PDFs; **watermarking** for NDA variants; **distribution** off-repo.

```
Goal: procurement officers download one ZIP: DPA, subprocessors, SLA,
CAIQ/SIG excerpts, trust links index (assessment §1.16).

Read first:
- docs/go-to-market/TRUST_CENTER.md
- docs/security/ (CAIQ, SIG, DPA paths)
- ArchLucid.Application/Pilots/MarkdownPdfRenderer.cs (if PDF index needed) OR static packaging script
- docs/PENDING_QUESTIONS.md procurement-adjacent items

Do this:
1. Add scripts/build_procurement_pack.ps1 and .sh sibling that assembles
   docs into dist/procurement-pack.zip with a manifest.json listing file hashes and source paths.
2. Optionally expose GET /v1/marketing/procurement-pack.zip (anonymous, DemoEnabled-style gate OR static export only — pick one consistent with security posture; document the choice in SECURITY.md).
3. CI: script runs on PR to ensure ZIP rebuild is deterministic (or weekly scheduled workflow).
4. Document in docs/go-to-market/TRUST_CENTER.md and INTEGRATION_CATALOG.md.
5. Do not duplicate legal text — symlink or copy from canonical docs paths only.

Stop and ask the user before:
- Including pen-test redacted summary before assessor publication
- Hosting the ZIP only in a private portal vs public path

Exit criteria: reproducible pack build + doc links + CI or schedule note;
OpenAPI snapshot if a new public API route is introduced.
```

---

## Prompt G — Per-run traceability ZIP bundle (operator / auditor)

**Owner gate.** **Export policy** for customer-attached bundles; **PII** in audit payloads; **large-run** size caps.

```
Goal: GET /v1/runs/{runId}/traceability-bundle.zip assembles audit slice,
decision traces, manifest summary, comparison delta refs — assessment §1.17.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md §1.17
- ArchLucid.Api/Controllers/ (audit search, run detail, export patterns)
- ArchLucid.Persistence/ (audit query limits — align with existing CSV export caps)
- docs/AUDIT_COVERAGE_MATRIX.md

Do this:
1. Implement a new application service TraceabilityBundleBuilder in
   ArchLucid.Application/ (single responsibility; interfaces in abstractions if repo pattern requires).
2. Controller: GET /v1/runs/{runId}/traceability-bundle under ReadAuthority (or Auditor policy if stricter — match audit export precedent).
3. Stream ZIP from memory with size guard; return 413 or ProblemDetails when over cap; unit tests for builder; integration test for small run.
4. Operator UI: one button on run detail next to existing export affordances.
5. Document in docs/API_CONTRACTS.md and OPERATOR_QUICKSTART.md.

Stop and ask the user before:
- Including full LLM prompts if redaction policy says omit
- Raising default ZIP cap without storage cost review

Exit criteria: happy-path ZIP + tests + UI + docs; OpenAPI snapshot updated.
```

---

## Prompt H — Simmy chaos: quarterly game day runbook + workflow hook

**Owner gate.** **Production** chaos execution; **customer notification**; **SLO breach** ownership.

```
Goal: promote Simmy from "workflow exists" to a quarterly game day with a
published outcome template (assessment §1.18).

Read first:
- .github/workflows/simmy-chaos-scheduled.yml
- docs/DEGRADED_MODE.md, docs/runbooks/ (existing incident patterns)
- docs/MUTATION_TESTING_STRYKER.md or reliability docs mentioning Simmy
- infra/ (any existing game-day doc)

Do this:
1. Add docs/runbooks/GAME_DAY_CHAOS_QUARTERLY.md: pre-flight checklist,
   blast radius limits, abort criteria, post-run metrics to capture, RACI stub.
2. Extend or duplicate the scheduled workflow with workflow_dispatch inputs:
   scenario id, environment (staging-only default), dry-run flag.
3. Wire workflow conclusion to append a short summary file under docs/quality/game-day-log/YYYY-MM-DD.md (or artifact-only if branch protection blocks commits — document).
4. Link from README or docs/TEST_EXECUTION_MODEL.md Tier context.
5. Append PENDING_QUESTIONS.md if production chaos needs explicit owner approval item.

Stop and ask the user before:
- Running fault injection against production
- Disabling circuit breakers or RLS for the exercise

Exit criteria: runbook + dispatchable workflow + documented artifact path;
no default schedule change that surprises on-call without doc notice.
```

---

## How to use these prompts

- **Pairing:** Prompt **A** reduces architectural risk; **B–D** commercial motion; **E–F** procurement; **G** audit customer handoff; **H** reliability culture.
- **OpenAPI:** Any new public route → regenerate [`ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json`](../ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json) per `OPENAPI_CONTRACT_DRIFT.md`.
- **Migrations:** New SQL → new numbered migration + `ArchLucid.sql` + `Rollback/R*.sql` per repo rules.

---

## Related

| Doc | Role |
|-----|------|
| [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) | Source recommendations §1.9–1.18 |
| [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md) | Primary eight prompts (Improvements 1–8) |
