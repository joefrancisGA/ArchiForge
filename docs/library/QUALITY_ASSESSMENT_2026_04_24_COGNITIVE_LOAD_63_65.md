> **Scope:** Independent V1 cognitive-load quality review (2026-04-24); deferred V1.1/V2 items excluded — not a substitute for user research or UX sign-off.

# ArchLucid Cognitive Load Quality Assessment — 63.65%

**Date:** 2026-04-24
**Type:** Independent assessment (single-dimension deep-dive: Cognitive Load)
**Definition:** Cognitive Load = how much mental effort the system imposes while a target user completes tasks effectively.

---

## Scoring methodology

Cognitive load is decomposed into seven sub-dimensions, each weighted by its contribution to the user's moment-to-moment mental effort. Areas needing the most improvement are presented first.

| # | Sub-dimension | Weight | Score | Weighted |
|---|---|---|---|---|
| 1 | Terminology burden | 15% | 55 | 8.25 |
| 2 | Information architecture clarity | 25% | 59 | 14.75 |
| 3 | Conceptual model coherence | 10% | 62 | 6.20 |
| 4 | Configuration complexity | 5% | 52 | 2.60 |
| 5 | Navigation efficiency | 15% | 67 | 10.05 |
| 6 | Progressive disclosure effectiveness | 20% | 72 | 14.40 |
| 7 | Error recovery guidance | 10% | 74 | 7.40 |
| | **Weighted total** | **100%** | | **63.65** |

---

## 1. Terminology burden — 55 / 100 (weight: 15%, contribution: 8.25)

**What this measures:** How much new vocabulary a user must internalize before and during productive use.

### Strengths

- A `GLOSSARY.md` exists with ~20 defined terms and cross-references to deeper docs.
- Core Pilot language is deliberately narrow: "request", "run", "commit", "manifest", "artifacts" are the primary vocabulary.
- CLI commands use recognizable verbs (`run`, `commit`, `try`, `health`, `doctor`).

### Weaknesses

- **"Authority" is severely overloaded.** It appears as: authority pipeline, authority run, authority chain, authority run orchestrator, required authority (nav), `ExecuteAuthority` / `ReadAuthority` / `AdminAuthority` (policies), authority-shaped events, authority commit projection. An operator encountering "authority" in the sidebar, the API error messages, and the pipeline timeline must infer which sense is meant from context. This is the single largest terminology tax in the product.
- **"Golden manifest"** is opaque to newcomers. It sounds aspirational rather than descriptive. "Committed architecture manifest" communicates the same thing with zero learning cost.
- **"Effective governance"** is a compound abstraction (merged policy packs applied to a scope). Users encountering "Governance resolution" in the sidebar cannot infer what it does without reading docs.
- **"Findings"** reads generically. An evaluator unfamiliar with the product cannot distinguish ArchLucid "findings" from test results, SAST findings, or audit findings without additional context.
- **Glossary is documentation-only.** It is not surfaced contextually in the operator UI — no tooltips, no first-use definitions, no inline help. Users must leave the product to learn vocabulary.
- **Scope triple (tenant / workspace / project)** is three levels of hierarchy that every API call carries. Users must understand and correctly set all three even when they have a single project — adding conceptual overhead for the simple case.

### Trade-offs

Renaming terms like "golden manifest" and "authority" would reduce learning cost but imposes a large migration across API contracts, code, docs, and tests. The vocabulary was chosen for internal precision; the cost is external legibility.

---

## 2. Information architecture clarity — 59 / 100 (weight: 25%, contribution: 14.75)

**What this measures:** How well the documentation and in-product information is organized and discoverable.

### Strengths

- The system recognizes the information architecture problem explicitly and has invested in it: `START_HERE.md`, `READ_THIS_FIRST.md`, `NAVIGATOR.md`, `FIRST_5_DOCS.md`, `BUYER_FIRST_30_MINUTES.md`, and the audience split table.
- The `docs/library/` consolidation (2026-04-23) moved ~150 files out of `docs/` root, reducing visual clutter for evaluators.
- The `NAVIGATOR.md` "I want to..." table maps 15 common tasks to a single best document.
- The five-document contributor spine provides a clear reading order with estimated time.
- Archive folder prevents stale docs from polluting active navigation.

### Weaknesses

- **579+ markdown files** remain in the repository. Even with library consolidation, this is a substantial documentation corpus for a pre-GA product. No evaluator will absorb it; the question is whether the entry points reliably shield them from the sprawl. Currently they do not always succeed.
- **Multiple competing "start here" entry points.** A new arrival may encounter `START_HERE.md`, `READ_THIS_FIRST.md`, `NAVIGATOR.md`, `FIRST_5_DOCS.md`, `FIRST_FIVE_DOCS.md`, `BUYER_FIRST_30_MINUTES.md`, and `README.md`. Each has slightly different framing and audience; the redirect stubs (`FIRST_5_DOCS`, `FIRST_FIVE_DOCS`) add noise. A user who finds `NAVIGATOR.md` first may never see `START_HERE.md`, and vice versa. The effort to create multiple entry documents has paradoxically increased the "which one do I read?" question.
- **Operator documentation is fragmented across 7+ files:** `OPERATOR_ATLAS.md`, `OPERATOR_QUICKSTART.md`, `OPERATOR_DECISION_GUIDE.md`, `operator-shell.md`, `PILOT_GUIDE.md` (redirect), `CORE_PILOT.md`, `FIRST_RUN_WIZARD.md`, `FIRST_RUN_WALKTHROUGH.md`, `SECOND_RUN.md`. Each covers a slice of the operator journey. A new operator cannot tell from titles alone which file to open, and the redirect stubs (e.g., `PILOT_GUIDE.md` → `OPERATOR_QUICKSTART.md`) waste a click.
- **Doc-to-doc linking creates rabbit holes.** Most docs contain 10–20 relative links to other docs. A reader following links can easily reach 6–8 documents deep without reaching an answer. The Navigator partially addresses this but covers only 15 tasks.
- **Buyer first-30-minutes has placeholder copy** (`<<placeholder copy — replace before external use>>`). An evaluator who reaches this doc via GitHub encounters unfinished content, which undermines the entry point's purpose.

### Trade-offs

Documentation depth is a genuine asset for governance-minded buyers (SOC 2 readiness, audit evidence, procurement packs). The cost is that the same depth creates a maze for first-time users. The tension is real and cannot be fully resolved without accepting that some docs exist for due-diligence rather than usability.

---

## 3. Conceptual model coherence — 62 / 100 (weight: 10%, contribution: 6.20)

**What this measures:** How easy it is to build and maintain an accurate mental model of how the system works.

### Strengths

- **Core Pilot four-step model is excellent.** Request → Execute → Commit → Review is clean, memorable, and well-documented. The anti-creep rule ("if a feature does not help an operator complete these four steps more clearly or more quickly, it should not become part of the default mental model") is a strong design principle.
- **Three-layer model (Pilot → Operate · analysis → Operate · governance)** maps well to maturity stages. The Operator Decision Guide's "stay in Core Pilot unless you have a reason" is strong guidance.
- **Pipeline stages** (Context → Graph → Findings → Manifest) in the wizard tracker are clearly named and linearly ordered.
- **The buyer column (SaaS, no install) vs contributor column (repo, toolchain)** audience split is genuinely clarifying.

### Weaknesses

- **Dual pipeline heritage creates confusion.** The coordinator/authority unification (ADR 0021, ADR 0030) means the codebase still carries references to both pipeline families. Users encountering "coordinator run" alongside "authority run" must understand a strangler migration that is irrelevant to their task. Deprecation headers (`Sunset: 2026-05-15`) are visible in API responses.
- **"Two nav groups under Operate but one buyer layer" creates a naming mismatch.** Marketing says "Pilot" and "Operate." The UI says "Pilot", "Operate · analysis", and "Operate · governance." The buyer sees two layers; the operator sees three groups. This small gap forces mental reconciliation.
- **Governance has three separate surfaces:** Dashboard (cross-run view), Resolution (effective policy read), and Workflow (mutations). The distinction between "what is my effective governance" and "what actions can I take on governance" is architecturally clean but not obvious from nav labels. An operator looking for "governance" must decide which of three items to click.
- **Alert surface has six nav items** (Alerts, Alert rules, Alert routing, Composite rules, Alert simulation, Alert tuning). For a V1 product, this granularity forces operators to understand the alert model's internal decomposition before they can configure basic alerting. The distinction between "Alert rules" and "Composite rules" is not self-evident.

### Trade-offs

The system's internal architecture is well-decomposed (clean interfaces, clear pipeline stages). The issue is that this internal decomposition leaks into the user-facing surface more than it should. Collapsing nav items would reduce cognitive load but may also reduce discoverability for power users.

---

## 4. Configuration complexity — 52 / 100 (weight: 5%, contribution: 2.60)

**What this measures:** How much cognitive overhead configuration imposes on operators.

### Strengths

- Startup configuration validation (`ArchLucidConfigurationRules.cs`) catches misconfigurations before they cause runtime failures.
- `archlucid doctor` / `check` provides a CLI diagnostic that validates local setup.
- Default values are reasonable for development (DevelopmentBypass auth, localhost URLs, simulator mode).
- Key Vault reference pattern is documented with a sample file.

### Weaknesses

- **Five configuration surfaces:** `appsettings.json`, environment variables, `archlucid.json` (CLI project), user-secrets, and Key Vault references. Each has different precedence rules and different applicable contexts (API vs CLI vs Docker vs Azure). No single reference enumerates all configuration keys across all surfaces.
- **Three authentication modes** (DevelopmentBypass, JWT bearer, API key) with different setup requirements. Switching from dev bypass to JWT requires understanding Entra ID app registrations, token claims, and policy-to-role mapping. The conceptual gap between "it works locally" and "it works with real auth" is wide.
- **Three-level scope** (tenant / workspace / project) on every API call adds cognitive overhead for the common case where a pilot has one tenant, one workspace, and one project. The system does not auto-infer scope from the auth context.
- **Docker Compose overlays** (`docker-compose.yml`, `docker-compose.demo.yml`, `docker-compose.real-aoai.yml`) require understanding which overlays apply in which scenario. `archlucid try` and `pilot up` abstract this well; manual setup does not.
- **Hosting roles** (Api / Worker / Combined) add another configuration axis. The distinction matters for production but forces new users to understand a deployment topology question during initial setup.

### Trade-offs

Configuration complexity is partly inherent to enterprise SaaS (multi-tenant, multi-auth, multi-environment). ArchLucid has moved complexity into reasonable defaults for the local case, but the gap between "demo on my laptop" and "real deployment" is steep. A configuration wizard or interactive setup command would narrow this gap.

---

## 5. Navigation efficiency — 67 / 100 (weight: 15%, contribution: 10.05)

**What this measures:** How quickly and reliably users can reach the right surface for their current task.

### Strengths

- **Keyboard shortcuts** on 9 high-frequency destinations (alt+h, alt+n, alt+r, alt+c, alt+p, alt+y, alt+a, alt+l, alt+g). This is above average for enterprise SaaS.
- **Ctrl+K command palette** provides search-based navigation.
- **Sidebar nav groups** with labels and captions give structural context.
- **Breadcrumb links** on key pages (Home → Runs → Compare → Graph).
- **Core Pilot checklist** on Home page provides task-oriented navigation (do this, then this, then this).
- **"New run" wizard** links directly to next steps after completion (Compare, Replay, Graph, Run detail).

### Weaknesses

- **37 total nav links** across three groups. Even with progressive disclosure (essential → extended → advanced), a user who clicks "Show more links" and "Show advanced links" sees all 37 at once. This is a high link count for a single sidebar.
- **Six alert-related nav items** (Alerts, Alert rules, Alert routing, Composite rules, Alert simulation, Alert tuning) in a single group. An operator who wants "I need to set up alerting" must mentally parse six destinations to find the right starting point.
- **Overlapping Pilot links:** Home, Onboarding, and First Session serve related but slightly different purposes. A new user clicking through all three encounters overlapping content before understanding the distinction.
- **"Show more links" is not descriptive.** The label does not indicate what will appear. "Show analysis tools" or "Show investigation features" would reduce the click-to-discover-what-I-got gap.
- **No nav search filtering.** The Ctrl+K palette provides search, but the sidebar itself has no filter/search. With 37 items, a sidebar filter would be useful.

### Trade-offs

A rich nav is a signal of product depth. Collapsing items into hub pages or tabs would reduce sidebar density but would add one more click to reach specific destinations. For power users who visit alert rules daily, the direct link is valuable. The question is whether the cognitive load on new users outweighs the efficiency gain for repeat users. For a V1 product without a large power-user base, optimizing for new users is the right bet.

---

## 6. Progressive disclosure effectiveness — 72 / 100 (weight: 20%, contribution: 14.40)

**What this measures:** How well the system reveals complexity gradually, matching the user's current need and expertise.

### Strengths

- **Three-tier nav model (essential → extended → advanced)** is structurally sound. Essential links are always visible; extended and advanced require explicit opt-in.
- **Role-aware shaping** hides nav items the current user's permissions cannot satisfy. This prevents exposure to surfaces the user cannot use.
- **Core Pilot boundary** is well-enforced: the four-step model is the default, and all deeper features are explicitly labeled as optional.
- **LayerHeader component** adds contextual guidance ("what question this answers") at the top of Operate pages. This prevents a user from arriving at an advanced page without orientation.
- **Inline cues** on mutation surfaces (soft-disabled controls for read-only users, "inspect before configure" language) signal capability without hiding content.
- **`archlucid try` single-command experience** collapses six steps (pilot up, seed, request, execute, commit, report) into one. This is a genuinely excellent disclosure pattern for first contact.
- **`archlucid second-run` with TOML/JSON template** provides a minimal-friction bridge from "demo data" to "my own data."
- **Operator Decision Guide** explicitly tells users when to move between layers — not just what each layer contains, but the trigger for moving.

### Weaknesses

- **"Show more links" / "Show advanced links" labels are not self-describing.** A user does not know what will appear until they click. More descriptive labels ("Show analysis tools" / "Show governance and audit controls") would set expectations before the click.
- **The opt-in tour is not yet delivering finished copy.** Tour steps are wrapped in `<TourStepPendingApproval>` markers and will not display until the owner approves all five copies (Improvement 5 in `PENDING_QUESTIONS.md`). This means the in-product progressive disclosure for first-session orientation is incomplete. The system has the mechanism but not the content.
- **Operate · analysis group shows 12 items after disclosure.** Even with tier filtering, an operator who clicks "Show more" encounters Ask, Search, Advisory, Recommendation learning, Pilot feedback, Planning, Evolution candidates, Schedules, Digests, Subscriptions, Exec digest, and Tenant cost. Several of these (Digests, Subscriptions, Exec digest) are closely related but appear as separate items, inflating the perceived surface.
- **Extended vs advanced tier distinction is not explained in the UI.** A user seeing "extended" links after one click and "advanced" links after another has no mental model for what "advanced" means relative to "extended." The labels are structurally useful but semantically empty.

### Trade-offs

The progressive disclosure model is fundamentally sound. Its gaps are in labeling (what does "more" mean?) and completeness (tour copy pending). The mechanism is above average; the content layer needs finishing.

---

## 7. Error recovery guidance — 74 / 100 (weight: 10%, contribution: 7.40)

**What this measures:** When something goes wrong, how clearly the system guides the user toward resolution.

### Strengths

- **RFC 9457 Problem Details** on all API error responses with structured `title`, `detail`, `type`, and `extensions`.
- **`supportHint` extension** on common errors provides a concrete "what to try next" without exposing internals.
- **`correlationId`** in both response headers and JSON body enables log correlation without requiring the user to understand distributed tracing.
- **`OperatorTryNext` component** in the UI provides "Try next:" guidance after failures — not just "something went wrong" but "here is one sensible next action."
- **Distinct empty / loading / error / malformed states** (`OperatorEmptyState`, `OperatorLoadingNotice`, `OperatorApiProblem`, `OperatorMalformedCallout`) prevent the common SaaS failure where "no data" and "broken request" look identical.
- **CLI exit codes** are well-defined (0 = success, 1 = usage, 2 = config, 3 = unreachable, 4 = operation failed) and the CLI prints "Next:" lines on stderr with suggested remediation.
- **Troubleshooting quick matrix** maps symptoms to likely causes with specific remediation steps.
- **`archlucid doctor` / `support-bundle`** provide structured diagnostics without requiring the user to manually collect state.

### Weaknesses

- **Not all API errors carry `supportHint`.** Less common error paths may return Problem Details with `detail` but no actionable next step.
- **Troubleshooting doc covers ~12 symptoms.** A production system will encounter failures not in the matrix. The doc is a good start but not comprehensive for day-two operations.
- **Error copy in the UI is sometimes generic.** `OperatorApiProblem` renders the Problem Details title and detail, but when the API returns a terse `detail` string, the UI has no fallback guidance of its own.
- **Circuit breaker state** (Open / HalfOpen / Closed) is surfaced in health JSON but not in the operator UI. An operator whose Azure OpenAI calls are failing must `GET /health` via curl to see breaker state rather than seeing it in the UI.

### Trade-offs

Error recovery guidance is one of the stronger areas of the product. The RFC 9457 adoption, structured exit codes, and Try Next pattern are above median for enterprise SaaS. The gaps are in coverage (not all errors) and surface (some diagnostics require CLI/API access when the UI would be more natural).

---

## Deferred-scope acknowledgment

The following items are explicitly deferred to V1.1 or V2 per `V1_DEFERRED.md` and owner decisions in `PENDING_QUESTIONS.md`, and are **not scored against V1 cognitive load readiness:**

- ITSM connectors (Jira, ServiceNow, Confluence) → V1.1
- Slack connector → V2
- Commerce un-hold (Stripe live keys, Marketplace publication) → V1.1
- Pen-test summary publication → V1.1
- PGP key drop → V1.1
- Reference-customer publication → V1.1
- Cross-tenant analytics → deferred indefinitely
- Product learning deterministic theme-derivation → deferred

---

## Top ten improvements (eight actionable, two DEFERRED — ordered by expected impact on weighted score)

### Improvement 1 — Consolidate alert navigation into a tabbed hub page

**Area:** Navigation efficiency (15%) + Progressive disclosure (20%)
**Current state:** Six separate nav items (Alerts, Alert rules, Alert routing, Composite rules, Alert simulation, Alert tuning) force operators to understand the alert model's internal decomposition before using basic alerting.
**Expected impact:** Reduces Operate · governance nav count from 14 to 9–10. Reduces "where do I start?" paralysis for alert setup. Improves navigation efficiency score by ~4 points and progressive disclosure score by ~2 points.
**Owner input needed:** None. This is a UI and routing change.

### Improvement 2 — Add contextual glossary tooltips in the operator UI

**Area:** Terminology burden (15%)
**Current state:** Domain-specific terms (golden manifest, findings, effective governance, authority pipeline, scope) appear throughout the UI without inline definitions. The glossary exists only as a static markdown file.
**Expected impact:** Reduces first-session vocabulary tax. Operators can learn terms in context rather than leaving the product to read docs. Improves terminology burden score by ~8 points.
**Owner input needed:** None for the mechanism. Term definitions already exist in `GLOSSARY.md`.

### Improvement 3 — Reduce documentation entry-point proliferation

**Area:** Information architecture clarity (25%)
**Current state:** Six or more "start here" variants (`START_HERE.md`, `READ_THIS_FIRST.md`, `NAVIGATOR.md`, `FIRST_5_DOCS.md`, `FIRST_FIVE_DOCS.md`, `BUYER_FIRST_30_MINUTES.md`, `README.md`) compete for first-click attention. Redirect stubs add noise.
**Expected impact:** A single unambiguous entry point (one file, two audience columns) eliminates "which start doc do I read?" friction. Improves information architecture score by ~5 points.
**Owner input needed:** None for the consolidation. Redirect stubs can be preserved for bookmark stability.

### Improvement 4 — Collapse governance navigation into a tabbed hub

**Area:** Navigation efficiency (15%) + Conceptual model coherence (10%)
**Current state:** Governance dashboard, Governance resolution, and Governance workflow are three separate nav items. The distinctions (cross-run view vs effective policy read vs mutation surface) are architecturally clean but not obvious from labels.
**Expected impact:** A single "Governance" nav item with tabs (Dashboard / Policy / Workflow) reduces Operate · governance nav count by 2 and makes the governance model easier to navigate. Improves navigation efficiency score by ~3 points.
**Owner input needed:** None. This is a UI routing change.

### Improvement 5 — Replace generic "Show more links" with descriptive disclosure labels

**Area:** Progressive disclosure effectiveness (20%)
**Current state:** "Show more links" and "Show advanced links" do not indicate what will appear. Users must click to discover.
**Expected impact:** "Show analysis and investigation tools" / "Show governance and audit controls" set expectations before the click. Small change, disproportionate clarity gain. Improves progressive disclosure score by ~3 points.
**Owner input needed:** None. This is a copy change in `nav-shell-visibility.ts` or the sidebar component.

### Improvement 6 — Merge overlapping Operate · analysis nav items

**Area:** Navigation efficiency (15%) + Progressive disclosure (20%)
**Current state:** Digests, Subscriptions, and Exec digest are three separate nav items for closely related digest functionality. Advisory and Schedules are also closely related. 12 total items in Operate · analysis is high.
**Expected impact:** Merging Digests + Subscriptions + Exec digest into a "Digests" hub (tabs: Browse / Subscriptions / Schedule) and merging Advisory + Schedules into an "Advisory" hub reduces the group from 12 to 8–9. Improves navigation efficiency score by ~3 points.
**Owner input needed:** None. This is a UI routing and layout change.

### Improvement 7 — Create unified configuration reference with `archlucid config check` CLI command

**Area:** Configuration complexity (5%) + Error recovery guidance (10%)
**Current state:** Configuration keys are spread across `appsettings.json`, environment variables, `archlucid.json`, user-secrets, and Key Vault references. No single reference enumerates all keys. `archlucid doctor` checks health but does not validate configuration completeness.
**Expected impact:** A `config check` CLI command that validates all known config sources and prints a status report per key (set / missing / default / override source) reduces debugging time for configuration issues. A companion `CONFIGURATION_REFERENCE.md` that lists every key, its source, default, and description reduces the "what config do I need?" question to one lookup. Improves configuration complexity score by ~8 points.
**Owner input needed:** None for the mechanism. All config keys are already in code.

### Improvement 8 — Add persistent layer-context indicator to all Operate routes

**Area:** Conceptual model coherence (10%) + Navigation efficiency (15%)
**Current state:** `LayerHeader` exists on some Operate pages, but it is not universal and does not indicate which layer the user is currently in relative to the three-layer model.
**Expected impact:** A small persistent strip below the header showing "Operate · analysis" or "Operate · governance" on every non-Pilot page, with a one-line "what question this layer answers" reminds users where they are in the product's conceptual model. Reduces "am I in the right place?" uncertainty. Improves conceptual model coherence score by ~4 points.
**Owner input needed:** None. The layer metadata already exists in `nav-config.ts` group IDs and `layer-guidance.ts`.

### Improvement 9 — DEFERRED: Rename opaque product terminology

**Area:** Terminology burden (15%)
**Current state:** "Golden manifest", "authority pipeline", "effective governance", and the overloaded use of "authority" impose vocabulary learning costs that clearer terms would eliminate.
**Why deferred:** Renaming core product terms affects API contracts, ~579 markdown files, code identifiers, test assertions, CLI output, and marketing copy. This is a product-level vocabulary decision requiring owner approval of replacement terms, a migration plan, and possibly an API versioning strategy. The assistant cannot execute this without owner sign-off on the replacement vocabulary.
**Recommended owner decision:** Choose replacement terms (e.g., "committed manifest" for "golden manifest", "analysis pipeline" for "authority pipeline") and approve a phased migration plan similar to the ArchiForge → ArchLucid rename.

### Improvement 10 — DEFERRED: Complete in-product onboarding tour

**Area:** Progressive disclosure effectiveness (20%)
**Current state:** The five-step opt-in tour exists in code but all step content is wrapped in `<TourStepPendingApproval>` markers. The tour will not display until the owner approves all five copy blocks (Improvement 5 in `PENDING_QUESTIONS.md`, option B — batch all five in one PR).
**Why deferred:** The tour copy requires owner review and approval before the `<TourStepPendingApproval>` wrappers can be removed. The assistant cannot approve marketing/UX copy.
**Recommended owner action:** Review and approve the five tour step copies, then the assistant will batch-remove the wrappers in a single PR per the agreed pattern.

---

## Pending questions for the owner (deferred for when you have time)

1. **Improvement 9 — Term replacement vocabulary:** Which replacement terms do you prefer for "golden manifest" (proposed: "committed manifest" or "architecture manifest"), "authority pipeline" (proposed: "analysis pipeline" or "evaluation pipeline"), and the general use of "authority" in user-facing copy? This decision gates a large rename similar to ArchiForge → ArchLucid.

2. **Improvement 10 — Tour copy approval:** Are you ready to review the five tour step copies currently wrapped in `<TourStepPendingApproval>` so the batch PR can proceed? (Same question as item 38 / Improvement 5 in `PENDING_QUESTIONS.md`.)

---

## Assessment integrity notes

- This assessment was produced independently without reference to prior quality assessment scores or improvement lists.
- Scoring reflects V1 surfaces only; deferred V1.1/V2 items are not charged.
- The buyer first-30-minutes placeholder copy is noted but not heavily penalized because the placeholder markers are explicitly flagged and the SaaS trial funnel is the primary buyer path, not the GitHub stub.
- The opt-in tour's pending state is scored as a progressive disclosure gap (the mechanism exists but does not deliver content) rather than a missing feature.
