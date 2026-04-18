# ArchLucid Core Pilot path

**Audience:** Operators and design partners completing their first pilot.

**Purpose:** Define the default, minimal journey from an empty tenant to a reviewed, exportable architecture run — without requiring any advanced features.

**Canonical buyer narrative:** For sponsor-facing summary and outward buyer messaging, start with **[docs/EXECUTIVE_SPONSOR_BRIEF.md](EXECUTIVE_SPONSOR_BRIEF.md)**. This document is the operator-facing first-pilot path, not a second buyer summary.

**Related docs:**
- [docs/V1_SCOPE.md](V1_SCOPE.md) §4 — authoritative V1 boundary and extended-operation inventory.
- [docs/operator-shell.md](operator-shell.md) — operator UI workflow and API contract expectations.
- [docs/PILOT_GUIDE.md](PILOT_GUIDE.md) — full pilot onboarding narrative.
- [docs/OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) — copy-paste CLI and API commands.
- [docs/PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) — measurement companion for pilot success.

---

## 1. What "Core Pilot" means

The Core Pilot path is **four steps**. Completing them produces a committed golden manifest and a downloadable artifact bundle — the primary deliverable of an ArchLucid pilot.

```
1. Create request
      ↓
2. Execute run  (pipeline fills automatically)
      ↓
3. Commit manifest
      ↓
4. Review manifest and artifacts
```

Everything in ArchLucid beyond these four steps — graph, compare, replay, advisory, alerts, governance, export packages, DOCX — is **available but secondary**. Pilots do not need to touch any of it to demonstrate value.

The **operator Home page**, **sidebar progressive disclosure**, and route-level layer guidance may surface those deeper layers for discovery, but copy and ordering treat them as **maturity paths**, not co-equal prerequisites to the four steps above.

**Anti-creep rule:** if a feature does not help an operator complete these four steps more clearly or more quickly, it should not become part of the default mental model for a first pilot.

**Operator UI note:** the shell may **surface** Advanced Analysis and Enterprise Controls for discovery (sidebar disclosure, **LayerHeader** on deeper routes). That is **packaging and progressive disclosure**, not a requirement to use those layers in the first session. UI visibility and soft-disabled controls follow **`docs/PRODUCT_PACKAGING.md`** §3 and **`GET /api/auth/me`** — **not** a substitute for API authorization. Sidebar inclusion is **tier then authority** (**`nav-shell-visibility.ts`**): higher role rank does not bypass “Show more” / extended disclosure. For the **code-level seam map** (which TS modules implement which layer), use §3 *Code seams*; §3 *Contributor drift guard* indexes **Vitest** files that lock rank, nav filtering, tier order, and refetch conservatism (**`archlucid-ui/src/components/OperatorNavAuthorityProvider.test.tsx`**).

---

## 2. Why this boundary exists

The platform has a large surface area by design: it supports governance workflows, comparison replay, multi-agent authority chains, knowledge graphs, and more. For a first pilot, that breadth is friction, not value.

The Core Pilot boundary lets a pilot:
- Produce a clear deliverable in a single session.
- Avoid confusion about which features are "required" vs "available."
- Evaluate the core value proposition (AI-driven analysis → structured manifest → artifacts) before exploring advanced capabilities.

---

## 3. Step-by-step walkthrough

### Step 1 — Create an architecture request

**Operator UI:** Sidebar → **New run** → seven-step wizard (system identity, requirements, constraints, advanced inputs, submit). The wizard POSTs `POST /v1/architecture/request` and enters pipeline-tracking mode.

**CLI:** `archlucid run` (reads `archlucid.json` + `inputs/brief.md`). Quick dev path: `archlucid run --quick` seeds fake results and commits in one step.

**API:** `POST /v1/architecture/request` — see [docs/API_CONTRACTS.md](API_CONTRACTS.md) for the request body shape.

### Step 2 — Execute the run

After creation, the coordinator fills context snapshots and authority steps automatically. In **simulator mode** (default in dev/demo), the pipeline completes in seconds.

**Check status:** Operator UI → Runs → open the row → Pipeline timeline. Or: `archlucid status <runId>`.

The run is ready to commit when the pipeline timeline shows no in-progress steps and the run status is `ReadyToCommit` or equivalent.

### Step 3 — Commit the manifest

Commit produces the **golden manifest** and synthesizes **artifacts**. Nothing is reviewable, exportable, or comparable before this step.

**Operator UI:** Run detail → **Commit run** button.

**CLI:** `archlucid commit <runId>`.

**API:** `POST /v1/architecture/run/{runId}/commit`.

> **Pre-commit governance gate (optional):** If `ArchLucid:Governance:PreCommitGateEnabled` is true, a gate checks findings before allowing commit. For a first pilot, leave this off unless you specifically want to evaluate governance behavior. See [docs/PRE_COMMIT_GOVERNANCE_GATE.md](PRE_COMMIT_GOVERNANCE_GATE.md).

### Step 4 — Review manifest and artifacts

**Operator UI:** Run detail (after commit) shows:
- **Manifest summary** — decisions, findings, structured metadata.
- **Artifacts table** — each artifact with a **Review** link (in-shell preview + raw disclosure) and a **Download** button.
- **Bundle ZIP** — full artifact bundle for offline review.

**CLI:** `archlucid artifacts <runId>` — prints the manifest. `archlucid artifacts <runId> --save` writes `outputs/manifest-{version}.json`.

**API:** `GET /v1/architecture/manifest/{version}` — retrieve the committed manifest JSON.

At this point, the Core Pilot deliverable is complete.

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

Minimum required configuration (see [docs/PILOT_GUIDE.md](PILOT_GUIDE.md) for full setup):

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
| Governance workflows (approval chains, SLA tracking) | [docs/PRE_COMMIT_GOVERNANCE_GATE.md](PRE_COMMIT_GOVERNANCE_GATE.md) |
| Two-run comparison and drift detection | `/compare` + [docs/COMPARISON_REPLAY.md](COMPARISON_REPLAY.md) |
| Knowledge and provenance graph | `/graph` |
| Advisory scans and digest subscriptions | `/advisory`, `/digests` |
| Alert rules and routing | `/alerts`, `/alert-rules` |
| Integration events (Azure Service Bus) | [docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md) |
| DOCX and PDF export for stakeholders | Run detail → Artifacts → DOCX export |
| Multi-tenant isolation and OIDC auth | [docs/security/MULTI_TENANT_RLS.md](security/MULTI_TENANT_RLS.md) |

These are valuable, but they should remain **follow-on layers** after the first four-step proof path is already successful.
