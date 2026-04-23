# ArchLucid Operator Shell — Architecture

This document describes the architectural design of the ArchLucid operator shell (`archlucid-ui`). It follows the same C4-style conventions used in the backend architecture docs (`docs/ARCHITECTURE_CONTEXT.md`, `docs/ARCHITECTURE_CONTAINERS.md`, etc.) and the project's required architecture output sections.

---

## 1. Objective

Provide a thin, read-mostly operator interface for ArchLucid that lets an operator inspect runs, review manifests and artifacts, compare runs, replay authority chains, explore provenance graphs, manage alerts and advisory workflows, and download exports — all backed by the **ArchLucid** C# API.

The UI is **not** a general-purpose SPA. It is a dashboard for operators who understand the ArchLucid domain. Design priority is determinism and clarity over interactivity.

---

## 2. Assumptions

| Assumption | Implication |
|-----------|-------------|
| Operators are technical users (architects, engineers, SREs) | Minimal hand-holding UX; show IDs, hashes, raw metadata |
| The C# API is the single source of truth | UI never writes to a database; all mutations go through API endpoints |
| Runs, manifests, and artifacts are immutable once committed | Caching is not implemented but could be added safely (cache-on-read) |
| The API is deployed behind private endpoints in production | The UI proxy route adds API keys; direct browser-to-API access is not assumed |
| Development-bypass auth is the default for local dev | No OAuth/OIDC flow is wired yet; the UI has a stub for JWT when ready |
| Artifact content is UTF-8 text (markdown, JSON, Mermaid source) | Binary artifacts (images, compiled output) are not previewed in-shell |

---

## 3. Constraints

| Constraint | Reason |
|-----------|--------|
| No heavy client-side state management (Redux, Zustand) | Read-mostly dashboard with no cross-page state; simplicity over power |
| No CSS framework or design system (Tailwind, MUI, etc.) | Keeps the dependency surface minimal; inline styles for now |
| No component library beyond React Flow (for graphs) | Avoids lock-in; shell components are small enough to maintain directly |
| Server components by default; `"use client"` only when interactivity requires it | Minimizes JavaScript shipped to the browser; aligns with Next.js App Router best practices |
| All API secrets stay server-side | `ARCHLUCID_API_KEY` is never exposed to the browser; proxy route enforces this |
| TypeScript strict mode | Catches type errors at compile time; all types are explicit |

---

## 4. Architecture Overview

### System context (C4 Level 1)

```
┌──────────────────────────────────────────────────────────────┐
│                      OPERATOR (browser)                      │
│                                                              │
│  Navigates to operator shell pages.                          │
│  Sees runs, manifests, artifacts, comparisons, graphs.       │
│  Downloads ZIPs, DOCX packages.                              │
└────────────────────────┬─────────────────────────────────────┘
                         │ HTTPS (port 3000)
                         ▼
┌──────────────────────────────────────────────────────────────┐
│               ARCHLUCID OPERATOR SHELL                      │
│               (Next.js 15 / Node.js)                         │
│                                                              │
│  Server components render HTML from API data.                │
│  Client components (graph, compare, replay) run in browser.  │
│  Proxy route forwards browser API calls with credentials.    │
└────────────────────────┬─────────────────────────────────────┘
                         │ HTTP (port 5128, internal)
                         ▼
┌──────────────────────────────────────────────────────────────┐
│               ARCHLUCID C# API                              │
│               (ASP.NET Core)                                 │
│                                                              │
│  Authority, artifact, comparison, replay, graph,             │
│  advisory, alert, policy-pack, and governance endpoints.     │
└──────────────────────────────────────────────────────────────┘
```

### Container view (C4 Level 2)

The operator shell is a single Next.js application. Inside it are four logical containers:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     ARCHLUCID OPERATOR SHELL                           │
│                                                                         │
│  ┌───────────────────────┐  ┌────────────────────────────────────────┐  │
│  │  SERVER COMPONENTS    │  │  CLIENT COMPONENTS                     │  │
│  │  (run on Node.js)     │  │  (run in browser)                     │  │
│  │                       │  │                                        │  │
│  │  • Home page          │  │  • Graph viewer (React Flow)           │  │
│  │  • Run list           │  │  • Compare form + results              │  │
│  │  • Run detail         │  │  • Replay form + results               │  │
│  │  • Manifest detail    │  │  • Ask (conversational)                │  │
│  │  • Artifact review    │  │  • Search                              │  │
│  │  • Loading fallbacks  │  │  • Advisory / alerts / policy-packs    │  │
│  │                       │  │  • All interactive forms                │  │
│  │  Fetch API data       │  │                                        │  │
│  │  directly via HTTP    │  │  Fetch API data via /api/proxy         │  │
│  │  (server-to-server)   │  │  (browser-to-proxy-to-server)          │  │
│  └───────────┬───────────┘  └──────────────────┬─────────────────────┘  │
│              │                                  │                       │
│              ▼                                  ▼                       │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  API LAYER (src/lib/)                                           │   │
│  │                                                                  │   │
│  │  • api.ts — all fetch functions (apiGet, apiPostJson, …)        │   │
│  │  • resolveRequest() — routes server vs browser calls            │   │
│  │  • operator-response-guards.ts — coerce* shape validators       │   │
│  │  • artifact-review-helpers.ts — artifact type labels, formatters│   │
│  │  • graph-api.ts, advisory-api.ts — domain-specific wrappers     │   │
│  │  • config.ts, scope.ts, auth-config.ts — env + scope + auth     │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  PROXY ROUTE (src/app/api/proxy/[...path]/route.ts)             │   │
│  │                                                                  │   │
│  │  • Catch-all: /api/proxy/* → forwards to C# API                │   │
│  │  • Adds X-Api-Key from env (server-side secret)                 │   │
│  │  • Adds scope headers (x-tenant-id, x-workspace-id, etc.)      │   │
│  │  • Passes through Authorization header from browser             │   │
│  │  • Streams response body (Content-Type, Content-Disposition)    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  TYPE SYSTEM (src/types/)                                       │   │
│  │                                                                  │   │
│  │  TypeScript types mirroring C# DTOs.                            │   │
│  │  authority.ts, graph.ts, comparison.ts, explanation.ts,         │   │
│  │  alerts.ts, advisory.ts, policy-packs.ts, governance, etc.      │   │
│  └──────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 4a. Role-aware UI shaping (first wave, implemented)

The shell **does** shape navigation (and light Enterprise copy) using the authenticated principal from **`GET /api/proxy/api/auth/me`** plus optional per-link **`requiredAuthority`** aligned with API policy names. **`loadCurrentPrincipal`** in `current-principal.ts` normalizes `/me` into **`CurrentPrincipal`** (including **`authorityRank`**); **`OperatorNavAuthorityProvider`** holds that read-model as **`currentPrincipal`** and mirrors **`callerAuthorityRank`** for **`listNavGroupsVisibleInOperatorShell`** / **`filterNavLinksForOperatorShell`**. Enterprise **`LayerHeader`** strips add **`layerHeaderEnterpriseReaderRankLine`** or **`layerHeaderEnterpriseOperatorRankLine`** from **`enterprise-controls-context-copy.ts`** (below **`enterpriseFootnote`** in **`layer-guidance.ts`**). **Intent:** operational accountability and less arbitrary hiding—**not** pricing, billing, or entitlements (see repo **`docs/PRODUCT_PACKAGING.md`** *Role-based restriction* vs *Future entitlement*, and **`docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md`** Stage 1).

| Layer | Shaping posture |
|-------|-----------------|
| **Core Pilot** | Keep default links broadly visible; avoid sprinkling `requiredAuthority` on first-pilot essentials without an explicit product decision. |
| **Enterprise Controls** | Primary target for `requiredAuthority`, nav omission, and short context lines. |

**Source files:** `src/lib/nav-config.ts`, `src/lib/nav-authority.ts`, `src/lib/current-principal.ts`, `src/lib/nav-shell-visibility.ts`, `src/components/OperatorNavAuthorityProvider.tsx`, `src/lib/enterprise-controls-context-copy.ts`, `src/components/EnterpriseControlsContextHints.tsx`, `src/lib/layer-guidance.ts`. **Product doc:** [../../docs/PRODUCT_PACKAGING.md](../../docs/library/PRODUCT_PACKAGING.md).

---

## 5. Component Breakdown

### 5.1 Route pages (src/app/)

| Route | Component | Server/Client | Responsibility |
|-------|-----------|--------------|----------------|
| `/` | `HomePage` | Server | Static landing with quick links |
| `/runs` | `RunsPage` | Server | List runs for a project, coerce + render table |
| `/runs/[runId]` | `RunDetailPage` | Server | Run metadata, authority chain, manifest summary, artifact table, downloads |
| `/runs/[runId]/artifacts/[artifactId]` | (redirect page) | Server | Resolves run → manifest, redirects to canonical artifact URL |
| `/manifests/[manifestId]` | `ManifestDetailPage` | Server | Manifest summary, artifact table, bundle download |
| `/manifests/[manifestId]/artifacts/[artifactId]` | (artifact review page) | Server | Artifact metadata, content preview (pretty + raw), sibling navigation |
| `/graph` | `GraphPage` | Client | Run ID input, mode selector, graph loading, React Flow rendering |
| `/compare` | `ComparePage` | Client | Two run IDs, parallel fetch (legacy + structured + AI), three result views |
| `/replay` | `ReplayPage` | Client | Run ID input, mode selector, replay submission, validation display |
| `/ask` | `AskPage` | Client | Conversational AI interface for architecture questions |
| `/search` | `SearchPage` | Client | Semantic search across run content |
| `/advisory` | `AdvisoryPage` | Client | Improvement plans, recommendation governance |
| `/alerts` | `AlertsPage` | Client | Alert list with lifecycle actions |
| `/alert-rules` | `AlertRulesPage` | Client | Simple alert rule CRUD |
| `/composite-alert-rules` | `CompositeRulesPage` | Client | Multi-condition rule CRUD |
| `/alert-simulation` | `AlertSimulationPage` | Client | Rule simulation and candidate comparison |
| `/alert-tuning` | `AlertTuningPage` | Client | Threshold recommendation with noise scoring |
| `/alert-routing` | `AlertRoutingPage` | Client | Routing subscription CRUD + delivery attempts |
| `/digests` | `DigestsPage` | Client | Architecture digest browsing |
| `/digest-subscriptions` | `DigestSubscriptionsPage` | Client | Subscription CRUD + delivery attempts |
| `/advisory-scheduling` | `AdvisorySchedulingPage` | Client | Scan schedule CRUD + execution history |
| `/recommendation-learning` | `LearningPage` | Client | Learning profile display + rebuild |
| `/policy-packs` | `PolicyPacksPage` | Client | Pack creation, publishing, assignment |
| `/governance-resolution` | `GovernanceResolutionPage` | Client | Effective resolution display |

### 5.2 Shared components (src/components/)

```
OperatorShellMessage.tsx
├── OperatorErrorCallout      (HTTP / network failures)
├── OperatorEmptyState        (valid empty results)
├── OperatorLoadingNotice     (loading fallbacks)
├── OperatorMalformedCallout  (bad JSON shapes)
└── OperatorWarningCallout    (non-fatal secondary failures)

ArtifactListTable.tsx         (artifact table with review + download)
ArtifactReviewContent.tsx     (pretty + raw content panel)
GraphViewer.tsx               (React Flow wrapper with type filter + detail panel)
SectionCard.tsx               (bordered section container)
AuthPanel.tsx                 (dev bypass + OIDC sign-in strip)

compare/
├── StructuredComparisonView.tsx     (golden manifest delta tables)
├── LegacyRunComparisonView.tsx      (flat diff display)
└── AiComparisonExplanationView.tsx  (LLM narrative display)
```

### 5.3 API layer (src/lib/)

```
api.ts
├── resolveRequest()              ← Routes server vs browser calls
├── resolveBinaryGetRequest()     ← Same routing, Accept: */* for artifacts
├── apiGet<T>() / apiPostJson<T>()← Core fetch primitives
├── listRunsByProject()           ← Typed wrapper → GET /api/authority/projects/{id}/runs
├── getRunDetail()                ← Typed wrapper → GET /api/authority/runs/{id}
├── getManifestSummary()          ← Typed wrapper → GET /api/authority/manifests/{id}/summary
├── listArtifacts()               ← Typed wrapper → GET /api/artifacts/manifests/{id}
├── getArtifactDescriptor()       ← Typed wrapper → artifact metadata
├── fetchArtifactContentUtf8()    ← Binary fetch + UTF-8 decode + truncation
├── compareRuns()                 ← Legacy flat-diff comparison
├── compareGoldenManifestRuns()   ← Structured golden manifest comparison
├── explainComparisonRuns()       ← AI comparison explanation
├── replayRun()                   ← Authority chain replay
├── getArtifactDownloadUrl()      ← Proxy URL for browser download
├── getBundleDownloadUrl()        ← Proxy URL for ZIP download
├── getRunExportDownloadUrl()     ← Proxy URL for run export
├── getArchitecturePackageDocxUrl()← Proxy URL for DOCX package
└── (40+ more wrappers for alerts, advisory, policy, governance, etc.)

operator-response-guards.ts
├── isRecord()                    ← Private helper
├── coerceRunSummaryList()        ← Validates RunSummary[]
├── coerceRunDetail()             ← Validates RunDetail
├── coerceManifestSummary()       ← Validates ManifestSummary
├── coerceArtifactDescriptorList()← Validates ArtifactDescriptor[]
├── coerceArtifactDescriptor()    ← Validates single ArtifactDescriptor
├── coerceGraphViewModel()        ← Validates GraphViewModel
├── coerceGoldenManifestComparison()← Validates GoldenManifestComparison
├── coerceRunComparison()         ← Validates RunComparison
├── coerceReplayResponse()        ← Validates ReplayResponse
└── coerceComparisonExplanation() ← Validates ComparisonExplanation

artifact-review-helpers.ts
├── ARTIFACT_TYPE_COPY            ← Labels + descriptions for artifact types
├── classifyArtifactView()        ← Determines rendering format (markdown/json/mermaid/plain)
├── getArtifactTypeLabel()        ← Human label for an artifact type
├── getArtifactTypeDescription()  ← One-line description
└── prepareArtifactBodyText()     ← JSON pretty-print, raw vs readable
```

---

## 6. Data Flow

### 6.1 Server component flow (runs, manifests, artifacts)

```
  Browser GET /runs?projectId=default
         │
         ▼
  Next.js matches app/runs/page.tsx
         │
         ▼
  app/runs/loading.tsx shown (OperatorLoadingNotice)
         │
         ▼
  RunsPage() executes on Node.js server
         │
         ├── listRunsByProject("default", 20)
         │     │
         │     └── resolveRequest("/api/authority/projects/default/runs")
         │           │
         │           ├── isBrowser() === false → direct HTTP to C# API
         │           ├── Adds X-Api-Key header from process.env
         │           └── Adds scope headers (x-tenant-id, etc.)
         │
         ├── C# API responds with JSON → apiGet<T> returns parsed body
         │
         ├── coerceRunSummaryList(raw)
         │     ├── ok: true → runs = items
         │     └── ok: false → malformedMessage = message
         │
         └── Renders JSX → HTML
               ├── loadError? → OperatorErrorCallout (red)
               ├── malformedMessage? → OperatorMalformedCallout (purple)
               ├── runs.length === 0? → OperatorEmptyState (gray)
               └── runs.length > 0? → <table> with run rows
         │
         ▼
  HTML sent to browser (no JS needed for display)
```

### 6.2 Client component flow (compare, replay, graph)

```
  Browser loads /compare (JavaScript bundle hydrates)
         │
         ▼
  CompareForm() renders in browser
         │
         ├── useState() for leftRunId, rightRunId, loading, result, error, …
         │
         ├── Operator types two run IDs and clicks "Compare"
         │     │
         │     └── onClick → onCompare() async handler
         │           │
         │           ├── compareRuns(left, right)
         │           │     │
         │           │     └── resolveRequest(path)
         │           │           │
         │           │           ├── isBrowser() === true → /api/proxy/api/authority/compare/…
         │           │           └── Does NOT add X-Api-Key (stays server-side)
         │           │
         │           └── Browser fetches /api/proxy/…
         │                 │
         │                 ▼
         │           Proxy route (route.ts)
         │                 │
         │                 ├── Adds X-Api-Key from env
         │                 ├── Adds scope headers
         │                 └── Forwards to C# API → response back to browser
         │
         ├── coerceRunComparison(raw) → validates shape
         │
         ├── setResult(coerced.value) → React re-renders
         │
         └── StructuredComparisonView / LegacyRunComparisonView / AiComparisonExplanationView
```

### 6.3 File download flow

```
  Browser clicks download link
         │
         ▼
  <a href="/api/proxy/api/artifacts/manifests/{id}/artifact/{id}">
         │
         ▼
  Proxy route (route.ts)
         │
         ├── Adds X-Api-Key
         ├── Forwards GET to C# API
         ├── C# API returns binary body with Content-Disposition header
         └── Proxy passes body + Content-Type + Content-Disposition to browser
         │
         ▼
  Browser saves the file (native download behavior)
```

---

## 7. Security Model

### 7.1 Credential isolation

```
┌─────────────────────────────────────────────┐
│                BROWSER                       │
│                                              │
│  • Never sees ARCHLUCID_API_KEY             │
│  • Never sees ARCHLUCID_API_BASE_URL        │
│  • Sends requests to same-origin /api/proxy  │
│  • May forward Authorization: Bearer (JWT)   │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│         NEXT.JS SERVER (Node.js)             │
│                                              │
│  • Holds ARCHLUCID_API_KEY (env var)        │
│  • Holds ARCHLUCID_API_BASE_URL (env var)   │
│  • Adds X-Api-Key to upstream requests       │
│  • Adds scope headers                        │
│  • Forwards browser Authorization header     │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│         ARCHLUCID C# API                    │
│                                              │
│  • Validates X-Api-Key                       │
│  • Validates scope headers                   │
│  • Returns scoped data                       │
└─────────────────────────────────────────────┘
```

**Why this matters:**

- The `ARCHLUCID_API_KEY` is a server-only secret (`process.env`, not `NEXT_PUBLIC_*`). It never reaches the browser.
- `NEXT_PUBLIC_*` variables are compiled into the JavaScript bundle and visible in DevTools. Only non-secret configuration uses this prefix.
- The proxy route is the single point where server-side credentials are attached. Browser JavaScript never constructs authenticated requests directly.

### 7.2 Auth mode

| Mode | Description | Status |
|------|-------------|--------|
| `development-bypass` | API auto-authenticates a dev principal; no sign-in flow | Default, active |
| `jwt` / `jwt-bearer` | OIDC bearer tokens; `getBearerToken()` returns access token | Stub wired, not implemented |

When JWT mode is activated:
1. Browser obtains a token from an OIDC provider.
2. `getBearerToken()` in `api.ts` returns the token.
3. `resolveRequest()` adds `Authorization: Bearer <token>` to proxy requests.
4. Proxy route forwards the `Authorization` header to the C# API.
5. C# API validates the bearer token.

### 7.3 Scope headers

Every request includes tenant, workspace, and project scope headers:

```
x-tenant-id:    11111111-1111-1111-1111-111111111111
x-workspace-id: 22222222-2222-2222-2222-222222222222
x-project-id:   33333333-3333-3333-3333-333333333333
```

In development, these are hardcoded in `scope.ts`. In production, they would come from user session context. The proxy route merges incoming browser scope headers with dev defaults (browser values take precedence when present).

### 7.4 Network exposure

| Surface | Exposure |
|---------|----------|
| Next.js port (3000) | Public (operator-facing) |
| C# API port (5128) | Private (server-to-server only in production) |
| API key | Server-only env var |
| SMB (port 445) | Never exposed (per workspace security rule) |

### 7.5 Content security

- All artifact content is rendered in `<pre>` elements with `textContent` — no HTML injection.
- `prepareArtifactBodyText()` never uses `dangerouslySetInnerHTML`.
- JSON is parsed with `JSON.parse` and re-serialized with `JSON.stringify` for pretty-printing — no eval.

---

## 8. Operational Considerations

### 8.1 Deployment

| Concern | Approach |
|---------|----------|
| **Build** | `npm run build` produces a standalone Next.js output |
| **Runtime** | Node.js 18+ |
| **Container** | Standard Next.js Dockerfile (multi-stage: install → build → run) |
| **Environment variables** | `ARCHLUCID_API_BASE_URL`, `ARCHLUCID_API_KEY`, `NEXT_PUBLIC_ARCHLUCID_AUTH_MODE` |
| **Health** | Next.js serves `/` as a static page; add a `/api/health` route if needed |
| **Port** | Default 3000, configurable via `-p` flag |

### 8.2 Scalability

| Dimension | Current state | Evolution path |
|-----------|--------------|----------------|
| **Concurrent users** | Single-instance Node.js handles moderate operator load | Scale horizontally behind a load balancer; Next.js is stateless |
| **API call volume** | Every page load fetches fresh data (`cache: "no-store"`) | Add `revalidate` caching for runs/manifests (immutable after commit) |
| **Large artifact preview** | Truncated at 2 MB for in-shell preview | Download link provides full file; could add streaming preview |
| **Graph rendering** | Grid layout with React Flow; works for ~500 nodes | Add server-side layout (dagre/elk) for graphs with 1000+ nodes |

### 8.3 Reliability

| Failure mode | Handling |
|-------------|----------|
| C# API unreachable | `OperatorErrorCallout` with diagnostic message; page still renders shell chrome |
| API returns unexpected JSON shape | `OperatorMalformedCallout` with coerce function message; distinct from HTTP error |
| API returns empty collection | `OperatorEmptyState` with actionable guidance |
| Artifact content fetch fails | `OperatorWarningCallout` alongside valid descriptor metadata; download link still works |
| Browser JavaScript error | Server components continue rendering; only client pages (graph/compare/replay) are affected |

### 8.4 Observability

| Signal | Current state | Recommendation |
|--------|--------------|----------------|
| **Server errors** | Logged to Node.js stdout (visible in `npm run dev` terminal) | Wire structured logging (pino) for production |
| **Client errors** | Logged to browser console | Add error boundary component for crash recovery |
| **API latency** | Not instrumented | Add OpenTelemetry tracing to `apiGet`/`apiPostJson` |
| **Build health** | `npm run build` type-checks + lints | Run in CI as a gate |

### 8.5 Cost considerations

| Item | Cost driver | Mitigation |
|------|------------|------------|
| Node.js runtime | Single small instance | Operator shell is not high-traffic; smallest container SKU is sufficient |
| API calls | One per page load per data source | Add caching for immutable data (committed runs, manifests) |
| React Flow (client) | JavaScript bundle size (50.9 kB for `/graph`) | Only loaded on the graph page; code-split by default |
| No CDN needed | Static assets served by Next.js | Add CDN if latency matters for geographically distributed operators |

---

## 9. Architectural Decisions

### 9.1 Why Next.js App Router (not Pages Router, not Create React App)?

**Decision:** Use Next.js 15 with App Router.

**Reasoning:**
- Server components reduce JavaScript sent to the browser — most pages need zero client-side JS.
- File-system routing eliminates route configuration boilerplate.
- The proxy route (`route.ts`) provides a clean BFF pattern for credential isolation.
- Streaming and Suspense (`loading.tsx`) provide deterministic loading states without custom logic.

**Trade-off:** App Router is newer and has more framework-specific conventions than Pages Router. Operators familiar with older React patterns may need to adjust.

### 9.2 Why no global state management?

**Decision:** No Redux, Zustand, MobX, or Context providers.

**Reasoning:**
- The operator shell is read-mostly. Each page fetches its own data and displays it.
- There is no cross-page state (no cart, no user preferences, no wizard flow).
- Server components cannot use client-side state stores.
- Page-local state (`useState` in client components, `let` in server components) is sufficient.

**Trade-off:** If a future feature requires shared state (e.g. operator preferences, notification queue), a lightweight store would need to be added.

### 9.3 Why coerce functions instead of Zod/Yup/io-ts?

**Decision:** Hand-written `coerce*` functions in `operator-response-guards.ts`.

**Reasoning:**
- Avoids adding a schema validation library to the dependency tree.
- Each function checks only the fields the UI actually uses — not the full API contract.
- Error messages are operator-oriented ("One or more run rows are missing a string runId") rather than generic schema errors.
- The pattern is simple enough that any developer can read and extend it.

**Trade-off:** More boilerplate than a schema library. If the number of coerce functions grows significantly (20+), migrating to Zod would be justified.

### 9.4 Why inline styles instead of a CSS framework?

**Decision:** Inline `style={{ ... }}` objects, no Tailwind/CSS Modules/styled-components.

**Reasoning:**
- The shell has ~15 pages with simple layouts. A CSS framework adds complexity without proportional benefit.
- Inline styles are co-located with the component — no file switching.
- No class name conflicts, no specificity wars, no build-time CSS extraction issues.

**Trade-off:** No responsive design, no theme system, no dark mode. If the shell grows to 50+ pages or needs branding, migrate to CSS Modules or Tailwind.

### 9.5 Why server-to-server fetches instead of client-only?

**Decision:** Server components call the C# API directly from Node.js; only interactive pages use the browser proxy.

**Reasoning:**
- Server-to-server calls are faster (no browser round-trip, no CORS, no proxy hop).
- API key stays on the server; no credential exposure risk.
- Server-rendered HTML is immediately visible — no loading spinner.

**Trade-off:** Data is fetched on every page load (no client-side caching). Acceptable for an operator dashboard; would need rethinking for high-frequency polling.

---

## 10. Interfaces, Services, Data Models, Orchestration

### Interfaces (boundaries)

| Interface | Protocol | Direction |
|-----------|----------|-----------|
| Browser → Next.js server | HTTPS (port 3000) | Inbound: page requests, proxy API calls |
| Next.js server → C# API | HTTP (port 5128) | Outbound: data fetching (server components, proxy route) |
| Browser → Next.js proxy | HTTPS `/api/proxy/*` | Inbound: client component API calls |
| Next.js proxy → C# API | HTTP (port 5128) | Outbound: forwarded requests with credentials |

### Services (logical responsibilities)

| Service | Location | Responsibility |
|---------|----------|----------------|
| Request routing | `api.ts` → `resolveRequest()` | Decides server-direct vs browser-proxy path |
| Shape validation | `operator-response-guards.ts` | Runtime validation of API response JSON |
| Artifact formatting | `artifact-review-helpers.ts` | Type labels, content classification, JSON pretty-print |
| Graph mapping | `graph-mapper.ts` | Converts API graph model to React Flow nodes/edges |
| Credential proxy | `api/proxy/[...path]/route.ts` | Attaches server-side secrets to browser API calls |

### Data models

All data models live in `src/types/` and mirror C# DTOs:

| Model | File | C# DTO |
|-------|------|--------|
| `RunSummary` | `authority.ts` | `RunSummaryResponse` |
| `RunDetail` | `authority.ts` | Run detail envelope |
| `ManifestSummary` | `authority.ts` | `ManifestSummaryResponse` |
| `ArtifactDescriptor` | `authority.ts` | `ArtifactDescriptorResponse` |
| `RunComparison` | `authority.ts` | `RunComparisonResponse` |
| `ReplayResponse` | `authority.ts` | `ReplayResponse` |
| `GoldenManifestComparison` | `comparison.ts` | `ComparisonResult` |
| `GraphViewModel` | `graph.ts` | `GraphViewModel` |
| `ComparisonExplanation` | `explanation.ts` | `ComparisonExplanation` |
| `AlertRule` / `AlertRecord` | `alerts.ts` | Alert DTOs |
| `PolicyPack` / `PolicyPackVersion` | `policy-packs.ts` | Policy pack DTOs |
| (15+ more) | Various | Corresponding C# DTOs |

### Orchestration

There is no explicit orchestration layer. Each page is a self-contained orchestrator:

1. **Server pages** orchestrate in the `async function` body: fetch → coerce → render.
2. **Client pages** orchestrate in event handlers: clear state → fetch → coerce → set state → React re-renders.
3. The **proxy route** orchestrates credential attachment: read env → merge headers → forward → pass through response.

This is intentional. The operator shell is thin — it does not have business logic to orchestrate. All business logic lives in the C# API.

---

## Where to go next

- **Operator workflow (55R, repo root):** `docs/operator-shell.md`
- **Tutorial (for learning):** `archlucid-ui/docs/OPERATOR_SHELL_TUTORIAL.md`
- **C# ↔ React translation:** `archlucid-ui/docs/CSHARP_TO_REACT_ROSETTA.md`
- **Line-by-line code reading:** `archlucid-ui/docs/ANNOTATED_PAGE_WALKTHROUGH.md`
- **Component API reference:** `archlucid-ui/docs/COMPONENT_REFERENCE.md`
- **Data flow diagrams:** `archlucid-ui/docs/DATA_FLOW_AND_STATE.md`
- **Testing guide:** `archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md`
- **Backend architecture:** `docs/ARCHITECTURE_INDEX.md`
