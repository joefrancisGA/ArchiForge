# ArchLucid operator shell (Change Set 55R)

**Audience:** Internal operators and design partners using the thin Next.js UI in `archlucid-ui/` against the ArchLucid API.

---

## What it is

A read-focused **operator shell**: inspect runs, manifests, and synthesized artifacts; compare runs; replay authority chains; explore provenance/architecture graphs; download ZIPs. It is not a replacement for Swagger or the CLI.

---

## Main workflow

1. **Start** — Open the app root (`/`). First-time users: expand **First-run workflow** on Home for a guided checklist (create run → pipeline → commit → artifacts → compare/replay → export); **Hide guide** collapses it (preference in browser `localStorage`). Use the header **Start & review** group anytime: **Home**, **Runs**, **Graph**, **Compare runs**, **Replay run**.
2. **Runs** — `Runs` → pick a project (default `default`) → **Open run** on a row (empty list shows **Create your first run (wizard)**).
3. **Run detail** — After commit, you see manifest summary, **Artifacts** (table with **Review** / **Download**), and shortcuts to **Compare** (base = this run) and **Replay**.
4. **Manifest / artifact** — From the golden manifest link or **Review**, you land on manifest-scoped or artifact review pages: metadata, in-shell preview (when available), raw disclosure, sibling artifact list.
5. **Compare / replay** — **Compare runs**: enter base (left) and target (right) run IDs; structured manifest deltas first, then legacy flat diff; optional AI explanation. **Replay run**: pick mode and read validation flags/notes.
6. **Graph** — Enter a **run ID** (from Runs or run detail), choose a view (full provenance, decision subgraph, neighborhood, architecture), **Load graph**. Use this when you need a **visual** graph, not the tabular compare flow.

Breadcrumb links on key pages tie **Home · Runs · Graph · Compare** together.

---

## Artifact review

- **List:** `GET api/artifacts/manifests/{manifestId}` returns a **JSON array** (possibly empty) when the manifest exists in scope. Rows are ordered **by name, then artifact id** (deterministic for UI and ZIP).
- **Descriptor:** `GET …/artifact/{id}/descriptor` — metadata only for the review header (type, format, hash, timestamps).
- **Preview:** The UI fetches bytes through the same-origin proxy for a truncated UTF-8 preview; **Download** uses the full file. If preview fails, metadata and download may still succeed.
- **Bundle vs list:** If the manifest exists but there is **no bundle or zero artifacts**, the **list** returns `[]`; **bundle ZIP** returns **404** with a distinct problem type from **unknown manifest** (see API expectations below).

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
- **Empty vs missing:** **Runs list** can be `[]`. **Artifact list** for a valid manifest can be `[]`. **404** on run/manifest/artifact routes should carry **ProblemDetails** (`title` / `detail` / `type`) where the API provides them.
- **Artifact bundle 404:** Prefer distinguishing **manifest not in scope** (`manifest-not-found`) from **no bundle / zero artifacts** (`resource-not-found`) when interpreting bundle download failures next to an empty artifact table.
- **Ordering:** Treat artifact list and ZIP entry order as **stable** (name, then id) for screenshots and diffs.

Deeper UI architecture: [archlucid-ui/docs/ARCHITECTURE.md](../archlucid-ui/docs/ARCHITECTURE.md). Tutorial: [archlucid-ui/docs/OPERATOR_SHELL_TUTORIAL.md](../archlucid-ui/docs/OPERATOR_SHELL_TUTORIAL.md).
