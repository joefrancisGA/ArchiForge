# Azure Pipelines template: ArchLucid manifest delta (job summary)

This folder mirrors **[`integrations/github-action-manifest-delta/`](../github-action-manifest-delta/)**: it calls **`GET /v1/compare`** via the **same** [`fetch-manifest-delta.mjs`](../github-action-manifest-delta/fetch-manifest-delta.mjs) script and publishes the Markdown to the **Azure Pipelines run summary** using `##vso[task.uploadsummary]` (see [`job-summary.mjs`](./job-summary.mjs)).

## Inputs

| Name | Required | Description |
| --- | --- | --- |
| `api-base-url` | yes | API origin without trailing slash. |
| `api-token` | yes | `X-Api-Key` value with **ReadAuthority** (map from a secret variable group). |
| `base-run-id` | yes | Baseline run id (GUID string). |
| `target-run-id` | yes | Candidate run id (GUID string). |
| `operator-compare-url-template` | no | Optional deep link template using `{baseRunId}` and `{targetRunId}`. |

## Optional: soft compare failures (404)

Set pipeline variable **`ARCHLUCID_COMPARE_WARN_ONLY=1`** on the step that runs `fetch-manifest-delta.mjs` (via `job-summary.mjs` env) so a **404** from `GET /v1/compare` exits **0** and prints a **WARNING** line instead of failing the job — useful while the target run is not yet committed.

## Smoke test (Node)

Run from repo root (Node 22):

```bash
node --test integrations/azure-devops-task-manifest-delta/job-summary.test.mjs
```

## Usage

See **[`docs/integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md`](../../docs/integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md)** and the example pipeline **[`example.azure-pipelines.yml`](./example.azure-pipelines.yml)**.

## Related

- **[`integrations/azure-devops-task-manifest-delta-pr-comment/`](../azure-devops-task-manifest-delta-pr-comment/)** — sticky PR thread + PR status (GitHub “PR comment” action equivalent).
