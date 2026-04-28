> **Scope:** Go-to-market and sales-enablement readers reviewing a synthetic Contoso Retail modernization vignette; it is not real customer results, tenant SQL metrics, or a measured pilot write-up.

> **SYNTHETIC — NOT REAL CUSTOMER DATA.** This narrative is fabricated for sales-enablement and DOCX examples. Do not cite figures as observed outcomes without replacing them with measured pilot data.

> **Measurement companion:** [PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md)

# Synthetic case study — Contoso Retail (demo tenant)

**Label:** SYNTHETIC · Contoso Retail Modernization (aligned with trusted-baseline demo seed)

## Executive summary

Contoso Retail is modernizing checkout onto Azure while preserving its existing payment-processor integration. This **synthetic** vignette shows how the same measurement scaffolding used in `ValueReportRawMetrics` can tell a conservative before/after story: shorter review cycles, fewer re-review loops, and less manual evidence assembly.

## Baseline (pre-ArchLucid) — illustrative

| Dimension | Value | Notes |
|-----------|------:|-------|
| Review cycle time (request → reviewable package) | **40 hours** | Sponsor estimate for one representative architecture change |
| Review iterations per change | **3** | Rework driven by ambiguity and missing governance evidence |
| Evidence assembly (narrative, traceability, review pack) | **8 hours** | Manual collation across email, tickets, and slide decks |

## After ArchLucid — illustrative (conservative)

| Dimension | Value | Notes |
|-----------|------:|-------|
| Review cycle time | **12 hours** | Same team, measured over a short pilot window (N small by design) |
| Review iterations | **1.5** (average) | Fewer round-trips because decisions and deltas are explicit |
| Evidence assembly | **2 hours** | Manifest, findings, and exports reduce manual reconstruction |

## Indicative deltas (non-claim)

- **Review-cycle hours saved per cycle:** 40 − 12 = **28 hours** (~**70%** reduction vs the synthetic baseline).
- **Iteration load:** (3 − 1.5) / 3 ≈ **50%** reduction in average iterations (illustrative).
- **Evidence assembly:** (8 − 2) / 8 = **75%** reduction vs the synthetic baseline.

Annualization and payback depend on how many architecture changes your enterprise runs per year; see `PILOT_ROI_MODEL.md` for the guardrails ArchLucid uses in pilot evaluation.

## Illustrative throughput and FTE levers (synthetic)

Assume **six** EA-relevant modernization threads per calendar year matching the **`PILOT_ROI_MODEL.md`** illustrative team sizing (principal + five senior reviewers). Applying the illustrative hour deltas above yields:

| Lever | Synthetic value | Notes |
|-------|-----------------|-------|
| Review-cycle hour savings vs baseline | **168 h/year** (6 × 28 h) | Same 28 h/thread delta as §Indicative deltas |
| Evidence-assembly savings vs baseline | **36 h/year** (6 × 6 h) | From 8 h → 2 h per thread × six threads |

At a **fully loaded blended rate** of **$150/h**, the illustrative **annualized value** lands near **$30.6k/year** from review/evidence deltas alone (**204** professional hours: 168 + 36, × **$150**). This is deliberately conservative versus large-program savings and excludes vendor change costs or cloud spend — it exists to show arithmetic alignment with ROI-model guardrails rather than promised customer outcomes.

## Disclaimer

These numbers are **not** SQL-backed tenant metrics. Replace them with measured pilot data before external publication. The structured twin lives in `SyntheticCaseStudyDataProvider` for DOCX and sample rendering.
