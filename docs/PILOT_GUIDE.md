> **Scope:** Pilot guide (redirect) - full detail, tables, and links in the sections below.

# Pilot guide (redirect)

**Operator / pilot** material is merged into the command-first quickstart. **V1 boundary** (scope, gates) stays in **[V1_SCOPE.md](V1_SCOPE.md)**.

**Canonical:** [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md)

**Prior pilot narrative:** [archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md](archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md)

## When you report an issue

Include **API `GET /version`**, **`X-Correlation-ID`**, relevant logs, and (if policy allows) a **support bundle** (`dotnet run --project ArchLucid.Cli -- support-bundle --zip`). Full checklist: [archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md#when-you-report-an-issue](archive/ONBOARDING_PILOT_GUIDE_2026_04_17.md#when-you-report-an-issue).

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

## Pull-request decoration in your CI

ArchLucid surfaces **`GET /v1/compare`** Markdown in CI/CD for both GitHub Actions and Azure DevOps Pipelines — pick the entry point that matches your vendor:

- **Navigator:** [GitHub job summary](integrations/GITHUB_ACTION_MANIFEST_DELTA.md) · [GitHub PR comment](integrations/GITHUB_ACTION_MANIFEST_DELTA_PR_COMMENT.md) · [Azure DevOps job summary](integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md) · [Azure DevOps PR comment](integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) · [Azure DevOps server-side (Worker)](integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md)

The server-side path is optional and posts to a **single configured PR** from Worker settings; the pipeline templates are the usual choice for ADO-shop pilots who want YAML snippets.

