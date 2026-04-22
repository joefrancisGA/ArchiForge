> **Scope:** Product and operations decisions the repo cannot resolve alone — consolidated pending list (supersedes scattered assessment §9 lists).

# Pending questions (product and operations)

**Last updated:** 2026-04-22 (assessment owner Q&A — 16 decisions — *Resolved 2026-04-22 (assessment owner Q&A — 16 decisions)* table below; covers items **6**, **9**, **10**, **12**, **14**, **15 / 25**, **20**, **22**, **26**, **28**, **34**, **35d**, **35e**, plus four free-text answers on items **8**, **9**).
Prior: 2026-04-22 (owner Q&A on items **35a–c** + **35f**) — *Resolved 2026-04-22 (35c + 35f — ADR 0030)* + *Resolved 2026-04-22 (ADR 0030 owner sub-decisions — 35a + 35b)*.
Prior: 2026-04-21 (interactive owner Q&A session + same-day 5-decision follow-up + bundled DDL change set + Phase 3 PR A re-scope finding — see *Resolved 2026-04-21 (owner Q&A — 19 decisions)*, *Resolved 2026-04-21 (follow-up Q&A — 5 decisions)*, *Resolved 2026-04-21 (bundled DDL change set — Teams + RLS)*, and *Resolved 2026-04-21 (Phase 3 PR A re-scoped — ADR 0030)* tables below).

Single place to track **decisions only a human owner** can make. When you ask what is still open, start here. Items marked **Resolved** stay for audit trail; remove them only when you intentionally shrink the file.

---

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
| **19 — first PLG row owner** | **Owner solo.** Owner watches the trial-to-paid event, validates the case study draft with the customer, and flips the row in `docs/go-to-market/reference-customers/README.md` from `Customer review` to `Published`. | Item 19 closed. |

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

14. **Cross-tenant pattern library — Resolved 2026-04-22.** Implementing **ADR 0031** is drafted for owner sign-off: [`docs/adr/0031-cross-tenant-pattern-library.md`](adr/0031-cross-tenant-pattern-library.md). **Status remains Proposed** until the owner flips to **Accepted**; no implementation merge until then.

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

34. **Production Simmy / fault-injection game day** — The `simmy-chaos-scheduled.yml` workflow is **staging-only** for `environment` and rejects a non-empty optional workflow_dispatch **`production`** string (fail-fast guard). **Default remains staging-only execution.** Owner must approve any real production chaos (customer notification, SLO ownership, blast radius, rollback) before any future widening of that gate. See [`docs/runbooks/GAME_DAY_CHAOS_QUARTERLY.md`](runbooks/GAME_DAY_CHAOS_QUARTERLY.md) and the calendar in [`docs/quality/game-day-log/README.md`](quality/game-day-log/README.md).

35. **Coordinator → Authority pipeline unification — sequenced multi-PR plan ([ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md))** — Phase 3 PR A's grounding read (2026-04-21) found three structural mismatches that block a single-session deletion. The ADR splits the work into PRs **A0 → A4**; the items below are the **per-sub-PR owner decisions** that have to land before the corresponding sub-PR can merge. Each is **owner-only** — the assistant cannot answer them from repository state.

    - **a. PR A0 — Authority engine projection shape. (Resolved 2026-04-22 — see `Resolved 2026-04-22 (ADR 0030 owner sub-decisions — 35a + 35b)` table above.)** Owner picked **(ii) new mapper class** (`AuthorityCommitProjectionBuilder`) consumed by `RunCommitOrchestratorFacade` — Authority engine stays pure. Plus four field-level sub-decisions resolved the same day: 35a.1 = `sibling-row` for `SystemName`; 35a.2 = `empty-with-guard` for typed `Services` + `Datastores` (populated later in new PR A0.5); 35a.3 = `empty-with-guard` for `Relationships` (deferred until PR A2 planning); 35a.4 = `yes` to the JSON allow-list + CI guard mechanism. **PR A0 drafting unblocked.**

    - **b. PR A1 — `IGoldenManifestRepository` overload return shape. (Resolved 2026-04-22 — see `Resolved 2026-04-22 (ADR 0030 owner sub-decisions — 35a + 35b)` table above.)** Owner expanded the original `Task` vs `Task<Guid>` framing to a third option and chose it: **`Task<Decisioning.Models.GoldenManifest>`** (return the produced Authority-shape manifest so the caller keeps idempotency-key reasoning). **PR A1 drafting unblocked.**

    - **c. PR A2 — feature-flag scope for facade target swap. (Resolved 2026-04-22 — see *Resolved 2026-04-22 (35c + 35f — ADR 0030)* table above; mechanical wiring follow-on.)** **(c.1) = (ii) global** `Coordinator:LegacyRunCommitPath` (`LegacyRunCommitPathOptions` in `ArchLucid.Core`). **(c.2) = (B)** long-term default **`false`**; **interim** shipped `appsettings` stays **`true`** until `RunCommitPathSelector` + `AuthorityDrivenArchitectureRunCommitOrchestrator` merge. Next small PR: register the selector, implement the authority orchestrator (idempotency + UoW persistence parity with the pipeline), flip default to `false`, and update test hosts.

    - **d. PR A4 — `dbo.GoldenManifestVersions` table drop — backfill / archival policy. (Resolved 2026-04-22 — see *Resolved 2026-04-22 (assessment owner Q&A — 16 decisions)* → **ADR 0030 sub-decisions (items 35d / 35e)** and [ADR 0030](adr/0030-coordinator-authority-pipeline-unification.md) § Component breakdown / PR A4 + § Owner sub-decisions row **35d**).** Owner chose **(i) hard drop** — no historical Coordinator-shape rows preserved; backfill / archival branch removed from ADR 0030. Merge-time gate is no-rollback sign-off only.

    - **e. Phase 3 PR B placeholder tracker (`docs/architecture/PHASE_3_PR_B_TODO.md`). (Resolved 2026-04-22 — see *Resolved 2026-04-22 (assessment owner Q&A — 16 decisions)* → **ADR 0030 sub-decisions (items 35d / 35e)** row **35e**, [ADR 0029](adr/0029-coordinator-strangler-acceleration-2026-05-15.md) § Lifecycle § **PR B — audit-constant retirement checklist**, and [`docs/architecture/PHASE_3_PR_B_TODO.md`](architecture/PHASE_3_PR_B_TODO.md)).** Owner chose **both**: authoritative inline checklist on ADR 0029 plus the standalone working-surface file; `scripts/ci/assert_pr_b_tracker_in_sync.py` compares them (**warn-only** until PR B has exercised the workflow).

    - **f. PR A0.5 — typed-services source for `ManifestService.ServiceType` / `RuntimePlatform`. (Resolved 2026-04-22 — see *Resolved 2026-04-22 (35c + 35f — ADR 0030)* table above.)** **(i) graph `Properties` metadata** — `GraphNode.Properties` keys `serviceType` and `runtimePlatform` (and `datastoreType` for storage-category nodes) hold enum names. `DefaultGoldenManifestBuilder` populates `Decisioning.Models.GoldenManifest.Services` / `Datastores` from `TopologyResource` nodes; `AuthorityCommitProjectionBuilder` maps them onto the coordinator-shaped `Contracts.Manifest.GoldenManifest`. **PR A0.5 implementation in progress in the same change set as 35c.**

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
