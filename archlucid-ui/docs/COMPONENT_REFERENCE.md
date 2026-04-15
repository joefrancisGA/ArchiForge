# ArchLucid Operator Shell — Component Reference

> **Audience:** Developers maintaining or extending the operator shell UI.  
> See `OPERATOR_SHELL_TUTORIAL.md` for foundational concepts. This document is the reference.

---

## Table of contents

1. [Component inventory](#1-component-inventory)
2. [OperatorShellMessage components](#2-operatorshellmessage-components)
3. [ArtifactListTable](#3-artifactlisttable)
4. [ArtifactReviewContent](#4-artifactreviewcontent)
5. [GraphViewer](#5-graphviewer)
6. [SectionCard](#6-sectioncard)
7. [AuthPanel](#7-authpanel)
8. [Compare sub-components](#8-compare-sub-components)
9. [Layout and navigation](#9-layout-and-navigation)
10. [Helper libraries](#10-helper-libraries)
11. [RunExplanationSection](#11-runexplanationsection)

---

## 1. Component inventory

| Component | File | Server/Client | Purpose |
|-----------|------|--------------|---------|
| `OperatorErrorCallout` | `components/OperatorShellMessage.tsx` | Either | HTTP error display |
| `OperatorEmptyState` | `components/OperatorShellMessage.tsx` | Either | Valid empty result or idle |
| `OperatorLoadingNotice` | `components/OperatorShellMessage.tsx` | Either | In-progress indicator |
| `OperatorMalformedCallout` | `components/OperatorShellMessage.tsx` | Either | Bad JSON shape |
| `OperatorWarningCallout` | `components/OperatorShellMessage.tsx` | Either | Non-fatal secondary failure |
| `ArtifactListTable` | `components/ArtifactListTable.tsx` | Server | Artifact table with review + download |
| `ArtifactReviewContent` | `components/ArtifactReviewContent.tsx` | Server | Pretty + raw content panel |
| `GraphViewer` | `components/GraphViewer.tsx` | Client | React Flow graph canvas |
| `SectionCard` | `components/SectionCard.tsx` | Either | Bordered section container |
| `AuthPanel` | `components/AuthPanel.tsx` | Client | Dev bypass notice; OIDC sign-in / sign-out in JWT mode |
| `StructuredComparisonView` | `components/compare/StructuredComparisonView.tsx` | Client | Golden manifest delta tables |
| `LegacyRunComparisonView` | `components/compare/LegacyRunComparisonView.tsx` | Client | Flat diff display |
| `AiComparisonExplanationView` | `components/compare/AiComparisonExplanationView.tsx` | Client | LLM explanation display |
| `RunExplanationSection` | `components/RunExplanationSection.tsx` | Client | Aggregate run explanation (themes, posture, confidence, provenance) |

---

## 2. OperatorShellMessage components

**File:** `src/components/OperatorShellMessage.tsx`

These are the five standard callout components used across all pages. They share a common `calloutBase` style (rounded corners, padding, max width of 720px).

### Visual summary

```
┌──────────────────────────────────────────────────────┐
│  OperatorErrorCallout                                │
│  Border: #b91c1c (red)   Background: #fef2f2        │
│  Role: alert   Color: #7f1d1d                        │
│  Use: HTTP errors, missing resources                 │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│  OperatorEmptyState                                  │
│  Border: #d4d4d4 (gray)  Background: #fafafa         │
│  Role: status  Color: #404040                        │
│  Use: Zero results, idle state                       │
│  Extra: takes a `title` prop shown as <strong>       │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│  OperatorLoadingNotice                               │
│  Border: #cbd5e1 (slate) Background: #f8fafc         │
│  Role: status (aria-live: polite)  Color: #334155    │
│  Use: loading.tsx files, async waits                 │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│  OperatorMalformedCallout                            │
│  Border: #7c3aed (purple) Background: #f5f3ff        │
│  Role: alert   Color: #4c1d95                        │
│  Use: coerce function failures (bad JSON shape)      │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│  OperatorWarningCallout                              │
│  Border: #ca8a04 (amber)  Background: #fffbeb        │
│  Role: status  Color: #713f12                        │
│  Use: Non-fatal failures (artifact list failed)      │
└──────────────────────────────────────────────────────┘
```

### Props

All five accept `{ children: ReactNode }`. `OperatorEmptyState` additionally requires `{ title: string }`.

### Usage pattern

Every page follows the same priority order:

```tsx
if (loadError)        → OperatorErrorCallout    (blocks everything)
if (malformedMessage) → OperatorMalformedCallout (blocks rendering)
if (items.length === 0) → OperatorEmptyState     (valid but empty)
otherwise             → render data
```

Non-blocking secondary failures (e.g. manifest summary failed but run detail is fine) use `OperatorWarningCallout` *alongside* the main content.

---

## 3. ArtifactListTable

**File:** `src/components/ArtifactListTable.tsx`  
**Type:** Server component (no `"use client"`)

### Purpose

Renders a sorted table of artifacts with Review and Download links. Used on:
- Run detail page (`/runs/{runId}`)
- Manifest detail page (`/manifests/{manifestId}`)
- Artifact review page (sibling navigation)

### Props

```ts
{
  manifestId: string;              // manifest these artifacts belong to
  artifacts: ArtifactDescriptor[]; // array from listArtifacts()
  currentArtifactId?: string;      // highlight this row (artifact review page)
  runId?: string;                  // if set, Review links use /runs/… path
}
```

### Behavior

1. **Sorts** artifacts alphabetically by `name`.
2. **Columns:** Artifact (name), Type (label from helper), Format, Created (localized), Hash (first 8 chars + `…`), Actions.
3. **Review link:** When `runId` is provided, links to `/runs/{runId}/artifacts/{artifactId}` (redirects to manifest canonical URL). Without `runId`, links directly to `/manifests/{manifestId}/artifacts/{artifactId}`.
4. **Download link:** Uses `getArtifactDownloadUrl(manifestId, artifactId)` — a proxy URL for binary download.
5. **Current row highlighting:** Background turns `#eff6ff` (light blue) when `artifactId === currentArtifactId`.

### How Review link routing works

```
From run detail (/runs/abc):
  Review link → /runs/abc/artifacts/xyz
    → page.tsx loads run, finds manifestId
    → redirect() to /manifests/{manifestId}/artifacts/xyz
    → canonical artifact review page renders

From manifest detail (/manifests/def):
  Review link → /manifests/def/artifacts/xyz
    → canonical artifact review page renders directly
```

---

## 4. ArtifactReviewContent

**File:** `src/components/ArtifactReviewContent.tsx`  
**Type:** Server component

### Purpose

Renders the body of an artifact for human review, with a collapsible raw-text fallback.

### Props

```ts
{
  prepared: PreparedArtifactBody;  // from prepareArtifactBodyText()
  contentType: string;             // e.g. "application/json"
  byteLength: number;              // total file size in bytes
  truncated: boolean;              // true if preview was capped
  contentError: string | null;     // error if content fetch failed
}
```

### Behavior

1. **If `contentError` is set:** Shows `OperatorWarningCallout` with the error and a suggestion to download.
2. **If `truncated` is true:** Shows `OperatorWarningCallout` with byte count and download suggestion.
3. **Caption line:** Describes the view kind (e.g. "JSON (pretty-printed for review)"), content type, and byte count.
4. **Readable view:** `<pre>` block with the `prepared.readableText`.
5. **Raw disclosure:** `<details>/<summary>` with `prepared.rawText`. Labeled "(exact, unmodified from API)" when different from readable, or "(same as readable above)" when identical.

### View kinds

| `viewKind` | Caption |
|-----------|---------|
| `markdown` | "Markdown (rendered as pre-wrapped text; download for editors or viewers)" |
| `mermaid` | "Mermaid source (paste into a Mermaid viewer or download this file)" |
| `json` (valid) | "JSON (pretty-printed for review)" |
| `json` (invalid) | "JSON (invalid — showing raw bytes as text)" |
| `plain` | "Text content" |

---

## 5. GraphViewer

**File:** `src/components/GraphViewer.tsx`  
**Type:** Client component (`"use client"`)

### Purpose

Wraps React Flow to render a pannable, zoomable graph of nodes and edges.

### Props

```ts
{
  nodes: GraphNodeVm[];  // from API (coerced)
  edges: GraphEdgeVm[];  // from API (coerced)
}
```

### Behavior

1. **Type filter dropdown:** When more than one `nodeType` exists, shows a `<select>` to filter. "All types" is the default.
2. **Mapping:** Uses `mapNodesToReactFlow()` and `mapEdgesToReactFlow()` from `graph-mapper.ts`.
3. **Empty states:**
   - Zero total nodes → `OperatorEmptyState` "Graph has no nodes."
   - Filter hides all → `OperatorEmptyState` "No matching nodes for current filter."
4. **Selection:** Clicking a node shows a detail panel with id, label, type, and metadata key-value pairs.
5. **Controls:** MiniMap + zoom/pan controls from React Flow.

---

## 6. SectionCard

**File:** `src/components/SectionCard.tsx`  
**Type:** Either (no `"use client"`)

### Purpose

A bordered card with optional title. Used when a page section needs visual separation.

### Props

```ts
{
  title?: string;
  children: ReactNode;
}
```

---

## 7. AuthPanel

**File:** `src/components/AuthPanel.tsx`  
**Type:** Client component

### Purpose

Shows auth at the top of every page. In **development-bypass** mode, explains that the API auto-authenticates. When **`NEXT_PUBLIC_ARCHLUCID_AUTH_MODE=jwt`**, shows sign-in / sign-out, OIDC session display name (from JWT payload), and links to **`/auth/signin`**.

---

## 8. Compare sub-components

All three live in `src/components/compare/` and are client components (used inside the `/compare` client page).

### StructuredComparisonView

**Props:** `{ comparison: GoldenManifestComparison }`  
**Renders:** Tables for each delta section (decision changes, requirement changes, security changes, topology changes, cost changes). Shows a total delta count badge.

### LegacyRunComparisonView

**Props:** `{ comparison: RunComparison }`  
**Renders:** Run-level diffs (section/key/diffKind/before/after) and manifest comparison diffs. Shows count badges.

### AiComparisonExplanationView

**Props:** `{ explanation: ComparisonExplanation }`  
**Renders:** Summary paragraph, major changes list, tradeoffs list, full narrative text. Each section appears only if the API returned non-empty content.

---

## 9. Layout and navigation

**File:** `src/app/layout.tsx`

The root layout renders:
1. `<html>` and `<body>` tags
2. A `<header>` with the "ArchLucid" title
3. A `<nav>` with `<Link>` elements for every page
4. `<AuthPanel />`
5. `{children}` — the current page

The `<Link>` component from `next/link` performs client-side navigation (no full page reload) when the user clicks a link. For back-end developers: this is like a SPA router link, but Next.js pre-renders pages on the server.

### Metadata

```ts
export const metadata = {
  title: "ArchLucid",
  description: "ArchLucid operator shell",
};
```

Next.js uses this to set `<title>` and `<meta name="description">` in the HTML head.

---

## 10. Helper libraries

### `src/lib/api.ts`

| Function | HTTP | Purpose |
|----------|------|---------|
| `apiGet<T>(path)` | GET | Core JSON fetch (server or browser) |
| `apiPostJson<T>(path, body)` | POST | Core JSON post |
| `fetchArchLucidJson<T>(path)` | GET | Alias for `apiGet` (used by graph API). Targets the **ArchLucid** API via same-origin proxy or server-side base URL. |
| `listRunsByProject(projectId, take)` | GET | `/api/authority/projects/{id}/runs` |
| `getRunSummary(runId)` | GET | `/api/authority/runs/{id}/summary` |
| `getRunDetail(runId)` | GET | `/api/authority/runs/{id}` |
| `getRunExplanationSummary(runId)` | GET | `/v1/explain/runs/{id}/aggregate` (aggregate explanation + themes) |
| `getManifestSummary(manifestId)` | GET | `/api/authority/manifests/{id}/summary` |
| `listArtifacts(manifestId)` | GET | `/api/artifacts/manifests/{id}` |
| `getArtifactDescriptor(manifestId, artifactId)` | GET | Artifact metadata (no body) |
| `fetchArtifactContentUtf8(manifestId, artifactId, maxBytes)` | GET | Artifact body as UTF-8 text |
| `compareRuns(leftRunId, rightRunId)` | GET | Legacy run comparison |
| `compareGoldenManifestRuns(baseRunId, compareRunId)` | GET | Structured golden manifest comparison |
| `replayRun(runId, mode)` | POST | Authority replay |
| `getArtifactDownloadUrl(manifestId, artifactId)` | — | Returns proxy URL for browser download |

### `src/lib/operator-response-guards.ts`

| Function | Validates |
|----------|----------|
| `coerceRunSummaryList(data)` | `RunSummary[]` — array of objects with `runId` |
| `coerceGraphViewModel(data)` | `GraphViewModel` — object with `nodes` and `edges` arrays |
| `coerceGoldenManifestComparison(data)` | `GoldenManifestComparison` — structured delta sections |
| `coerceReplayResponse(data)` | `ReplayResponse` — replay result with validation object |
| `coerceRunDetail(data)` | `RunDetail` — envelope with nested `run.runId` |
| `coerceManifestSummary(data)` | `ManifestSummary` — manifest metadata with required keys |
| `coerceArtifactDescriptorList(data)` | `ArtifactDescriptor[]` — array with `artifactId` per row |
| `coerceArtifactDescriptor(data)` | `ArtifactDescriptor` — single descriptor shape |
| `coerceRunComparison(data)` | `RunComparison` — flat diff comparison |
| `coerceComparisonExplanation(data)` | `ComparisonExplanation` — AI narrative shape |

All return `{ ok: true, value/items }` or `{ ok: false, message }`.

### `src/lib/artifact-review-helpers.ts`

| Export | Purpose |
|--------|---------|
| `ARTIFACT_TYPE_COPY` (internal) | Labels + descriptions for each `ArtifactType` enum value |
| `classifyArtifactView(format, artifactType)` | Returns `"markdown"`, `"json"`, `"mermaid"`, or `"plain"` |
| `getArtifactTypeLabel(artifactType)` | Human label (e.g. "Cost summary") |
| `getArtifactTypeDescription(artifactType)` | One-line description for the review panel |
| `prepareArtifactBodyText(utf8Text, format, artifactType)` | Returns `{ readableText, rawText, viewKind, jsonPrettyFailed }` |

### `src/lib/graph-mapper.ts`

| Export | Purpose |
|--------|---------|
| `mapNodesToReactFlow(nodes)` | Converts `GraphNodeVm[]` to React Flow `Node[]` |
| `mapEdgesToReactFlow(edges)` | Converts `GraphEdgeVm[]` to React Flow `Edge[]` |

### `src/lib/api-error.ts`

| Export | Purpose |
|--------|---------|
| `readApiFailureMessage(response)` | Reads RFC 9457 Problem Details or falls back to status text |

### `src/lib/config.ts`

| Export | Purpose |
|--------|---------|
| `getServerApiBaseUrl()` | Server-side API base URL (env var with fallback) |
| `PUBLIC_API_BASE_URL` | Browser-visible fallback (documentation only) |

### `src/lib/scope.ts`

| Export | Purpose |
|--------|---------|
| `getScopeHeaders()` | Returns `x-tenant-id`, `x-workspace-id`, `x-project-id` for dev |

### `src/lib/auth-config.ts`

| Export | Purpose |
|--------|---------|
| `AUTH_MODE` | Auth mode string from `NEXT_PUBLIC_ARCHLUCID_AUTH_MODE` |

---

## 11. RunExplanationSection

**File:** `src/components/RunExplanationSection.tsx`  
**Type:** Client component (`"use client"`)

### Purpose

Renders the **aggregate** run explanation from `GET /v1/explain/runs/{runId}/aggregate` (`getRunExplanationSummary` in `api.ts`): executive assessment, risk posture badge, model confidence (`Progress` from shadcn/ui), theme bullets, key drivers / risk implications from the nested `explanation`, and optional provenance in a `<details>` block.

### Props

```ts
{
  summary: RunExplanationSummary | null;
  loading: boolean;
  error: string | null;
}
```

On **run detail** (`/runs/[runId]`), the server component fetches the summary when a golden manifest exists; failures use `OperatorApiProblem` above this component (warning variant), matching manifest summary / artifacts.

### Exports

| Export | Purpose |
|--------|---------|
| `riskPostureBadgeColors(posture)` | Maps `Low` / `Medium` / `High` / `Critical` (case-insensitive) to badge colors for the posture pill. |
