> **Scope:** Azure Pipelines — manifest delta job summary (ArchLucid) — buyer-facing runbook.

> **Picking a vendor:** [GitHub job summary](GITHUB_ACTION_MANIFEST_DELTA.md) · [GitHub PR comment](GITHUB_ACTION_MANIFEST_DELTA_PR_COMMENT.md) · [Azure DevOps job summary](AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md) · [Azure DevOps PR comment](AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) · [Azure DevOps server-side](AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md)

# Azure Pipelines — manifest delta (job summary)

**Audience:** Platform engineers wiring ArchLucid into **Azure DevOps Pipelines** who want the same **`GET /v1/compare`** Markdown as the GitHub composite action, but rendered on the **pipeline run** summary page (the ADO equivalent of GitHub Actions’ job summary).

**Purpose:** Surface structured golden-manifest delta between two **committed** runs without opening the operator UI first.

**Template path:** [`integrations/azure-devops-task-manifest-delta/`](../../integrations/azure-devops-task-manifest-delta/) (`task.yml` + [`job-summary.mjs`](../../integrations/azure-devops-task-manifest-delta/job-summary.mjs)).

---

## Prerequisites

- Both runs must exist in the **same tenant scope** as the API key and must already have **golden manifests** (committed). Otherwise `GET /v1/compare` returns **404** — see [`docs/API_CONTRACTS.md`](../API_CONTRACTS.md).
- API key must satisfy **ReadAuthority** (`X-Api-Key`), same as GitHub automation.
- **Node.js 22** on the agent (declared via `NodeTool@0` in `task.yml`).

---

## Markdown source of truth

The Markdown shape is produced by **[`integrations/github-action-manifest-delta/fetch-manifest-delta.mjs`](../../integrations/github-action-manifest-delta/fetch-manifest-delta.mjs)** only. The Azure Pipelines wrapper **does not fork** that script — it runs it via `job-summary.mjs` so a single edit updates both GitHub and Azure DevOps surfaces.

---

## Secrets

Store the API key in an Azure DevOps **variable group** (e.g. `archlucid-readonly-api-key`) and map it to the template parameter `api-token` at queue time (e.g. `$(ARCHLUCID_READONLY_API_KEY)`). **Never** commit keys to YAML.

---

## Soft compare failures (optional)

If you want the pipeline to stay **green** when the target run is not yet committed (transient 404), set **`ARCHLUCID_COMPARE_WARN_ONLY=1`** in the environment for the step that ultimately invokes `fetch-manifest-delta.mjs` (see `job-summary.mjs` — you can add `env:` on the template step in your pipeline). The fetch script then prints a **WARNING** and emits a short Markdown stub instead of exiting non-zero.

---

## Example (copy-paste)

See **[`integrations/azure-devops-task-manifest-delta/example.azure-pipelines.yml`](../../integrations/azure-devops-task-manifest-delta/example.azure-pipelines.yml)**.

---

## Related

- [`AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md`](AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) — sibling template that posts the same Markdown to a **sticky PR thread** + **PR status**.
- [ADR 0024 — Azure DevOps pipeline task parity](../adr/0024-azure-devops-pipeline-task-parity-with-github-action.md)
