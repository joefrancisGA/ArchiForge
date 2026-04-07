# ArchLucid Operator Shell — Testing and Troubleshooting Guide

> **Audience:** Back-end developers running, debugging, and extending the front-end test suite.

---

## Table of contents

1. [Test stack overview](#1-test-stack-overview)
2. [Running tests](#2-running-tests)
3. [55R / review workflow smoke tests (Change Set 55R)](#3-55r--review-workflow-smoke-tests-change-set-55r)
4. [How unit tests work](#4-how-unit-tests-work)
5. [Writing your first component test](#5-writing-your-first-component-test)
6. [Writing pure function tests](#6-writing-pure-function-tests)
7. [Test file inventory](#7-test-file-inventory)
8. [E2E tests (Playwright)](#8-e2e-tests-playwright)
9. [Troubleshooting common issues](#9-troubleshooting-common-issues)
10. [Debugging techniques](#10-debugging-techniques)

---

## 1. Test stack overview

| Tool | Role | C# equivalent |
|------|------|--------------|
| **Vitest** | Test runner + assertions | xUnit / NUnit |
| **Testing Library** (`@testing-library/react`) | Render React components in simulated DOM | bUnit (for Blazor) |
| **jsdom** | Simulated browser environment (no real browser) | Similar to bUnit's test context |
| **Playwright** | Real browser automation (E2E) | Playwright for .NET (same tool!) |

### Key insight for C# developers

Vitest tests run **in Node.js**, not in a browser. The `jsdom` library fakes enough of the browser API (`document`, `window`, `HTMLElement`) that React components can render and be queried. This is fast (milliseconds) but does not test real CSS, layout, or browser quirks.

Playwright tests launch a **real Chromium browser** and navigate to the running app. They are slower but test everything end-to-end.

---

## 2. Running tests

### Unit tests (Vitest)

```powershell
cd archiforge-ui

# Run all tests once (CI mode):
npm test

# Watch mode (re-runs on file changes — great during development):
npm run test:watch

# Run a specific test file:
npx vitest run src/lib/operator-response-guards.test.ts

# Run tests matching a name pattern:
npx vitest run -t "coerceRunSummaryList"
```

### From the repo root (convenience scripts)

```powershell
# Windows CMD:
test-ui-unit.cmd

# PowerShell:
.\test-ui-unit.ps1
```

### E2E tests (Playwright)

```powershell
cd archiforge-ui

# Install browser (one-time):
npx playwright install --with-deps chromium

# Run E2E (Playwright starts Next + mock API — no separate dev server or C# API):
npm run test:e2e
```

From the repo root: **`test-ui-smoke.cmd`** or **`test-ui-smoke.ps1`**. **What is covered, mocks, and limits:** [section 8 — E2E tests (Playwright)](#8-e2e-tests-playwright).

---

## 3. 55R / review workflow smoke tests (Change Set 55R)

Focused **component** and **API client contract** coverage for the operator shell (no heavy E2E in this set). Run from `archiforge-ui/`:

```powershell
# Full suite (includes 55R tests):
npm test

# Navigation + landing:
npx vitest run src/app/page.test.tsx src/components/ShellNav.test.tsx

# Review workflow patterns + compare views:
npx vitest run src/review-workflow

# Artifact table, preview panel, graph viewer:
npx vitest run src/components/ArtifactListTable.test.tsx src/components/ArtifactReviewContent.test.tsx src/components/GraphViewer.test.tsx

# Compare UI + API paths used by compare/list/descriptor:
npx vitest run src/review-workflow/compare-views.test.tsx src/lib/api.review-workflow.test.ts

# Response guards (run/manifest/artifact/compare shapes):
npx vitest run src/lib/operator-response-guards.test.ts
```

**Operator-facing doc** (workflow + contract expectations): [docs/operator-shell.md](../../../docs/operator-shell.md) (repo root).

---

## 4. How unit tests work

### Anatomy of a test file

```tsx
// 1. Imports
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { MyComponent } from "./MyComponent";

// 2. Test suite (like [Collection] or a test class)
describe("MyComponent", () => {
  
  // 3. Individual test (like [Fact] or [Test])
  it("renders the title", () => {
    // Arrange: render the component
    render(<MyComponent title="Hello" />);
    
    // Act + Assert: query the DOM and check
    expect(screen.getByText("Hello")).toBeInTheDocument();
  });

  it("shows empty state when items is empty", () => {
    render(<MyComponent title="List" items={[]} />);
    
    expect(screen.getByText("No items")).toBeInTheDocument();
  });
});
```

### C# comparison

```csharp
// This is roughly equivalent to the above in xUnit:
public class MyComponentTests
{
    [Fact]
    public void Renders_The_Title()
    {
        // Arrange
        var cut = RenderComponent<MyComponent>(p => p.Add(c => c.Title, "Hello"));
        
        // Assert
        cut.Find("*").TextContent.ShouldContain("Hello");
    }
}
```

### Key Testing Library concepts

| Concept | What it does | Analogy |
|---------|-------------|---------|
| `render(<Component />)` | Renders component into simulated DOM | `RenderComponent<T>()` in bUnit |
| `screen.getByText("...")` | Finds element containing exact text | `cut.Find("*:contains('...')")` |
| `screen.getByRole("alert")` | Finds element by ARIA role | Accessibility-first querying |
| `screen.queryByText("...")` | Like `getByText` but returns `null` instead of throwing | `FindAll().FirstOrDefault()` |
| `screen.getAllByRole("row")` | Finds all elements with that role | `FindAll("tr")` |
| `expect(element).toBeInTheDocument()` | Asserts element exists in DOM | `Assert.NotNull(element)` |
| `expect(element).toHaveAttribute("href", "/foo")` | Checks an HTML attribute | `Assert.Equal("/foo", element.GetAttribute("href"))` |

### Query priority (Testing Library philosophy)

Testing Library encourages querying by what the **user** sees, not by implementation details:

1. `getByRole` — best (ARIA roles like `"alert"`, `"link"`, `"button"`)
2. `getByText` — good (visible text content)
3. `getByLabelText` — good (form labels)
4. `getByTestId` — last resort (data-testid attribute)

Avoid querying by CSS class or element type — those are implementation details that can change without breaking functionality.

---

## 5. Writing your first component test

### Example: testing the `ArtifactListTable`

Here is the actual test file with annotations:

```tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { ArtifactListTable } from "./ArtifactListTable";
import type { ArtifactDescriptor } from "@/types/authority";

// Helper: creates a minimal valid artifact descriptor
function makeDescriptor(overrides?: Partial<ArtifactDescriptor>): ArtifactDescriptor {
  return {
    artifactId: "art-1",
    artifactType: "Inventory",
    name: "inventory.json",
    format: "json",
    createdUtc: "2025-01-01T00:00:00Z",
    contentHash: "abc123def456",
    ...overrides,
  };
}

describe("ArtifactListTable", () => {
  it("renders manifest-scoped Review link when runId is not provided", () => {
    const art = makeDescriptor();
    
    // Render with manifestId but no runId
    render(<ArtifactListTable manifestId="man-1" artifacts={[art]} />);
    
    // Find the Review link and check its href
    const reviewLink = screen.getByRole("link", { name: "Review" });
    expect(reviewLink).toHaveAttribute("href", "/manifests/man-1/artifacts/art-1");
  });

  it("renders run-scoped Review link when runId is provided", () => {
    const art = makeDescriptor();
    
    // Render with runId
    render(<ArtifactListTable manifestId="man-1" artifacts={[art]} runId="run-1" />);
    
    const reviewLink = screen.getByRole("link", { name: "Review" });
    expect(reviewLink).toHaveAttribute("href", "/runs/run-1/artifacts/art-1");
  });
});
```

### Step-by-step to write a new test

1. Create a file next to the component: `MyComponent.test.tsx`.
2. Import `render` and `screen` from `@testing-library/react`.
3. Import `describe`, `expect`, `it` from `vitest`.
4. Import the component.
5. Write a `describe` block.
6. Write `it` blocks that each test one behavior.
7. Use `render()` to mount the component and `screen` to query the DOM.
8. Run `npm test` to verify.

### Testing components that use `next/link`

The `<Link>` component from Next.js renders as an `<a>` tag in the test environment. You can query it with `screen.getByRole("link", { name: "..." })` and check its `href` attribute.

No special mocking is needed for `next/link` in Vitest with jsdom.

---

## 6. Writing pure function tests

Pure functions (no React, no DOM) are the easiest to test.

### Example: testing coerce functions

```ts
import { describe, expect, it } from "vitest";
import { coerceRunSummaryList } from "./operator-response-guards";

describe("coerceRunSummaryList", () => {
  it("accepts a valid array of run summaries", () => {
    const input = [
      { runId: "r1", projectId: "p1", createdUtc: "2025-01-01T00:00:00Z" },
      { runId: "r2", projectId: "p1", createdUtc: "2025-01-02T00:00:00Z" },
    ];

    const result = coerceRunSummaryList(input);

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.items).toHaveLength(2);
      expect(result.items[0].runId).toBe("r1");
    }
  });

  it("rejects a non-array", () => {
    const result = coerceRunSummaryList({ not: "an array" });
    
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.message).toContain("non-array");
    }
  });

  it("rejects rows missing runId", () => {
    const result = coerceRunSummaryList([{ projectId: "p1" }]);
    
    expect(result.ok).toBe(false);
  });
});
```

### Example: testing artifact helpers

```ts
import { describe, expect, it } from "vitest";
import { classifyArtifactView, prepareArtifactBodyText } from "./artifact-review-helpers";

describe("classifyArtifactView", () => {
  it("classifies markdown format", () => {
    expect(classifyArtifactView("markdown", "ArchitectureNarrative")).toBe("markdown");
  });

  it("classifies DiagramAst as json regardless of format", () => {
    expect(classifyArtifactView("text", "DiagramAst")).toBe("json");
  });
});

describe("prepareArtifactBodyText", () => {
  it("pretty-prints valid JSON", () => {
    const result = prepareArtifactBodyText('{"a":1}', "json", "Inventory");
    
    expect(result.viewKind).toBe("json");
    expect(result.readableText).toContain("  "); // indented
    expect(result.jsonPrettyFailed).toBe(false);
    expect(result.rawText).toBe('{"a":1}'); // original preserved
  });

  it("falls back gracefully for invalid JSON", () => {
    const result = prepareArtifactBodyText("not json", "json", "Inventory");
    
    expect(result.jsonPrettyFailed).toBe(true);
    expect(result.readableText).toBe("not json");
  });
});
```

---

## 7. Test file inventory

| Test file | What it tests | Type |
|-----------|--------------|------|
| `src/app/page.test.tsx` | Home / landing workflow links (55R) | Component |
| `src/components/ShellNav.test.tsx` | Grouped nav links (Start & review, Q&A, Alerts) | Component |
| `src/components/GraphViewer.test.tsx` | Graph empty states, filter behavior, non-empty panel | Component |
| `src/components/ArtifactListTable.test.tsx` | Review/Download URLs, empty list, sorted rows | Component |
| `src/components/ArtifactReviewContent.test.tsx` | Preview, truncation, preview-unavailable | Component |
| `src/review-workflow/artifact-list-states.test.tsx` | Failed vs empty artifact list UI patterns | Component |
| `src/review-workflow/compare-views.test.tsx` | Structured + legacy compare with/without data | Component |
| `src/lib/api.review-workflow.test.ts` | `listArtifacts`, descriptor, compare URL contracts (mocked fetch) | Pure / contract |
| `src/lib/compare-display-sort.test.ts` | Deterministic compare table ordering | Pure function |
| `src/lib/replay-display.test.ts` | Replay mode labels, sorted notes | Pure function |
| `src/components/SectionCard.test.tsx` | Generic card rendering | Component |
| `src/lib/operator-response-guards.test.ts` | Coerce functions (run, manifest, artifact, compare, …) | Pure function |
| `src/lib/artifact-review-helpers.test.ts` | View classification, labels, body prep | Pure function |
| `src/lib/graph-mapper.test.ts` | Node/edge mapping to React Flow | Pure function |
| `src/lib/api-error.test.ts` | ProblemDetails parsing | Pure function |
| `src/lib/config.test.ts` | Server URL resolution from env vars | Pure function |

---

## 8. E2E tests (Playwright)

### Change Set 57R — operator-journey E2E contract

The checked-in Playwright suite under **`e2e/`** is **smoke / operator-journey** coverage: a small set of linear flows with **deterministic mocked HTTP**, not exhaustive browser automation (single Chromium project, one worker in CI, no visual regression grid, no full route matrix).

**Important:** These tests do **not** require or use a **live ArchLucid.Api** (C#). Do not read them as proof that the UI works against your real database or deployment.

**How data is supplied**

| Mechanism | Used for |
|-----------|----------|
| **`e2e/start-e2e-with-mock.ts`** + **`e2e/mock-archiforge-api-server.ts`** | Playwright **`webServer`**: production **`next start`** with **`ARCHIFORGE_API_BASE_URL`** pointing at a loopback mock (default **127.0.0.1:18765**). Server components (run detail, manifest pages) receive **typed JSON fixtures** from `e2e/fixtures/`. |
| **`page.route` + `e2e/helpers/register-operator-api-routes.ts`** | **Client** compare flows: browser calls to **`/api/proxy/...`** are fulfilled in-process with the same fixture shapes (no backend). |

**Specs and operator journeys** (one focused scenario each)

| File | What it asserts |
|------|-----------------|
| **`e2e/smoke.spec.ts`** | Home renders **ArchLucid** (h1) and **Start here** (h2). |
| **`e2e/run-manifest-journey.spec.ts`** | Run detail (mock API) → open manifest link → manifest summary, artifacts table, bundle affordance → **Run detail** back. |
| **`e2e/manifest-empty-artifacts.spec.ts`** | Manifest whose artifact list is **`200` + `[]`**: valid-empty status region and copy; **no** “could not be loaded” failure callouts; bundle link present; no artifact table headers. |
| **`e2e/compare-journey.spec.ts`** | **`/compare?leftRunId&rightRunId`** prefills inputs; **Compare** runs mocked legacy + structured responses; structured-before-legacy guidance and **Review order** nav; **Last compare request** outcome region. |
| **`e2e/compare-stale-input-warning.spec.ts`** | After a successful compare, changing a run ID shows the stale-input warning; restoring the prior left ID clears it. |
| **`e2e/compare-proxy-mock.spec.ts`** | Client compare + **Explain changes (AI)** with mocked proxy responses (legacy + structured + explanation). |

**Out of scope for this suite:** auth flows, real CLI/API integration, multi-project runs lists, graph interactions, downloads/ZIP bytes, performance, accessibility audits, and cross-browser matrices unless explicitly added later.

### How to run (checked-in E2E only)

Playwright **`webServer`** runs **`npm run build`** and the mock launcher (see **`playwright.config.ts`**). You do **not** start **`npm run dev`** or **`dotnet run`** for the default suite.

```powershell
cd archiforge-ui

# One-time per machine (Chromium for the configured project):
npx playwright install --with-deps chromium

npm run test:e2e
```

Optional: **`npm run typecheck:e2e`** — TypeScript check for **`e2e/`** only.

**From the repo root:** **`test-ui-smoke.cmd`** / **`test-ui-smoke.ps1`** ( **`npm ci`**, browser install, **`npm run test:e2e`** ).

**Release smoke (optional):** repo root **`.\release-smoke.ps1 -RunPlaywright`** runs this same **`npm run test:e2e`** after the usual release-smoke steps. That UI gate uses **archiforge-ui’s mocks**, not the C# API instance **`release-smoke`** may have started for steps 5–6 — see **[docs/RELEASE_SMOKE.md](../../../docs/RELEASE_SMOKE.md)**.

### Manual testing against a live API

To exercise the shell against a real **`ArchLucid.Api`**, run **`npm run dev`**, set **`ARCHIFORGE_API_BASE_URL`** in **`.env.local`**, start the API, and use the browser manually (or add **local-only** tests). The committed Playwright suite is intentionally **mock-backed** so CI does not depend on SQL or the .NET stack.

### How to write a Playwright test

```ts
import { test, expect } from "@playwright/test";

test("home page has ArchLucid heading", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "ArchLucid", level: 1 })).toBeVisible();
});
```

Tests live in **`e2e/`** as **`*.spec.ts`** (see **`playwright.config.ts`** **`testDir`**).

---

## 9. Troubleshooting common issues

### "npm test fails with module resolution errors"

```
Error: Cannot find module '@/lib/api'
```

**Cause:** Path aliases (`@/`) need Vitest configuration.  
**Fix:** Check `vitest.config.ts` has the alias:
```ts
resolve: {
  alias: { "@": path.resolve(__dirname, "src") },
},
```

### "Tests pass locally but npm run build fails"

**Cause:** `npm run build` runs TypeScript type checking + ESLint. Tests run in Vitest which is more lenient.  
**Fix:** Run `npm run build` before committing. Fix type errors and ESLint warnings.

### "I see 'ReferenceError: document is not defined'"

**Cause:** A test is importing a server component that uses `"use server"` APIs, or a test environment is not set to `jsdom`.  
**Fix:** Check `vitest.config.ts` has `environment: "jsdom"`.

### "My component renders differently in tests vs the browser"

**Cause:** Tests use jsdom which does not support CSS, `window.matchMedia`, or real layout.  
**Fix:** For visual/layout testing, use Playwright E2E tests. Unit tests verify structure and logic, not appearance.

### "Playwright passes but the UI fails against my real API"

**Cause:** E2E tests use **fixtures and loopback mocks**, not your C# API or data.  
**Fix:** Validate integration manually or add separate tests; see [section 8 — E2E tests (Playwright)](#8-e2e-tests-playwright).

### "The dev server shows a blank page or 'Internal Server Error'"

**Cause:** Usually a server component threw an error (API unreachable, missing env var, bad import).  
**Fix:**
1. Check the terminal where `npm run dev` is running — error messages appear there.
2. Verify `.env.local` has `ARCHIFORGE_API_BASE_URL=http://localhost:5128`.
3. Verify the C# API is running: `curl http://localhost:5128/api/authority/projects/default/runs`.

### "I get CORS errors in the browser console"

**Cause:** Browser code is calling the C# API directly instead of going through the proxy.  
**Fix:** All `fetch()` calls from `api.ts` should go through `/api/proxy` when `isBrowser()` is true. If you added a new fetch call, make sure it uses `apiGet()` or `apiPostJson()`, not raw `fetch()`.

### "TypeError: Cannot read properties of undefined (reading 'runId')"

**Cause:** The API returned a different shape than expected, and the code did not check for it.  
**Fix:** Add a coerce function for this endpoint and handle the `{ ok: false }` case.

### "ESLint warns about unused variables after editing"

**Cause:** You imported something or declared a variable that is no longer used.  
**Fix:** Remove the unused import/variable. ESLint rules:
- `@typescript-eslint/no-unused-vars` — warns on unused variables
- Check the ESLint output for the specific variable name and file

### "PowerShell doesn't accept && between npm commands"

**Cause:** PowerShell uses `;` to chain commands, not `&&` (which is a bash/cmd operator).  
**Fix:** Use `;` instead:
```powershell
npm run build; npm test
```

---

## 10. Debugging techniques

### Console logging (server components)

Add `console.log(...)` in a server component. Output appears in the **terminal** where `npm run dev` is running (not in the browser).

```tsx
export default async function RunsPage() {
  const raw = await listRunsByProject("default", 20);
  console.log("API returned:", JSON.stringify(raw).slice(0, 200));
  // ...
}
```

### Console logging (client components)

Add `console.log(...)` in a client component. Output appears in the **browser DevTools console** (F12).

```tsx
"use client";
export default function GraphPage() {
  const [result, setResult] = useState(null);
  
  async function handleLoad() {
    const raw = await apiGet("/api/graph/...");
    console.log("Graph data:", raw);  // visible in browser console
    // ...
  }
}
```

### Inspecting network requests

1. Open browser DevTools (F12) → **Network** tab.
2. Click a button or navigate to trigger a fetch.
3. Look for requests to `/api/proxy/...`.
4. Click a request → **Response** tab to see the raw JSON.
5. If the request failed, the **Status** column shows the HTTP code.

### Inspecting the rendered DOM

1. Open browser DevTools (F12) → **Elements** tab.
2. Click the inspector icon (top-left of DevTools) → click an element on the page.
3. The DOM tree highlights that element. You can see its attributes, styles, and text content.

### Using the React DevTools extension

Install the [React Developer Tools](https://react.dev/learn/react-developer-tools) browser extension. It adds a "Components" tab to DevTools where you can:
- See the component tree (which components are rendered)
- Inspect props and state of each component
- Change state values live to test edge cases

### Debugging Vitest tests

```powershell
# Run tests with verbose output:
npx vitest run --reporter=verbose

# Run a single test file:
npx vitest run src/lib/operator-response-guards.test.ts

# Show test names:
npx vitest run --reporter=verbose 2>&1 | Select-String "✓|✗"
```

For breakpoint debugging in VS Code / Cursor:
1. Add a `debugger;` statement in the test or component.
2. Run: `npx vitest run --inspect-brk` (opens a debug port).
3. Attach the VS Code / Cursor debugger.
