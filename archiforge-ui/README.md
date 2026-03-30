# ArchiForge UI (operator shell)

Thin Next.js App Router UI for runs, manifest summary, artifacts, compare, replay, and ZIP downloads.

## Documentation

| Document | What it covers |
|----------|---------------|
| [Architecture](docs/ARCHITECTURE.md) | **Architecture document.** System context, container view, component breakdown, data flow, security model, operational considerations, and architectural decisions with trade-offs. |
| [Operator Shell Tutorial](docs/OPERATOR_SHELL_TUTORIAL.md) | **Start here for learning.** Full tutorial for back-end developers new to React/Next.js. Covers concepts, architecture, every route, and common tasks. |
| [C# to React Rosetta Stone](docs/CSHARP_TO_REACT_ROSETTA.md) | **Read second.** Side-by-side C# and React/TypeScript code for every pattern used in the codebase. |
| [Annotated Page Walkthrough](docs/ANNOTATED_PAGE_WALKTHROUGH.md) | **Read third.** Every line of `runs/page.tsx` explained, with C# equivalents and reasoning. |
| [Component Reference](docs/COMPONENT_REFERENCE.md) | Detailed reference for every React component, prop, and helper library. |
| [Data Flow and State](docs/DATA_FLOW_AND_STATE.md) | How data moves from the C# API to the screen. State management patterns. Diagrams for every page. Templates for adding new pages. |
| [Testing and Troubleshooting](docs/TESTING_AND_TROUBLESHOOTING.md) | How to run and write tests. Common errors and fixes. Debugging techniques. |

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

## Run

Start the ArchiForge API, then:

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Tests

- **Unit (Vitest, jsdom):** `npm ci` then `npm test` (or `npm run test:watch`). Specs: `src/**/*.test.{ts,tsx}`.
- **E2E smoke (Playwright):** `npx playwright install --with-deps chromium` then `npm run test:e2e`.
- **Repo root:** `test-ui-unit.cmd` / `test-ui-smoke.cmd` (or `.ps1`) from the ArchiForge solution directory.

## Routes

| Path | Purpose |
|------|---------|
| `/` | Home / quick links |
| `/runs?projectId=...` | List runs |
| `/runs/[runId]` | Run detail, manifest summary, artifacts, downloads |
| `/manifests/[manifestId]` | Manifest + artifact list + bundle download |
| `/compare` | Compare two runs (client) |
| `/replay` | Replay run (client) |

Downloads use **`/api/proxy/...`** so the browser receives files without attaching `X-Api-Key` manually.

## API alignment

- Authority: `/api/authority/...`
- Artifacts: `/api/artifacts/...`
- Replay modes: `ReconstructOnly`, `RebuildManifest`, `RebuildArtifacts` (see `ArchiForge.Persistence.Replay.ReplayMode`).

## Auth

- **`NEXT_PUBLIC_ARCHIFORGE_AUTH_MODE`**: `development-bypass` (default) matches the API’s `ArchiForgeAuth:Mode` = `DevelopmentBypass` (no real sign-in; API authenticates a dev principal).
- For **`JwtBearer`** API mode, set `ARCHIFORGE_API_KEY` only if you still use a gateway key; otherwise forward **`Authorization: Bearer`** from the browser (proxy passes it through) and implement `getBearerToken()` in `src/lib/api.ts`.
- Verify the API principal: `GET /api/auth/me` (requires Reader+).
