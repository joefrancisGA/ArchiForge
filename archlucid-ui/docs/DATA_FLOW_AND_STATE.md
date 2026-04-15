# ArchLucid Operator Shell — Data Flow and State Management

> **Audience:** Back-end developers learning how data moves through the front end.  
> Prerequisite: `OPERATOR_SHELL_TUTORIAL.md` (concepts) and `COMPONENT_REFERENCE.md` (components).

---

## Table of contents

1. [Overall data flow architecture](#1-overall-data-flow-architecture)
2. [Server-side data flow (the common path)](#2-server-side-data-flow-the-common-path)
3. [Client-side data flow (interactive pages)](#3-client-side-data-flow-interactive-pages)
4. [State management patterns](#4-state-management-patterns)
5. [Error state machine](#5-error-state-machine)
6. [API response lifecycle](#6-api-response-lifecycle)
7. [Page-by-page data flow diagrams](#7-page-by-page-data-flow-diagrams)
8. [Adding a new page (step-by-step)](#8-adding-a-new-page-step-by-step)

---

## 1. Overall data flow architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                           BROWSER                                │
│                                                                  │
│  ┌──────────────┐  click link   ┌──────────────────────────────┐ │
│  │  <Link>      │ ────────────► │  Next.js Client Router       │ │
│  │  (nav bar)   │               │  (intercepts, avoids reload) │ │
│  └──────────────┘               └──────────┬───────────────────┘ │
│                                            │                     │
│                              request page  │                     │
│                                            ▼                     │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              NEXT.JS SERVER (Node.js)                       │ │
│  │                                                             │ │
│  │  1. Route matched → run page.tsx (server component)         │ │
│  │  2. page.tsx calls api.ts functions (listRunsByProject…)    │ │
│  │  3. api.ts → resolveRequest() → HTTP GET/POST              │ │
│  │  4. ──────────────────────────────────────────────────────► │ │
│  │     │          ARCHLUCID C# API (localhost:5128)           │ │
│  │     │          X-Api-Key + scope headers                    │ │
│  │  5. ◄──────────── JSON response ─────────────────────────  │ │
│  │  6. page.tsx → coerce*(raw) → validate shape               │ │
│  │  7. Render JSX → HTML                                      │ │
│  │  8. Return HTML to browser                                 │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              │                                   │
│                 finished HTML│                                    │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              BROWSER DOM                                    │ │
│  │  Operator sees the rendered page immediately.               │ │
│  │  No JavaScript needed for display (SSR).                    │ │
│  └─────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

For **client component** pages (graph, compare, replay), the flow is different:

```
┌──────────────────────────────────────────────────────────────────┐
│                           BROWSER                                │
│                                                                  │
│  1. Page loads (JS bundle hydrates the client component)         │
│  2. User fills form (run IDs, mode) and clicks a button          │
│  3. Event handler calls api.ts (e.g. compareRuns())              │
│  4. api.ts → resolveRequest() detects isBrowser() === true       │
│     → URL becomes /api/proxy/api/authority/compare/runs?…        │
│  5. fetch() to same-origin proxy                                 │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │        NEXT.JS PROXY ROUTE (/api/proxy/[...path])          │ │
│  │  - Adds X-Api-Key                                          │ │
│  │  - Adds scope headers                                      │ │
│  │  - Forwards to C# API                                      │ │
│  │  - Streams response back                                   │ │
│  └────────────────────────────────┬────────────────────────────┘ │
│                                   │                              │
│                              JSON │                              │
│                                   ▼                              │
│  6. Event handler receives JSON                                  │
│  7. coerce*(raw) validates shape                                 │
│  8. useState setter updates state → React re-renders             │
│  9. Component renders result/error/empty                         │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. Server-side data flow (the common path)

Most pages in the operator shell are server components. Here is the data flow pattern they all follow.

### Template: server component page

```tsx
// This is a SIMPLIFIED template. Real pages have more variables.

import { apiGet } from "@/lib/api";
import { coerceMyList } from "@/lib/operator-response-guards";
import { OperatorErrorCallout, OperatorMalformedCallout, OperatorEmptyState } from "@/components/OperatorShellMessage";
import type { MyItem } from "@/types/my-types";

export default async function MyPage() {
  // ── Step 1: Declare state buckets ──
  let items: MyItem[] = [];
  let loadError: string | null = null;
  let malformedMessage: string | null = null;

  // ── Step 2: Fetch + validate ──
  try {
    const raw: unknown = await apiGet("/api/my-endpoint");
    const coerced = coerceMyList(raw);

    if (!coerced.ok) {
      malformedMessage = coerced.message;
    } else {
      items = coerced.items;
    }
  } catch (e) {
    loadError = e instanceof Error ? e.message : "Fetch failed.";
  }

  // ── Step 3: Render (priority: error → malformed → empty → data) ──
  return (
    <main>
      <h2>My Page</h2>

      {loadError && (
        <OperatorErrorCallout>
          <strong>Load failed.</strong>
          <p>{loadError}</p>
        </OperatorErrorCallout>
      )}

      {!loadError && malformedMessage && (
        <OperatorMalformedCallout>
          <strong>Bad response shape.</strong>
          <p>{malformedMessage}</p>
        </OperatorMalformedCallout>
      )}

      {!loadError && !malformedMessage && items.length === 0 && (
        <OperatorEmptyState title="No items">
          <p>Empty collection from the API.</p>
        </OperatorEmptyState>
      )}

      {!loadError && !malformedMessage && items.length > 0 && (
        <table>
          {/* render items */}
        </table>
      )}
    </main>
  );
}
```

### Why `let` and not `useState`?

In a server component, there is **no re-rendering**. The function runs once per request and returns HTML. There is no browser-side reactivity. So plain `let` variables are correct — they are set once during the fetch, then read during JSX rendering.

`useState` is a browser concept. It only works in client components.

---

## 3. Client-side data flow (interactive pages)

### Template: client component page

```tsx
"use client";

import { useState } from "react";
import { apiGet } from "@/lib/api";
import { coerceMyResult } from "@/lib/operator-response-guards";
import {
  OperatorLoadingNotice,
  OperatorErrorCallout,
  OperatorMalformedCallout,
  OperatorEmptyState,
} from "@/components/OperatorShellMessage";

export default function MyClientPage() {
  // ── Step 1: State variables ──
  const [inputId, setInputId] = useState("");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<MyResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [malformed, setMalformed] = useState<string | null>(null);

  // ── Step 2: Event handler ──
  async function handleSubmit() {
    setLoading(true);
    setError(null);
    setMalformed(null);
    setResult(null);

    try {
      const raw: unknown = await apiGet(`/api/things/${encodeURIComponent(inputId)}`);
      const coerced = coerceMyResult(raw);

      if (!coerced.ok) {
        setMalformed(coerced.message);
      } else {
        setResult(coerced.value);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Fetch failed.");
    } finally {
      setLoading(false);
    }
  }

  // ── Step 3: Render ──
  return (
    <main>
      <h2>My Client Page</h2>
      <input value={inputId} onChange={(e) => setInputId(e.target.value)} />
      <button onClick={handleSubmit} disabled={loading || !inputId.trim()}>
        Go
      </button>

      {loading && <OperatorLoadingNotice>Loading…</OperatorLoadingNotice>}
      {error && <OperatorErrorCallout><strong>Error.</strong><p>{error}</p></OperatorErrorCallout>}
      {malformed && <OperatorMalformedCallout><strong>Bad shape.</strong><p>{malformed}</p></OperatorMalformedCallout>}
      {!loading && !error && !malformed && !result && (
        <OperatorEmptyState title="Idle"><p>Enter an ID and click Go.</p></OperatorEmptyState>
      )}
      {result && <div>{/* render result */}</div>}
    </main>
  );
}
```

### How `useState` works (for C# developers)

Think of `useState` as declaring a property with change notification:

```csharp
// C# analogy (WPF/MAUI):
private string _inputId = "";
public string InputId
{
    get => _inputId;
    set { _inputId = value; OnPropertyChanged(); }  // triggers re-render
}
```

In React:

```tsx
const [inputId, setInputId] = useState("");
//      ↑ read       ↑ write (triggers re-render)
```

When you call `setInputId("abc")`:
1. React stores "abc" as the new value.
2. React re-runs the component function.
3. This time, `inputId` is "abc".
4. The returned JSX reflects the new value.
5. React updates only the changed parts of the DOM (efficient diff).

**You never mutate state directly.** Always use the setter. `inputId = "abc"` does nothing — React does not know about it.

---

## 4. State management patterns

### No global state store

This codebase does **not** use Redux, Zustand, MobX, or any global state library. Each page manages its own state. This is intentional:

- The operator shell is read-mostly. Pages fetch, display, and the operator navigates.
- There is no cross-page state to share (no shopping cart, no user preferences).
- Global stores add complexity that is not justified here.

### State is local to the page

Each page (server or client) has its own state variables. When you navigate away and come back, the state is gone — the page re-fetches.

### URL as state (client pages)

The compare page reads initial run IDs from URL params (`?leftRunId=...&rightRunId=...`). This lets operators share links. The pattern:

```tsx
"use client";
import { useSearchParams } from "next/navigation";

export default function ComparePage() {
  const params = useSearchParams();
  const [leftRunId, setLeftRunId] = useState(params.get("leftRunId") ?? "");
  // ...
}
```

`useSearchParams()` reads the browser's current URL query string. It only works in client components.

---

## 5. Error state machine

Every page implements the same deterministic state machine:

```
                    ┌─────────┐
                    │  FETCH  │
                    └────┬────┘
                         │
              ┌──────────┼──────────┐
              │          │          │
         catch(e)    coerced.ok?   coerced.ok
              │      === false     === true
              ▼          ▼          │
       ┌──────────┐ ┌──────────┐   │
       │  ERROR   │ │MALFORMED │   │
       │ (red)    │ │ (purple) │   │
       └──────────┘ └──────────┘   │
                                   │
                         ┌─────────┴─────────┐
                         │                   │
                    items.length         items.length
                    === 0                > 0
                         │                   │
                         ▼                   ▼
                  ┌──────────┐        ┌──────────┐
                  │  EMPTY   │        │   DATA   │
                  │ (gray)   │        │ (table)  │
                  └──────────┘        └──────────┘
```

For client pages, add `LOADING` as the initial state while the fetch is in progress.

### States are mutually exclusive (for the primary content)

The conditional rendering uses `{condition && ...}` chains that guarantee only one state is visible:

```tsx
{loadError && <OperatorErrorCallout>…</OperatorErrorCallout>}
{!loadError && malformedMessage && <OperatorMalformedCallout>…</OperatorMalformedCallout>}
{!loadError && !malformedMessage && items.length === 0 && <OperatorEmptyState>…</OperatorEmptyState>}
{!loadError && !malformedMessage && items.length > 0 && <table>…</table>}
```

### Non-blocking warnings

Some pages have primary data that succeeded but secondary data that failed. Example: on the run detail page, the run loads fine, but the manifest summary fails. In that case:

- Primary content renders (run metadata, authority chain).
- An `OperatorWarningCallout` appears inline where the manifest summary would be.
- This is **not** mutually exclusive — the warning sits alongside the primary content.

---

## 6. API response lifecycle

Here is the full lifecycle of an API response, from network to screen:

```
1. fetch()
   │
   ├─ Network error (DNS, timeout, connection refused)
   │  → catch block → loadError = "Failed to fetch"
   │  → OperatorErrorCallout
   │
   ├─ HTTP error (4xx, 5xx)
   │  → readApiFailureMessage(response)
   │    → tries to parse RFC 9457 Problem Details
   │    → falls back to "HTTP {status}: {statusText}"
   │  → throw new Error(message)
   │  → catch block → loadError = message
   │  → OperatorErrorCallout
   │
   └─ HTTP 200
      │
      ├─ response.json() fails (not valid JSON)
      │  → throw → catch → loadError
      │  → OperatorErrorCallout
      │
      └─ response.json() succeeds
         │
         └─ coerce*(data)
            │
            ├─ { ok: false, message }
            │  → malformedMessage = message
            │  → OperatorMalformedCallout
            │
            └─ { ok: true, value/items }
               │
               ├─ empty array/object
               │  → OperatorEmptyState
               │
               └─ has data
                  → render tables/content
```

### Error message quality

The `readApiFailureMessage()` function in `api-error.ts` tries to extract useful information:
1. Parse the response body as JSON.
2. If it has a `detail` field (RFC 9457 Problem Details), use that.
3. If it has a `title` field, use that.
4. Otherwise, fall back to `"HTTP {status}: {statusText}"`.

This ensures operators see "Run not found" instead of "HTTP 404: Not Found".

---

## 7. Page-by-page data flow diagrams

### `/runs` — Run list

```
RunsPage()
  │
  ├── listRunsByProject("default", 20)
  │   └── apiGet("/api/authority/projects/default/runs")
  │       └── HTTP GET → C# API
  │           └── JSON: RunSummary[]
  │
  ├── coerceRunSummaryList(raw)
  │   ├── ok: false → malformedMessage
  │   └── ok: true → runs = items
  │
  └── Render
      ├── error → OperatorErrorCallout
      ├── malformed → OperatorMalformedCallout
      ├── empty → OperatorEmptyState
      └── data → <table> with run rows
```

### `/runs/{runId}` — Run detail

```
RunDetailPage({ params: { runId } })
  │
  ├── getRunDetail(runId) ─────────────────────┐
  │   └── apiGet("/api/authority/runs/{id}")    │ parallel if manifest exists
  │       └── JSON: RunDetail                   │
  │                                             │
  ├── coerceRunDetail(raw)                      │
  │   ├── ok: false → full-page error           │
  │   └── ok: true → resolvedDetail             │
  │                                             │
  ├── goldenManifestId = resolvedDetail          │
  │   .run.goldenManifestId                     │
  │   └── if null → OperatorEmptyState          │
  │                                             │
  ├── getManifestSummary(manifestId) ──────────►│
  │   └── coerceManifestSummary()               │
  │       ├── ok: false → WarningCallout        │
  │       └── ok: true → manifestSummary        │
  │                                             │
  ├── listArtifacts(manifestId) ───────────────►│
  │   └── coerceArtifactDescriptorList()        │
  │       ├── ok: false → WarningCallout        │
  │       └── ok: true → artifacts              │
  │                                             │
  └── Render
      ├── Run metadata (id, project, created)
      ├── Authority chain (snapshot IDs)
      ├── Manifest summary (or warning)
      ├── ArtifactListTable (or warning/empty)
      └── Download links (ZIP, manifest JSON)
```

### `/manifests/{manifestId}/artifacts/{artifactId}` — Artifact review

```
ArtifactReviewPage({ params: { manifestId, artifactId } })
  │
  ├── getArtifactDescriptor(manifestId, artifactId)
  │   └── apiGet("…/descriptor")
  │       └── JSON: ArtifactDescriptor
  │
  ├── fetchArtifactContentUtf8(manifestId, artifactId)
  │   └── resolveBinaryGetRequest("…/artifact/{id}")
  │       └── Binary response → TextDecoder → utf8Text
  │
  ├── listArtifacts(manifestId) (for sibling list)
  │
  ├── coerceArtifactDescriptor(raw)
  │
  ├── prepareArtifactBodyText(utf8Text, format, type)
  │   └── { readableText, rawText, viewKind, jsonPrettyFailed }
  │
  └── Render
      ├── "What this artifact is" panel
      │   ├── getArtifactTypeDescription(type)
      │   ├── Metadata grid (type, format, hash, etc.)
      │   └── Download link
      │
      ├── ArtifactReviewContent (pretty + raw)
      │   ├── Truncation warning (if applicable)
      │   ├── <pre> with readableText
      │   └── <details> with rawText
      │
      └── "Artifacts in this manifest" table
          └── ArtifactListTable (currentArtifactId highlighted)
```

### `/compare` — Compare runs (client)

```
ComparePage() — "use client"
  │
  ├── State: leftRunId, rightRunId, loading,
  │          result, golden, aiExplanation,
  │          error, goldenError, aiError,
  │          legacyMalformed, goldenMalformed, aiMalformed
  │
  ├── handleCompare()
  │   ├── compareRuns(left, right) ──────────────────────┐
  │   │   └── coerceRunComparison()                      │ parallel
  │   │                                                  │
  │   ├── compareGoldenManifestRuns(left, right) ───────►│
  │   │   └── coerceGoldenManifestComparison()           │
  │   │                                                  │
  │   └── (on success) getComparisonExplanation(left, right)
  │       └── coerceComparisonExplanation()
  │
  └── Render
      ├── Form (two inputs + Compare button)
      ├── loading → OperatorLoadingNotice
      ├── errors → OperatorErrorCallout (per section)
      ├── malformed → OperatorMalformedCallout (per section)
      ├── StructuredComparisonView (golden deltas)
      ├── LegacyRunComparisonView (flat diffs)
      └── AiComparisonExplanationView (narrative)
```

---

## 8. Adding a new page (step-by-step)

### Server component page (data display)

1. **Create the route folder:** `src/app/my-feature/`
2. **Create `page.tsx`:**
   ```tsx
   import { apiGet } from "@/lib/api";
   // import coerce function, types, callout components
   
   export default async function MyFeaturePage() {
     let data = null;
     let loadError: string | null = null;
     // ... fetch, coerce, render pattern from Section 2
   }
   ```
3. **Create `loading.tsx`:**
   ```tsx
   import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";
   export default function Loading() {
     return <OperatorLoadingNotice>Loading my feature.</OperatorLoadingNotice>;
   }
   ```
4. **Add a nav link** in `src/app/layout.tsx`:
   ```tsx
   <Link href="/my-feature">My Feature</Link>
   ```
5. **Add types** in `src/types/` if the API returns a new shape.
6. **Add a coerce function** in `operator-response-guards.ts` if the page is critical.
7. **Run tests:** `npm test` and `npm run build`.

### Client component page (interactive)

Same as above, but:
1. Add `"use client";` as the first line of `page.tsx`.
2. Use `useState` for all mutable state.
3. Put fetch logic inside an `async function` called from `onClick`.
4. Use `try/catch/finally` with `setLoading`, `setError`, etc.

### Adding a new API function

1. Open `src/lib/api.ts`.
2. Add a typed function:
   ```ts
   export async function getMyThing(id: string): Promise<MyThing> {
     return apiGet<MyThing>(`/api/my-things/${encodeURIComponent(id)}`);
   }
   ```
3. The proxy handles it automatically.
4. Add a coerce function in `operator-response-guards.ts` if needed.

### Adding a new coerce function

1. Open `src/lib/operator-response-guards.ts`.
2. Follow the existing pattern:
   ```ts
   export function coerceMyThing(
     data: unknown,
   ): { ok: true; value: MyThing } | { ok: false; message: string } {
     if (!isRecord(data)) {
       return { ok: false, message: "Expected a JSON object." };
     }
     if (typeof data.id !== "string") {
       return { ok: false, message: "Missing required string field: id." };
     }
     // ... more checks
     return { ok: true, value: data as MyThing };
   }
   ```
3. Add tests in `operator-response-guards.test.ts`.

---

## Key takeaways for C# developers

| React/Next.js concept | C#/ASP.NET equivalent |
|-----------------------|----------------------|
| `page.tsx` (server) | Controller action returning a Razor view |
| `page.tsx` (client) | JavaScript-heavy Razor page with AJAX |
| `loading.tsx` | Partial view shown during `IAsyncEnumerable` rendering |
| `layout.tsx` | `_Layout.cshtml` |
| `useState` | `ObservableProperty` in MVVM |
| `useEffect` | `OnAfterRenderAsync` in Blazor |
| `props` | View model / method parameters |
| JSX | Razor syntax (`@Html.Partial(...)`) |
| `apiGet<T>()` | `HttpClient.GetFromJsonAsync<T>()` |
| `coerce*()` | `TryValidateModel()` / FluentValidation |
| `fetch()` | `HttpClient.SendAsync()` |
| `encodeURIComponent()` | `Uri.EscapeDataString()` |
| `try/catch` | Same (`try/catch`) |
| `import` | `using` |
| `export default function` | `public class MyPage : PageModel` |
