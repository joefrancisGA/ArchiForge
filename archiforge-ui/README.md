# ArchiForge UI (operator shell)

Thin Next.js App Router UI for runs, manifest summary, artifacts, compare, replay, and ZIP downloads.

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
