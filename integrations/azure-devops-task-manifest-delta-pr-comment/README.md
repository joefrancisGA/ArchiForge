# Azure Pipelines template: ArchLucid manifest delta — sticky PR comment + PR status

This folder mirrors **[`integrations/github-action-manifest-delta-pr-comment/`](../github-action-manifest-delta-pr-comment/)**:

1. Runs the shared **[`fetch-manifest-delta.mjs`](../github-action-manifest-delta/fetch-manifest-delta.mjs)** (single source of truth for Markdown **byte shape**).
2. Upserts a **single sticky PR thread comment** (hidden marker `<!-- archlucid:manifest-delta -->` by default) and posts an **informational PR status** (`state: succeeded`, `context.name: archlucid-manifest`, `context.genre: archlucid`) via Azure DevOps Git REST **7.1** — same JSON bodies as **`AzureDevOpsPullRequestWireFormat`** in **`ArchLucid.Integrations.AzureDevOps`** (see ADR 0024).

**No `az` CLI** and **no extra npm packages** — only Node **22** built-ins (`fetch`, `node:test`).

## Inputs

| Name | Required | Description |
| --- | --- | --- |
| `api-base-url` | yes | ArchLucid API base URL (no trailing slash). |
| `api-token` | yes | ReadAuthority API key (`X-Api-Key`). |
| `base-run-id` | yes | Baseline committed run id (GUID). |
| `target-run-id` | yes | Candidate committed run id (GUID). |
| `pr-id` | yes | Azure DevOps pull request **id** (integer). |
| `repository-id` | yes | Azure DevOps Git **repository id** (GUID). |
| `organization` | yes | Organization name (URL segment). |
| `project` | yes | Project name (URL segment). |
| `azure-devops-pat` | no | Personal Access Token (**Code Read & write**). Leave empty to use **`System.AccessToken`** (Bearer mode). |
| `marker` | no | HTML comment marker; default `<!-- archlucid:manifest-delta -->`. |
| `operator-compare-url-template` | no | Optional `{baseRunId}` / `{targetRunId}` template for PR status `targetUrl`. |

## Auth modes

| Mode | When | Header |
| --- | --- | --- |
| **A — System token** (preferred) | `azure-devops-pat` is empty and the pipeline exposes **`System.AccessToken`** | `Authorization: Bearer $(System.AccessToken)` |
| **B — PAT** | `azure-devops-pat` is set | `Authorization: Basic` with `:{PAT}` base64 (same wire format as the server-side C# decorator) |

**Mode A** requires **`checkout: self` with `persistCredentials: true`** (or equivalent) so `System.AccessToken` is populated, and the **Project Collection Build Service** must have **Contribute to pull requests** on the repo. The script logs only **`archlucid:ado-auth mode=Bearer`** or **`mode=Basic`** — never the secret value.

## Smoke test (Node)

```bash
node --test integrations/azure-devops-task-manifest-delta-pr-comment/post-pr-thread.test.mjs
```

## Usage

See **[`docs/integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md`](../../docs/integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md)** and **[`example.azure-pipelines.yml`](./example.azure-pipelines.yml)**.
