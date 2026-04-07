# Annotated Page Walkthrough

> **How to use this:** Read this with the actual source file open side by side.  
> Every line is explained. Nothing is skipped. If you understand this one file, you can read any page in the codebase.

---

## The file: `src/app/runs/page.tsx`

This page handles the URL `/runs?projectId=default`. It fetches a list of runs from the API and shows them in a table. It is a **server component** — the code runs on the Node.js server, never in the browser.

I chose this file because it demonstrates every pattern used in the codebase: imports, async data fetching, response validation, conditional rendering, error/empty states, and navigation links.

---

## Line-by-line

### Lines 1–10: Imports

```tsx
import Link from "next/link";
```

`Link` is Next.js's client-side navigation component. It renders an `<a>` tag but intercepts clicks to avoid a full page reload. **C# analogy:** like `<a asp-page="/Runs">` in Razor Pages — it outputs an `<a>` tag but the framework handles routing.

```tsx
import {
  OperatorEmptyState,
  OperatorErrorCallout,
  OperatorMalformedCallout,
} from "@/components/OperatorShellMessage";
```

Three status components from our shared library. The `@/` prefix is a path alias defined in `tsconfig.json` — it means `src/`. So `@/components/OperatorShellMessage` resolves to `src/components/OperatorShellMessage.tsx`.

**C# analogy:** `using ArchLucid.UI.Components;`

```tsx
import { coerceRunSummaryList } from "@/lib/operator-response-guards";
```

Our runtime shape validator. This checks that the API response is actually an array of objects with `runId` fields.

```tsx
import { listRunsByProject } from "@/lib/api";
```

The function that calls `GET /api/authority/projects/{projectId}/runs`.

```tsx
import type { RunSummary } from "@/types/authority";
```

The TypeScript type (like a C# class/record). The `type` keyword in `import type` tells TypeScript "this is only used for type checking, not at runtime — don't include it in the compiled JavaScript." This is a performance hint, not a behavioral difference.

---

### Lines 12–17: The function signature

```tsx
export default async function RunsPage({
  searchParams,
}: {
  searchParams: Promise<{ projectId?: string; take?: string }>;
}) {
```

Let's break this down piece by piece:

- **`export default`** — This is the main export of the file. Next.js requires each `page.tsx` to have a default export. **C# analogy:** `public class RunsPage : PageModel` — the framework knows to use this class for the route.

- **`async function`** — This is an async function, like `async Task<IActionResult>` in a controller. It can use `await`. This is only possible in server components (not in client components).

- **`RunsPage`** — The name does not matter to Next.js (the file path determines the route). But it helps with debugging and stack traces.

- **`{ searchParams }`** — This is **destructuring**. The function receives one object argument, and we extract the `searchParams` property from it. Equivalent C#:

  ```csharp
  // C# equivalent:
  public async Task<IActionResult> OnGet([FromQuery] string? projectId, [FromQuery] string? take)

  // But in React, they come as one object:
  // { searchParams: { projectId?: string, take?: string } }
  ```

- **`searchParams: Promise<...>`** — In Next.js 15, search params are a `Promise` that must be awaited. This is a framework convention for async server rendering.

- **`{ projectId?: string; take?: string }`** — The `?` means the property is optional (might be `undefined`). Like `string?` in C#.

---

### Lines 18–20: Reading query parameters

```tsx
const resolved = await searchParams;
const projectId = resolved.projectId ?? "default";
const take = Number(resolved.take ?? "20");
```

- **`await searchParams`** — Unwraps the Promise to get the actual values.
- **`??`** — The **nullish coalescing operator**. Returns the right side if the left side is `null` or `undefined`. Identical to `??` in C#.
- **`Number(...)`** — Converts a string to a number. Like `int.Parse()` but returns `NaN` instead of throwing on bad input.

---

### Lines 22–24: Declaring state variables

```tsx
let runs: RunSummary[] = [];
let loadError: string | null = null;
let malformedMessage: string | null = null;
```

Three `let` variables. In a server component, these are just local variables — set once during the fetch, read once during rendering. **No `useState` needed** because server components do not re-render.

- **`RunSummary[]`** — An array of `RunSummary` objects. Like `List<RunSummary>` in C#.
- **`string | null`** — A union type: either a string or null. Like `string?` in C#.

---

### Lines 26–38: Fetching data

```tsx
try {
  const raw: unknown = await listRunsByProject(projectId, take);
  const coerced = coerceRunSummaryList(raw);

  if (!coerced.ok) {
    malformedMessage = coerced.message;
    runs = [];
  } else {
    runs = coerced.items;
  }
} catch (e) {
  loadError = e instanceof Error ? e.message : "Failed to load runs.";
}
```

This is the data-fetching pattern used on every server page:

1. **`const raw: unknown`** — We deliberately type the result as `unknown` (not `RunSummary[]`) because we want to validate the shape before trusting it. `unknown` is TypeScript's "I don't know what this is yet" type — you can't access properties on it without checking first.

2. **`coerceRunSummaryList(raw)`** — Validates the shape. Returns `{ ok: true, items: RunSummary[] }` or `{ ok: false, message: string }`. This is a **discriminated union** — the `ok` field tells you which variant you have. **C# analogy:**

   ```csharp
   // Similar pattern in C#:
   var result = TryParseRunList(raw);
   if (!result.Success) { errorMessage = result.ErrorMessage; }
   else { runs = result.Items; }
   ```

3. **`catch (e)`** — Catches HTTP errors (404, 500, network failures). The `listRunsByProject` function throws on non-200 responses.

4. **`e instanceof Error ? e.message : "Failed to load runs."`** — A **ternary expression** (like `? :` in C#). Checks if the caught value is an `Error` object (it usually is, but TypeScript does not guarantee it).

---

### Lines 40–104: Rendering (JSX)

```tsx
return (
  <main>
    <h2>Runs</h2>
    <p>Project: {projectId}</p>
```

This is **JSX** — HTML-like syntax inside a function. The function returns a tree of elements that React converts to HTML.

- **`<main>`** — A standard HTML5 semantic element.
- **`{projectId}`** — Curly braces embed a JavaScript expression inside JSX. Whatever the expression evaluates to gets rendered as text. **C# analogy:** `@projectId` in Razor.

---

### Lines 45–54: Error state

```tsx
{loadError && (
  <OperatorErrorCallout>
    <strong>Could not load runs.</strong>
    <p style={{ margin: "8px 0 0" }}>{loadError}</p>
    <p style={{ margin: "8px 0 0", fontSize: 14 }}>
      Check that the API is running...
    </p>
  </OperatorErrorCallout>
)}
```

**`{loadError && (...)}`** — This is **conditional rendering**. It is the most important JSX pattern to understand:

- If `loadError` is `null` (falsy), the entire expression evaluates to `null`, and React renders nothing.
- If `loadError` is a string like `"HTTP 500: Internal Server Error"` (truthy), the expression evaluates to the JSX after `&&`, and React renders it.

**C# analogy (Razor):**
```csharp
@if (loadError != null)
{
    <div class="error-callout">
        <strong>Could not load runs.</strong>
        <p>@loadError</p>
    </div>
}
```

**`style={{ margin: "8px 0 0" }}`** — Inline styles in JSX use a JavaScript object, not a CSS string. The outer `{}` is the JSX expression delimiter, the inner `{}` is a JavaScript object literal. **C# analogy:** `style="margin: 8px 0 0"` in HTML, but as a dictionary.

**`<OperatorErrorCallout>`** — A React component (custom element). The content between the opening and closing tags becomes the `children` prop. Think of it as calling a function that wraps its output in a styled `<div>`.

---

### Lines 56–65: Malformed state

```tsx
{!loadError && malformedMessage && (
  <OperatorMalformedCallout>
    ...
  </OperatorMalformedCallout>
)}
```

Same pattern, but with two conditions: only show malformed if there was **no** load error **and** a malformed message exists. The `!` is logical NOT, `&&` is logical AND. This guarantees only one callout is visible at a time.

---

### Lines 67–75: Empty state

```tsx
{!loadError && !malformedMessage && runs.length === 0 && (
  <OperatorEmptyState title="No runs in this project">
    <p style={{ margin: 0 }}>
      There are no runs for this project...{" "}
      <Link href="/">Back to home</Link>.
    </p>
  </OperatorEmptyState>
)}
```

- **Three conditions before rendering:** no error, no malformed, and the array is empty.
- **`{" "}`** — A space character in JSX. JSX collapses whitespace, so if you need a space between inline elements (like before `<Link>`), you use `{" "}`.
- **`<Link href="/">`** — Client-side navigation link. Renders `<a href="/">`.

---

### Lines 77–102: Data table

```tsx
{!loadError && !malformedMessage && runs.length > 0 && (
  <table style={{ borderCollapse: "collapse", width: "100%", marginTop: 16 }}>
    <thead>
      <tr>
        <th style={{ textAlign: "left", borderBottom: "1px solid #ccc", padding: 8 }}>Run ID</th>
        ...
      </tr>
    </thead>
    <tbody>
      {runs.map((run) => (
        <tr key={run.runId}>
          <td style={{ padding: 8, fontFamily: "monospace", fontSize: 13 }}>{run.runId}</td>
          <td style={{ padding: 8 }}>{run.description ?? ""}</td>
          <td style={{ padding: 8 }}>{new Date(run.createdUtc).toLocaleString()}</td>
          <td style={{ padding: 8 }}>
            <Link href={`/runs/${run.runId}`}>Open</Link>
          </td>
        </tr>
      ))}
    </tbody>
  </table>
)}
```

**`runs.map((run) => (...))`** — This is the **most important collection pattern in React**. It replaces `foreach`:

```csharp
// C# Razor equivalent:
@foreach (var run in runs)
{
    <tr>
        <td>@run.RunId</td>
    </tr>
}
```

In JavaScript, `.map()` transforms each element of an array into something else and returns a new array. Here, each `RunSummary` object is transformed into a `<tr>` element.

**`(run) => (...)`** — An **arrow function** (lambda). Like `run => ...` in C# LINQ. The parentheses around the JSX return are needed because the JSX spans multiple lines.

**`key={run.runId}`** — React requires a unique `key` on each element in a list so it can efficiently update the DOM when the list changes. If you forget it, React warns in the console. **There is no C# equivalent** — this is a React-specific optimization hint.

**`` `/runs/${run.runId}` ``** — A **template literal** (backtick string with `${...}` interpolation). Like `$"/runs/{run.RunId}"` in C#.

**`{run.description ?? ""}`** — If `description` is null/undefined, show an empty string instead. Without `?? ""`, React would render `null` as nothing, but it is clearer to be explicit.

---

## The complete control flow

```
1. Browser navigates to /runs?projectId=default
2. Next.js matches app/runs/page.tsx
3. While loading: shows app/runs/loading.tsx (OperatorLoadingNotice)
4. RunsPage() runs on the server:
   a. Reads searchParams → projectId="default", take=20
   b. Calls listRunsByProject("default", 20) → HTTP GET to C# API
   c. API returns JSON → coerceRunSummaryList validates shape
   d. Sets runs, loadError, or malformedMessage
5. JSX renders exactly one of:
   - Error callout (red)         ← if HTTP/network failed
   - Malformed callout (purple)  ← if JSON shape was wrong
   - Empty state (gray)          ← if array was valid but empty
   - Data table                  ← if runs.length > 0
6. Server sends finished HTML to browser
7. Browser displays it immediately — no JavaScript needed
```

---

## Reading any other page

Every server page in this codebase follows the exact same structure:

1. **Imports** (components, API functions, coerce functions, types)
2. **`export default async function`** with route params or search params
3. **`let` variables** for data, error, and malformed message
4. **`try/catch`** with `await apiFunction()` → `coerce*(raw)` → check `ok`
5. **`return` with JSX** using `{condition && (...)}` for each state
6. **Priority: error → malformed → empty → data**

Client pages (`"use client"`) add `useState` and event handlers, but the rendering logic is identical.
