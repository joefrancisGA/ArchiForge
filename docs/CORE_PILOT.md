> **Scope:** ArchLucid Core Pilot path - full detail, tables, and links in the sections below.

# ArchLucid Core Pilot path

**Audience:** Operators and design partners completing their first pilot.

**Purpose:** Define the default, minimal journey from an empty tenant to a reviewed, exportable **architecture review package** — without requiring any advanced features.

This document is the operator-facing first-pilot path. The sponsor-facing narrative lives in the executive brief. C4 context, route maps, and UI seam rules are linked below when your work touches layout, nav, or API policy alignment.

<a id="first-session-checklist"></a>

## First session checklist (buyer / evaluator)

| Question | Answer |
| --- | --- |
| **What do I do first?** | Follow the **four steps** in §3: create an architecture **request**, let the **pipeline** finish, **finalize** when ready, then **review outputs** on review detail. Entry from [START_HERE.md](START_HERE.md) → this doc — no deep architecture reading required. |
| **What output proves value?** | A **finalized** architecture review: manifest summary, ranked findings, and **exportable artifacts** (table + bundle). That bundle is the **review package** you can walk a sponsor through. |
| **What do I send a sponsor?** | Narrative: [EXECUTIVE_SPONSOR_BRIEF.md](EXECUTIVE_SPONSOR_BRIEF.md). Artifacts: export from review detail after finalization. Optional pilot metrics: [PILOT_ROI_MODEL.md](library/PILOT_ROI_MODEL.md). |
| **What should I ignore for now?** | **Compare**, **replay**, **graph**, advisory-heavy flows, governance dashboards, alerts tuning, and anything labeled extended/advanced in §4 — until the first package is done. |
| **How is checklist progress counted (server-side)?** | The operator-panel checklist can emit **anonymous, rate-limited** acknowledgements (`POST /v1/diagnostics/core-pilot-rail-step` with step index 0–3). This increments aggregated counter **`archlucid_core_pilot_rail_checklist_step_total`** (label **`step`** = `create_request` … `review_outputs`) for adoption dashboards — never a substitute for first-party analytics you control. |
| **Verification ladder (truth vs release smoke)** | **A)** `run-readiness-check` / deployment checks → **B)** **`docs/library/RELEASE_SMOKE.md`** (API+CLI) → **C)** CI **live Playwright** against a SQL-backed API (**`live-api-accessibility*.spec.ts`**, **`LIVE_E2E_HAPPY_PATH.md`**) for UI+SQL truth. See [RELEASE_SMOKE.md](library/RELEASE_SMOKE.md#release-smoke-ui-sql-parity) and [LIVE_E2E_HAPPY_PATH.md](library/LIVE_E2E_HAPPY_PATH.md). |
| **Why do I still see “run” in the UI?** | Same four steps: the product stores each architecture review as one **run** (run ID, APIs, telemetry). **Architecture review** / **review package** is the buyer outcome; **run** is the technical spine. |

---

## Related

- **[EXECUTIVE_SPONSOR_BRIEF.md](EXECUTIVE_SPONSOR_BRIEF.md)** — sponsor story of record
- **[ARCHITECTURE_ON_ONE_PAGE.md](ARCHITECTURE_ON_ONE_PAGE.md)** — C4 + ownership
- **[OPERATOR_ATLAS.md](library/OPERATOR_ATLAS.md)** — route × API × CLI
- **[PRODUCT_PACKAGING.md](library/PRODUCT_PACKAGING.md)** §3 — operator nav, `LayerHeader`, API policies, Vitest seam index (`authority-seam-regression.test.ts`, `authority-execute-floor-regression.test.ts`, `authority-shaped-ui-regression.test.ts`, `authority-shaped-layout-regression.test.tsx`)
- **[V1_SCOPE.md](library/V1_SCOPE.md)** §4 — V1 boundary
- **[operator-shell.md](library/operator-shell.md)** — operator UI workflow and API contracts
- **[PILOT_GUIDE.md](library/PILOT_GUIDE.md)** — full pilot onboarding narrative
- **[OPERATOR_QUICKSTART.md](library/OPERATOR_QUICKSTART.md)** — copy-paste CLI and API commands
- **[PILOT_ROI_MODEL.md](library/PILOT_ROI_MODEL.md)** — measurement companion
- **[DOGFOOD_PILOT_KIT.md](library/DOGFOOD_PILOT_KIT.md)** — internal Core Pilot worksheets + **[PMF_VALIDATION_TRACKER.md](go-to-market/PMF_VALIDATION_TRACKER.md)** (**Pilot A**) update rules without inventing baselines/results
- **[SECOND_RUN.md](library/SECOND_RUN.md)** — your own data after the demo (`archlucid second-run`)

---

## 1. What "Core Pilot" means

The Core Pilot path is **four steps**. Completing them produces a committed **golden manifest** (technical term) and a downloadable **artifact bundle** — together, the **architecture review package** that is the primary deliverable of a pilot.

```
1. Create architecture review
      ↓
2. Pipeline runs  (coordinator fills steps automatically — UI shows review/progress)
      ↓
3. Finalize / commit manifest
      ↓
4. Open review package (manifest summary and artifacts)
```

Everything in ArchLucid beyond these four steps — graph, compare, replay, advisory, alerts, governance, export packages, DOCX — is **available but secondary**. Pilots do not need to touch any of it to demonstrate value.

The **operator Home page**, **sidebar progressive disclosure**, and route-level layer guidance may surface those deeper layers for discovery, but copy and ordering treat them as **maturity paths**, not co-equal prerequisites to the four steps above.

**Anti-creep rule:** if a feature does not help an operator complete these four steps more clearly or more quickly, it should not become part of the default mental model for a first pilot.

**Operator UI note:** the shell may **surface** Operate (analysis workloads) and Operate (governance and trust) for discovery (sidebar disclosure, **LayerHeader** on deeper routes). That is **packaging and progressive disclosure**, not a requirement to use those layers in the first session. UI visibility and soft-disabled controls follow **`docs/PRODUCT_PACKAGING.md`** §3 and **`GET /api/auth/me`** — **not** a substitute for API authorization. Sidebar inclusion is **tier then authority** (**`nav-shell-visibility.ts`**): higher role rank does not bypass “Show more” / extended disclosure. For the **code-level seam map** (which TS modules implement which layer), use §3 *Code seams* and §3 *Four UI shaping surfaces* (shell vs mutation hook vs strip vs inline cues — avoid merging concerns in refactors). §3 *Contributor drift guard* indexes **Vitest** files that lock rank, nav filtering, tier order, refetch conservatism (**`archlucid-ui/src/components/OperatorNavAuthorityProvider.test.tsx`**), cross-module ordering, bootstrap-rank, and **`LAYER_PAGE_GUIDANCE`** Enterprise vs Advanced **`enterpriseFootnote`** (**`archlucid-ui/src/lib/authority-seam-regression.test.ts`**), the **Execute** nav vs mutation boolean (**`archlucid-ui/src/lib/authority-execute-floor-regression.test.ts`**), catalog **`ExecuteAuthority`** rows (**`archlucid-ui/src/lib/authority-shaped-ui-regression.test.ts`**), **LayerHeader** strip **`aria-label`**, page-level mutation **`disabled`** / governance submit **`readOnly`** plus governance resolution **Change related controls** wiring (**`archlucid-ui/src/app/(operator)/enterprise-authority-ui-shaping.test.tsx`**), and inspect-first **layout** on selected Enterprise pages (**`archlucid-ui/src/app/(operator)/authority-shaped-layout-regression.test.tsx`**).

---

## 2. Why this boundary exists

The platform has a large surface area by design: it supports governance workflows, comparison replay, multi-agent authority chains, knowledge graphs, and more. For a first pilot, that breadth is friction, not value.

The Core Pilot boundary lets a pilot:
- Produce a clear deliverable in a single session.
- Avoid confusion about which features are "required" vs "available."
- Evaluate the core value proposition (AI-driven analysis → structured manifest → artifacts) before exploring advanced capabilities.

---

## 3. Step-by-step walkthrough

### Zero-config sample first

On the operator Home page, **Start with sample review** opens the curated Claims Intake sample review package. It is already finalized and shows the same output shape as a real Core Pilot: reviewed manifest, findings, evidence trail, artifacts, and a clear demo-data warning. Use it to understand the destination before filling out the real-input wizard; do **not** use the sample numbers as customer ROI evidence.

<a id="new-run"></a>

### Step 1 — Create an architecture review

**Operator UI:** Sidebar → **New run** → seven-step wizard (system identity, requirements, constraints, advanced inputs, submit). The wizard creates the architecture review and POSTs `POST /v1/architecture/request`; the resulting run ID is support metadata.

**CLI:** `archlucid run` (reads `archlucid.json` + `inputs/brief.md`). Quick dev path: `archlucid run --quick` seeds fake results and commits in one step.

**API:** `POST /v1/architecture/request` — see [docs/API_CONTRACTS.md](library/API_CONTRACTS.md) for the request body shape.

<a id="pipeline-status"></a>

### Step 2 — Execute the run

After creation, the coordinator fills context snapshots and authority steps automatically. In **simulator mode** (default in dev/demo), the pipeline completes in seconds.

**Check status:** Operator UI → Runs → open the row → Pipeline timeline. Or: `archlucid status <runId>`.

The run is ready to commit when the pipeline timeline shows no in-progress steps and the run status is `ReadyToCommit` or equivalent.

<a id="commit"></a>

### Step 3 — Commit the manifest

Commit produces the **golden manifest** and synthesizes **artifacts**. Nothing is reviewable, exportable, or comparable before this step.

**Operator UI:** Run detail → **Commit run** button.

**CLI:** `archlucid commit <runId>`.

**API:** `POST /v1/architecture/run/{runId}/commit`.

<a id="governance-gate"></a>

> **Pre-commit governance gate (optional):** If `ArchLucid:Governance:PreCommitGateEnabled` is true, a gate checks findings before allowing commit. For a first pilot, leave this off unless you specifically want to evaluate governance behavior. See [docs/PRE_COMMIT_GOVERNANCE_GATE.md](library/PRE_COMMIT_GOVERNANCE_GATE.md).

<a id="manifest-review"></a>

### Step 4 — Open the review package

**Operator UI:** Run detail (after commit) shows:
- **Manifest summary** — decisions, findings, structured metadata.
- **Artifacts table** — each artifact with a **Review** link (in-shell preview + raw disclosure) and a **Download** button.
- **Bundle ZIP** — full artifact bundle for offline review.

**CLI:** `archlucid artifacts <runId>` — prints the manifest. `archlucid artifacts <runId> --save` writes `outputs/manifest-{version}.json`.

**API:** `GET /v1/architecture/manifest/{version}` — retrieve the committed manifest JSON.

At this point, the Core Pilot deliverable is complete.

### Step 5 — Same four steps with **your** inputs (no doc stack)

After `archlucid try` (or the operator wizard demo), the lowest-friction “real” second run is a **one-page** `SECOND_RUN.toml` / `.json` file plus a single CLI command — no need to read OPERATOR_QUICKSTART, PILOT_GUIDE, or CONTEXT_INGESTION for the happy path.

**CLI:** `archlucid second-run SECOND_RUN.toml` — see **[docs/SECOND_RUN.md](library/SECOND_RUN.md)** for the 60-second template, limits, and failure hints (correlation id + audit event names for log grep).

**Operator UI:** On **New run → Starting point**, use **Paste SECOND_RUN.toml** (or JSON) to pre-fill the wizard from the same schema, then continue through identity and constraints as usual.

---

## 4. Extended operations (not required — enable via "Show more links")

These are available once you have a committed run. In the operator UI sidebar, click **Show more links** to surface Graph, Compare, and Replay.

**These are follow-on maturity paths, not first-pilot proof.** Use them only when a real question requires them.

| Operation | Where | When to use |
|-----------|-------|-------------|
| **Compare two runs** | `/compare` | Diff two manifests structurally — useful for "before vs after" analysis. |
| **Replay a run** | `/replay` | Re-validate the authority chain and surface any drift. |
| **Graph** | `/graph` | Visual provenance or architecture graph for a single run ID. |
| **Export DOCX** | Run detail → Artifacts → DOCX export | Consulting-grade report for stakeholder review. |
| **Download ZIP** | Run detail → Artifacts → Bundle / Run export | Full offline artifact bundle. |

---

## 5. What stays in the sidebar by default (essential tier)

The sidebar progressive disclosure system exposes three tiers: `essential`, `extended`, and `advanced`. At first launch (no `localStorage` values), only `essential` links are visible:

| Group | Essential links (visible by default) |
|-------|--------------------------------------|
| Runs & review | Home, Onboarding, New run, Runs |
| Q&A & advisory | Ask |
| Alerts & governance | *(group collapsed by default)* |

**Show more links** (sidebar footer button) reveals `extended` links:

| Group | Extended links (behind "Show more links") |
|-------|------------------------------------------|
| Runs & review | Graph, Compare two runs, Replay a run |
| Q&A & advisory | Advisory, Recommendation learning, Pilot feedback |
| Alerts & governance | Policy packs, Governance resolution, Governance dashboard |

**Navigation settings** (gear icon, sidebar footer) also exposes `advanced` links (audit, alert tuning, schedules, etc.).

The default expectation remains the same: **Core Pilot first, deeper layers later and only when needed**.

---

## 6. Back-end surface

The Core Pilot path uses the following API endpoints. No advanced configuration is required beyond a running API and a valid connection string.

| Endpoint | Purpose |
|----------|---------|
| `POST /v1/architecture/request` | Create a run |
| `GET /v1/architecture/run/{runId}` | Poll run status and task list |
| `POST /v1/architecture/run/{runId}/commit` | Commit — produces manifest + artifacts |
| `GET /v1/architecture/manifest/{version}` | Retrieve the committed manifest |
| `GET /v1/artifacts/manifests/{manifestId}` | List artifacts for a manifest |
| `GET /health/live`, `GET /health/ready` | Liveness and readiness |
| `GET /version` | Build and version identity |

Optional for dev/testing only:
- `POST /v1/architecture/run/{runId}/seed-fake-results` — seed deterministic fake results (Development mode only).

---

## 7. Configuration for a first pilot

Minimum required configuration (see [docs/PILOT_GUIDE.md](library/PILOT_GUIDE.md) for full setup):

```jsonc
// appsettings.json (or env vars / user secrets)
{
  "ConnectionStrings": {
    "ArchLucid": "Server=...;Database=ArchLucid;..."
  },
  "ArchLucid": {
    "StorageProvider": "Sql",
    "AgentExecution": {
      "Mode": "Simulator"   // no LLM costs; deterministic; recommended for first pilot
    }
  },
  "ArchLucidAuth": {
    "Mode": "DevelopmentBypass"   // or ApiKey / JwtBearer for production pilots
  }
}
```

---

## 8. What to evaluate in a Core Pilot

At the end of the four steps, a pilot evaluator should be able to answer:

1. Does the architecture request capture our system description accurately?
2. Are the findings (topology, cost, compliance, quality) relevant and plausible?
3. Is the manifest structure clear and the artifact content useful?
4. Does the governance pre-commit gate (if enabled) behave correctly for our finding severity thresholds?

These map directly to the **minimum success criteria** in [docs/go-to-market/PILOT_SUCCESS_SCORECARD.md](go-to-market/PILOT_SUCCESS_SCORECARD.md).

---

## 9. Cleanup later (post-Core Pilot)

Once the Core Pilot is validated, the following areas are available for deeper evaluation:

| Area | Where to start |
|------|---------------|
| Governance workflows (approval chains, SLA tracking) | [docs/PRE_COMMIT_GOVERNANCE_GATE.md](library/PRE_COMMIT_GOVERNANCE_GATE.md) |
| Two-run comparison and drift detection | `/compare` + [docs/COMPARISON_REPLAY.md](library/COMPARISON_REPLAY.md) |
| Knowledge and provenance graph | `/graph` |
| Advisory scans and digest subscriptions | `/advisory`, `/digests` |
| Alert rules and routing | `/alerts`, `/alert-rules` |
| Integration events (Azure Service Bus) | [docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md](library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) |
| DOCX and PDF export for stakeholders | Run detail → Artifacts → DOCX export |
| Multi-tenant isolation and OIDC auth | [docs/security/MULTI_TENANT_RLS.md](security/MULTI_TENANT_RLS.md) |

These are valuable, but they should remain **follow-on layers** after the first four-step proof path is already successful.
