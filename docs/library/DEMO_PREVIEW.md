> **Scope:** Marketing and API integrators — public demo commit-page preview route, caching, and privacy boundaries; not production tenant configuration.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Demo commit-page preview (`/demo/preview`)

## Objective

Give **marketing visitors** a **read-only** view of what the operator **commit page** looks like for the latest **committed demo-seed** run — sourced from the same ArchLucid services as production (`IRunRepository`, `IAuthorityQueryService`, `IRunExplanationSummaryService`, artifacts, pipeline timeline), **without** an account, API key, or operator install.

## Why it exists

- **Buyer outcome:** a sponsor can follow **`/welcome` → “See a real commit page” → `/demo/preview`** and see a credible, live-shaped page instead of a static screenshot.
- **Anchored in real services:** the payload is assembled server-side under the hard-pinned demo scope (same pattern as **`GET /v1/demo/explain`**).
- **Cheap under spikes:** marketing links can go viral; the route is **cached** so repeat views do not hammer SQL.

## API contract (`GET /v1/demo/preview`)

- **Auth:** **`[AllowAnonymous]`** — no `Authorization` header.
- **Gate:** **`[FeatureGate(FeatureGateKey.DemoEnabled)]`** — when **`Demo:Enabled`** is not **`true`**, the deployment returns **`404`** (no route hint).
- **Rate limit:** **`[EnableRateLimiting("fixed")]`** — same window as **`/v1/demo/explain`**.
- **Body:** **`DemoCommitPagePreviewResponse`** (camelCase JSON) — run header, authority chain ids, manifest summary, first **10** pipeline timeline rows, artifact descriptors (no download URLs), aggregate **`RunExplanationSummary`**.
- **HTTP cache:** **`Cache-Control: public, max-age=300, s-maxage=300, stale-while-revalidate=60`**; **`ETag`** = SHA-256 over the serialized JSON body; **`304 Not Modified`** when **`If-None-Match`** matches.
- **In-process cache:** **`IHotPathReadCache`** with TTL **`Demo:PreviewCacheSeconds`** (default **300**, clamped **30–3600**). Stable key **`demo-preview:bundle:v1:latest`** — the **resolved run id and manifest** live **inside** the cached value so a **re-seed** that creates a new run id still hot-swaps on the **next cache miss** (there is **no** manual flush API).

## Marketing UI (`archlucid-ui`)

- **Route:** **`src/app/(marketing)/demo/preview/page.tsx`** — **no** operator sidebar; uses the marketing chrome from **`(marketing)/layout.tsx`**.
- **Fetch:** server-side **`fetch`** to **`NEXT_PUBLIC_DEMO_PREVIEW_API_BASE`** (trimmed, no trailing slash), falling back to **`ARCHLUCID_API_BASE_URL`** then **`NEXT_PUBLIC_ARCHLUCID_API_BASE_URL`** — **not** `/api/proxy` (proxy adds operator auth).
- **ISR:** **`export const revalidate = 300`** aligns with the API TTL.
- **Robots:** metadata sets **`noindex, nofollow`** so demo numbers are not indexed as production telemetry.
- **Banner:** amber **demo tenant** strip (same wording family as **`/demo/explain`**).

## Cache staleness after re-seed

There is **no** distributed invalidation hook. After **`POST /v1/demo/seed`** (or **`archlucid try`**), **`/demo/preview`** may show **pre-reseed** data for up to **TTL** (default five minutes) on each API instance.

## Privacy / data shape

- Demo seed uses **fictional Contoso** identifiers; responses always include **`isDemoData: true`**.
- **No** anonymous artifact downloads — the artifacts table is **read-only**.

## Production safety

Hosts without **`Demo:Enabled=true`** return **`404`** for the API route; the marketing page renders the friendly “not available” notice on **HTTP 404**.

## Telemetry

Counters (no `tenant_id` label — single demo tenant):

- **`archlucid.demo.preview.cache_hit_total`**
- **`archlucid.demo.preview.cache_miss_total`**

## Follow-ups

- **Playwright E2E** for `/demo/preview` in a seeded CI host (not implemented here); track when marketing E2E harness exists.
