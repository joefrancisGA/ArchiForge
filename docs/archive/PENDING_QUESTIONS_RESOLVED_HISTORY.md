> **Scope:** Archived resolved owner Q&A moved from [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) for spine line budget.

# Pending questions — resolved history (archive)

**Do not** use this file as the place to record *new* owner decisions — append to [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) first; only move content here when trimming the spine again.

---

## Part A — through 2026-04-21 (prior session)

## Resolved 2026-04-22 (assessment owner Q&A — 16 decisions)

These decisions came out of a structured owner Q&A session driven by the latest independent quality assessment. They are recorded here as the single source of truth; downstream files (Trust Center, ORDER_FORM_TEMPLATE, ACCESSIBILITY, TEAMS, ADR 0030, the SOC 2 row, etc.) will be updated against this table in the implementation PRs that follow. **No production code touched in this entry** — this is a decision snapshot.

### Marketplace + Stripe commerce un-hold (item 22 / 8 / 9)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **22 — cutover shape** | **Single-window cutover** — same maintenance window for Azure Marketplace "Go live" + Stripe live keys. | Item 22 sub-bullet (a) closed; sub-bullet (b) calendar still owner-only inside the Q2 2026 quarter; (c) staging stays on Stripe TEST; (d) preflight runner named in implementation PR. |
| **22 — calendar quarter** | **Q2 2026** un-hold target. Specific month/day still owner-only and to be picked closer to the date. | Item 22 sub-bullet (b) narrowed; ADR 0029 / ADR 0030 / strangler PR sequencing now have a hard external deadline (PR A2 + PR A3 must merge well before the first paying customer). |
| **9a — Stripe statement descriptor** | **`ARCHLUCID PLATFORM`** (18 chars, fits the 22-char Stripe limit). Configured as the **prefix** in Stripe Dashboard → Settings → Public details. | Item 9 sub-bullet (a) closed; runbook entry to be added in the implementation PR. |
| **9b — chargeback / refund / dunning policy text** | **Assistant scaffolds a draft** for the order-form template + Trust Center, clearly marked **"pending legal sign-off"**. Owner / legal sign before commerce un-hold. | Item 9 sub-bullet (b) drafting authorized; legal sign-off remains owner-only and is the gate to publication. |
| **9d / 8 — Microsoft Partner Center publisher identity** | **Publisher display name: `ArchLucid`.** **MPN ID** and **Marketplace Offer ID slug** are owner-to-provide-later (not yet established). | Item 8 sub-bullets (a) partial; (b) and (c) explicitly deferred (assistant cannot create Microsoft IDs). Footnote: if a separate legal entity (e.g., `ArchLucid Inc.`) is incorporated later, the Partner Center tax + payout profile takes the legal name; the listing card display name stays `ArchLucid`. |
| **9d — Stripe webhook secret rotation** | **Owner self / quarterly + on-incident.** Documented in the commerce runbook as the default cadence; on-incident rotation triggered by any failed webhook delivery sequence after deploy or any suspected secret leak. | Item 9 sub-bullet (d) closed for runbook drafting purposes; Key Vault binding still owner-only at commerce un-hold time. |

### Accessibility (items 12 / 26)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **12 — WCAG 2.2 AA publication channel** | **Public `/accessibility` page** on the marketing site (in addition to the Trust Center / `ACCESSIBILITY.md`). | Item 12 main sub-bullet closed; new marketing page work added to the next-improvements queue. |
| **12 — accessibility mailbox** | **New alias `accessibility@archlucid.com`**, routing to the **same custodian as `security@archlucid.com`**. | Item 12 mailbox sub-bullet closed; alias provisioning is the same operational task as `security@`. |
| **26 — VPAT publication** | **Self-attestation only** for v1 (formal VPAT deferred). | Item 26 closed for v1; revisit only if an enterprise procurement requires a formal VPAT. |
| **26 — self-attestation cadence** | **Annually** — `/accessibility` page carries `Last reviewed: <date>` updated once per year. | Calendar reminder belongs in the same place as the quality-assessment cadence reminder. |

### Public price list (item 13)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **13 — public price list** | **Publish on the marketing site simultaneously with Marketplace go-live.** | Item 13 closed; the public price list publication PR sequences with the commerce un-hold PR (single window). |

### Customer-supplied baseline (item 28)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **28 — soft-required baselineReviewCycleHours at signup** | **Deferred** — owner not ready to sign off on the UX change yet. | Item 28 stays open; no implementation work scheduled. |

### Production chaos / Simmy game day (item 34)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **34 — production Simmy / fault-injection** | **Production never** for v1 (and beyond unless explicitly re-opened). The fail-fast guard on `simmy-chaos-scheduled.yml` stays in force; staging-only chaos is the standing posture. | Item 34 closed as **"production never"**. The runbook can drop its "owner approval gate before any future widening" wording and replace it with "production chaos out-of-scope per owner decision 2026-04-22; reopen requires explicit ADR." |

### PGP key (items 10 / 21)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **10 / 21 — PGP key custodian** | **Owner self.** | Items 10 / 21 custodian sub-bullets closed; key generation is on the owner. |
| **10 / 21 — PGP scaffold timing** | **Scaffold the recipe now.** Assistant adds `docs/security/PGP_KEY_GENERATION_RECIPE.md` (gpg recipe, key parameters Ed25519 / RSA 4096, file-drop location `archlucid-ui/public/.well-known/pgp-key.txt`, fingerprint publication checklist) in the next implementation PR. Owner generates and drops the public key when ready; the existing CI guard turns green automatically. | Items 10 / 21 scaffold sub-bullets closed. |

### Reference customer (item 19)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **19 — first PLG row owner** | **Owner solo.** Owner watches the trial-to-paid event, validates the case study draft with the customer, and flips the row in `docs/go-to-market/reference-customers/README.md` from `Customer review` to `Published`. | Item 19 closed for ownership. **Update 2026-04-23:** the entire publication milestone is now release-window-pinned to **V1.1** — see *Resolved 2026-04-23 (Reference-customer publication scope)* below. The owner is still the executor, but V1 GA no longer waits on this milestone. |

### Pen-test publication (item 20)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **20 — Aeronova pen-test summary publication** | **NDA-gated only** for v1. Public Trust Center carries the existence of the engagement and the high-level posture ("most recent assessment completed YYYY-MM-DD; redacted summary available under NDA"); the redacted summary itself is not on the public site. | Item 20 closed for publication-channel; vendor scheduling still owner-only. |

### SOC 2 ARR threshold (item 6)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **6 — SOC 2 revisit-trigger ARR** | **$1M ARR** band (directional, not contractual). Trust Center wording: *"We will pursue SOC 2 Type 1 readiness once we cross approximately $1M in ARR; until then, we publish a self-attested security and compliance summary."* | Item 6 sub-question closed. |

### Cross-tenant pattern library (item 14)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **14 — implementing ADR ownership** | **Resolved 2026-04-22** — **ADR 0031** drafted in full for owner sign-off: [`docs/adr/0031-cross-tenant-pattern-library.md`](adr/0031-cross-tenant-pattern-library.md) (**Status: Proposed** until owner flips to **Accepted**). | Item 14 closed for drafting; **implementation PRs remain blocked** until ADR **Accepted**. |

### Golden-cohort real-LLM (items 15 / 25)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **15 / 25 — monthly Azure OpenAI token budget for the dedicated golden-cohort deployment** | **Up to $50 / month** ceiling. Sized for **20 rows × 1 nightly run × small prompt**, with effectively zero headroom for re-runs or parameter sweeps. Implementation must add a **kill-switch** when month-to-date spend approaches the cap. | Items 15 / 25 closed at the budget level; deployment provisioning + key injection still owner-only at production-environment time. |

### ADR 0030 sub-decisions (items 35d / 35e)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **35d — `dbo.GoldenManifestVersions` drop policy (PR A4)** | **(i) hard drop** — no historical Coordinator-shape rows preserved. Pre-release acceptable per the same waiver as ADR 0029 gates (i)/(iv); the Q2 2026 commerce calendar puts the legacy table out of reach of any paying customer. | Mechanical alignment: [ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md) § Component breakdown **PR A4** row, § Operational considerations (**PR A4 backfill — N/A**), and § Owner sub-decisions row **35d** (2026-04-22). |
| **35e — Phase 3 PR B placeholder tracker shape** | **Both** — standalone `docs/architecture/PHASE_3_PR_B_TODO.md` **and** inline checklist on ADR 0029 § Lifecycle. The standalone tracker is the working surface for PR B execution; the ADR checklist is the authoritative inline tracker for the 2026-05-15 deadline. | Mechanical alignment: [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md) § Lifecycle § **PR B — audit-constant retirement checklist** + [`docs/architecture/PHASE_3_PR_B_TODO.md`](architecture/PHASE_3_PR_B_TODO.md) + `scripts/ci/assert_pr_b_tracker_in_sync.py` (warn-only CI advisory). |

---

## Resolved 2026-04-22 (35c + 35f — ADR 0030)

| Item | Decision | Affects |
|------|----------|---------|
| **35c.1** — feature-flag scope for `RunCommitOrchestratorFacade` (PR A2) | **(ii) global config** — `Coordinator:LegacyRunCommitPath` in `appsettings` (and environment-variable override). Rollback = config flip + rolling restart. | No per-tenant row; smallest code surface for pre-release. |
| **35c.2** — default of the legacy / coordinator commit path flag | **(B) pre-release** — **product intent: `false`** (Authority path the default) once `RunCommitPathSelector` + `AuthorityDrivenArchitectureRunCommitOrchestrator` land. **Interim** [`appsettings.json`](../ArchLucid.Api/appsettings.json) uses `LegacyRunCommitPath: true` so the existing operator merge path keeps working until that wiring ships in a follow-on PR. | The owner decision is recorded; the flip to `false` in shipped config is gated on the authority orchestrator PR (keeps `main` buildable). |
| **35f** — typed `ManifestService` / `ManifestDatastore` source (PR A0.5) | **(i) graph node metadata** — `GraphNode.Properties` carries optional `serviceType` and `runtimePlatform` string keys (enum names, case-insensitive). `DefaultGoldenManifestBuilder` maps topology nodes to typed service/datastore rows; when keys are absent, `Unknown` enum values apply (see new `Unknown = 0` on `ServiceType`, `RuntimePlatform`, `DatastoreType`). | Rule corpus / ingestion can populate `Properties` incrementally; no second classifier service in v1. |

---

## Resolved 2026-04-22 (ADR 0030 owner sub-decisions — 35a + 35b)

These six decisions came out of a structured walk-through of [`adr/0030-coordinator-authority-pipeline-unification.md`](adr/0030-coordinator-authority-pipeline-unification.md) sub-bullets **35a** and **35b** (interactive owner Q&A — recommended answer set accepted in full, plus a one-word `yes` on the write-overload return type). They unblock drafting of **PR A0** (Authority projection builder, additive) and **PR A1** (Authority repository write overload). **35c** / **35f** are resolved in the *Resolved 2026-04-22 (35c + 35f — ADR 0030)* table above; **35d** / **35e** are resolved in the *Resolved 2026-04-22 (assessment owner Q&A — 16 decisions)* → **ADR 0030 sub-decisions (items 35d / 35e)** table above.

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **35a (top-level)** — Where does the Authority → Contracts projection live? | **(ii) new mapper class** — `AuthorityCommitProjectionBuilder` consumed by `RunCommitOrchestratorFacade`. Authority engine itself stays pure (no opt-in projection flag inside it). | Implicit from the `IAuthorityCommitProjectionBuilder` design recorded in [ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md) § Component breakdown / PR A0. Closes sub-bullet 35a. |
| **35a.1** — `SystemName` source on the projected manifest | **`sibling-row`** — read from existing `Run` / `ArchitectureRequest` row via the existing `IRunRepository`. No new Authority schema field. | `AuthorityCommitProjectionBuilder` takes a constructor dependency on `IRunRepository`. Captured in [ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md) § Owner sub-decisions row 35a.1. |
| **35a.2** — Typed `Services` + `Datastores` populated from rule-engine resource strings, or left empty? | **`empty-with-guard`** — leave empty in PR A0; populate from typed source in **new sub-PR A0.5**. Brittle string-parser rejected. | New PR A0.5 row added to [ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md) § Component breakdown. New file `docs/architecture/AUTHORITY_PROJECTION_KNOWN_EMPTY.json` ships with PR A0 to enforce that the empty set does not silently grow. |
| **35a.3** — `Relationships` populated from graph snapshot in PR A0, or left empty? | **`empty-with-guard`** — leave empty in PR A0; populate in a future Relationships-graph PR (scope deferred until PR A2 planning). | Allow-list rationale row points at the deferred PR. Assistant will surface a follow-up question when scoping PR A2. |
| **35a.4** — Adopt the JSON allow-list + CI guard mechanism for "intentionally empty" projection fields? | **`yes`** | New file `docs/architecture/AUTHORITY_PROJECTION_KNOWN_EMPTY.json` + new CI script `scripts/ci/assert_authority_projection_known_empty.py` + workflow step in `.github/workflows/ci.yml`. Self-eroding: when PR A0.5 (Services + Datastores) and the future Relationships-graph PR merge, those rows must be removed from the allow-list **inside the same PR** (script enforces). |
| **35b** — Write-overload return type on `IGoldenManifestRepository.SaveAsync(Contracts.Manifest.GoldenManifest, ...)` | **`Task<Decisioning.Models.GoldenManifest>`** (return the produced Authority-shape manifest). Owner expanded the original `Task` vs `Task<Guid>` framing to a third option and chose it. | Caller keeps idempotency-key reasoning (same `ManifestId` it would have written); one extra in-memory allocation, much clearer caller code than re-reading after the write. Overload signature pinned in [ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md) § Component breakdown / PR A1. |

These six decisions also triggered a self-amendment to ADR 0030 (recorded in its front matter): every internal cross-reference to "pending question item **34** / **34a–d**" is corrected to **35** / **35a–e** (the original draft mis-numbered them). New sub-bullet **35f** is opened below for the typed-services source decision PR A0.5 needs before it can start.

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

## Resolved 2026-04-21 (Phase 3 PR A re-scoped — ADR 0030)

The single-session "Phase 3 PR A" was the other dedicated-session item queued by the same-day follow-up table. A grounding read of the actual code state (not just the optimistic ADR text) found a **hard blocker** that required pivoting from "execute PR A" to "author the unification ADR that re-scopes PR A into a sequenced multi-PR plan". No production code touched in this entry.

| Decision | Answer | Affects |
|----------|--------|---------|
| **Phase 3 PR A — single-session deletion?** | **No — mechanically impossible.** Two pipelines persist incompatible domain models (`Contracts.Manifest.GoldenManifest` vs `Decisioning.Models.GoldenManifest`) to incompatible SQL tables (`dbo.GoldenManifestVersions` vs `dbo.GoldenManifests` + 6 satellite tables) using different decision engines. `RunCommitOrchestratorFacade` is a 12-line thin pass-through, not a Coordinator-vs-Authority bridge. Owner sign-off on no-rollback was for the original single-PR scope; the assistant pivoted to documenting the re-scope rather than silently downgrading. | New [ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md). Amends [ADR 0021](adr/0021-coordinator-pipeline-strangler-plan.md) § Phase 3 mechanism (a), [ADR 0022](adr/0022-coordinator-phase3-deferred.md) (PR A → PR A0–A4 framing), [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md) (2026-05-15 deadline reassigned to PR B). Updates [`archive/dual-pipeline-navigator-superseded.md`](archive/dual-pipeline-navigator-superseded.md) + [`COORDINATOR_STRANGLER_INVENTORY.md`](architecture/COORDINATOR_STRANGLER_INVENTORY.md). Captures unanswered owner questions as item **35** sub-bullets a–e below (one per sub-PR that needs a fresh decision). |
| **Authority interface write port — overload vs new writer port?** | Owner already chose **overload on `IGoldenManifestRepository`** (single port per kind) in answer to in-session question `q_pra_authority_writes`. ADR 0030 § Component breakdown row for **PR A1** records this as the chosen shape. | Resolves the structural question for PR A1; the field-level shape (return type) is captured as item **35b**. |
| **Phase 3 PR B placeholder tracker?** | Owner answer to in-session question `q_pra_audit_pr_b_scope` was **`prb_create_tracker`**. **Deferred to a follow-on session** because the ADR 0030 re-scope dominated this session's scope; ADR 0030 § Operational considerations now records the "PR B inherits the 2026-05-15 deadline" framing in lieu of the standalone tracker. The standalone `docs/architecture/PHASE_3_PR_B_TODO.md` file is a queued follow-on task. | Captured as item **35e** below. |

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
| Cross-tenant pattern library | **Approved** (opt-in, k-anonymity, DPA carve-out) 2026-04-21 — implementing **ADR 0031** drafted 2026-04-22: [`docs/adr/0031-cross-tenant-pattern-library.md`](adr/0031-cross-tenant-pattern-library.md) (**Proposed** until owner **Accepted**). |
| Azure subscriptions | **Staging:** existing subscription. **Production:** **second subscription** dedicated to prod (create empty; wire Terraform/CD after staging is green). |
| Production Azure subscription ID | **`aab65184-5005-4b0d-a884-9e28328630b1`** — recorded in [`AZURE_SUBSCRIPTIONS.md`](library/AZURE_SUBSCRIPTIONS.md) as the single source of truth. Operator action: set GitHub Environment secret `AZURE_SUBSCRIPTION_ID` on the **`production`** environment to this value (and confirm sibling `AZURE_TENANT_ID` / `AZURE_CLIENT_ID` are populated for OIDC). Default region: **`centralus`**. |
| DNS / TLS | Owner **approves** DNS and TLS cutover for production hostnames. |
| Domain | **archlucid.com** — registration fee paid; confirm WHOIS when registrar completes. |
| Reference customer (GTM) | **Ship self-serve trial first** — first **paying** tenant becomes the first publishable reference (`TRIAL_FIRST_REFERENCE_CASE_STUDY.md`). |
| SOC 2 Type I/II | **Deferred** — interim posture is self-assessment + Trust Center honesty; revisit when ARR justifies CPA attestation. |
| ServiceNow + Confluence as **first-party** workflow integrations | **Out of scope for now (2026-04-21)** — **ServiceNow** is operational ITSM / CMDB-centric; ArchLucid is intentionally **upstream** (design-time architecture, governance, manifests). **Confluence** is deferred because the integration posture is **Microsoft-first** (Entra, Azure DevOps, Teams, Logic Apps per [`docs/adr/0019-logic-apps-standard-edge-orchestration.md`](adr/0019-logic-apps-standard-edge-orchestration.md); GitHub + ADO manifest-delta already shipped). Revisit only if product strategy changes. |
| **Customer-shipped Docker / container production bundles** | **Out of scope (2026-04-21)** — ArchLucid is a **vendor-operated SaaS** product. We do **not** treat shipping **production** Docker images, Helm charts, or customer-operable full-stack compose bundles as a standard customer deliverable. **Customer-facing artifacts** are the **CLI**, **published API client libraries** (for example `ArchLucid.Api.Client`), **OpenAPI / REST contracts**, and **documentation**. **`docker compose` / `archlucid pilot up`** remain **optional local evaluation and engineering** paths in the repo, not a committed “bring your own container” product track unless a future ADR reopens it. |

---


---

## Part B — 2026-04-23 batches (SaaS framing through assessment §4)

## Resolved 2026-04-23 (SaaS-framing follow-on Q&A — 9 decisions)

Owner answers gathered in-session on 2026-04-23 after items 36 and 37 were surfaced (and with two ADR 0030 deferred PRs still pending). All 9 are recorded here so future sessions do not re-ask.

| # | Decision area | Owner answer (2026-04-23) | Implementation impact |
|---|---|---|---|
| A | **ADR 0030 PR A3 / A4 unblock path** (DemoSeedService + ReplayRunService cannot write to `dbo.GoldenManifests` because they don't produce snapshots / decision traces / evidence required by FK constraints) | Build the **full Authority FK chain** in demo-seed + replay (snapshots, decision traces, evidence). | Unblocks A3 (delete coordinator repos from composition + delete legacy orchestrators) and A4 (drop `dbo.GoldenManifestVersions`). New work item: rewrite `DemoSeedService` and `ReplayRunService` to emit the full Authority entity chain. **Shipped 2026-04-24 (PR A3)** — see [`docs/CHANGELOG.md` 2026-04-24 entry "Coordinator → Authority pipeline unification PR A3"](CHANGELOG.md). PR A4 (drop `dbo.GoldenManifestVersions` table — already a no-op since the table is gone) stays separate per ADR 0030 § Component breakdown rollback-boundary rule. |
| B | **ADR 0030 demo seed — completeness** | **Tier by preset:** `quickstart` demo gets a minimum FK-satisfying skeleton (one snapshot + one decision trace + one evidence row per manifest); **vertical** presets get production-realistic data (every snapshot, decision trace, evidence chain, and finding). | Adds one config flag (e.g. `Demo:SeedDepth = quickstart \| vertical`) and matches what each preset is trying to prove. |
| C | **Buyer-facing first-30-minutes doc — location** (item 36 part a) | **Both:** short repo stub at `docs/BUYER_FIRST_30_MINUTES.md` for evaluators arriving via GitHub, full copy on the `archlucid-ui/src/app/(marketing)/get-started/` route. | Repo doc is contributor-controlled (assistant scaffolds, owner approves copy); marketing-route copy is owner-controlled (brand voice). |
| D | **Buyer-facing first-30-minutes — sample preset** (item 36 part b) | **Vertical-picker first:** show the five-vertical picker before starting any sample run. No auto-pick from signup metadata, no Contoso default. | Trial funnel UI must pause between signup and the first sample run to collect industry. |
| E | **Buyer-facing first-30-minutes — guided-demo CTA** (item 36 part c) | *(Not selected in this batch — defer with item 36 part c reopened separately when the live funnel ships.)* | Treat as a Phase-2 marketing-page question once Improvement 2 is live in production. |
| F | **In-product support-bundle download — role** (item 37 part a) | **Allowed for anyone with `ExecuteAuthority` on the tenant** (broader than Tenant Admin only; matches existing `/v1/admin/*` policy convention). | Wire `[Authorize(Policy = "ExecuteAuthority")]` on the new endpoint. |
| G | **In-product support-bundle download — UI location** (item 37 part b) | **New `/admin/support` page** in the operator UI (clean home for support actions; room to add "open ticket" later). | Add `archlucid-ui/src/app/(operator)/admin/support/page.tsx` with the download button; link from the existing `/admin/api-keys` page. |
| H | **Contributor folder formalisation** | **Yes — move now to `docs/engineering/`** with stub redirects at the old paths. Folder name **`engineering/`** chosen over `contributor/`, `internal/`, or `dev/` (more inviting tone; closer to industry norm). | Six docs moved 2026-04-23 (`FIRST_30_MINUTES.md`, `INSTALL_ORDER.md`, `BUILD.md`, `CONTAINERIZATION.md`, `DEVCONTAINER.md`, `DEPLOYMENT.md`); stubs left at old paths; canonical entry points (`START_HERE.md`, `README.md`, `dist/procurement-pack/README.md`, the 68.60 assessment) and CI scripts (`check_onboarding_spine_line_budget.py`, `DoctorCommand.cs`) updated to new paths. |
| I | **Scope of next slice** | **Just the contributor-folder move + audience-banner sweep** across the four remaining files (`BUILD.md`, `CONTAINERIZATION.md`, `DEVCONTAINER.md`, `DEPLOYMENT.md`). Defer ADR 0030 A3/A4 work and the support-bundle UI to separate sessions. | Done in the same change set as decision H. ADR 0030 A3/A4 remains deferred; support-bundle UI is a follow-on. |

**Items 36 and 37 below are now PARTIALLY RESOLVED by this table** (parts (a), (b), (c) for item 36 — except CTA which is decision E above; parts (a), (b) for item 37 — redaction policy still open). They remain in this file for traceability; future sessions should consult the table above before re-asking.

---

## Resolved 2026-04-23 (Jira connector scope)

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **Jira ITSM connector — release window** | **Out of scope for V1; in scope for V1.1.** Owner-affirmed split between V1 contract and V1.1 commitment. The minimum-viable V1.1 surface is one-way (finding → Jira issue with correlation back-link); two-way status sync is part of the same V1.1 release window but may ship as a fast-follow inside it. Calendar date is **not** pinned by this decision; pinning a date requires a follow-up owner entry. | [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) § 3 (added explicit "Jira connectors" out-of-scope row pointing at V1.1); [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) § 6 (new "ITSM connectors — V1.1 candidates" section); [`docs/go-to-market/INTEGRATION_CATALOG.md`](go-to-market/INTEGRATION_CATALOG.md) § 2 (Jira row promoted from `[Planned]` to **`[V1.1 — planned]`**); [`docs/CHANGELOG.md`](CHANGELOG.md) 2026-04-23 entry. **Update (same day, second pass):** **ServiceNow** is no longer deferred-without-window — it is now release-window-pinned to V1.1; see *Resolved 2026-04-23 (ServiceNow + Slack connector scope)* below. **Confluence** continues to be deferred without a release window. **Azure DevOps Work Items** stays at `[Planned]` (not promoted to V1.1 by this decision). |

---

## Resolved 2026-04-23 (ServiceNow + Slack connector scope)

Owner decisions (2026-04-23, second pass — same day as the Jira-scope resolution above): the **ServiceNow** ITSM connector is **explicitly in scope for V1.1**, and the **Slack** chat-ops connector is **explicitly in scope for V2**. **Microsoft Teams** stays as the V1 first-party chat-ops surface (already shipped — see [`docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md`](integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md)) and is **not** retracted by this decision. These resolutions tighten previously open-ended "Planned" / "deferred without window" messaging into named release windows so buyer-facing copy stops reading as "someday".

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **ServiceNow ITSM connector — release window** | **Out of scope for V1; in scope for V1.1.** Minimum-viable V1.1 surface is one-way: finding → ServiceNow `incident` with correlation back-link. **Open V1.1-planning question (do not assume in V1.1 ADR):** whether the same V1.1 release also ships `cmdb_ci` mapping, or whether `cmdb_ci` ships as a V1.1 fast-follow. **Two-way status sync** (ServiceNow → ArchLucid) is **not** committed for V1.1 unless a separate owner decision adds it. Calendar date for V1.1 is **not** pinned by this decision. | [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) § 3 (new "ServiceNow connectors" out-of-scope row pointing at V1.1); [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) § 6 (ServiceNow row added to ITSM table); [`docs/go-to-market/INTEGRATION_CATALOG.md`](go-to-market/INTEGRATION_CATALOG.md) § 2 (new ServiceNow row at **`[V1.1 — planned]`**); [`docs/CHANGELOG.md`](CHANGELOG.md) 2026-04-23 entry; free-text question **11 above** updated to drop ServiceNow from its deferral list. |
| **Slack chat-ops connector — release window** | **Out of scope for V1 and V1.1; in scope for V2.** Minimum-viable V2 surface is **parity with the shipped Microsoft Teams connector**: outbound notification sink driven by the same `EnabledTriggersJson` per-tenant opt-in matrix, secret material in **Azure Key Vault** with only a secret-name reference in SQL, the same canonical event-type catalog, no parallel notification model. **In-Slack action affordances** (acknowledge / approve from Slack) are **stretch** for V2, not committed. Microsoft Teams remains the supported first-party chat-ops surface for V1 and V1.1. Calendar date for V2 is **not** pinned by this decision. | [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) § 3 (new "Slack connectors" out-of-scope row pointing at V2); [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) § 6a (new "Chat-ops connectors — V2 candidates" section); [`docs/go-to-market/INTEGRATION_CATALOG.md`](go-to-market/INTEGRATION_CATALOG.md) § 2 (new Slack row at **`[V2 — planned]`**); [`docs/CHANGELOG.md`](CHANGELOG.md) 2026-04-23 entry. |

**Rules:**

- These are **release-window** promises, not dates. Pinning calendar dates requires a follow-up owner entry recorded here.
- **Microsoft Teams stays shipped in V1**; this decision does not retract or downgrade Teams.
- A new chat-ops or ITSM surface **must not** be added to the corresponding `V1_DEFERRED.md` table without its own owner decision recorded in this file.

---

## Resolved 2026-04-23 (Reference-customer publication scope)

Owner decision (2026-04-23, third pass — same day as the Jira and ServiceNow + Slack scope resolutions above): the **first named, public reference customer** milestone — at least one row in [`docs/go-to-market/reference-customers/README.md`](go-to-market/reference-customers/README.md) at `Status: Published`, with a published case study and customer-permissioned logo on the marketing site — is **explicitly out of scope for V1, in scope for V1.1**. V1 GA does **not** wait on this milestone, and V1 quality assessments **must not** charge points against the qualities most affected by its absence (Marketability, Proof-of-ROI Readiness, Differentiability, Trustworthiness, Procurement Readiness). The CI guard [`scripts/ci/check_reference_customer_status.py`](../scripts/ci/check_reference_customer_status.py) **stays in `continue-on-error: true` warn-mode** for the entire V1 window — flipping it to merge-blocking is a V1.1 task. The `−15%` reference discount in [`PRICING_PHILOSOPHY.md` § 5.3](go-to-market/PRICING_PHILOSOPHY.md) remains **notional** for V1; re-rate becomes a candidate at V1.1.

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **Reference-customer publication — release window** | **Out of V1; in scope for V1.1.** Minimum V1.1 commitment is one row at `Status: Published` with a customer-approved case study and a customer-permissioned logo. The owner (per the existing item 19 resolution) remains the executor; this decision changes only **when** that work is required, not **who** does it. Calendar date for V1.1 is **not** pinned. | [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) § 6b (new "Commercial — V1.1 candidates" section); [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) item 19 row updated to point at this resolution; [`docs/archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) § 0.2 *Reference-customer-deferral re-score addendum* (weighted total moves from **68.60% → 70.53%** because Marketability, Proof-of-ROI Readiness, Differentiability, Trustworthiness, and Procurement Readiness are no longer charged for this V1.1 milestone); [`docs/CHANGELOG.md`](CHANGELOG.md) 2026-04-23 entry. **Item 27 dependency unchanged:** the aggregate ROI bulletin still waits on the first `Published` row, and that bulletin therefore implicitly slips to the V1.1 window unless a separate owner decision detaches it. |

**Rules:**

- The CI guard's V1 contract is **warn-mode** — do not flip it merge-blocking before V1.1 land.
- Future quality assessments (after this date) **must not** treat the reference-customer absence as a V1 deficit. Pre-2026-04-23 assessments are correct *for their date*; this decision retroactively re-scores the open 68.60 assessment via its §0.2 addendum, but does not invalidate archived assessments under `docs/archive/quality/`.
- This decision does **not** retract or downgrade the executed pen test summary publication, the Marketplace listing, the Stripe live keys flip, or any other commercial/security milestone — those remain **live V1 obligations** unless their own owner decision defers them.
- A new commercial milestone **must not** be added to `V1_DEFERRED.md` § 6b without its own owner decision recorded here.

---

## Resolved 2026-04-23 (Commerce un-hold scope)

Owner decision (2026-04-23, fourth pass — same day as the Jira, ServiceNow + Slack, and reference-customer scope resolutions above): the **commerce un-hold** milestone — Stripe **live** API keys flipped on, the Azure Marketplace SaaS offer transitioned to `Published` in Partner Center, and DNS cutover for `signup.archlucid.com` to the production Front Door custom domain — is **explicitly out of scope for V1, in scope for V1.1**. V1 GA does **not** wait on live commerce, and V1 quality assessments **must not** charge points against Adoption Friction, Decision Velocity, or Commercial Packaging Readiness for the absence of live keys / a `Published` listing. The V1 commercial motion is **sales-led**: `/pricing` displays numbers, `ORDER_FORM_TEMPLATE.md` drives quote-to-cash, and the trial funnel runs in **Stripe TEST mode on staging** as a sales-engineer-led product evaluation (Improvement 2 in [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) §3 — **stays a live V1 obligation**).

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **Commerce un-hold — release window** | **Out of V1; in scope for V1.1.** Minimum V1.1 commitment: (a) Stripe live keys configured with production webhook secret rotated, (b) Marketplace SaaS offer at `Published` with seller verification + payout + tax profile complete, (c) DNS cutover for `signup.archlucid.com`, (d) the existing `BillingProductionSafetyRules` startup gate passes against the live configuration. The Stripe-live-keys flip and the Marketplace `Published` state are both **owner-only** — Partner Center seller verification, tax profile, and payout account cannot be filed by the assistant. Calendar date for V1.1 is **not** pinned. | [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) § 6b (commerce-un-hold row added alongside the existing reference-customer row); [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) § 3 (new "Out of scope for V1" row); [`docs/archive/quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) § 0.3 *Commerce-un-hold-deferral re-score addendum* (weighted total moves from **70.53% → 71.71%** because Adoption Friction, Decision Velocity, and Commercial Packaging Readiness are no longer charged for this V1.1 milestone); [`docs/CHANGELOG.md`](CHANGELOG.md) 2026-04-23 entry. **Items 8, 9, 22 status:** all three remain **owner-only** but are now release-window-pinned to V1.1 rather than indefinitely open. **Item 27 dependency unchanged** (aggregate ROI bulletin still waits on first `Published` reference customer, also V1.1). |

**Rules:**

- The trial funnel TEST-mode end-to-end work (Improvement 2) is **not** deferred — it stays a live V1 obligation. The CLI smoke (`archlucid trial smoke`), Playwright spec against deterministic mocks, and `docs/runbooks/TRIAL_FUNNEL_END_TO_END.md` runbook all remain V1.
- The `BillingProductionSafetyRules` startup gate stays **shipped in V1**. Its purpose is to make the V1.1 un-hold safe; do not remove it as part of V1.1 work.
- Future quality assessments (after this date) **must not** treat the commerce un-hold as a V1 deficit. Pre-2026-04-23 assessments are correct *for their date*; this decision retroactively re-scores the open 68.60 assessment via its §0.3 addendum.
- This decision does **not** retract or downgrade the executed pen test summary publication, the PGP key generation, the board-pack PDF endpoint (Improvement 9), or the trial-funnel TEST-mode work (Improvement 2) — those remain **live V1 obligations**.
- A new commercial milestone **must not** be added to `V1_DEFERRED.md` § 6b without its own owner decision recorded here.

---

## Resolved 2026-04-23 (sixth pass — fresh independent assessment §10 owner Q&A — 17 decisions)

Owner decisions (2026-04-23, sixth pass — same day as the Jira, ServiceNow + Slack, reference-customer, commerce un-hold, and assessment §4 items 29 / 31–38 scope resolutions above): a fresh first-principles independent quality assessment (in-conversation, weighted readiness **65.34%** before this Q&A) surfaced **17 owner-only questions** across improvements **1**, **4**, **7**, **9**, **10** plus three cross-cutting items. All 17 are recorded here so future sessions do not re-ask. The new file [`docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md`](../QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md) and its paired [`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md`](../CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md) reflect the post-Q&A V1 contract.

| # | Topic | Decision | Affects |
|---|-------|----------|---------|
| **Q1** — Improvement 1 buyer-facing brand voice | **Consultative / pragmatic** (architect-to-architect, plain English) | Voice for both `docs/BUYER_FIRST_30_MINUTES.md` and the marketing `(marketing)/get-started/` route. | Improvement 1 implementation PR — copy is consultative, not formal-corporate or playful-developer. Closes item 36 voice sub-question. |
| **Q2** — Improvement 1 vertical-picker labels | **Use existing `templates/briefs/*` folder slugs** as the visible labels (defaults today). | Improvement 1 implementation PR — picker source-of-truth is the existing folder slugs (`financial-services`, `healthcare`, `manufacturing`, `public-sector`, `public-sector-us`); no new owner-supplied label set required. Closes item 36 vertical-name sub-question. |
| **Q3** — Improvement 1 buyer-page screenshots | **Real anonymized tenant** — owner names `tenantId` and `runId` later. | Improvement 1 implementation PR ships placeholder screenshot slots in the buyer-facing first-30-minutes copy; the anonymized real-tenant capture is a follow-on owner task. Closes item 36 screenshot sub-question (deferred capture, not deferred decision). |
| **Q4** — Improvement 1 placeholder copy in repo stub | **Yes — ship with q35-style markers** (`<<placeholder copy — replace before external use>>`). | Improvement 1 implementation PR uses the existing q35 placeholder discipline; the repo stub at `docs/BUYER_FIRST_30_MINUTES.md` ships immediately, with all owner-blocked prose marked. Closes item 36 placeholder sub-question. |
| **Q5** — Improvement 1 "talk to a human" CTA | **V1.1** (defer). | Decision E in *Resolved 2026-04-23 (SaaS-framing follow-on Q&A — 9 decisions)* now has a release window. Improvement 1 ships without the CTA; V1.1 adds it. Closes the deferred sub-question. |
| **Q6** — Improvement 4 brand-category replacement string | **AI Architecture Review Board** (the leading repositioning candidate). | Improvement 4 implementation PR ships the brand-neutral content seam (`brand-category.ts`) defaulted to today's "AI Architecture Intelligence"; the V1 rebrand workstream (Q7) flips it to "AI Architecture Review Board". Closes pending question **39** name sub-decision. |
| **Q7** — Improvement 4 rebrand workstream schedule | **V1** — schedule it now. | The rebrand workstream (marketing site `/why` + `/pricing` + `/get-started`, sponsor brief, competitive landscape, per-vertical briefs, Trust Center, in-product copy) is in scope for V1. Closes pending question **39** schedule sub-decision. |
| **Q8** — Improvement 7 tour-step copy | **Assistant drafts a first cut, marked "pending owner approval"**. | Improvement 7 implementation PR ships five placeholder strings clearly marked `<<tour copy — pending owner approval>>`. Owner replaces before any tenant sees the tour as primary nav. |
| **Q9** — Improvement 7 tour audience | **Opt-in via "Show me around" button — never auto-launches**. | Improvement 7 implementation PR adds the button to the operator-shell home; no first-sign-in interception, no auto-launch on tenant creation. Lower-friction safety posture. |
| **Q10** — Improvement 9 (pen-test publication, Aeronova summary) | **V1.1 deferred**. | Pen-test summary publication moves out of V1 actionable. Existing items **2**, **5**, **20** stay open but are now **release-window-pinned to V1.1** rather than indefinitely open. New `docs/library/V1_DEFERRED.md` § 6c row added; `docs/library/V1_SCOPE.md` § 3 gains a new "Out of scope for V1" row pointing at V1.1. Trust Center **`docs/trust-center.md`** "Recent assurance activity" wording does **not** update in V1; it updates when V1.1 publishes per Q11. |
| **Q11** — Improvement 9 Trust Center "Recent assurance activity" specificity (when V1.1 publishes) | **May name finding categories** (e.g. authn surface, RAG threat surface). | Future V1.1 PR — when the Aeronova redacted summary lands, the Trust Center row may name specific category headings rather than the standing "redacted summary available under NDA" wording. Owner accepted the trade-off that category headings are public; specific findings remain NDA-gated. |
| **Q12** — Improvement 10 PGP keypair status | **Generate later** (deferred). | The recipe at `docs/security/PGP_KEY_GENERATION_RECIPE.md` stays in place; no public key drops in V1. Items **3**, **10**, **21** stay open but are now **release-window-pinned to V1.1**. |
| **Q13** — Improvement 10 PGP UID | **V1.1 deferred** — depends on `archlucid.com` domain acquisition. | UID is gated on domain ownership confirmation. Default proposal `ArchLucid Security <security@archlucid.com>` is the V1.1 starting point; if `archlucid.com` is never acquired, owner provides the alternate UID at V1.1 planning. |
| **Q14** — Improvement 10 PGP publication timing (when key does drop in V1.1) | **Same-day single PR**: key + `SECURITY.md` + marketing `/security` page. | Future V1.1 PR — single change set drops the key block at `archlucid-ui/public/.well-known/pgp-key.txt`, references it from `SECURITY.md`, and updates the marketing `/security` page in the same PR. CI guard turns green automatically. |
| **Q15** — Cross-cutting Azure OpenAI ~$50/month budget for golden-cohort real-LLM gate | **Approved** — provision and wire the gate. | **Items 15 and 25 — budget portion fully Resolved 2026-04-24 (Improvement 11 shipped)**: dual-band kill-switch in place at warn 80% / kill 95% of cap (Q15-conditional rule); Workbook Terraform module + nightly issue auto-creation + merge-blocking CI guard all merged. Dedicated Azure OpenAI deployment provisioning + secret injection on the protected GitHub Environment **remain owner-only operational tasks**; flipping `cohort-real-llm-gate` from optional to required is a separate owner-only one-line PR after the deployment exists (one-line diff documented in [`docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md`](runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md) § 2). |
| **Q16** — Cross-cutting SOC 2 Type I ARR revisit threshold | **Keep $1M directional**. | Trust Center wording at item **6** stays at the existing "approximately $1M in ARR" line. No change. |
| **Q17** — Cross-cutting Marketplace + Stripe live cutover dates | **No month pin** — keep at "V1.1 release window". | Already V1.1-deferred per *Resolved 2026-04-23 (Commerce un-hold scope)*; this confirms no month pin within Q2 2026. Item **22** stays open at the V1.1-window granularity. |

**Knock-on V1 scope changes:**

- **Improvement 9 (pen-test publication, Aeronova summary)** moves from V1 actionable to **V1.1**. Trust Center "Recent assurance activity" wording (per Q11) ships with the row when V1.1 lands.
- **Improvement 10 (PGP key drop)** moves from V1 actionable to **V1.1**, gated on `archlucid.com` domain acquisition + `security@archlucid.com` mailbox provisioning.
- **Improvements 1, 4, 7** all remain **V1 actionable** per the answers above.
- **Q15 approval** unblocks the golden-cohort real-LLM regression gate (PENDING_QUESTIONS items 15 and 25 — both partially Resolved for the budget portion; Azure OpenAI deployment provisioning + secret injection still owner-only).
- Two **new V1 actionable improvements** promoted to maintain the ≥ 8 Cursor-prompt floor:
  - **Improvement 11** — Azure OpenAI cost-and-latency dashboard for the golden-cohort gate (natural pair to Q15 approval — measures the new spend, enforces the kill-switch).
  - **Improvement 12** — first-tenant onboarding telemetry funnel (instruments the opt-in tour from Q9 so the 30-minute success rate can be measured before any marketing claim).

**Score impact:** the prior quick in-conversation 65.34% estimate is superseded by the cell-by-cell tally in [`docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md`](../QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md), which lands at **73.20%** under the post-Q&A V1 contract. The headline jump versus the in-conversation estimate is the result of scoring each quality consistently against the post-Q&A contract (V1.1-deferred milestones excluded as the operating rule requires) rather than as deltas from the more pessimistic earlier pass. See § 0 *Headline*, § 0.1 *Sixth-pass deferral re-score addendum*, and § 1.31 *Bucket totals (sanity check)* in that file for the per-quality arithmetic.

**Rules:**

- For **Q10** and **Q12 / Q13**, the pen-test publication and PGP key drop are **release-window-pinned to V1.1**, not indefinitely deferred. They join the existing reference-customer (V1.1) and commerce-un-hold (V1.1) rows in [`V1_DEFERRED.md`](library/V1_DEFERRED.md) § 6b / new § 6c.
- For **Q15**, the budget approval is **conditional on the kill-switch being shipped at warn = 80% / kill = 95% of cap** (the Q15-conditional rule). If the kill-switch is bypassed (e.g., a future change to the nightly workflow) or if either ratio is weakened away from 0.80 / 0.95, real-LLM execution must revert to disabled until the kill-switch is restored. **Ratios are pinned by the merge-blocking guard at `scripts/ci/assert_golden_cohort_kill_switch_present.py`** — a PR that weakens either ratio cannot land. Operator runbook: [`docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md`](runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md).
- For **Q7 (rebrand schedule = V1)**, the rebrand workstream consumes **separate session(s)** — it touches marketing site routes + sponsor brief + competitive landscape + per-vertical briefs + Trust Center + in-product copy. Sequence the workstream after Improvement 4's content seam ships so the seam can carry the new value as a one-line flip when each surface is rewritten.
- For **Q9 (opt-in only tour)**, the "Show me around" button must not be promoted to a primary nav slot or auto-launch on first sign-in. If telemetry from Improvement 12 later shows < 5% engagement and a future ADR proposes a cold-start opener, the change requires its own owner decision.

---

## Resolved 2026-04-23 (assessment §4 items 29, 31–38 + two cross-cutting — 11 decisions)

Owner decision (2026-04-23, fifth pass — same day as the Jira, ServiceNow + Slack, reference-customer, and commerce un-hold scope resolutions above): the eleven owner-only questions listed in [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](quality/2026-04-21-assessments/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) §4 (items 29, 31–38) plus two cross-cutting product items were resolved in a single owner Q&A. Items 30 (Marketplace publisher legal entity name on customer statements) is **deferred to the V1.1 commerce un-hold** and is not in this batch.

| Pending-questions item | Decision | Affects |
|------------------------|----------|---------|
| **29 — `BeforeAfterDeltaPanel` placement** (Improvement 3) | **All three placements:** top of `/runs` list page, sidebar widget, AND inline on each `/runs/[runId]` page. Assistant picks reasonable defaults for each (uses the same component instance gated by route context). | Improvement 3 implementation PR — single component (`BeforeAfterDeltaPanel.tsx`) wired into three routes; Vitest spec covers all three render contexts. |
| **31 — `/why` comparison artefact format** (Improvement 5) | **Both surfaces:** inline page section visible without download AND a "Download PDF" button. Same artefact, two surfaces (SEO-friendly inline; procurement-pack-friendly download). | Improvement 5 implementation PR — `/why` page renders the comparison block server-side; PDF endpoint emits the same content via existing `Pdf.Renderer` infrastructure. |
| **32 — Microsoft Teams trigger set** (Improvement 7) | **All five triggers:** `run.committed`, `governance.approval.requested`, `alert.raised`, `compliance.drift.escalated`, `seat.reservation.released`. Single Logic Apps workflow listens on the Service Bus topic and dispatches per trigger type to the per-tenant Adaptive Card template. | Improvement 7 implementation PR — `INTEGRATION_CATALOG.md` Microsoft Teams row updated to list five triggers; `MICROSOFT_TEAMS_NOTIFICATIONS.md` updated; Logic Apps Terraform module covers all five event types in its Service Bus subscription rule. |
| **33 — Golden-cohort baseline lock** (Improvement 8) | **Lock SHAs today** from a single approved simulator run. Assistant runs `archlucid golden-cohort lock-baseline --write` once after shipping the CLI; commits the resulting `cohort.json` as the baseline. Future cohort scenario expansions go through normal review. | Improvement 8 implementation PR — after the CLI and contract test land, the assistant runs the lock-baseline command in one PR, captures the resulting SHA-256 fields in `tests/golden-cohort/cohort.json`, and flips `ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCKED=true` in the nightly workflow. |
| **34 — Stale `IMPROVEMENTS_COMPLETE.md` at repo root** | **Delete it.** Git history preserves it for anyone who needs to find it later. **2026-04-23 verification:** file was already absent at repo root (verified via `Test-Path`); removed in a prior cleanup. No deletion PR needed. | No-op — stale file already gone. Assessment §1.23 and §2.1 entry 7 updated to mark the finding resolved. |
| **35 — Board-pack PDF cover narrative** (Improvement 9) | **Assistant drafts placeholder narrative** from `EXECUTIVE_SPONSOR_BRIEF.md` boilerplate; owner approves before any external use. Cover string in V1 ships as the literal placeholder text `<<sponsor cover narrative — owner approval before external use>>`. | Improvement 9 implementation PR — board-pack PDF endpoint never embeds non-placeholder cover prose without an owner-approved string in the request payload. Updated in [`docs/archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md) Prompt 1 (replacement). |
| **36 — Monthly exec-digest cadence default for new tenants** (Improvement 9) | **Opt-out** — every newly provisioned tenant receives a Monthly executive digest by default; existing tenants stay 'Weekly' (no retroactive cadence change). Tenants can disable in `/settings/exec-digest`. | Improvement 9 implementation PR — migration 104 uses a **three-step backfill shape** (add column with backfill default `'Weekly'`, drop the backfill constraint, add a forward-looking new-row default `'Monthly'`) so SQL Server's `ADD … NOT NULL DEFAULT` behaviour does not silently flip every existing tenant. Updated in [`docs/archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md) Prompt 1 (replacement) step 1. **Note:** owner accepted the trade-off that opt-out monthly emails to new tenants (paying B2B SaaS, not consumer marketing) is acceptable; if any future tenant disputes, the unsubscribe link in `/settings/exec-digest` is the documented remediation. |
| **37 — Governance dry-run audit metadata** (Improvement 10) | **Capture override count AND payload** (full forensic visibility — owner accepted the trade-off that anyone with `ReadAuditAuthority` in the same tenant can see proposed policy values). Payload MUST pass through the existing `LlmPromptRedaction`-style PII redaction pipeline before serialisation. **Shipped 2026-04-24** as part of [`Improvement 5 — governance dry-run / what-if mode`](../CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md) (see the 2026-04-24 row in [`CHANGELOG.md`](../CHANGELOG.md)). The new `AuditEventTypes.GovernanceDryRunRequested` row writes `proposedThresholdsRedacted` (the proposed-thresholds JSON after `IPromptRedactor`) plus `evaluatedRunIds[]` and `deltaCounts`; `ArchLucid.Api.Tests/PolicyPackDryRunIntegrationTests.cs` asserts the persisted `DataJson` contains `[REDACTED]` and **not** the raw email / SSN values. | Improvement 10 implementation PR — `GovernanceDryRunRequested` audit-event payload schema includes `overridePayloadJson`; redaction pipeline mandatory; integration test asserts the redaction-marker pattern. Updated in [`docs/archive/quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](quality/2026-04-21-assessments/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md) Prompt 4 (replacement). |
| **38 — Governance dry-run pagination cap** (Improvement 10) | **20-default / 100-max** (matches assistant's safe default). **Shipped 2026-04-24** — `IPolicyPackDryRunService.DefaultPageSize = 20`, `IPolicyPackDryRunService.MaxPageSize = 100`; `archlucid-ui/src/types/policy-pack-dry-run.ts` exports `POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE` and `POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE`; `GovernanceDryRunModal.test.tsx` asserts the default is exactly 20 so a silent regression fails the spec. | Improvement 10 implementation PR — modal default page size = 20, server-side cap = 100; Vitest assertion on default. |
| **Cross-cutting — Trust Center "Recent assurance activity" timing** | **Update immediately on assessor delivery** of the Aeronova pen test redacted summary. Customer comms catch up after; the public trust signal does not wait for marketing draft cycles. | When the Aeronova summary lands (Improvement 6 owner-side), the [`docs/trust-center.md`](trust-center.md) "Recent assurance activity" row is updated in the same PR that publishes the redacted summary; no comms draft gate. |
| **Cross-cutting — "AI Architecture Intelligence" category name** | **Open to repositioning** toward "AI Architecture Review Board" (more buyer-recognisable). Assistant proposes a structured rebrand workstream covering: marketing site `/why` + `/pricing` + `/get-started`, [`docs/EXECUTIVE_SPONSOR_BRIEF.md`](EXECUTIVE_SPONSOR_BRIEF.md), [`docs/go-to-market/COMPETITIVE_LANDSCAPE.md`](go-to-market/COMPETITIVE_LANDSCAPE.md), the per-vertical brief docs under [`docs/go-to-market/`](go-to-market/) (industry briefs live alongside other GTM artifacts), and Trust Center. **Owner approval needed** before the rebrand workstream is scheduled — surfaces as new pending question 39 below. | New pending question 39 added below; current product copy unchanged until owner approves the rebrand timing. |

**Rules:**

- For **q36 (opt-out monthly digest)**, do **not** ship a single `ADD COLUMN … NOT NULL DEFAULT 'Monthly'` migration. The three-step shape in the Cursor prompt is mandatory because SQL Server backfills existing rows with the new column's default — a single-statement migration would silently switch every existing tenant to Monthly, violating the "no retroactive cadence change" boundary.
- For **q37 (capture override payload)**, payload capture is **conditional on the redaction pipeline being applied**. If the redaction pipeline is bypassed (e.g., a future change to the `GovernanceDryRunRequested` write path), payload capture must be turned off until redaction is restored.
- For **q11 (AI category repositioning)**, a separate owner approval (pending question 39) is required before the rebrand workstream begins; this resolution only opens the door, it does not schedule the rename.

---

