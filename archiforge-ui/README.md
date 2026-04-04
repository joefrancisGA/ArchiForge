# ArchiForge UI (operator shell)

Thin Next.js App Router UI for runs, manifest summary, **artifact review**, **graphs**, compare, replay, and ZIP downloads.

## Change Set 55R (operator workflow)

**End-to-end path:** Home → **Runs** → **Open run** → manifest summary & **Artifacts** table → **Review** (or manifest page) → preview + download → optional **Compare runs** / **Replay** / **Graph** from breadcrumbs or run actions.

- **Artifact review:** List (`[]` when empty), descriptor metadata, in-shell preview with raw disclosure, stable table order (name, then id — aligned with API).
- **Graph:** One run ID, multiple graph modes — for **visual** provenance/architecture, not two-run diff.
- **Compare / replay:** Two-run diff vs single-run authority replay — see [docs/operator-shell.md](../docs/operator-shell.md) in the repo root.

## Pilot feedback (58R)

**Nav:** **Pilot feedback** (not **Learning**, which is recommendation learning). Scoped dashboard, improvement opportunities, triage queue, Markdown/JSON export. Workflow: [docs/PRODUCT_LEARNING.md](../docs/PRODUCT_LEARNING.md).

## Documentation

| Document | What it covers |
|----------|---------------|
| [Pilot guide (56R)](../docs/PILOT_GUIDE.md) | **Pilots / design partners:** first run, artifacts, logs, readiness checks. |
| [Operator quickstart (56R)](../docs/OPERATOR_QUICKSTART.md) | Copy-paste commands (API, CLI, UI, tests). |
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

## Setup

```bash
cd archiforge-ui
npm install
cp .env.example .env.local
```

Edit `.env.local`:

- **`ARCHIFORGE_API_BASE_URL`** — ArchiForge API base (default in repo: `http://localhost:5128` per `ArchiForge.Api` launchSettings).
- **`ARCHIFORGE_API_KEY`** — Required when the API has `Authentication:ApiKey:Enabled` = `true`. Sent from the Next.js server (RSC + `/api/proxy`). Do not rely on public env for secrets in production; keep this server-only.

Optional:

- **`NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL`** — Fallback if `ARCHIFORGE_API_BASE_URL` is unset (documentation only; browser calls use `/api/proxy`).

### OIDC / JWT (Entra ID)

When the API uses **`ArchiForgeAuth:Mode = JwtBearer`** (see `ArchiForge.Api/appsettings.Entra.sample.json`):

1. Set **`NEXT_PUBLIC_ARCHIFORGE_AUTH_MODE=jwt`** (or `jwt-bearer`).
2. Register a **single-page application** client in Entra; add redirect URI **`http://localhost:3000/auth/callback`** (and production origins).
3. Expose an API scope on the ArchiForge API app registration; grant the SPA **delegated** permission to that scope.
4. Set **`NEXT_PUBLIC_OIDC_AUTHORITY`**, **`NEXT_PUBLIC_OIDC_CLIENT_ID`**, and **`NEXT_PUBLIC_OIDC_SCOPES`** (must include `openid` and your API scope so the access token validates against **`ArchiForgeAuth:Audience`**).
5. Leave **`ARCHIFORGE_API_KEY`** empty when using delegated user tokens — the proxy forwards **`Authorization: Bearer`** and omits the API key when a bearer token is present.

Sign-in flow: **`/auth/signin`** → IdP → **`/auth/callback`** → tokens in **sessionStorage** (short-lived access token; refresh when `offline_access` is granted).

## Run

Start the ArchiForge API, then:

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Tests

- **All unit/component tests:** `npm test` (or `npm run test:watch`). Pattern: `src/**/*.test.{ts,tsx}`.
- **55R / review workflow smoke:** see commands in [docs/TESTING_AND_TROUBLESHOOTING.md](docs/TESTING_AND_TROUBLESHOOTING.md#3-55r--review-workflow-smoke-tests-change-set-55r).
- **57R / operator-journey E2E (Playwright):** six specs in **`e2e/`** — home smoke, run→manifest→back, manifest empty artifact list, compare prefill + review order, compare stale-input warning, compare + AI explain (all **mock-backed**; no live C# API). Run: `npx playwright install --with-deps chromium` then **`npm run test:e2e`**. Full contract: [docs/TESTING_AND_TROUBLESHOOTING.md](docs/TESTING_AND_TROUBLESHOOTING.md#8-e2e-tests-playwright).
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
- Replay modes: `ReconstructOnly`, `RebuildManifest`, `RebuildArtifacts` (see `ArchiForge.Persistence.Replay.ReplayMode`).

## Auth

- **`NEXT_PUBLIC_ARCHIFORGE_AUTH_MODE`**: `development-bypass` (default) matches the API’s `ArchiForgeAuth:Mode` = `DevelopmentBypass` (no real sign-in; API authenticates a dev principal).
- For **`JwtBearer`** API mode, set `ARCHIFORGE_API_KEY` only if you still use a gateway key; otherwise forward **`Authorization: Bearer`** from the browser (proxy passes it through) and implement `getBearerToken()` in `src/lib/api.ts`.
- Verify the API principal: `GET /api/auth/me` (requires Reader+).
