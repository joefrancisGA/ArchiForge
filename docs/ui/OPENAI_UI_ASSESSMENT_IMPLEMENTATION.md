> **Scope:** For engineers maintaining ArchLucid UI demo/showcase flows; summarizes what shipped for the OpenAI UI assessment path; not a product spec, roadmap, or buyer-facing guarantee.

# OpenAI UI Assessment — Implementation Notes

This note captures what was implemented for the OpenAI UI assessment demo path (operator + marketing). **Do not treat this as a product spec** — it is engineering bookkeeping.

## Objective

Improve screenshot/demo reliability, reduce misleading loading states, align marketing showcase copy with operator timelines, and tighten invalid-route handling.

## What shipped (high level)

| Area | Behavior |
|------|-----------|
| **Teams integration** | Spinner only while loading *and* connection unresolved (`loading && !conn`), so errors are not buried under “Loading…”. |
| **Demo builds** | Screenshot / mock E2E flows document `NEXT_PUBLIC_DEMO_MODE` / `NEXT_PUBLIC_DEMO_STATIC_OPERATOR` (see `archlucid-ui/README.md`, `playwright.mock.config.ts`, screenshot scripts). |
| **Policy packs** | Server redirect for invalid dynamic tokens; **mock E2E** asserts `/governance/policy-packs/undefined` leaves that segment (`demo-readiness.spec.ts`). |
| **Planning plan detail** | Client redirect to `/planning` when `planId` is empty/`undefined`/`null`; skips fetch spinners while redirecting. |
| **Ask run picker** | On API failure + demo mode, synthetic “Claims Intake Modernization Run” option + auto-select static demo run id. |
| **Showcase / demo preview** | Single hero banner (“Demo data”) when demo mode; **suppress duplicate** status banner inside `DemoPreviewMarketingBody` on `/showcase`; **Review trail** uses `AuthorityPipelineTimeline` (merged duplicate “Pipeline timeline” section); demo mode **omits per-event technical `<details>`** on the timeline to reduce raw UUID noise (chain IDs remain under collapsed technical details). |
| **Canonical demo naming** | Static showcase payload manifest summary leads with **Finalized Architecture Manifest**; outcome strip cards use **Manifest finalized**, **PHI Minimization Risk**. |
| **Tests** | `pipeline-event-type-labels.test.ts`; extended `AuthorityPipelineTimeline.test.tsx`; updated `page.test.tsx` for merged review-trail section. |

## Diagram (logical flow)

```text
[Marketing /showcase] --> ShowcaseHero (demo banner)
       |
       +--> DemoPreviewMarketingBody (optional suppress inner banner)
                 |
                 +--> ShowcaseOutcomeStrip (deep links)
                 +--> Review trail --> AuthorityPipelineTimeline (omit technical details in demo)
                 +--> Technical details <details> (authority chain ids)
```

## Security / reliability / cost

- **Security:** Invalid route tokens redirect instead of hitting downstream loaders with bogus IDs; no new public endpoints.
- **Reliability:** Fewer stuck loading states; Ask + planning degrade to static demo content when configured.
- **Cost:** Negligible — UI-only; E2E adds one navigation assertion.

## Operational verification

- Mock E2E: `npx playwright test -c playwright.mock.config.ts e2e/demo-readiness.spec.ts`
- Unit tests (delta): `npx vitest run src/lib/pipeline-event-type-labels.test.ts src/components/AuthorityPipelineTimeline.test.tsx "src/app/(marketing)/demo/preview/page.test.tsx" --run`
