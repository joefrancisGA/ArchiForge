# ArchLucid UI (operator shell)

Thin Next.js App Router UI for the ArchLucid operator experience. The default job of the UI is to help a team move from an architecture request to a reviewable, defensible output quickly through the **Core Pilot** path, then expand into **Advanced Analysis** or **Enterprise Controls** only when needed.

**Canonical buyer narrative:** For sponsor-facing and outward buyer messaging, start with [../docs/EXECUTIVE_SPONSOR_BRIEF.md](../docs/EXECUTIVE_SPONSOR_BRIEF.md). This UI README explains operator flow and implementation-facing shaping; it is not the primary buyer summary. Pilot measurement: [../docs/PILOT_ROI_MODEL.md](../docs/PILOT_ROI_MODEL.md).

**In-product guidance:** sidebar group **captions** summarize each layer; key Advanced Analysis / Enterprise routes render a **`LayerHeader`** strip (what question the page answers)—including **governance resolution**, **policy packs**, **governance workflow**, and **alert operator tooling** (rules, routing, simulation, tuning, composite rules); **Home** lists Advanced/Enterprise **below** the Core Pilot checklist and marks them **not first-pilot proof**; after all checklist steps, **AfterCorePilotChecklistHint** nudges to analysis only when there is a real question; **run detail** shows an optional Advanced Analysis strip after commit (still not a success criterion). Enterprise pages add **short leads** (often **inspect vs change** and first-pilot deferral in `layer-guidance.ts` + page intros) so read-heavy context is not weighted the same as configuration forms. Full routing logic stays in **`docs/OPERATOR_DECISION_GUIDE.md`** (repo root).

## Role-aware shaping (first wave)

The shell **already** shapes nav and light copy by **principal + policy tier names** aligned with the C# API (`ReadAuthority` / `ExecuteAuthority` / `AdminAuthority`). This is **[COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](../docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md)** Stage 1–style **clarity**, not licensing: **Core Pilot** stays broadly accessible; **Enterprise Controls** are the first hardening target for `requiredAuthority` and omission hints.

| Concern | Source file(s) |
|---------|----------------|
| Link metadata + product grouping | `src/lib/nav-config.ts` |
| Policy tier names + rank helpers | `src/lib/nav-authority.ts` |
| `GET /api/auth/me` read-model | `src/lib/current-principal.ts` (`loadCurrentPrincipal`, `getCurrentPrincipal`, `getCurrentAuthority`, `getCurrentAuthorityRank`) |
| Tier + authority composition (sidebar, mobile, palette) | `src/lib/nav-shell-visibility.ts` |
| React context + refresh + shared `currentPrincipal` | `src/components/OperatorNavAuthorityProvider.tsx` |
| Enterprise one-liners | `src/lib/enterprise-controls-context-copy.ts`, `src/components/EnterpriseControlsContextHints.tsx`, `src/lib/layer-guidance.ts` (`enterpriseFootnote`) |

**Do not:** add parallel `/me` clients, re-implement policy matrices in TypeScript, or treat UI hiding as authZ. **Do:** keep `requiredAuthority` omitted only on Core Pilot essentials; set it on every Enterprise Controls link (and on selected Advanced links) so behavior matches `nav-config` comments.

**Nav authority:** `NavLinkItem.requiredAuthority` is **set on every Advanced and Enterprise link** in `nav-config.ts` and **omitted only on Core Pilot essentials**. Stable **`NAV_GROUPS[].id`** keys map to **docs/PRODUCT_PACKAGING.md** layers: `runs-review` (**Core Pilot**), `qa-advisory` (**Advanced Analysis**), `alerts-governance` (**Enterprise Controls**). `OperatorNavAuthorityProvider` loads **`GET /api/proxy/api/auth/me`** once per refresh (via `loadCurrentPrincipal`) and exposes **`currentPrincipal`** plus **`callerAuthorityRank`**; `useNavCallerAuthorityRank()` applies a conservative Read rank while JWT `/me` is in flight for a signed-in session. **`nav-shell-visibility.ts`** composes **tier → authority** for the sidebar, mobile drawer, and command palette (empty groups omitted). Not a substitute for server-side 401/403.

**Enterprise mutation affordances:** `useEnterpriseMutationCapability()` (same **Execute+** threshold as `EnterpriseControlsExecutePageHint`) soft-disables governance dashboard/workflow, policy-pack **writes**, **alerts inbox** triage actions (and Alt+1/2/3 shortcuts), and **alert rules / routing / composite** create-or-toggle controls for read-tier principals, with **`enterpriseMutationControlDisabledTitle`** tooltips — API enforcement unchanged (`@/lib/enterprise-mutation-capability`, `@/hooks/use-enterprise-mutation-capability`). **Alert simulation** and **alert tuning** stay enabled at Read (controllers are `ReadAuthority`); **audit CSV export** remains **`RequireAuditor`** on the API (not mapped to this hook).

**Enterprise context copy:** `enterprise-controls-context-copy.ts` + `EnterpriseControlsNavGroupHint` / `EnterpriseControlsExecutePageHint` add short, rank-aware lines; **`LayerHeader`** adds optional **`enterpriseFootnote`** on governance dashboard, governance resolution, governance workflow, policy packs, alert tooling routes, alerts inbox, and audit (`layer-guidance.ts`). **Page-level rank cues:** `GovernanceResolutionRankCue`, `GovernanceDashboardReaderActionCue`, `AlertsInboxRankCue`, `AuditLogRankCue`, `AlertOperatorToolingRankCue`, and `EnterpriseExecutePlusPageCue` in `EnterpriseControlsContextHints.tsx` add muted `role="note"` lines on governance resolution, governance dashboard (reader line + operator `Execute+` line), governance workflow, policy packs, **alerts inbox**, **audit log**, and **alert rules / routing / simulation / tuning / composite rules**—without changing Core Pilot routes.

## Core Pilot path (start here)

The default experience is the **Core Pilot path** — four steps from an empty workspace to a reviewed, exportable run:

```
New run (wizard) → pipeline runs → Commit → Artifacts
```

**First-time operators:** open **Home** (`/`) and use the collapsible **Core Pilot checklist** panel (step-by-step links: new run wizard → runs list → commit → artifacts). **Hide checklist** stores preference in `localStorage`. The sidebar **Core Pilot** group shows **Home**, **Onboarding**, **New run**, and **Runs** by default; **Show more links** reveals Graph, Compare, and Replay. Use **Tab** first for **Skip to main content** (keyboard).

**End-to-end path:** Home → **New run** or **Runs** → **Open run** → manifest summary & **Artifacts** table → **Review** → preview + download → optional **Compare two runs** / **Replay a run** / **Graph** (enable via **Show more links**).

- **Artifact review:** List (`[]` when empty), descriptor metadata, in-shell preview with raw disclosure, stable table order (name, then id — aligned with API).
- **Graph:** One run ID, multiple graph modes — for **visual** provenance/architecture, not two-run diff.
- **Compare / replay:** Two-run diff vs single-run authority replay — see [docs/operator-shell.md](../docs/operator-shell.md) in the repo root.

## Advanced Analysis (available once you have a committed run)

Enable via **Show more links** in the sidebar. These are **Advanced Analysis** layer features.

Use this layer when the next question is analytical:

- what changed,
- why it changed,
- how to replay the authority chain,
- or how to inspect architecture/provenance more deeply.

- **Graph** (`/graph`) — provenance or architecture graph for one run ID.
- **Compare** (`/compare`) — structured manifest diff between two runs.
- **Replay** (`/replay`) — re-validate the authority chain for a run.
- **Export** — bundle ZIP and run-export ZIP from run detail → Artifacts.
- **Ask / Advisory / Pilot feedback** — Q&A, advisory scans, and pilot signal collection.

## Enterprise Controls (governance, audit, compliance)

Enable extended and advanced links in the sidebar. These are **Enterprise Controls** layer features.

Use this layer when the next question is governance or trust:

- approvals,
- policy enforcement,
- audit evidence,
- compliance drift,
- alerts,
- or operational control.

### First-wave role-native hardening

The first hardening pass now treats the most operational Enterprise Controls surfaces as explicitly **operator/admin-oriented** in the navigation and link descriptions rather than as neutral feature links.

This first wave is intentionally narrow and currently covers:

- **Alert rules**
- **Alert routing**
- **Composite rules**
- **Alert simulation**
- **Alert tuning**
- **Policy packs**
- **Governance resolution**
- **Governance dashboard**
- **Governance workflow**
- **Audit log**

This is a boundary-hardening step for clarity and operational accountability. It is **not** licensing, billing, or plan gating.

- **Governance dashboard / Policy packs / Governance resolution** — approval workflows, segregation of duties, effective policy. Enable **Show more links**.
- **Alerts** — open and acknowledged alerts (essential tier by default); rules, routing, and tuning via **Show advanced links**.
- **Audit log** — append-only event search and CSV export. Enable **Show advanced links**.
- **Governance workflow** — full approval, promotion, and activation surface. Enable **Show advanced links**.

## Pilot feedback (58R)

**Nav:** **Pilot feedback** (not **Learning**, which is recommendation learning). Scoped dashboard, improvement opportunities, triage queue, Markdown/JSON export. Workflow: [docs/PRODUCT_LEARNING.md](../docs/PRODUCT_LEARNING.md).

## Documentation

| Document | What it covers |
|----------|---------------|
| [Executive sponsor brief](../docs/EXECUTIVE_SPONSOR_BRIEF.md) | **Canonical buyer narrative:** what ArchLucid does, what a pilot proves, and why expansion matters. |
| [Pilot ROI model](../docs/PILOT_ROI_MODEL.md) | **How to judge pilot success:** measures, scorecard, sponsor-ready outcome language. |
| [Operator decision guide](../docs/OPERATOR_DECISION_GUIDE.md) | **Which layer to use next:** stay narrow unless a real question requires expansion. |
| [Commercial boundary hardening sequence](../docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md) | **What should harden first:** UI shaping → role-native boundaries → selective future commercialization. |
| [Operator quickstart](../docs/OPERATOR_QUICKSTART.md) | **Pilots / operators:** commands, health, first run. |
| [Product learning (58R)](../docs/PRODUCT_LEARNING.md) | Pilot feedback dashboard, triage export. |
| [Troubleshooting (56R)](../docs/TROUBLESHOOTING.md) | Common failures (health, auth, SQL, proxy). |
| [Operator shell guide (55R)](../docs/operator-shell.md) | **Start here for operators.** Workflow, artifacts, graph vs compare/replay, UI test commands, API expectations. |
| [Architecture](docs/ARCHITECTURE.md) | System context, components, data flow, security, operations. |
| [Operator Shell Tutorial](docs/OPERATOR_SHELL_TUTORIAL.md) | React/Next.js tutorial for back-end developers. |
| [C# to React Rosetta Stone](docs/CSHARP_TO_REACT_ROSETTA.md) | Side-by-side patterns. |
| [Annotated Page Walkthrough](docs/ANNOTATED_PAGE_WALKTHROUGH.md) | Line-by-line `runs/page.tsx`. |
| [Component Reference](docs/COMPONENT_REFERENCE.md) | Components, props, helpers. |
| [Data Flow and State](docs/DATA_FLOW_AND_STATE.md) | Data flow, state patterns, templates. |
| [Testing and Troubleshooting](docs/TESTING_AND_TROUBLESHOOTING.md) | Tests, 55R Vitest smoke, **57R Playwright** operator journeys (mocked E2E), debugging. |

## UI stack (Tailwind + shadcn-style)

- **Tailwind CSS** (`tailwind.config.ts`, `postcss.config.mjs`, `src/app/globals.css`) for layout and tokens.
- **shadcn/ui-style primitives** — `components.json`, `src/lib/utils.ts` (`cn`), Radix **Slot**, **CVA**, and `src/components/ui/button.tsx`. Add more components with `npx shadcn@latest add …` when needed.

## Setup

```bash
cd archlucid-ui
npm install
cp .env.example .env.local
```

Edit `.env.local`:

- **`ARCHLUCID_API_BASE_URL`** — ArchLucid API base (default in repo: `http://localhost:5128` per `ArchLucid.Api` launchSettings).
- **`ARCHLUCID_API_KEY`** — Required when the API has `Authentication:ApiKey:Enabled` = `true`. Sent from the Next.js server (RSC + `/api/proxy`). Keep server-only in production.

Optional:

- **`NEXT_PUBLIC_ARCHLUCID_API_BASE_URL`** — Public fallback (documentation / tooling; browser calls use `/api/proxy`).

### OIDC / JWT (Entra ID)

When the API uses **JWT bearer** auth (see `ArchLucid.Api/appsettings.Entra.sample.json` and API **`ArchLucidAuth`** section):

1. Set **`NEXT_PUBLIC_ARCHLUCID_AUTH_MODE=jwt`** (or `jwt-bearer`).
2. Register a **single-page application** client in Entra; add redirect URI **`http://localhost:3000/auth/callback`** (and production origins).
3. Expose an API scope on the ArchLucid API app registration; grant the SPA **delegated** permission to that scope.
4. Set **`NEXT_PUBLIC_OIDC_AUTHORITY`**, **`NEXT_PUBLIC_OIDC_CLIENT_ID`**, and **`NEXT_PUBLIC_OIDC_SCOPES`** (must include `openid` and your API scope so the access token validates against the API audience).
5. Leave **`ARCHLUCID_API_KEY`** empty when using delegated user tokens — the proxy forwards **`Authorization: Bearer`** and omits the API key when a bearer token is present.

Sign-in flow: **`/auth/signin`** → IdP → **`/auth/callback`** → tokens in **sessionStorage** (short-lived access token; refresh when `offline_access` is granted).

## Run

Start the ArchLucid API (`ArchLucid.Api`), then:

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Tests

- **All unit/component tests:** `npm test` (or `npm run test:watch`). Pattern: `src/**/*.test.{ts,tsx}`.
- **55R / review workflow smoke:** see commands in [docs/TESTING_AND_TROUBLESHOOTING.md](docs/TESTING_AND_TROUBLESHOOTING.md#3-55r--review-workflow-smoke-tests-change-set-55r).
- **Vitest axe (components):** **`npm run test:axe-components`** — **`src/accessibility/**`** + **jest-axe** (CI job **`ui-axe-components`**).
- **57R / operator-journey E2E (Playwright, mock):** six specs in **`e2e/`** (non-`live-api-*`) — home smoke, run→manifest→back, compare flows, etc. (**`playwright.mock.config.ts`** via **`npm run test:e2e`**). **Live** API + SQL: default **`playwright.config.ts`** / **`npx playwright test`** — see [docs/LIVE_E2E_HAPPY_PATH.md](../docs/LIVE_E2E_HAPPY_PATH.md). Run mock: `npx playwright install --with-deps chromium` then **`npm run test:e2e`**. Full contract: [docs/TESTING_AND_TROUBLESHOOTING.md](docs/TESTING_AND_TROUBLESHOOTING.md#8-e2e-tests-playwright).
- **Repo root:** `test-ui-unit.cmd` / `test-ui-smoke.cmd` (or `.ps1` for Playwright + `npm ci`). Optional after full product smoke: **`.\release-smoke.ps1 -RunPlaywright`** (see repo [docs/RELEASE_SMOKE.md](../docs/RELEASE_SMOKE.md)).

## Routes

| Path | Purpose |
|------|---------|
| `/` | Home — start here, workflow links |
| `/runs?projectId=...` | List runs |
| `/runs/[runId]` | Run detail, manifest summary, artifacts, compare/replay shortcuts, downloads |
| `/manifests/[manifestId]` | Manifest summary, artifact list, bundle download |
| `/manifests/[manifestId]/artifacts/[artifactId]` | Artifact review (metadata + preview + siblings) |
| `/graph` | Provenance / architecture graph for a run |
| `/compare` | Compare two runs (structured + legacy + optional AI) |
| `/replay` | Replay authority chain for a run |
| `/auth/signin` | Start OIDC sign-in (JWT mode only) |
| `/auth/callback` | OAuth redirect handler (PKCE token exchange) |

Downloads use **`/api/proxy/...`** so the browser receives files without attaching `X-Api-Key` manually.

## API alignment

- Authority: `/api/authority/...`
- Artifacts: `/api/artifacts/...` — list returns `200` + array (empty allowed); bundle ZIP may return `404` when there is no bundle (distinct problem type from unknown manifest when the API is configured that way).
- Replay modes: `ReconstructOnly`, `RebuildManifest`, `RebuildArtifacts` (see `ArchLucid.Persistence.Replay.ReplayMode`).

## Auth

- **`NEXT_PUBLIC_ARCHLUCID_AUTH_MODE`**: `development-bypass` (default) matches the API’s development-bypass auth mode (no real sign-in; API authenticates a dev principal).
- For **`JwtBearer`** API mode, set `ARCHLUCID_API_KEY` only if you still use a gateway key; otherwise forward **`Authorization: Bearer`** from the browser (proxy passes it through) and implement `getBearerToken()` in `src/lib/api.ts`.
- Verify the API principal: `GET /api/auth/me` (requires Reader+), proxied as **`GET /api/proxy/api/auth/me`**.
- **UI read-model:** use **`src/lib/current-principal.ts`** (`loadCurrentPrincipal` / `getCurrentPrincipal` / `getCurrentAuthority` / `getCurrentAuthorityRank`) for name, roles, `maxAuthority` (`ReadAuthority` \| `ExecuteAuthority` \| `AdminAuthority`), `authorityRank`, and `hasEnterpriseOperatorSurfaces`. In the App Router shell, prefer **`useOperatorNavAuthority().currentPrincipal`** so identity stays aligned with nav filtering; do not re-scatter ad-hoc `/me` fetches.
