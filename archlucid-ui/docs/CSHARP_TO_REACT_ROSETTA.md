# C# to React/TypeScript Rosetta Stone

> **What this is:** Side-by-side code showing the C# way and the React/TypeScript way.  
> Every example uses real patterns from this codebase — not abstract toy examples.

---

## Table of contents

1. [Types and models](#1-types-and-models)
2. [Nullable / optional values](#2-nullable--optional-values)
3. [Collections](#3-collections)
4. [Async / await](#4-async--await)
5. [Error handling](#5-error-handling)
6. [Conditional display](#6-conditional-display)
7. [Loops / iteration](#7-loops--iteration)
8. [String interpolation](#8-string-interpolation)
9. [Properties / props](#9-properties--props)
10. [Imports / using](#10-imports--using)
11. [Enums and constants](#11-enums-and-constants)
12. [Pattern matching / discriminated unions](#12-pattern-matching--discriminated-unions)
13. [Inline styles](#13-inline-styles)
14. [Event handlers](#14-event-handlers)
15. [State (the one with no C# equivalent)](#15-state-the-one-with-no-c-equivalent)
16. [Side effects](#16-side-effects)
17. [Common gotchas](#17-common-gotchas)

---

## 1. Types and models

### C#
```csharp
public record RunSummary
{
    public string RunId { get; init; }
    public string ProjectId { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedUtc { get; init; }
    public string? GoldenManifestId { get; init; }
}
```

### TypeScript
```typescript
export type RunSummary = {
  runId: string;
  projectId: string;
  description?: string | null;
  createdUtc: string;        // dates are strings in JSON
  goldenManifestId?: string | null;
};
```

### Key differences
| C# | TypeScript |
|----|-----------|
| `public record` / `public class` | `export type` or `export interface` |
| `{ get; init; }` | Just list the field |
| `string?` | `string \| null` or `string?` (for optional fields) |
| `DateTime` | `string` (ISO 8601 from JSON) |
| PascalCase (`RunId`) | camelCase (`runId`) — JSON convention |

---

## 2. Nullable / optional values

### C#
```csharp
string? description = run.Description;
string display = description ?? "No description";
string safe = description?.Trim() ?? "";
```

### TypeScript
```typescript
const description: string | null = run.description;
const display: string = description ?? "No description";
const safe: string = description?.trim() ?? "";
```

**Identical syntax.** `??` (nullish coalescing) and `?.` (optional chaining) work the same way.

### Optional object fields

```csharp
// C#: nullable property
public string? GoldenManifestId { get; init; }
// Usage: if (run.GoldenManifestId != null) { ... }
```

```typescript
// TypeScript: optional field (note the ?)
goldenManifestId?: string | null;
// Usage: if (run.goldenManifestId != null) { ... }
```

The `?` after the field name means the field might not exist at all (undefined). The `| null` means it might exist but be null. Both are handled by `?? ""`.

---

## 3. Collections

### C#
```csharp
List<RunSummary> runs = new List<RunSummary>();
int count = runs.Count;
RunSummary first = runs[0];
bool any = runs.Any();
List<string> ids = runs.Select(r => r.RunId).ToList();
RunSummary? found = runs.FirstOrDefault(r => r.RunId == targetId);
List<RunSummary> sorted = runs.OrderBy(r => r.CreatedUtc).ToList();
```

### TypeScript
```typescript
const runs: RunSummary[] = [];
const count: number = runs.length;
const first: RunSummary = runs[0];
const any: boolean = runs.length > 0;
const ids: string[] = runs.map(r => r.runId);
const found: RunSummary | undefined = runs.find(r => r.runId === targetId);
const sorted: RunSummary[] = [...runs].sort((a, b) => a.createdUtc.localeCompare(b.createdUtc));
```

### Quick reference

| C# (LINQ) | TypeScript (Array) |
|-----------|-------------------|
| `.Select(x => ...)` | `.map(x => ...)` |
| `.Where(x => ...)` | `.filter(x => ...)` |
| `.Any()` | `.length > 0` |
| `.Any(x => ...)` | `.some(x => ...)` |
| `.All(x => ...)` | `.every(x => ...)` |
| `.FirstOrDefault()` | `.find(x => ...)` (returns `undefined`, not `null`) |
| `.Count` | `.length` |
| `.OrderBy(x => ...)` | `.sort((a, b) => ...)` (**mutates!** use `[...arr].sort(...)` for safety) |
| `.Contains(x)` | `.includes(x)` |
| `.Distinct()` | `[...new Set(arr)]` |
| `.ToDictionary(x => x.Key, x => x.Value)` | `Object.fromEntries(arr.map(x => [x.key, x.value]))` |
| `.ForEach(x => ...)` | `.forEach(x => ...)` (but prefer `.map()` in React) |

### Important: `.sort()` mutates the original array

```typescript
// WRONG — modifies the original array:
artifacts.sort((a, b) => a.name.localeCompare(b.name));

// RIGHT — creates a copy first:
const sorted = [...artifacts].sort((a, b) => a.name.localeCompare(b.name));
```

`[...artifacts]` is the **spread operator** — it creates a shallow copy of the array. Like `artifacts.ToList()` creating a new list.

---

## 4. Async / await

### C#
```csharp
public async Task<RunSummary[]> ListRuns(string projectId)
{
    HttpResponseMessage response = await _httpClient.GetAsync($"/api/authority/projects/{projectId}/runs");
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<RunSummary[]>();
}
```

### TypeScript
```typescript
export async function listRunsByProject(projectId: string, take = 20): Promise<RunSummary[]> {
  return apiGet<RunSummary[]>(
    `/api/authority/projects/${encodeURIComponent(projectId)}/runs?take=${take}`,
  );
}
```

| C# | TypeScript |
|----|-----------|
| `async Task<T>` | `async function(): Promise<T>` |
| `await` | `await` (identical) |
| `Task<T>` | `Promise<T>` |
| `Uri.EscapeDataString(s)` | `encodeURIComponent(s)` |
| `HttpClient` | `fetch()` (built into the language) |

---

## 5. Error handling

### C#
```csharp
try
{
    var runs = await ListRuns("default");
}
catch (HttpRequestException ex)
{
    errorMessage = ex.Message;
}
catch (Exception ex)
{
    errorMessage = "Unexpected error: " + ex.Message;
}
```

### TypeScript
```typescript
try {
  const runs = await listRunsByProject("default");
} catch (e) {
  loadError = e instanceof Error ? e.message : "Failed to load runs.";
}
```

**Key difference:** TypeScript's `catch` does not let you catch by type. The caught value is `unknown` — it could be an `Error`, a string, or anything. You must check with `instanceof`.

There is no `catch (SpecificException)` syntax in TypeScript/JavaScript.

---

## 6. Conditional display

This is the biggest difference between Razor and React.

### C# (Razor)
```csharp
@if (loadError != null)
{
    <div class="error">@loadError</div>
}
else if (runs.Count == 0)
{
    <div class="empty">No runs found.</div>
}
else
{
    <table>
        @foreach (var run in runs)
        {
            <tr><td>@run.RunId</td></tr>
        }
    </table>
}
```

### React (JSX)
```tsx
{loadError && (
  <div className="error">{loadError}</div>
)}

{!loadError && runs.length === 0 && (
  <div className="empty">No runs found.</div>
)}

{!loadError && runs.length > 0 && (
  <table>
    {runs.map(run => (
      <tr key={run.runId}><td>{run.runId}</td></tr>
    ))}
  </table>
)}
```

### Why `&&` instead of `if/else`?

JSX is **not** a templating language. It is JavaScript expressions inside `return`. You cannot use `if/else` statements inside a `return`. You **can** use:

1. **`&&` (short-circuit AND):** `{condition && <Component />}` — render if true
2. **Ternary:** `{condition ? <A /> : <B />}` — render A or B
3. **Early return:** Put `if` checks before the `return` statement

This codebase uses pattern 1 (`&&`) consistently.

### How `&&` works

```typescript
// JavaScript truth table for &&:
null && "anything"       // → null       (falsy, React renders nothing)
undefined && "anything"  // → undefined  (React renders nothing)
"" && "anything"         // → ""         (React renders nothing)
0 && "anything"          // → 0          (⚠️ React renders "0" — see gotchas)
false && "anything"      // → false      (React renders nothing)
"hello" && <Component /> // → <Component /> (truthy, renders the component)
```

---

## 7. Loops / iteration

### C# (Razor)
```csharp
@foreach (var run in runs)
{
    <tr>
        <td>@run.RunId</td>
        <td>@run.Description</td>
    </tr>
}
```

### React (JSX)
```tsx
{runs.map((run) => (
  <tr key={run.runId}>
    <td>{run.runId}</td>
    <td>{run.description ?? ""}</td>
  </tr>
))}
```

### Why `.map()` instead of `foreach`?

`.map()` returns an array of JSX elements. React knows how to render an array. `.forEach()` returns `undefined`, which React cannot render.

```typescript
// .map() returns a new array:
[1, 2, 3].map(x => x * 2)  // → [2, 4, 6]

// .forEach() returns nothing:
[1, 2, 3].forEach(x => x * 2)  // → undefined
```

### The `key` prop

Every element in a `.map()` **must** have a unique `key` prop. React uses it to track which items changed, were added, or removed. Use a stable identifier (like `runId`), not the array index.

```tsx
// GOOD — stable ID:
{runs.map(run => <tr key={run.runId}>...</tr>)}

// BAD — index changes when list is reordered:
{runs.map((run, index) => <tr key={index}>...</tr>)}
```

---

## 8. String interpolation

### C#
```csharp
string url = $"/runs/{run.RunId}";
string message = $"Found {runs.Count} runs in {projectId}";
```

### TypeScript
```typescript
const url: string = `/runs/${run.runId}`;
const message: string = `Found ${runs.length} runs in ${projectId}`;
```

**Use backticks (`` ` ``), not quotes.** Single quotes (`'`) and double quotes (`"`) do not support interpolation.

---

## 9. Properties / props

### C# (Razor partial / component)
```csharp
// Defining a component:
public class ErrorCallout : ViewComponent
{
    public string Message { get; set; }
    public RenderFragment ChildContent { get; set; }
}

// Using it:
<ErrorCallout Message="Load failed">
    <p>Details here.</p>
</ErrorCallout>
```

### React
```tsx
// Defining a component:
function OperatorErrorCallout({ children }: { children: ReactNode }) {
  return <div role="alert">{children}</div>;
}

// Using it:
<OperatorErrorCallout>
  <p>Details here.</p>
</OperatorErrorCallout>
```

### What is `{ children }`?

React passes the content between `<Component>` and `</Component>` as a special prop called `children`. The curly braces **destructure** it from the props object:

```typescript
// These are equivalent:
function MyComponent({ children }: { children: ReactNode }) { ... }
function MyComponent(props: { children: ReactNode }) { const children = props.children; ... }
```

### Multiple props
```tsx
// Defining:
function ArtifactListTable(props: {
  manifestId: string;
  artifacts: ArtifactDescriptor[];
  currentArtifactId?: string;    // optional
  runId?: string;                // optional
}) {
  const { manifestId, artifacts, currentArtifactId, runId } = props;
  // ...
}

// Using:
<ArtifactListTable
  manifestId="abc-123"
  artifacts={artifactArray}
  currentArtifactId="xyz"
/>
```

**C# analogy:** Props are like constructor parameters for a view component. You pass them as XML-like attributes.

---

## 10. Imports / using

### C#
```csharp
using System.Collections.Generic;
using ArchLucid.Models;
using ArchLucid.Services;
```

### TypeScript
```typescript
import type { RunSummary } from "@/types/authority";
import { listRunsByProject } from "@/lib/api";
import Link from "next/link";
```

| C# | TypeScript |
|----|-----------|
| `using Namespace;` | `import { Name } from "module";` |
| (no equivalent) | `import Name from "module";` (default export) |
| `using X = Alias;` | `import { Original as Alias } from "module";` |
| `using static Class;` | `import * as Name from "module";` |

**`import type`** — Only imports the type for compile-time checking, not at runtime. Removes the import from the compiled JavaScript. Use it for types/interfaces.

---

## 11. Enums and constants

### C#
```csharp
public enum ReplayMode
{
    ReconstructOnly,
    RebuildManifest,
    RebuildArtifacts
}
```

### TypeScript (as used in this codebase)
```typescript
const replayModes = ["ReconstructOnly", "RebuildManifest", "RebuildArtifacts"] as const;

// Or as a type:
type GraphMode = "provenance-full" | "decision-subgraph" | "node-neighborhood" | "architecture";
```

TypeScript has `enum` but this codebase uses **string literal unions** and `as const` arrays instead. They are simpler, work better with JSON (which is all strings), and do not generate extra runtime code.

---

## 12. Pattern matching / discriminated unions

### C# (pattern matching)
```csharp
var result = TryParseRunList(data);
switch (result)
{
    case { Success: true, Items: var items }:
        runs = items;
        break;
    case { Success: false, Message: var msg }:
        malformedMessage = msg;
        break;
}
```

### TypeScript (discriminated union)
```typescript
const coerced = coerceRunSummaryList(raw);

if (coerced.ok) {
  runs = coerced.items;       // TypeScript knows .items exists because ok is true
} else {
  malformedMessage = coerced.message;  // TypeScript knows .message exists because ok is false
}
```

The `ok` field is the **discriminant**. When TypeScript sees `if (coerced.ok)`, it narrows the type in each branch. This is called **type narrowing** — the compiler tracks which branch you are in and adjusts the available properties.

---

## 13. Inline styles

### C# (Razor)
```html
<div style="border: 1px solid red; padding: 12px; background-color: #fef2f2;">
  Error content
</div>
```

### React (JSX)
```tsx
<div style={{
  border: "1px solid red",
  padding: 12,           // numbers are pixels
  backgroundColor: "#fef2f2",  // camelCase, not kebab-case
}}>
  Error content
</div>
```

| CSS property | JSX style key |
|-------------|---------------|
| `background-color` | `backgroundColor` |
| `font-size` | `fontSize` |
| `border-radius` | `borderRadius` |
| `margin-top` | `marginTop` |
| `text-align` | `textAlign` |

Rule: replace hyphens with camelCase.

Numbers without units default to pixels. Strings are used as-is: `"8px 0 0"`.

---

## 14. Event handlers

### C# (Blazor)
```csharp
<button @onclick="HandleClick" disabled="@isLoading">Compare</button>

@code {
    private async Task HandleClick()
    {
        isLoading = true;
        result = await CompareRuns(leftRunId, rightRunId);
        isLoading = false;
    }
}
```

### React
```tsx
<button onClick={handleCompare} disabled={loading}>Compare</button>

async function handleCompare() {
  setLoading(true);
  try {
    const data = await compareRuns(leftRunId, rightRunId);
    setResult(data);
  } finally {
    setLoading(false);
  }
}
```

| Blazor | React |
|--------|-------|
| `@onclick="Method"` | `onClick={functionRef}` |
| `@onchange="Method"` | `onChange={(e) => setX(e.target.value)}` |
| `@oninput` | `onChange` (React fires on every keystroke) |
| `disabled="@bool"` | `disabled={bool}` |

**Note:** React uses `onClick` (camelCase), not `onclick` (lowercase HTML). All event handlers are camelCase.

---

## 15. State (the one with no C# equivalent)

Server components (like ASP.NET controllers) do not have state. But **client components** do.

### The closest C# analogy: WPF/MAUI data binding

```csharp
// WPF/MAUI:
private string _runId = "";
public string RunId
{
    get => _runId;
    set { _runId = value; OnPropertyChanged(); }  // triggers UI update
}
```

### React
```tsx
const [runId, setRunId] = useState("");
//      ^ read   ^ write (triggers re-render)
```

`useState` returns an array of two things:
1. The current value
2. A setter function that updates the value AND triggers React to re-render the component

**You never assign directly.** `runId = "abc"` does nothing. You must call `setRunId("abc")`.

### Multiple state variables

```tsx
const [leftRunId, setLeftRunId] = useState("");
const [rightRunId, setRightRunId] = useState("");
const [loading, setLoading] = useState(false);
const [result, setResult] = useState<RunComparison | null>(null);
const [error, setError] = useState<string | null>(null);
```

Each is independent. Updating one does not affect the others.

---

## 16. Side effects

### C# (Blazor)
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await LoadData();
    }
}
```

### React
```tsx
useEffect(() => {
  // Runs after the component renders.
  // The [] means "only run once, when the component first appears."
  loadData();
}, []);

useEffect(() => {
  // Runs after render AND whenever searchParams changes.
  const left = searchParams.get("leftRunId");
  if (left) setLeftRunId(left);
}, [searchParams]);
```

The second argument (`[]` or `[searchParams]`) is the **dependency array**:
- `[]` — Run once after first render (like `OnAfterRenderAsync(firstRender: true)`)
- `[x, y]` — Run after render whenever `x` or `y` changes
- No array — Run after every render (usually a mistake)

---

## 17. Common gotchas

### `===` vs `==`

```typescript
// ALWAYS use === in TypeScript (strict equality):
if (status === "Active") { ... }

// NEVER use == (loose equality, does type coercion):
if (status == "Active") { ... }  // works but unreliable
```

C# `==` is like TypeScript `===`. TypeScript `==` is like nothing in C# — it converts types before comparing (`0 == ""` is `true`).

### `0 && <Component />` renders "0"

```tsx
// BUG — renders the text "0" on screen:
{items.length && <Component />}

// FIX — compare explicitly:
{items.length > 0 && <Component />}
```

When the left side of `&&` is `0` (a falsy number), JavaScript returns `0`, and React renders the string "0". Always compare to `> 0` for numbers.

### `undefined` vs `null`

```typescript
// Both mean "no value", but they are different:
let a: string | undefined;  // a is undefined (field does not exist)
let b: string | null = null; // b is null (field exists but has no value)

// In practice, ?? handles both:
const display = a ?? b ?? "fallback";  // works for both
```

### `const` does not mean immutable

```typescript
const items: string[] = ["a", "b"];
items.push("c");     // LEGAL — the array itself is mutated
items = ["x"];       // ILLEGAL — cannot reassign the variable
```

`const` means the variable binding cannot change. The value it points to can still be mutated. For true immutability, create copies: `const newItems = [...items, "c"]`.

### React re-renders on every state change

```tsx
const [count, setCount] = useState(0);

// Each call to setCount causes the ENTIRE function to re-run.
// This is normal. React is fast enough that this is fine.
function handleClick() {
  setCount(count + 1);  // component function re-runs, new JSX is compared, DOM is updated
}
```

This is why you should not put expensive calculations directly in the component body. Use `useMemo` to cache them:

```tsx
// Expensive calculation cached until graph changes:
const nodeTypes = useMemo(() => {
  const set = new Set(graph.nodes.map(n => n.type));
  return [...set].sort();
}, [graph]);  // only recompute when graph changes
```
