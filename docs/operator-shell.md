# ArchLucid operator shell (Change Set 55R)

**Audience:** Internal operators and design partners using the thin Next.js UI in `archlucid-ui/` against the ArchLucid API.

---

## What it is

A read-focused **operator shell** for the three ArchLucid product layers:

| Layer | What you do here |
|-------|-----------------|
| **Core Pilot** | Create runs, track execution, commit manifests, review and download artifacts |
| **Advanced Analysis** | Compare runs, replay authority chains, explore provenance graphs, run Q&A and advisory scans |
| **Enterprise Controls** | Governance approvals, policy packs, audit log, alerts, compliance drift |

It is not a replacement for Swagger or the CLI. See [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) for the full capability inventory.

### In-product layer hints (UI)

The shell surfaces the three-layer model without duplicating [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md):

- **Sidebar** — each nav group shows a one-line caption under the layer name (what that group is for).
- **LayerHeader** — Compare, Replay, Graph, Governance dashboard, Alerts, and Audit pages open with a short “what question this answers” strip and a first-pilot reminder where relevant.
- **Home** — after every Core Pilot checklist box is checked, a compact strip suggests Advanced Analysis next steps (still optional).
- **Run detail** — after a golden manifest exists, an optional strip links Compare / Replay / Graph for this run.

Long-form “when to expand” tables remain in **OPERATOR_DECISION_GUIDE.md**; the UI carries only minimal affordances.

### Navigation authority hints (structural)

The sidebar, mobile drawer, and **Ctrl+K** command palette can hide individual destinations when the signed-in principal is unlikely to satisfy the API for that workflow. Link metadata lives on **`NavLinkItem.requiredAuthority`** in `archlucid-ui/src/lib/nav-config.ts` and mirrors ASP.NET policy names **`ReadAuthority`**, **`ExecuteAuthority`**, and **`AdminAuthority`** (see repo root **`README.md`**, API authentication section).

The shell resolves a monotonic caller rank from **`GET /api/auth/me`** (same-origin **`/api/proxy/api/auth/me`**, role claims) via **`archlucid-ui/src/lib/current-principal.ts`** (`loadCurrentPrincipal`); `OperatorNavAuthorityProvider` consumes the same helper. Sidebar, mobile drawer, and command palette compose **tier + authority** in **`archlucid-ui/src/lib/nav-shell-visibility.ts`** and **omit whole nav groups** when every link in that group is filtered out (no empty headings). **This is not authorization:** routes still enforce policies server-side. Omitted `requiredAuthority` keeps a link visible for every resolved rank (used for **Home**, **Onboarding**, and other Core Pilot essentials so the default path stays open).

Short **Enterprise Controls** context lines (nav subtitle + `LayerHeader` footnotes + execute-page hints) live in **`archlucid-ui/src/lib/enterprise-controls-context-copy.ts`** and **`EnterpriseControlsContextHints.tsx`** so omission does not feel arbitrary for readers (see **OPERATOR_DECISION_GUIDE.md** §2).

**Contributors:** treat **`archlucid-ui/README.md`** (*Role-aware shaping*) as the canonical pointer list. Do not add ad-hoc `/me` fetches or duplicate policy logic in the browser; extend **`nav-config.ts`** + **`nav-shell-visibility.ts`** when adding routes so sidebar, mobile drawer, and palette stay consistent.

---

## Main workflow

### Core Pilot path (steps 1–4 — start here)

These four steps cover the complete first-pilot journey. They map directly to the **Core Pilot checklist** on the Home page.

1. **Start** — Open the app root (`/`). First-time users: use the **Core Pilot checklist** on Home for step-by-step links (create run → pipeline → commit → review artifacts); **Hide checklist** collapses it (preference in browser `localStorage`). The sidebar **Core Pilot** group shows **Home**, **Onboarding**, **New run**, and **Runs** by default. **New run** opens the seven-step wizard at **`/runs/new`** (same **`POST /v1/architecture/request`** body shape as the API — see **`docs/FIRST_RUN_WIZARD.md`**).
2. **Runs** — `Runs` → pick a project (default `default`) → **Open run** on a row (empty list shows **Create your first run (wizard)**).
3. **Run detail** — **Pipeline timeline** lists run-scoped audit events (oldest first) from **`GET /v1/authority/runs/{runId}/pipeline-timeline`**. After commit, you see manifest summary, **Artifacts** (table with **Review** / **Download**).
4. **Manifest / artifact** — From the golden manifest link or **Review**, you land on manifest-scoped or artifact review pages: metadata, in-shell preview (when available), raw disclosure, sibling artifact list.

### Advanced Analysis (available once you have a committed run)

Enable these by clicking **Show more links** in the sidebar footer. These are **Advanced Analysis** layer features.

5. **Compare / replay** — **Compare runs**: enter base (left) and target (right) run IDs; structured manifest deltas first, then legacy flat diff; optional AI explanation. **Replay run**: pick mode and read validation flags/notes.
6. **Graph** — Enter a **run ID** (from Runs or run detail), choose a view (full provenance, decision subgraph, neighborhood, architecture), **Load graph**. Use this when you need a **visual** graph, not the tabular compare flow.
7. **Ask / Advisory / Pilot feedback** — natural-language queries against architecture context; advisory scan results and digests; pilot feedback rollups.

Breadcrumb links on key pages tie **Home · Runs · Compare · Graph** together.

### Enterprise Controls (require extended or advanced links)

These are **Enterprise Controls** layer features. Most require an operator or admin role and may require explicit configuration per environment (see `docs/PRE_COMMIT_GOVERNANCE_GATE.md`, `docs/ALERTS.md`).

- **Governance dashboard** — cross-run pending approvals and policy changes. Enable **Show more links**.
- **Policy packs / Governance resolution** — versioned rule sets and effective policy view. Enable **Show more links**.
- **Audit log** — append-only search, filter, and CSV export. Enable **Show advanced links**.
- **Alerts / Alert rules / Routing / Tuning** — configurable alert pipeline. Alerts (inbox) are **essential** tier; rules, routing, and tuning require **Show advanced links**.
- **Governance workflow** — full approval, promotion, and activation surface. Enable **Show advanced links**.

---

## Trial banner (self-service workspaces)

When `GET /v1/tenant/trial-status` reports **Active**, **Expired**, or **ReadOnly**, the operator shell shows **`TrialBanner`** (see `archlucid-ui/docs/TRIAL_SIGNUP_UI.md`): remaining calendar days, **Convert to paid** (`POST /v1/tenant/billing/checkout`), checklist link to **`/onboarding/start`**, and a dismiss control that hides the strip for **24 hours** then re-evaluates on the next visit.

---

## Keyboard and accessibility (V1 polish)

- **Skip link:** Press **Tab** once on any page to reach **Skip to main content**; **Enter** moves focus into the page body (`#main-content`) so you can bypass the header nav and auth strip.
- **Visible focus:** Primary header nav links, first-run checklist actions, and auth **Sign in** / **Sign out** show a clear keyboard **focus ring** (do not rely on mouse-only hover).
- **Landmarks:** The auth strip is exposed as a named **region** (“Authentication status”) for screen readers.
- **Page titles:** Browser tabs use short route titles from Next.js metadata (for example **Runs list**, **Compare two runs**, **Run graph (provenance)**) in addition to the **· ArchLucid** template.
- **Copy tweaks:** Home uses **Operator home** as the main heading; the first-run panel title reads **First-run workflow (V1 checklist)**; the runs list heading includes the active **project** id inline.

This is a lightweight pass (focus, labels, contrast on small caps) — not a full WCAG audit.

---

## Empty, loading, and error states (operator copy)

Pages use shared callouts from `archlucid-ui/src/components/OperatorShellMessage.tsx`:

- **`OperatorLoadingNotice`** — in-progress fetches (explicit text, no spinners required).
- **`OperatorEmptyState`** — valid empty data (e.g. zero runs, zero alerts for a filter).
- **`OperatorApiProblem`** — HTTP / transport failures with ProblemDetails when present.
- **`OperatorMalformedCallout`** — HTTP succeeded but JSON failed **coerce\*** contract checks (distinct from empty).
- **`OperatorTryNext`** — short **Try next:** line after failures or malformed responses: concrete checks (health, `GET /version`, correlation ID, re-copy run IDs, try another filter).

Goal: operators see **what happened**, **how it differs from “nothing here”**, and **one sensible next action** without raw stack traces in the shell.

---

## Audit log (`/audit`)

Filter durable `IAuditService` rows (event type, local **from/to** window, correlation id, actor, run id). **Clear filters** resets inputs and immediately re-queries with no filters. **Export CSV** calls `GET /v1/audit/export` (same-origin proxy) with the current **from/to** range and triggers a browser download; the button stays disabled until both bounds are set (tooltip explains why). A summary line above the list shows **Showing N events** or **Showing N+ events** when more pages remain (**Load more** uses the search keyset cursor).

---

## Artifact review

- **List:** `GET api/artifacts/manifests/{manifestId}` returns a **JSON array** (possibly empty) when the manifest exists in scope. Rows are ordered **by name, then artifact id** (deterministic for UI and ZIP).
- **Descriptor:** `GET …/artifact/{id}/descriptor` — metadata only for the review header (type, format, hash, timestamps).
- **Preview:** The UI fetches bytes through the same-origin proxy for a truncated UTF-8 preview; **Download** uses the full file. If preview fails, metadata and download may still succeed.
- **Bundle vs list:** If the manifest exists but there is **no bundle or zero artifacts**, the **list** returns `[]`; **bundle ZIP** returns **404** with a distinct problem type from **unknown manifest** (see API expectations below).
- **Run detail / export / replay:** The API hydrates the synthesized **artifact bundle** for a run whenever the run row has a **golden manifest id**, by loading the bundle **by manifest id**. The optional `ArtifactBundleId` column on the run is not required for that path—useful when backfills or partial updates left the manifest link set but the bundle row pointer unset.
- **Run export ZIP** (`GET api/artifacts/runs/{runId}/export`): includes `README.txt` with run and manifest IDs, optional **manifest display name**, **rule set**, **manifest hash**, and a short description of each file (`manifest.json`, `decision-trace.json` when present, `artifacts/`, `package-metadata.json`).
- **DOCX architecture package** (`GET api/docx/runs/{runId}/architecture-package`): the **Architecture diagram** section prefers a **PNG** synthesized artifact (`png` / `image/png`, base64); otherwise it tries **Mermaid→PNG** via the host’s **Mermaid CLI** (`mmdc`) when `ArchLucid:MermaidCli:Enabled` is **true** in API configuration; if rasterization is unavailable, it embeds **Mermaid source** (same text as `architecture.mmd` in the bundle); if there is no diagram artifact, it states that and points to topology/decision counts below.

---

## Graph vs compare vs replay

| Area | Purpose |
|------|--------|
| **Graph** | Visual exploration of provenance or architecture **for one run** (nodes/edges, filters, node detail). |
| **Compare runs** | **Two runs** side by side: structured golden-manifest deltas + legacy diff (+ optional AI narrative). |
| **Replay run** | Re-execute the stored **authority chain** for **one run** and surface validation results (not a visual diff). |

---

## Running focused UI tests (55R smoke)

From `archlucid-ui/`:

```bash
npm test
```

Targeted suites (Vitest):

```bash
npx vitest run src/app/page.test.tsx src/components/ShellNav.test.tsx
npx vitest run src/review-workflow
npx vitest run src/components/ArtifactListTable.test.tsx src/components/ArtifactReviewContent.test.tsx
npx vitest run src/components/GraphViewer.test.tsx
npx vitest run src/review-workflow/compare-views.test.tsx
npx vitest run src/lib/api.review-workflow.test.ts src/lib/operator-response-guards.test.ts
```

Full detail: [archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md](../archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md).

---

## API / UI contract expectations (operator flow)

- **Versioning:** Browser calls go to **`/v1/...`** via the Next.js **`/api/proxy`** route; the server attaches scope and credentials. Do not expose API keys in the browser bundle.
- **JSON shapes:** The UI uses **coerce\*** guards on responses. Malformed JSON → operator “response not usable” states (distinct from HTTP failure and empty data).
- **Empty vs missing:** **Runs list** can be `[]`. **Artifact list** for a valid manifest can be `[]`. **404** on run/manifest/artifact routes should carry **RFC 9457 Problem Details** (`title` / `detail` / `type`) where the API provides them. Error JSON from the API includes **`correlationId`** (matches **`X-Correlation-ID`**) for log triage; proxy-generated errors do the same.
- **Artifact bundle 404:** Prefer distinguishing **manifest not in scope** (`manifest-not-found`) from **no bundle / zero artifacts** (`resource-not-found`) when interpreting bundle download failures next to an empty artifact table.
- **Ordering:** Treat artifact list and ZIP entry order as **stable** (name, then id) for screenshots and diffs.

Deeper UI architecture: [archlucid-ui/docs/ARCHITECTURE.md](../archlucid-ui/docs/ARCHITECTURE.md). Tutorial: [archlucid-ui/docs/OPERATOR_SHELL_TUTORIAL.md](../archlucid-ui/docs/OPERATOR_SHELL_TUTORIAL.md).
