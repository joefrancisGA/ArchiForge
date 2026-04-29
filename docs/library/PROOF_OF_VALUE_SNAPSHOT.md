> **Scope:** Proof-of-value sponsor snapshot — how to assemble real-mode timings, API load-test JSON, Pilot ROI deltas, and explainability completeness into one dated evidence narrative; **not** a financial guarantee or substitute for purchaser legal diligence.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md).


# Proof-of-value snapshot assembly

Use this playbook when stakeholders ask for **one** cohesive evidence package after a staged evaluation: **time-to-manifest** under real AOAI (full authority run), **API stability** under scripted concurrent traffic (**k6** summary JSON), **tangible savings** modeled with the Pilot ROI workbook (see **`docs/library/PILOT_ROI_MODEL.md`**, templates in **`docs/go-to-market/ROI_MODEL.md`**), and **explainability completeness** (**`ExplainabilityTrace`**) surfaced per finding via the deterministic explainability endpoint.

---

## 1. Objective

Deliver a reproducible dossier tying four independent signals:

1. **Throughput of the pilot workflow** — end-to-end wall clock from scripted create → committed manifest (**`benchmark-real-mode-e2e.ps1`**).
2. **Service behavior under scripted load** — aggregate HTTP latency and failure rate — **`k6`** `--summary-export` (**`tests/load/ci-smoke.js`** is the smallest merge-blocking profile; fuller baselines live in **`LOAD_TEST_BASELINE.md`**).
3. **Economics grounded in organizational inputs** — values copied from Pilot ROI spreadsheets or first-value Markdown tables (**`MEASURED_VS_BASELINE`**, deltas in **`PILOT_ROI_MODEL.md`** §4-5 narratives).
4. **Auditability completeness** — per-finding **`traceCompletenessRatio`** (**`GET /v1/explain/runs/{runId}/findings/{findingId}/explainability`**) aggregated qualitatively (table of sample findings + Prometheus histogram **`archlucid_explainability_trace_completeness_ratio`** in **`OBSERVABILITY.md`** when telemetry export is configured).

---

## 2. Assumptions

- Evidence is sourced from **non-production** stacks unless procurement explicitly sanctions production sampling.
- You will **redact** tenant-identifying strings before sharing externally (run IDs alone are often acceptable; customer names belong in prose, not pasted SQL).
- The benchmark JSON (**`artifacts/benchmark-real-mode-latest.json`**) is **gitignored** — store copies beside this narrative snapshot (SharePoint attachment, Evidence folder, Deal Room binder).

---

## 3. Constraints

- **Secrets never appear**: benchmark JSON exposes only **`environment.apiKeyEnvPresent`** booleans — never keys.
- k6 summaries contain **rates and percentiles**, not customer payloads.
- Regulatory sign-off (**SOC 2**, **legal**, **procurement**) remains separate from engineering evidence.

---

## 4. Inputs (minimal artifact set)

| Signal | Canonical source | Retrieval |
| --- | --- | --- |
| Full-run timing | Stable JSON (**`schemaVersion` `1.0.0`**, **`kind` `archlucid.benchmark.realModeE2e.v1`**) | Run **`scripts/benchmark-real-mode-e2e.ps1`** (`-SkipArtifact` for CI-only stdout) — see **`REAL_MODE_BENCHMARK.md`**. |
| Load-test envelope | **`k6-ci-summary.json`** (CI) or **`k6-summary.json`** (manual **`record_baseline`**) | `k6 run ... --summary-export path.json`; see **K6 summary export JSON** in **`REAL_MODE_BENCHMARK.md`**. |
| ROI narrative | Pilot ROI tables / computed deltas | Copy cells from spreadsheets aligned with **`PILOT_ROI_MODEL.md`** measurement rows; reconcile with **`docs/go-to-market/ROI_MODEL.md`** value levers — avoid double-counted savings categories. |
| Trace completeness score | Persisted **`ExplainabilityTrace`** | Call explainability REST shape returning **`traceCompletenessRatio`** and **`missingTraceFields`** (**`ExplanationController`**), or correlate OTel Prometheus metric family documented in **`OBSERVABILITY.md`**. |

---

## 5. Assembly workflow

1. **Capture benchmark JSON** immediately after each formal pilot run scenario (warm stack, AOAI quotas healthy). Archive with filename `benchmark-real-mode-<YYYYMMDD>-run-<suffix>.json` if retaining multiple attempts.
2. **Capture k6 summary** from the tier you need: merge-blocking CI (**`tests/load/ci-smoke.js`**) for operator API smoke, or Compose baseline (**`tests/load/hotpaths.js`**) for deeper hot-path evidence — store JSON alongside the benchmark file.
3. **Snapshot ROI numbers** from the pilot workbook (hours saved, delta vs baseline cost, compliance gap reduction the customer confirmed) — paste into the template table below.
4. **Sample explainability** on 3–5 representative findings (high/medium severity mix). Record **`traceCompletenessRatio`** and note whether **`missingTraceFields`** is empty for each.
5. **Write the sponsor one-pager** using **§6 template** (export to PDF or slide — both start from the same Markdown).

---

## 6. Sponsor narrative template (copy into an email or deck)

| Field | Value |
| --- | --- |
| **Evidence date / UTC window** |  |
| **Environment note** | e.g., dedicated pilot tenant vs. shared sandbox |
| **Real-mode benchmark** (`totalMs`, `targets.totalWallClockUnderFiveMinutes`) |  |
| **k6** — global `http_req_duration` **`p(95)`** ms; `http_req_failed` rate |  |
| **Pilot ROI deltas** — hours reclaimed / modeled annual savings bracket | cite spreadsheet row refs |
| **Explainability completeness** — min / avg ratio across sampled findings |  |

**Storyline bullets (adapt):**

1. We measured **X** minutes from request to committed manifest under real Azure OpenAI configuration — **meets / does not meet** the **< 5 minute** design target (see **`targets.totalWallClockUnderFiveMinutes`**).
2. Concurrent API smoke shows **p95 Y ms** with **failed rate Z** under the recorded k6 profile — **stable / needs capacity follow-up** before broad rollout.
3. Modeled savings using customer-supplied hours and risk assumptions land at **$A–$B** annualized (methodology: **`PILOT_ROI_MODEL.md`**, assumptions column completed by **< role >**).
4. Explainability traces average **ratio R** across pilot findings — evidence is structured for auditor follow-up (**`ExplainabilityTrace`** fields present / gaps listed per finding).

---

## 7. Security

- Never attach raw API keys, PATs, or Azure OpenAI endpoint keys.
- If sharing HTTP captures, strip cookies and auth headers.
- Treat committed manifest excerpts as **confidential** unless the customer approves distribution.

---

## 8. Operational considerations

- Re-run the bundle after **material** changes: model deployment swap, token budget change, new region, or SQL tier lift.
- Version-control this narrative in your **customer** workspace; the repository copy here is the **method** only — numbers change per engagement.

---

## 9. References

- **`REAL_MODE_BENCHMARK.md`** — PowerShell schema + k6 JSON format.
- **`LOAD_TEST_BASELINE.md`** — k6 scenarios, thresholds, baseline table conventions.
- **`PILOT_ROI_MODEL.md`**, **`docs/go-to-market/ROI_MODEL.md`** — measurement + business case linkage.
- **`OBSERVABILITY.md`** — trace completeness histogram and alert rationale.
- API: **`GET /v1/explain/runs/{runId}/findings/{findingId}/explainability`** (see OpenAPI / `ExplanationController`).
