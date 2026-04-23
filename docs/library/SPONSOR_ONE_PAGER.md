> **Scope:** Sponsor one-pager PDF — API, CLI, tier gate, and how it relates to pilot ROI docs.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Sponsor one-pager PDF

## Objective

Give sponsors a **single-page** PDF that ties one committed (or in-flight) architecture run to **30-day pilot aggregates** — without replacing canonical ROI narratives in `docs/EXECUTIVE_SPONSOR_BRIEF.md` or `docs/go-to-market/ROI_MODEL.md`.

## API

- **Route:** `POST /v1/pilots/runs/{runId}/sponsor-one-pager`
- **Auth:** `ReadAuthority` (inherits from `PilotsController`).
- **Tier:** **`RequiresCommercialTenantTier(Standard)`** — below Standard returns **402** with the standard packaging problem type.
- **Response:** `application/pdf` bytes (`QuestPDF`, community license at generation time).

## CLI

```bash
archlucid sponsor-one-pager <runId> [--save]
```

Without `--save`, the CLI prints **Base64** of the PDF to stdout (safe for binary). With `--save`, writes `sponsor-one-pager-<runId>.pdf` in the current directory. Uses `ARCHLUCID_API_KEY` when set (same as `first-value-report`).

## Data sources

- **Run headline:** `IRunDetailQueryService` / `ArchitectureRunDetail` (wall-clock hours when `CompletedUtc` is present; manifest headline fields when committed).
- **Pilot window:** `PilotScorecardBuilder` over the tenant scope (**last 30 days**, UTC) — committed ratio and illustrative bar mix (not dollar estimates).

## Operational considerations

- **Cost:** PDF generation is CPU-bound per request; keep behind Standard tier and existing rate limits on `PilotsController`.
- **Reliability:** Returns **404** when the run cannot be resolved for the current scope (same semantics as other pilot read routes).
