> Archived 2026-04-23 — superseded by [docs/START_HERE.md](../START_HERE.md) and the current assessment pair under ``docs/``. Kept for audit trail.

> **Scope:** Cursor prompts — Quality improvement 3 (load / k6 / CI performance gate) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Cursor prompts — Quality improvement 3 (load / k6 / CI performance gate)

> **Not the weighted-assessment “Improvement 3”:** for rename / single solution / archived quality reports, use **[CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md)**.

Standalone prompts derived from **`docs/QUALITY_IMPROVEMENT_PROMPTS_2026_04_14.md` § Prompt 3** and the **current** repo layout. Use them **one at a time** in a Cursor Agent session unless you explicitly want parallel work on disjoint files.

---

## Objective

Keep **repeatable** API load signals and a **merge-blocking** CI gate for operator paths (read + write) without relying on Docker Compose in PR CI. Improvement 3 in the April 14 assessment targeted: `tests/load/ci-smoke.js`, `k6-ci-smoke` in **`.github/workflows/ci.yml`**, summary assert + docs.

## Assumptions

- **DevelopmentBypass** + **Simulator** agent mode are acceptable for synthetic traffic on ephemeral SQL in Actions.
- **Rate limits** must be raised for k6 jobs (see existing `RateLimiting__FixedWindow__PermitLimit` in **`.github/workflows/ci.yml`**) or VUs will see mass **429**.
- k6 is a **host binary** or **container**; do not add it to **`package.json`**.

## Constraints

- Do **not** modify **`tests/load/smoke.js`** or **`scripts/load/hotpaths.js`** unless a prompt explicitly says so.
- Do **not** modify **`.github/workflows/load-test.yml`** unless a prompt explicitly says so (manual full-stack workflow).
- Do **not** modify **historical** SQL migration files **`001`–`028`**; use new migrations + **`ArchLucid.sql`** if schema is needed (unlikely for k6-only work).

---

## Current implementation inventory (do not re-build blindly)

| Artifact | Location / notes |
| --- | --- |
| CI read + write smoke (tag `k6ci`) | **`tests/load/ci-smoke.js`** — health live/ready, `POST /v1/architecture/request`, list runs, audit search; per-tag thresholds in-script |
| Operator-path smoke after full regression (tag `k6api`) | **`tests/load/k6-api-smoke.js`** — ready, version, create run, authority runs list |
| Merge-blocking CI jobs | **`.github/workflows/ci.yml`** → **`k6-smoke-api`** (needs **`dotnet-full-regression`**, **native k6** via apt), **`k6-ci-smoke`** (needs **`dotnet-fast-core`**, k6 via **`grafana/k6:latest` Docker**) |
| Summary gate | **`scripts/ci/assert_k6_ci_smoke_summary.py`** — today enforces **global** `http_req_failed` rate + **single** `http_req_duration` p95 cap (workflow passes **`--max-p95-ms 3000`**) |
| Human-readable summary | **`scripts/ci/print_k6_summary_metrics.py`** |
| Baseline doc | **`docs/LOAD_TEST_BASELINE.md`** — includes **“CI smoke (automated)”** |
| Test taxonomy | **`docs/TEST_STRUCTURE.md`** — k6 Tier 2c table (**verify script names** against **`ci.yml`**; **`k6-smoke-api` uses `k6-api-smoke.js`, not `smoke.js`**) |

**Nodes → edges (CI k6 path):** `GitHub Actions runner` → `sqlserver service` (DB create step) → `ArchLucid.Api` (background, `:5128`) → `k6` (HTTP) → `summary JSON` → `assert_k6_ci_smoke_summary.py` → pass/fail.

---

## Prompt 3.1 — Align Python CI gate with per-scenario k6 thresholds

```
You are working in the ArchLucid repo.

Problem: `tests/load/ci-smoke.js` defines per-tag thresholds (`http_req_duration{k6ci:health_live}`, `…health_ready`, `…create_run`, etc.), but `scripts/ci/assert_k6_ci_smoke_summary.py` only checks a single global `http_req_duration` p(95). That weakens the regression gate and can let one slow scenario slip through if another is fast.

Tasks:
1. Inspect k6 `--summary-export` JSON shape for **tagged** `http_req_duration` metrics (k6 v0.49+). Extend `assert_k6_ci_smoke_summary.py` to optionally enforce caps per `k6ci` tag matching the thresholds in `ci-smoke.js` (500 / 1500 / 3000 / 1500 / 1500 ms as documented in `docs/LOAD_TEST_BASELINE.md`). Keep backward compatibility: if tagged blocks are absent, fall back to today’s global `--max-p95-ms` behavior and emit a stderr warning once.
2. Update `.github/workflows/ci.yml` `k6-ci-smoke` step that invokes the assert script to pass the new flags (or a single `--per-tag-ci-smoke` switch that encodes the caps in Python constants).
3. Update `docs/LOAD_TEST_BASELINE.md` “Merge gate (summary JSON)” to describe tagged vs global checks.

Constraints:
- Do not change `ci-smoke.js` scenario timings in this prompt unless required to fix a bug uncovered by stricter asserts.
- Keep the script runnable locally: `python3 scripts/ci/assert_k6_ci_smoke_summary.py /tmp/k6-ci-summary.json …`
- Use clear argparse help strings; no new third-party dependencies.
```

---

## Prompt 3.2 — Run `k6-ci-smoke` with native k6 (match `k6-smoke-api`)

```
You are working in the ArchLucid repo.

Problem: `k6-ci-smoke` uses `docker run grafana/k6:latest` while `k6-smoke-api` installs k6 via the Grafana APT repo. Two paths increase maintenance cost (image pin, volume mounts, user mapping) and confuse operators reading `docs/TEST_STRUCTURE.md`.

Tasks:
1. Change `.github/workflows/ci.yml` job `k6-ci-smoke` to install k6 using the same bash block as `k6-smoke-api` (GPG key + `dl.k6.io` apt source + `apt-get install -y k6`).
2. Replace the “Run k6 CI smoke (Docker)” step with a shell step that runs `k6 run "${GITHUB_WORKSPACE}/tests/load/ci-smoke.js"` (or equivalent), passing `BASE_URL=http://127.0.0.1:5128` and `--summary-export` to `${{ runner.temp }}/k6-ci-summary.json`.
3. Update `docs/TEST_STRUCTURE.md` k6 section to state that **both** merge-blocking k6 jobs use **native** k6 on Ubuntu runners (remove or correct any claim that all k6 CI uses only the Docker image).

Constraints:
- Preserve existing env vars for the API process (SQL connection, rate limits, DevelopmentBypass, Simulator).
- Keep artifact upload paths and assert/print steps unchanged unless paths must move.
- Do not add Docker-in-Docker requirements for k6 itself.
```

---

## Prompt 3.3 — Add `/version` to `ci-smoke.js` (parity with operator-path script)

```
You are working in the ArchLucid repo.

Goal: `tests/load/k6-api-smoke.js` calls `GET /version`; `tests/load/ci-smoke.js` does not. Add a lightweight version check to the CI read+write script so deployment / build regressions surface in the write-path job too.

Tasks:
1. Extend `tests/load/ci-smoke.js`:
   - Add a small scenario (or fold into `healthFn` after ready) that `GET`s `/version` with the same correlation-id pattern and a new tag e.g. `k6ci:version`.
   - Add a threshold `http_req_duration{k6ci:version}` consistent with other read-only calls (suggest p95 < 1500 ms unless you have evidence to tighten).
   - Keep total wall-clock roughly within the existing CI budget (if needed, shorten another scenario by a few seconds — document in file header comment).
2. If Prompt 3.1 (per-tag assert) is merged, extend the Python caps table; otherwise ensure in-script k6 thresholds still fail the k6 run on regression.
3. Update `docs/LOAD_TEST_BASELINE.md` CI smoke bullet list to mention `/version`.

Constraints:
- Do not modify `k6-api-smoke.js` in this prompt except for shared comments only if unavoidable.
- Preserve unique `requestId` generation for `create_run`.
```

---

## Prompt 3.4 — Document and normalize k6 environment variables

```
You are working in the ArchLucid repo.

Problem: `ci-smoke.js` uses `BASE_URL`; `k6-api-smoke.js` prefers `ARCHLUCID_BASE_URL` then `BASE_URL`. Local copy-paste from README snippets fails when the wrong variable is exported.

Tasks:
1. Update `tests/load/README.md` with a matrix: script → required env vars → example one-liner → expected summary path.
2. Optionally (small, safe change): in `ci-smoke.js`, resolve base URL as `__ENV.ARCHLUCID_BASE_URL || __ENV.BASE_URL || "http://127.0.0.1:5128"` to match `k6-api-smoke.js`.
3. Cross-link from `docs/PERFORMANCE_TESTING.md` or `docs/PERFORMANCE.md` (whichever is the canonical operator-facing perf doc) to the README section.

Constraints:
- Do not break CI: workflows that only set `BASE_URL` must still work.
- No secrets in docs; use placeholders for API keys.
```

---

## Prompt 3.5 — Extract a reusable “SQL catalog + API ready” fragment for CI (optional, larger)

```
You are working in the ArchLucid repo.

Context: `k6-smoke-api`, `k6-ci-smoke`, and `ui-e2e-live` each duplicate patterns: SQL Server service, `docker run … sqlcmd` to create a database, `dotnet run` API in background, curl loop on `/health/ready`, rate limit env vars.

Tasks (choose the smallest viable approach):
1. Prefer a **composite GitHub Action** under `.github/actions/archlucid-api-ci-standup/` (or similar) with inputs: `database_name`, `connection_string`, `artifact_log_name`. Document inputs/outputs in `action.yml`.
2. Refactor **only** the two k6 jobs in `ci.yml` to consume the composite action first (prove it works), then leave a TODO comment for adopting it in `ui-e2e-live` in a follow-up PR if scope explodes.
3. Update `docs/TEST_EXECUTION_MODEL.md` (or `TEST_STRUCTURE.md`) with a short diagram: reusable action → API → k6.

Constraints:
- Must not weaken health checks or remove upload of API logs on failure.
- Keep secrets out of composite inputs; use `env` at call site for passwords already present in workflow.
- If composite actions are undesirable, instead extract a **bash script** under `scripts/ci/` and call it from both jobs — still document it.

Risk: This prompt can touch many YAML lines; schedule a dedicated session and keep CI green with a draft PR.
```

---

## Prompt 3.6 — Soak / scheduled load follow-up (nightly contract)

```
You are working in the ArchLucid repo.

Context: `tests/load/soak.js` and `.github/workflows/k6-soak-scheduled.yml` (or equivalent) may exist for low-rate long runs with `continue-on-error` and a secret base URL.

Tasks:
1. Read the scheduled workflow and `soak.js`. Document in `docs/LOAD_TEST_BASELINE.md` how soak differs from `ci-smoke.js` and `k6-api-smoke.js` (duration, VUs, auth, failure policy).
2. If the scheduled job lacks a **summary artifact** or **p95 printout**, add steps mirroring `print_k6_summary_metrics.py` so on-call can compare runs over time.
3. Add alerting guidance (even if manual): what to do when soak fails twice in a row.

Constraints:
- Do not make soak merge-blocking.
- Never print secrets in logs.
```

---

## Usage notes

- Run **3.2** before large doc edits that claim “native k6 everywhere,” or update docs after 3.2 lands.
- **3.1** and **3.3** combine well in one PR if you want one CI behavior change + script tweak.
- **3.5** is intentionally **optional**; use when duplication causes real merge pain.

## Related documents

- **`docs/QUALITY_IMPROVEMENT_PROMPTS_2026_04_14.md`** — original monolithic Prompt 3 spec  
- **`docs/LOAD_TEST_BASELINE.md`** — architecture, baseline table, CI smoke narrative  
- **`docs/TEST_STRUCTURE.md`** — tier table for k6 jobs  
