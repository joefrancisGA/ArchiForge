> **Scope:** Pilot guide (redirect) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Pilot guide (redirect)

**Operator / pilot** material is merged into the command-first quickstart. **V1 boundary** (scope, gates) stays in **[V1_SCOPE.md](V1_SCOPE.md)**.

**Canonical:** [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md)

**Evaluator / buyer — first review package without the deep stack:** [CORE_PILOT.md](../CORE_PILOT.md#first-session-checklist) (four steps + what to send a sponsor).

**First real Azure OpenAI on the demo stack:** [FIRST_REAL_VALUE.md](FIRST_REAL_VALUE.md) (`archlucid try --real`, **`ARCHLUCID_REAL_AOAI=1`**, ADR **[`../adr/0033-first-real-value-single-env-var-flip.md`](../adr/0033-first-real-value-single-env-var-flip.md)**).

**Prior pilot narrative:** [archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md](../archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md)

## Verification ladder

1. **A — Run / deploy readiness** — environment checks your team already runs before declaring “ready.”  
2. **B — Release smoke** — **[RELEASE_SMOKE.md](RELEASE_SMOKE.md)** (API + CLI + artifact checks).  
3. **C — Live E2E (UI + SQL truth)** — merge-blocking Playwright **`live-api-*.spec.ts`** per **`TEST_STRUCTURE.md`**; happy path **[LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)**. Release smoke alone does not prove SQL-backed UI parity — see **[RELEASE_SMOKE.md](RELEASE_SMOKE.md#release-smoke-ui-sql-parity)**.

## Reference architecture payloads (instant runs)

Starter **`ArchitectureRequest`** JSON files aligned with **`POST /v1/architecture/request`** live under **[`templates/reference-architectures/README.md`](../../templates/reference-architectures/README.md)**. Use them verbatim or as copy-paste baselines during pilots when teams do not yet have their own drafted request bodies.

There is no `archlucid request create --from-file` subcommand today; submit the file as the JSON body to **`POST /v1/architecture/request`** (same contract as the operator wizard import). From the repository root, after **`export BASE=https://your-api-host`** (or `http://localhost:5128`):

```bash
curl -sS -X POST "$BASE/v1/architecture/request" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: pilot-ref-arch-standard-3tier" \
  --data-binary "@templates/reference-architectures/standard-3-tier-web.json"
```

Use **`templates/reference-architectures/azure-serverless-api.json`** the same way for the serverless pattern. **`archlucid second-run`** expects the smaller **`SECOND_RUN`** schema ([`SECOND_RUN.md`](SECOND_RUN.md)), not a full **`ArchitectureRequest`** document.

## When you report an issue

Include **API `GET /version`**, **`X-Correlation-ID`**, relevant logs, and (if policy allows) a **support bundle** (`dotnet run --project ArchLucid.Cli -- support-bundle --zip`). Full checklist: [archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md#when-you-report-an-issue](../archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md#when-you-report-an-issue).

## Getting help

- **Product / pilot support:** **support@archlucid.net** — how-to, integration behavior, non-security defects during pilots.
- **Security vulnerabilities:** **security@archlucid.net** — coordinated disclosure only; see [SECURITY.md](../../SECURITY.md).
- **Accessibility barriers (non-security):** **accessibility@archlucid.net** — WCAG / usability in product or marketing surfaces.
- **Self-serve Q&A:** [FAQ.md](FAQ.md).

## Capturing your baseline at signup

Optional **review-cycle baseline** fields on anonymous **`POST /v1/register`** (see [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) §3.1):

| Field | Type | Rules |
|-------|------|--------|
| `baselineReviewCycleHours` | number (decimal) | Optional. When set: must be **greater than 0** and **at most 10000** (hours). |
| `baselineReviewCycleSource` | string | Optional. Max **256** characters after trim; control characters stripped. If you send a non-empty source, you **must** also send `baselineReviewCycleHours`. |

Example (PowerShell; replace organization and email):

```bash
curl -sS -X POST "https://localhost:5001/v1/register" ^
  -H "Content-Type: application/json" ^
  -d "{\"organizationName\":\"Acme Pilot Eval\",\"adminEmail\":\"you@example.com\",\"baselineReviewCycleHours\":18,\"baselineReviewCycleSource\":\"team estimate, two most recent reviews\"}"
```

The operator shell will gain a signup form for these fields in a follow-up UI PR; this API contract ships first.

## Post-commit sponsor banner (first commit clock)

After the first golden manifest commit, the operator-shell run detail page (`/runs/[runId]`) shows the **“Email this run to your sponsor”** banner when the run has a manifest. The banner may add a small **“Day N since first commit”** badge (UTC full days since this tenant’s first committed manifest, from **`GET /v1/tenant/trial-status`** field **`firstCommitUtc`**) so the sponsor pitch is anchored in the tenant’s own clock. Details: **[`SPONSOR_BANNER_FIRST_COMMIT_BADGE.md`](SPONSOR_BANNER_FIRST_COMMIT_BADGE.md)**.

Sponsors who do not have an operator install can preview a real commit page at **`/demo/preview`** on your marketing host (URL as deployed); the data is the **ArchLucid demo seed**, not your tenant. See **`docs/DEMO_PREVIEW.md`**.

## Pull-request decoration in your CI

ArchLucid surfaces **`GET /v1/compare`** Markdown in CI/CD for both GitHub Actions and Azure DevOps Pipelines — pick the entry point that matches your vendor:

- **Navigator:** [GitHub job summary](../integrations/GITHUB_ACTION_MANIFEST_DELTA.md) · [GitHub PR comment](../integrations/GITHUB_ACTION_MANIFEST_DELTA_PR_COMMENT.md) · [Azure DevOps job summary](../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md) · [Azure DevOps PR comment](../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) · [Azure DevOps server-side (Worker)](../integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md)

The server-side path is optional and posts to a **single configured PR** from Worker settings; the pipeline templates are the usual choice for ADO-shop pilots who want YAML snippets.

