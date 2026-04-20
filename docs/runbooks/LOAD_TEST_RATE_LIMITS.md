> **Scope:** Load test — expensive rate-limit boundary - full detail, tables, and links in the sections below.

# Load test — expensive rate-limit boundary

**Last reviewed:** 2026-04-16

**Objective:** Validate that burst traffic against **expensive** endpoints receives **429** under configured `RateLimiting:Expensive:*` windows.

**Prerequisites:** A running API with known base URL, test identity with **execute** authority, and optional k6 (`brew install k6` / CI image).

## Scripts

- **`scripts/load/k6-expensive-rate-limit.js`** — hammers a placeholder expensive route; **edit the URL** to match your environment (e.g. a safe replay or execute stub).
- **`scripts/load/k6-scenarios.js`** — multi-scenario read load (compare, run detail, advisory recommendations list). See **`scripts/load/README.md`** for env vars and examples.

## Run (example)

```bash
k6 run --vus 30 --duration 60s -e ARCHLUCID_BASE_URL=https://localhost:7123 scripts/load/k6-expensive-rate-limit.js
```

## Interpretation

- Expect a mix of **200/202** and **429** once the window is saturated.
- Correlate with API logs and `RateLimiting` configuration in `appsettings.json`.

**Reliability:** Run against non-production or a dedicated perf tenant; avoid shared production data.

**Cost:** k6 egress + Azure OpenAI (if the chosen route invokes models) — keep routes deterministic or mocked for cost control.
