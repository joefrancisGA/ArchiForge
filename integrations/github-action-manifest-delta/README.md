# GitHub Action: ArchLucid manifest delta

Composite action that calls **`GET /v1/compare`** with two committed run ids and appends a Markdown summary to **`GITHUB_STEP_SUMMARY`** (visible on the GitHub Actions run summary page).

## Inputs

| Name | Required | Description |
| --- | --- | --- |
| `api-base-url` | yes | API origin without trailing slash. |
| `api-token` | yes | `X-Api-Key` value with **ReadAuthority**. |
| `base-run-id` | yes | Baseline run id (GUID string). |
| `target-run-id` | yes | Candidate run id (GUID string). |
| `operator-compare-url-template` | no | Optional deep link template using `{baseRunId}` and `{targetRunId}`. |

## Usage

See **`docs/integrations/GITHUB_ACTION_MANIFEST_DELTA.md`** in this repository and **`.github/workflows/example-manifest-delta.yml`** at the repo root.

## Related

For an inline **sticky pull-request comment** (instead of a job-summary entry), use the sibling action **[`integrations/github-action-manifest-delta-pr-comment/`](../github-action-manifest-delta-pr-comment/)**, which reuses this script's `fetch-manifest-delta.mjs` for the Markdown body.
