# ArchLucid Operator Shell — Front-End Tutorial

> **Audience:** Back-end developers who know C# / SQL / HTTP APIs but are new to React and Next.js.
> Read this start-to-finish the first time; use the table of contents as a reference later.

**Operators and design partners:** For a short **workflow-first** guide (55R), artifact review, graph vs compare/replay, and API expectations — see **[docs/operator-shell.md](../../docs/library/operator-shell.md)** (repo root).

---

## Table of contents

1. [Why does this project exist?](#1-why-does-this-project-exist)
2. [Technology stack in 90 seconds](#2-technology-stack-in-90-seconds)
3. [Project structure](#3-project-structure)
4. [How Next.js App Router works (for back-end devs)](#4-how-nextjs-app-router-works-for-back-end-devs)
5. [Server Components vs Client Components](#5-server-components-vs-client-components)
6. [How the UI calls the API](#6-how-the-ui-calls-the-api)
7. [The proxy route and why it exists](#7-the-proxy-route-and-why-it-exists)
8. [Type system: how TS types mirror C# DTOs](#8-type-system-how-ts-types-mirror-c-dtos)
9. [Response guards: coerce functions](#9-response-guards-coerce-functions)
10. [Shared UI callouts: the OperatorShellMessage pattern](#10-shared-ui-callouts-the-operatorshellmessage-pattern)
11. [Route-by-route walkthrough](#11-route-by-route-walkthrough)
12. [Artifact review workflow](#12-artifact-review-workflow)
13. [Compare / replay flow](#13-compare--replay-flow)
14. [Graph viewer](#14-graph-viewer)
15. [Loading states: loading.tsx files](#15-loading-states-loadingtsx-files)
16. [Testing](#16-testing)
17. [Configuration and environment](#17-configuration-and-environment)
18. [Common tasks for back-end developers](#18-common-tasks-for-back-end-developers)
19. [Keyboard shortcuts](#19-keyboard-shortcuts)
20. [Glossary](#20-glossary)

---

## 1. Why does this project exist?

The **operator shell** (`archlucid-ui`) is a thin, read-mostly UI that lets an operator:

- browse architecture runs for a project,
- inspect golden manifests (decisions, warnings, unresolved issues),
- review synthesized artifacts (markdown narratives, Mermaid diagrams, JSON inventories, etc.),
- compare two runs side by side (structured + legacy + AI explanation),
- replay an authority chain and see validation results,
- explore provenance and architecture graphs.

It is **not** a full SPA with rich client-side routing and state management. It is closer to a server-rendered dashboard with a few interactive pages. Most pages fetch data on the server and send static HTML to the browser.

---

## 2. Technology stack in 90 seconds

| Layer | Tool | C# analogy |
|-------|------|------------|
| **Framework** | [Next.js 15](https://nextjs.org/) (App Router) | ASP.NET Core MVC (Razor Pages) |
| **Language** | TypeScript 5 | C# |
| **UI library** | React 19 | Razor tag helpers + partial views |
| **Package manager** | npm | NuGet |
| **Unit tests** | Vitest + Testing Library | xUnit + bUnit |
| **E2E tests** | Playwright | Playwright (same!) |
| **Graph rendering** | React Flow | No direct analogy; think of a canvas library |

### Mental model

- A **React component** is like a Razor partial view that returns HTML. It is a TypeScript function that returns JSX (HTML-like syntax inside TypeScript).
- **Props** are the parameters you pass into a component, like a view model in MVC.
- **State** (`useState`) is a variable that, when changed, causes React to re-render that component. There is nothing like this in server-side MVC; the closest analog is a WPF/MAUI observable property.
- **`useEffect`** runs side-effect code after the component renders (e.g. reading URL parameters). Think of it as an "on-render" callback.

---

## 3. Project structure

```
archlucid-ui/
├── package.json              ← dependencies + scripts
├── tsconfig.json             ← TypeScript config (strict, path alias @/*)
├── next.config.ts            ← Next.js config
│
├── src/
│   ├── app/                  ← ROUTES live here (file-system routing)
│   │   ├── layout.tsx        ← Root layout (shell header + nav)
│   │   ├── page.tsx          ← Home page  →  /
│   │   ├── loading.tsx       ← Loading fallback for /
│   │   │
│   │   ├── runs/
│   │   │   ├── page.tsx      ← /runs?projectId=…
│   │   │   ├── loading.tsx
│   │   │   └── [runId]/
│   │   │       ├── page.tsx  ← /runs/{runId}
│   │   │       ├── loading.tsx
│   │   │       └── artifacts/[artifactId]/
│   │   │           ├── page.tsx   ← /runs/{runId}/artifacts/{artifactId}
│   │   │           └── loading.tsx
│   │   │
│   │   ├── manifests/[manifestId]/
│   │   │   ├── page.tsx      ← /manifests/{manifestId}
│   │   │   ├── loading.tsx
│   │   │   └── artifacts/[artifactId]/
│   │   │       ├── page.tsx  ← /manifests/{manifestId}/artifacts/{artifactId}
│   │   │       └── loading.tsx
│   │   │
│   │   ├── graph/page.tsx    ← /graph          (client component)
│   │   ├── compare/page.tsx  ← /compare        (client component)
│   │   ├── replay/page.tsx   ← /replay         (client component)
│   │   │
│   │   └── api/proxy/[...path]/route.ts  ← API proxy (Next.js route handler)
│   │
│   ├── components/           ← Reusable UI pieces
│   │   ├── OperatorShellMessage.tsx   ← Error/empty/loading/malformed callouts
│   │   ├── ArtifactListTable.tsx      ← Artifact table (run + manifest pages)
│   │   ├── ArtifactReviewContent.tsx  ← Pretty + raw preview panel
│   │   ├── GraphViewer.tsx            ← React Flow wrapper
│   │   └── compare/                   ← Comparison sub-components
│   │       ├── StructuredComparisonView.tsx
│   │       ├── LegacyRunComparisonView.tsx
│   │       └── AiComparisonExplanationView.tsx
│   │
│   ├── lib/                  ← Pure functions + API wrappers
│   │   ├── api.ts                     ← All HTTP fetch functions
│   │   ├── api-error.ts               ← ProblemDetails → string
│   │   ├── graph-api.ts               ← Graph-specific fetches
│   │   ├── operator-response-guards.ts ← coerce* shape validators
│   │   ├── artifact-review-helpers.ts  ← Type labels, body formatting
│   │   ├── config.ts                  ← Server API base URL
│   │   ├── scope.ts                   ← Dev scope headers
│   │   └── auth-config.ts             ← Auth mode env var
│   │
│   └── types/                ← TypeScript type definitions (like C# DTOs)
│       ├── authority.ts      ← RunSummary, ManifestSummary, ArtifactDescriptor, …
│       ├── graph.ts          ← GraphViewModel, GraphNodeVm, GraphEdgeVm
│       ├── comparison.ts     ← GoldenManifestComparison deltas
│       └── explanation.ts    ← AI explanation response
```

### Key rule: one route = one `page.tsx`

In ASP.NET, you register routes in `Program.cs` or use `[Route]` attributes. In Next.js App Router, **the folder path IS the route**. A file at `src/app/runs/[runId]/page.tsx` handles `GET /runs/{any-guid}` — same idea as `[HttpGet("runs/{runId}")]` in a controller.

---

## 4. How Next.js App Router works (for back-end devs)

### File-system routing

| File path | URL | ASP.NET equivalent |
|-----------|-----|-------------------|
| `app/page.tsx` | `/` | `[HttpGet("/")]` |
| `app/runs/page.tsx` | `/runs` | `[HttpGet("runs")]` |
| `app/runs/[runId]/page.tsx` | `/runs/{runId}` | `[HttpGet("runs/{runId:guid}")]` |
| `app/api/proxy/[...path]/route.ts` | `/api/proxy/*` | Catch-all middleware / reverse proxy |

Square brackets `[runId]` are route parameters (like `{runId}` in ASP.NET). The triple-dot `[...path]` is a catch-all (like `{**path}`).

### What runs where

| Context | What runs | Analogy |
|---------|-----------|---------|
| **Server** (Node.js) | `page.tsx` without `"use client"`, `loading.tsx`, `layout.tsx`, `route.ts` | Razor view rendering on server |
| **Browser** (user's computer) | Files that start with `"use client"` | JavaScript in a Razor `@section Scripts` |

When you open `/runs`, Next.js:
1. Runs `app/runs/page.tsx` **on the server** (Node.js process).
2. That function calls `listRunsByProject()` which calls `fetch()` to the C# API.
3. Returns the finished HTML to the browser.
4. The browser shows it immediately — no spinner, no JavaScript needed for the first paint.

This is **Server-Side Rendering (SSR)** and it is why most of our pages do not need `"use client"`.

### When do we use `"use client"`?

Only when the page needs **browser interactivity**: button clicks that change what is shown, form inputs, or browser-only APIs like `useSearchParams()`.

In this codebase, these are client pages:
- `/graph` — user types a run ID, picks a mode, clicks "Load graph"
- `/compare` — user types two run IDs, clicks "Compare"
- `/replay` — user types a run ID, clicks "Replay"

Everything else (runs list, run detail, manifest, artifact review) is **server-rendered**.

---

## 5. Server Components vs Client Components

This is the single most confusing thing for back-end developers coming to React. Here is the rule:

### Server Component (default)

```tsx
// No "use client" at the top → this runs on the Node.js server, never in the browser.

export default async function RunsPage() {
  // You can use `await` directly — this is an async function, like a controller action.
  const runs = await listRunsByProject("default", 20);

  return (
    <main>
      <h2>Runs</h2>
      {runs.map(run => <p key={run.runId}>{run.runId}</p>)}
    </main>
  );
}
```

**Key facts:**
- Can use `await` (it is a normal async function).
- Can access `process.env` (server-side environment variables).
- Cannot use `useState`, `useEffect`, `onClick`, or any browser API.
- Runs once per request (like a Razor page).

### Client Component

```tsx
"use client";  // ← This directive is REQUIRED. Without it, the file is a server component.

import { useState } from "react";

export default function GraphPage() {
  const [runId, setRunId] = useState("");  // state lives in the browser

  return (
    <main>
      <input value={runId} onChange={(e) => setRunId(e.target.value)} />
      <button onClick={() => alert(runId)}>Go</button>
    </main>
  );
}
```

**Key facts:**
- Runs in the **browser** (after the page loads).
- Can use `useState`, `useEffect`, `onClick`, etc.
- Cannot use `await` at the top level of the function (you would use `useEffect` or call an `async` helper).
- Cannot access `process.env` server-side secrets.

### How to choose

| Need | Use |
|------|-----|
| Just show data from the API | Server Component (no directive) |
| User types in a form or clicks a button that changes what is shown | Client Component (`"use client"`) |
| Download link or navigation link (no state change) | Server Component is fine |

---

## 6. How the UI calls the API

All HTTP calls go through `src/lib/api.ts`. The two core functions are:

```
apiGet<T>(path)       → fetches JSON from the ArchLucid API and returns T
apiPostJson<T>(path, body) → POSTs JSON and returns T
```

### Server-side fetch (RSC pages like `/runs`)

```
Browser → GET /runs
           ↓
  Next.js server runs page.tsx
           ↓
  page.tsx calls listRunsByProject()
           ↓
  listRunsByProject() calls apiGet("/api/authority/projects/default/runs")
           ↓
  apiGet() uses resolveRequest() which sees isBrowser() === false:
    - builds URL: http://localhost:5128/api/authority/projects/default/runs
    - adds X-Api-Key header (from process.env.ARCHLUCID_API_KEY)
    - adds scope headers (x-tenant-id, x-workspace-id, x-project-id)
           ↓
  C# API responds with JSON
           ↓
  Next.js renders HTML, sends to browser
```

### Client-side fetch (pages like `/compare`)

```
Browser JavaScript calls compareRuns(leftRunId, rightRunId)
           ↓
  compareRuns() calls apiGet("/api/authority/compare/runs?leftRunId=…&rightRunId=…")
           ↓
  apiGet() uses resolveRequest() which sees isBrowser() === true:
    - builds URL: /api/proxy/api/authority/compare/runs?…   ← same-origin proxy!
    - does NOT add X-Api-Key (that stays on the server)
           ↓
  Browser fetches /api/proxy/…
           ↓
  Next.js proxy route (src/app/api/proxy/[...path]/route.ts)
    - adds X-Api-Key
    - adds scope headers
    - forwards to http://localhost:5128/api/authority/compare/runs?…
           ↓
  C# API responds → proxy passes response back → browser JavaScript receives JSON
```

### Why two paths?

**Security.** The `ARCHLUCID_API_KEY` is a server-side secret. Server components can use it directly. Browser code cannot — it would be visible in DevTools. So browser code goes through `/api/proxy`, which adds the key on the server.

This is exactly the same pattern as a BFF (Backend-for-Frontend) in ASP.NET.

---

## 7. The proxy route and why it exists

File: `src/app/api/proxy/[...path]/route.ts`

This is a Next.js **Route Handler** — the equivalent of a minimal API endpoint in ASP.NET (`app.MapGet`). It:

1. Receives any `GET` or `POST` to `/api/proxy/*`.
2. Strips the `/api/proxy` prefix.
3. Forwards the request to `ARCHLUCID_API_BASE_URL` with:
   - `X-Api-Key` from environment
   - Scope headers (`x-tenant-id`, etc.)
   - Authorization header from the browser (if present)
4. Returns the response verbatim (including `Content-Type` and `Content-Disposition` for file downloads).

**You rarely need to change this file.** If you add a new API endpoint, the proxy forwards it automatically because of the `[...path]` catch-all.

---

## 8. Type system: how TS types mirror C# DTOs

Every C# response DTO has a matching TypeScript type in `src/types/`.

| C# DTO | TS type | File |
|--------|---------|------|
| `RunSummaryResponse` | `RunSummary` | `types/authority.ts` |
| `ManifestSummaryResponse` | `ManifestSummary` | `types/authority.ts` |
| `ArtifactDescriptorResponse` | `ArtifactDescriptor` | `types/authority.ts` |
| `RunComparisonResponse` | `RunComparison` | `types/authority.ts` |
| `ReplayResponse` / `ReplayValidationResponse` | `ReplayResponse` / `ReplayValidation` | `types/authority.ts` |
| `ComparisonResult` (structured delta) | `GoldenManifestComparison` | `types/comparison.ts` |
| `GraphViewModel` | `GraphViewModel` | `types/graph.ts` |
| `ComparisonExplanation` | `ComparisonExplanation` | `types/explanation.ts` |

### How to add a new field from the API

Say you add `public string Severity { get; set; }` to `ArtifactDescriptorResponse` in C#.

1. Open `src/types/authority.ts`.
2. Add `severity?: string;` to the `ArtifactDescriptor` type (optional with `?` because older API versions might not have it).
3. If the UI needs to validate it, add a check in the matching coerce function in `operator-response-guards.ts`.
4. Use it in the page/component: `descriptor.severity ?? "—"`.

That is it. No code generation, no reflection. Just keep the TS type in sync manually.

---

## 9. Response guards: coerce functions

File: `src/lib/operator-response-guards.ts`

The API returns JSON. JSON has no compile-time types. A `fetch()` call returns `unknown` at the TypeScript level. We could cast directly:

```ts
const data = await apiGet<RunSummary[]>("/api/authority/projects/default/runs");
// TypeScript trusts us, but if the API shape changes, we get a runtime crash.
```

Instead, on critical pages we **coerce** the response:

```ts
const raw: unknown = await apiGet("/api/authority/projects/default/runs");
const coerced = coerceRunSummaryList(raw);

if (!coerced.ok) {
  // Show OperatorMalformedCallout with coerced.message
  return;
}

const runs: RunSummary[] = coerced.items;  // safe to use
```

Each coerce function checks the actual runtime shape (is it an array? does each row have a string `runId`?) and returns either `{ ok: true, ... }` or `{ ok: false, message: "..." }`.

**This is the equivalent of model validation in ASP.NET**, but applied to API responses on the client side.

### When to use coerce vs direct cast

| Situation | Approach |
|-----------|----------|
| Server component rendering a full page (error = blank page) | **Always coerce** |
| Client component showing a secondary section (error = warning) | Coerce if shape matters; direct cast is acceptable for non-critical display |
| Pure helper / mapper function | Direct cast (the caller already validated) |

---

## 10. Shared UI callouts: the OperatorShellMessage pattern

File: `src/components/OperatorShellMessage.tsx`

Every page uses the same set of status components. They are simple styled `<div>` elements with consistent borders, colors, and ARIA roles.

| Component | Purpose | Color | When to use |
|-----------|---------|-------|-------------|
| `OperatorErrorCallout` | Failed HTTP request or missing resource | Red border, light red background | `catch` block after `fetch` |
| `OperatorWarningCallout` | Non-fatal secondary failure | Amber border, light yellow background | e.g. artifact list failed but run detail is fine |
| `OperatorMalformedCallout` | JSON shape did not match expectations | Purple border, light purple background | `coerced.ok === false` |
| `OperatorEmptyState` | Valid empty result (zero rows) or idle state | Gray border, light gray background | Empty arrays, "enter an ID first" |
| `OperatorLoadingNotice` | Work in progress | Slate border, light slate background | While fetching (client) or route `loading.tsx` |

### Usage pattern (every page follows this)

```tsx
{loadError && (
  <OperatorErrorCallout>
    <strong>Could not load runs.</strong>
    <p>{loadError}</p>
  </OperatorErrorCallout>
)}

{malformedMessage && (
  <OperatorMalformedCallout>
    <strong>Response shape was not usable.</strong>
    <p>{malformedMessage}</p>
  </OperatorMalformedCallout>
)}

{!loadError && !malformedMessage && items.length === 0 && (
  <OperatorEmptyState title="No items">
    <p>Explanation of why this is empty and what to do.</p>
  </OperatorEmptyState>
)}

{items.length > 0 && (
  <table>...</table>
)}
```

The order matters: **error → malformed → empty → data**. This is deterministic — exactly one state is visible.

---

## 11. Route-by-route walkthrough

### `/` — Home

**File:** `app/page.tsx` (server component)  
**What it does:** Static landing page with quick links. No API calls.  
**States:** `OperatorEmptyState` explaining this page is static.

### `/runs/new` — First-run wizard (create architecture request)

**File:** `app/runs/new/page.tsx` (server shell) + `NewRunWizardClient.tsx` (**client component** — form state, stepper, `POST` via proxy).  
**What it does:** Seven-step wizard (preset → identity → description → constraints → advanced → review → pipeline tracking) that posts **`/v1/architecture/request`** with the full **`ArchitectureRequest`** surface and polls run summary on the last step.  
**Operator doc (purpose, field mapping, troubleshooting):** [docs/FIRST_RUN_WIZARD.md](../../docs/FIRST_RUN_WIZARD.md) in the repo root.

### `/runs?projectId=default` — Run list

**File:** `app/runs/page.tsx` (server component)  
**API call:** `listRunsByProject(projectId, take)` → `GET /api/authority/projects/{projectId}/runs`  
**Response guard:** `coerceRunSummaryList`  
**States:**
- Loading → `app/runs/loading.tsx`
- Error → `OperatorErrorCallout`
- Malformed → `OperatorMalformedCallout`
- Empty → `OperatorEmptyState` ("no runs in this project")
- Data → table with Run ID, Description, Created, "Open" link

### `/runs/{runId}` — Run detail

**File:** `app/runs/[runId]/page.tsx` (server component)  
**API calls (in order):**
1. `getRunDetail(runId)` → `{ data: run envelope, traceId }` (trace id from **`X-Trace-Id`** on the same response)
2. `getManifestSummary(manifestId)` → if manifest exists
3. `listArtifacts(manifestId)` → if manifest exists  
**Response guards:** `coerceRunDetail`, `coerceManifestSummary`, `coerceArtifactDescriptorList`  
**Trace viewer:** when **`NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE`** is set (see repo **`.env.example`**), **`RunTraceViewerLink`** shows **View trace** plus a short id preview and copy control.  
**States:**
- Run failed → full-page `OperatorErrorCallout` with back link
- Run malformed → full-page `OperatorMalformedCallout`
- No manifest → `OperatorEmptyState` ("commit the run first")
- Manifest summary failed → `OperatorWarningCallout` (non-blocking)
- Manifest malformed → `OperatorMalformedCallout`
- Artifacts failed / malformed / empty → respective callouts
- Success → run metadata, authority chain, manifest summary, artifact table, download links

### `/manifests/{manifestId}` — Manifest detail

**File:** `app/manifests/[manifestId]/page.tsx` (server component)  
**API calls:** `getManifestSummary`, `listArtifacts`  
**States:** Same pattern as run detail but manifest-focused.

### `/manifests/{manifestId}/artifacts/{artifactId}` — Artifact review

**File:** `app/manifests/[manifestId]/artifacts/[artifactId]/page.tsx` (server component)  
**API calls:**
1. `getArtifactDescriptor(manifestId, artifactId)` → metadata
2. `fetchArtifactContentUtf8(manifestId, artifactId)` → file body as text
3. `listArtifacts(manifestId)` → sibling list  
**Components:** `ArtifactReviewContent` (pretty + raw view), `ArtifactListTable` (sibling navigation)  
**Helpers:** `artifact-review-helpers.ts` (type labels, JSON pretty-print, view classification)

### `/runs/{runId}/artifacts/{artifactId}` — Run-scoped artifact entry

**File:** `app/runs/[runId]/artifacts/[artifactId]/page.tsx` (server component)  
**What it does:** Loads the run, finds `goldenManifestId`, then `redirect()` to the canonical manifest artifact URL. Provides error/empty states if the run is missing or has no manifest.

### `/graph` — Graph viewer

**File:** `app/graph/page.tsx` (**client component** — `"use client"`)  
See [Section 14](#14-graph-viewer).

### `/compare` — Run comparison

**File:** `app/compare/page.tsx` (**client component**)  
See [Section 13](#13-compare--replay-flow).

### `/replay` — Authority replay

**File:** `app/replay/page.tsx` (**client component**)  
See [Section 13](#13-compare--replay-flow).

---

## 12. Artifact review workflow

This is the end-to-end operator flow for reviewing synthesized artifacts.

### Step 1: Find a run with artifacts

Navigate to **Runs** → open a run → scroll to **Artifacts** section.

If the run has a golden manifest, the artifacts section shows a table:

| Column | Source |
|--------|--------|
| Artifact (file name) | `descriptor.name` (e.g. `architecture-narrative.md`) |
| Type | `getArtifactTypeLabel(descriptor.artifactType)` (e.g. "Architecture narrative") |
| Format | `descriptor.format` (e.g. `markdown`, `json`, `mermaid`) |
| Created | `descriptor.createdUtc` |
| Hash (short) | First 8 chars of `descriptor.contentHash` |
| Actions | **Review** (link) + **Download** (raw file) |

### Step 2: Open an artifact for review

Click **Review**. This opens `/manifests/{manifestId}/artifacts/{artifactId}`.

### Step 3: Understand the artifact

The review page has three sections:

**a) "What this artifact is"** — a boxed panel with:
- Human-readable description (from `ARTIFACT_TYPE_COPY` in `artifact-review-helpers.ts`)
- Metadata: type, API type key, format, artifact ID, content hash, created UTC

**b) "Content preview"** — the file body, rendered according to its format:
- **Markdown** → pre-wrapped text (download for a real Markdown editor)
- **JSON** → pretty-printed with 2-space indent (invalid JSON falls back to raw)
- **Mermaid** → raw source (paste into mermaid.live or similar)
- **Other** → plain text

Below the readable view, a `<details>` disclosure shows **"Raw UTF-8 content"**. If the readable and raw are different (e.g. JSON was pretty-printed), the raw section says "(exact, unmodified from API)".

If the file is larger than 2 MB, a warning says "Preview truncated; download for full file."

If the content fetch failed, a warning shows the error and suggests downloading.

**c) "Artifacts in this manifest"** — same table as the run/manifest page, with the current row highlighted. Click another row to navigate directly.

### Supported artifact types

All nine types from `ArchLucid.ArtifactSynthesis.Models.ArtifactType`:

| API type key | UI label | Format |
|---|---|---|
| `ReferenceArchitectureMarkdown` | Reference architecture (Markdown) | markdown |
| `ArchitectureNarrative` | Architecture narrative | markdown |
| `DiagramAst` | Diagram AST (JSON) | json |
| `MermaidDiagram` | Mermaid diagram | mermaid |
| `Inventory` | Inventory | json |
| `CostSummary` | Cost summary | json |
| `ComplianceMatrix` | Compliance matrix | json |
| `CoverageSummary` | Coverage summary | json |
| `UnresolvedIssuesReport` | Unresolved issues | json |

Unknown types fall back to PascalCase splitting (e.g. `FooBar` → "Foo Bar") and a generic description.

---

## 13. Compare / replay flow

### Compare (`/compare`)

This is a **client component** because the operator types two run IDs and clicks buttons.

**State variables (think of these as form fields + results):**
- `leftRunId`, `rightRunId` — text inputs
- `loading` — true while API calls are in flight
- `result` — legacy `RunComparison` (flat diffs)
- `golden` — structured `GoldenManifestComparison` (decision/requirement/security/topology/cost deltas)
- `aiExplanation` — LLM-generated narrative
- `error`, `goldenError`, `aiError` — HTTP failures
- `legacyMalformed`, `goldenMalformed`, `aiMalformed` — bad JSON shapes

**Flow:**
1. Operator enters two run IDs (or they come from URL params like `?leftRunId=...&rightRunId=...`).
2. Clicks **Compare** → calls two endpoints in parallel:
   - `compareRuns(left, right)` → legacy flat diff
   - `compareGoldenManifestRuns(left, right)` → structured delta
3. Results are validated with coerce functions.
4. Displayed via three extracted components:
   - `StructuredComparisonView` — tables for each delta section
   - `LegacyRunComparisonView` — run-level + manifest flat diffs
   - `AiComparisonExplanationView` — summary, major changes, tradeoffs, narrative

### Replay (`/replay`)

Another **client component**. Operator enters a run ID, picks a mode, clicks **Replay**.

**Modes:** `ReconstructOnly`, `RebuildManifest`, `RebuildArtifacts` (matching `ArchLucid.Persistence.Replay.ReplayMode`).

**Response:** Validation flags (context/graph/findings/manifest/trace/artifacts present, manifest hash matches, artifact bundle present after replay) + notes list.

The result uses a structured `<dl>` grid for validation flags and a list for notes.

---

## 14. Graph viewer

**File:** `app/graph/page.tsx` (client) + `components/GraphViewer.tsx` (client)

### How it works

1. Operator enters a run ID and selects a graph mode:
   - Full provenance graph
   - Decision subgraph (needs a decision ID)
   - Node neighborhood (needs a node ID + depth)
   - Architecture graph
2. Clicks **Load graph** → calls the matching `graph-api.ts` function.
3. Response is coerced with `coerceGraphViewModel`.
4. Passed to `<GraphViewer>`, which:
   - Optionally filters by node type
   - Maps nodes/edges to React Flow format (`graph-mapper.ts`)
   - Renders an interactive graph with pan, zoom, minimap, and controls
   - Shows a detail panel for the selected node (id, label, type, metadata)

### States in the graph page

| State | What shows |
|-------|-----------|
| Idle (no graph loaded) | `OperatorEmptyState` |
| Loading | `OperatorLoadingNotice` |
| HTTP error | `OperatorErrorCallout` |
| Malformed response | `OperatorMalformedCallout` |
| Empty graph (0 nodes) | `OperatorEmptyState` inside `GraphViewer` |
| Filtered empty (type filter hides all nodes) | Different `OperatorEmptyState` copy |
| Graph loaded | React Flow canvas + detail panel |

---

## 15. Loading states: loading.tsx files

In Next.js App Router, if you put a `loading.tsx` file next to a `page.tsx`, Next.js shows it **while the page is loading** (for server components) or **while the JavaScript bundle is downloading** (for client components).

Every route in the operator shell has one:

```
app/loading.tsx                          → "Loading."
app/runs/loading.tsx                     → "Loading runs."
app/runs/[runId]/loading.tsx             → "Loading run detail."
app/manifests/[manifestId]/loading.tsx   → "Loading manifest."
app/manifests/.../artifacts/.../loading.tsx → "Loading artifact review."
app/graph/loading.tsx                    → "Loading graph viewer."
app/compare/loading.tsx                  → "Loading compare."
app/replay/loading.tsx                   → "Loading replay."
```

They all use `OperatorLoadingNotice` — a calm, borderless status card with no spinner or animation.

---

## 16. Testing

### Unit tests (Vitest)

```bash
cd archlucid-ui
npm test          # runs once
npm run test:watch  # re-runs on file changes
```

Tests use **Testing Library** (`@testing-library/react`), which renders components in a simulated DOM (`jsdom`) and lets you query by role, text, etc.

**What is tested:**
| File | What |
|------|------|
| `page.test.tsx` | Home page renders heading + links |
| `GraphViewer.test.tsx` | Empty states for no-nodes and filtered-empty |
| `ArtifactListTable.test.tsx` | Review links use manifest vs run-scoped URLs |
| `operator-response-guards.test.ts` | All coerce functions (valid + invalid shapes) |
| `artifact-review-helpers.test.ts` | View classification, labels, JSON pretty-print |
| `graph-mapper.test.ts` | Node/edge mapping to React Flow format |
| `api-error.test.ts` | ProblemDetails parsing |
| `config.test.ts` | Server URL resolution |
| `SectionCard.test.tsx` | Generic card component |

### E2E tests (Playwright)

```bash
npx playwright install --with-deps chromium
npm run test:e2e
```

These launch a browser against the running dev server.

### How to write a new test

**Component test** (e.g. a new callout):

```tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { MyComponent } from "./MyComponent";

describe("MyComponent", () => {
  it("shows the title", () => {
    render(<MyComponent title="Hello" />);
    expect(screen.getByText("Hello")).toBeInTheDocument();
  });
});
```

**Pure function test** (e.g. a new guard):

```ts
import { describe, expect, it } from "vitest";
import { myGuard } from "./my-guard";

describe("myGuard", () => {
  it("accepts valid input", () => {
    expect(myGuard({ id: "x" }).ok).toBe(true);
  });

  it("rejects missing id", () => {
    expect(myGuard({}).ok).toBe(false);
  });
});
```

---

## 17. Configuration and environment

### `.env.local` (create from `.env.example`)

| Variable | Required | Where used | Purpose |
|----------|----------|-----------|---------|
| `ARCHLUCID_API_BASE_URL` | Yes | Server (RSC + proxy) | C# API URL |
| `ARCHLUCID_API_KEY` | When API key auth is on | Server (proxy) | Sent as `X-Api-Key` |
| `NEXT_PUBLIC_ARCHLUCID_AUTH_MODE` | No | Client + server | `"development-bypass"` (default) |
| `NEXT_PUBLIC_ARCHLUCID_API_BASE_URL` | No | Fallback | Documentation / legacy |

**Important:** Variables starting with `NEXT_PUBLIC_` are visible in browser JavaScript. Everything else is server-only. Never put secrets in `NEXT_PUBLIC_*`.

### Scripts

| Command | What it does |
|---------|-------------|
| `npm run dev` | Start dev server on port 3000 (with Turbopack — fast HMR) |
| `npm run build` | Production build (type-checks + lints + compiles) |
| `npm run start` | Start production server |
| `npm test` | Run Vitest once |
| `npm run test:watch` | Run Vitest in watch mode |
| `npm run lint` | Run ESLint |

---

## 18. Common tasks for back-end developers

### "I added a new field to a C# DTO. How do I show it in the UI?"

1. Add the field to the matching TS type in `src/types/`.
2. If it is optional (not always present), use `fieldName?: string`.
3. If a coerce function exists for this type, add a validation line (or skip if the field is truly optional).
4. Use the field in the page: `{data.fieldName ?? "—"}`.
5. Run `npm test` and `npm run build` to verify.

### "I added a new API endpoint. How do I call it from the UI?"

1. Add a function in `src/lib/api.ts`:
   ```ts
   export async function getNewThing(id: string): Promise<NewThing> {
     return apiGet<NewThing>(`/api/new-things/${encodeURIComponent(id)}`);
   }
   ```
2. Add a TS type in `src/types/` if the response shape is new.
3. Call the function from a `page.tsx` (server component) or from a client component's event handler.
4. The proxy route handles it automatically — no changes needed there.

### "I need a new page."

1. Create a folder: `src/app/my-page/`.
2. Create `page.tsx` in it (server component by default).
3. Optionally create `loading.tsx` for the loading state.
4. Add a `<Link>` in `layout.tsx` nav if it should appear in the global nav.

### "I need to make a page interactive (forms, buttons)."

Add `"use client";` as the very first line of the file. Then you can use `useState`, `useEffect`, and `onClick`.

### "What is JSX? It looks like HTML inside TypeScript."

It is. JSX is a syntax extension that lets you write HTML-like markup inside TypeScript functions. The compiler converts `<div>Hello</div>` into `React.createElement("div", null, "Hello")`. You never call `createElement` manually.

Key differences from HTML:
- `class` → `className` (because `class` is a reserved word in JS)
- `style` takes an object, not a string: `style={{ color: "red", fontSize: 14 }}`
- `for` → `htmlFor`
- Self-closing tags are required: `<input />` not `<input>`
- Expressions use curly braces: `<p>{user.name}</p>`
- Conditional rendering: `{condition && <Component />}` (if condition is truthy, render it)

### "How do I debug?"

1. `npm run dev` starts the dev server with hot reload.
2. Open browser DevTools (F12) → Console for client errors, Network for API calls.
3. For server component errors, check the terminal where `npm run dev` is running.
4. Add `console.log(...)` in server components — output goes to the terminal.
5. Add `console.log(...)` in client components — output goes to the browser console.

---

## 19. Keyboard shortcuts

The operator shell registers **Alt+letter** navigation (e.g. Alt+N → new run), a **Shift+?** help dialog, and **page-specific** shortcuts on Alerts. Shortcuts are **guarded** inside text fields and selects. For the full shortcut table, discoverability (nav `aria-keyshortcuts`, `<ShortcutHint>` badges), accessibility notes, and how to extend [`shortcut-registry.ts`](../src/lib/shortcut-registry.ts), read **[KEYBOARD_SHORTCUTS.md](./KEYBOARD_SHORTCUTS.md)**. Integration coverage: `src/integration/keyboard-shortcuts-*.test.tsx`.

---

## 20. Glossary

| Term | Meaning |
|------|---------|
| **RSC** | React Server Component — runs on Node.js, not in the browser |
| **Client Component** | Runs in the browser; file starts with `"use client"` |
| **App Router** | Next.js routing system where folder structure = URL structure |
| **`page.tsx`** | The main component for a route (like a controller action return) |
| **`loading.tsx`** | Shown while `page.tsx` is loading (like a loading placeholder) |
| **`layout.tsx`** | Wraps pages (like `_Layout.cshtml` in Razor) |
| **`route.ts`** | API endpoint in Next.js (like a minimal API handler) |
| **Props** | Parameters passed to a component (`{ title, children }`) |
| **State** (`useState`) | A variable that triggers re-render when changed |
| **Effect** (`useEffect`) | Code that runs after render (side effects) |
| **JSX** | HTML-like syntax inside TypeScript |
| **Coerce function** | Runtime shape validator for API responses |
| **`OperatorShellMessage`** | Our shared callout components (error, warning, empty, loading, malformed) |
| **Proxy route** | `/api/proxy/*` — forwards browser requests to the C# API with server-side credentials |
| **Turbopack** | Fast dev-server bundler (used by `npm run dev`) |
| **Vitest** | Unit test runner (like xUnit for TypeScript) |
| **Testing Library** | Renders components in a simulated browser DOM for testing |

---

## What to read next

- `docs/API_CONTRACTS.md` — the C# API contract reference (server side).
- `docs/COMPARISON_REPLAY.md` — comparison and replay architecture.
- `docs/CLI_USAGE.md` — how to create runs via the CLI (needed to populate the UI).
- Source files in `src/lib/` — start with `api.ts` and `operator-response-guards.ts`.
