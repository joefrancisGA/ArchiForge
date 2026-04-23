> **Scope:** OWASP ZAP baseline rules (baseline-pr.tsv) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# OWASP ZAP baseline rules (`baseline-pr.tsv`)

ArchLucid wires **OWASP ZAP** baseline scanning into **GitHub Actions** using `zap-baseline.py` and a small **rule override file** in the repository. The CI job builds **`ArchLucid.Api/Dockerfile`**, waits for **`/health/live`**, then runs the scanner against the API on an isolated Docker network.

The baseline **target URL** is the API origin (`http://archlucid-zap-api:8080`). ZAP’s automation expects **HTTP 200** on **`/`**, **`/robots.txt`**, and **`/sitemap.xml`**; the API serves minimal anonymous responses there so the spider plan does not fail on **404** bodies. **`SecurityHeadersMiddleware`** uses **`Cache-Control: public, max-age=3600`** on those paths (not `no-store`) so passive **10049-1** (non-storable) does not warn on anonymous stubs; that choice triggers **10049-3** (storable and cacheable), which is **`IGNORE`**’d in **`baseline-pr.tsv`** with rationale. The middleware adds **`Cross-Origin-Resource-Policy`**, **`Cross-Origin-Embedder-Policy`**, and **`Cross-Origin-Opener-Policy: same-origin`** for **90004-1 / 90004-2 / 90004-3** (Spectre isolation family).

## What “blocking” means

`zap-baseline.py` exits **non-zero** when:

- Any alert is classified as **FAIL**, or
- Any alert is classified as **WARN** and the **`-I`** flag is **not** used.

ArchLucid CI and the scheduled workflow run **without** **`-I`**, so **unresolved WARN findings fail the pipeline**. Rules listed in `baseline-pr.tsv` with level **`IGNORE`** are suppressed and do not count toward WARN/FAIL.

## File location and mount

| Repository path | Inside ZAP container (CI) |
|-----------------|---------------------------|
| `infra/zap/baseline-pr.tsv` | `/zap/wrk/config/baseline-pr.tsv` |

Workflows pass **`-c config/baseline-pr.tsv`** because the compose mount maps the **`infra/zap`** directory to **`/zap/wrk/config`**.

## `baseline-pr.tsv` format

The file is loaded by ZAP’s **`load_config`** helper (`zap_common.py`):

- **Encoding:** UTF-8 text.
- **Lines:** One rule per line.
- **Comments / blanks:** Lines starting with **`#`** or empty lines are **skipped**.
- **Data lines:** **At least three tab-separated fields** (tabs only — not spaces):

  1. **Plugin id** — ZAP passive rule id (e.g. `10035`). Comma-separated ids are only used for **`OUTOFSCOPE`** rows (not used in ArchLucid’s baseline today).
  2. **Level** — One of **`IGNORE`**, **`INFO`**, **`WARN`**, **`FAIL`** (ArchLucid uses **`IGNORE`** for accepted false positives).
  3. **Trailing comment** — Human-readable text after the second tab. Shown in logs; document **why** the rule is ignored or downgraded.

Example:

```text
10035	IGNORE	(Strict-Transport-Security not set) — ZAP CI targets plain HTTP on the Docker bridge; HSTS is applied at TLS termination.
```

Optional fourth tab-separated segment exists in ZAP for advanced messages; ArchLucid does not rely on it.

Official reference text in generated baselines:

> Only the rule identifiers are used — the names are just for info  
> You can add your own messages to each rule by appending them after a tab on each line.

## Adding a rule

1. Reproduce or capture the failing job log and note the **plugin id** in brackets (e.g. `[10035]`) and alert name.
2. Decide:
   - **True positive:** fix the API or deployment (headers, exposure, auth), then remove or avoid adding **`IGNORE`**.
   - **False positive / accepted risk:** add a line to **`infra/zap/baseline-pr.tsv`** with **`IGNORE`** and a **short, specific justification** (scan context, JSON API vs browser, ingress vs app responsibility).
3. Open a PR; **CI ZAP** must pass with the new line.

## Removing a rule

1. Delete or change the line in `baseline-pr.tsv`.
2. If the underlying issue is fixed, the scan should **stop raising** that alert; if not, the job will fail — either fix the product or restore a documented **`IGNORE`**.

## Triage process for new findings

1. **Identify:** From the Actions log, note **rule id**, **URL**, and **evidence** (response header/body snippet).
2. **Classify:**
   - **Exploitable or policy violation** → track as a defect; fix in code, config, or infrastructure.
   - **Noise for this scan profile** (e.g. HTTP-only URL inside CI, timestamps in JSON, HSTS only at the edge) → add **`IGNORE`** with an explanation that future readers can audit.
3. **Verify:** Push a branch and confirm **`security-zap-api-baseline`** (and optionally run the scheduled workflow manually via **workflow_dispatch**).
4. **Document:** Link the PR or issue in the rule comment if helpful (keep the TSV line readable).

## Related docs

- [infra/zap/README.md](../../infra/zap/README.md) — tiers, mounts, `RUNNER_TEMP` permissions.
- [docs/SECURITY.md](../library/SECURITY.md) — high-level pointer to ZAP as a blocking gate.
