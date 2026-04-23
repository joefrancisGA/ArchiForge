# ArchLucid — enterprise comparison (one page)

**Audience:** procurement, IT architecture, and security reviewers comparing ArchLucid to legacy EA / GRC platforms.

**Objective:** Summarize the dimensions buyers care about without uncited vendor internals. Competitor columns should paraphrase only your approved positioning matrix (`docs/go-to-market/COMPETITIVE_LANDSCAPE.md` §2.1).

---

## Comparison matrix (themes)

| Dimension | ArchLucid | Incumbent / manual baseline |
|-----------|-----------|-----------------------------|
| Time-to-first committed architecture run | Guided operator path + deterministic demo surfaces for procurement | Often weeks of tenant setup before a credible demo |
| Evidence and traceability | Golden manifest, artifacts, exports, audit events scoped per tenant | Spreadsheets, slide decks, and ad-hoc attachments |
| Tenant isolation | SQL row-level security scoped to tenant / workspace / project | Varies — confirm with your EA vendor DPA |
| LLM usage governance | Configurable quotas and UTC-day token budgets with durable warnings | Often opaque or per-seat pricing only |
| Deployment | Azure-native baseline (private networking patterns in reference architecture) | Varies |

---

## Footnote

This page is the Markdown source for `GET /v1/marketing/enterprise-comparison.pdf` and the procurement pack entry `ENTERPRISE_COMPARISON_ONE_PAGE.md`.
