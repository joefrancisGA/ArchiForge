> **Scope:** Azure DevOps — server-side PR decoration when an authority run completes (Worker integration handler).

> **Picking a vendor:** [GitHub job summary](GITHUB_ACTION_MANIFEST_DELTA.md) · [GitHub PR comment](GITHUB_ACTION_MANIFEST_DELTA_PR_COMMENT.md) · [Azure DevOps job summary](AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md) · [Azure DevOps PR comment](AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) · **Azure DevOps server-side (this page)**

# Azure DevOps — server-side PR decoration (manifest commit)

## Two paths — which one is yours?

| Path | When to use | Doc |
| --- | --- | --- |
| **Pipeline task (recommended for most ADO-shop pilots)** | You want a **YAML snippet** in `azure-pipelines.yml`, same inputs as the GitHub Actions, no Worker config. | [AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md](AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) |
| **Server-side fan-out (this page)** | You want **zero pipeline changes**: ArchLucid Worker posts to **one** configured `(RepositoryId, PullRequestId)` when `com.archlucid.authority.run.completed` fires. | This document |

Both paths call the same **Azure DevOps Git REST 7.1** surfaces (PR **threads** + **statuses**) and reuse the same JSON wire bodies as **`AzureDevOpsPullRequestWireFormat`** in **`ArchLucid.Integrations.AzureDevOps`** and the pipeline Node serializers (ADR 0024).

---

## Objective

Mirror the [GitHub Action manifest delta](GITHUB_ACTION_MANIFEST_DELTA.md) story for **Azure DevOps Repos** at the **platform** layer: when an authority run completes and emits `com.archlucid.authority.run.completed`, optionally post a **PR status** and **PR thread comment** with run and golden-manifest identifiers — **without** requiring the buyer to edit pipelines.

## Configuration

Section **`AzureDevOps`** in `appsettings.json` (see defaults in repo). Set:

| Key | Meaning |
|-----|---------|
| `Enabled` | Master switch; default `false`. |
| `Organization` | Azure DevOps organization name (URL segment). |
| `Project` | Project name (URL segment; spaces allowed). |
| `PersonalAccessToken` | PAT with permission to post threads and statuses on the target repo. Prefer **Key Vault reference** in production (`@Microsoft.KeyVault(...)`). |
| `RepositoryId` | Git repository UUID in Azure DevOps. |
| `PullRequestId` | Integer PR id to decorate (pilot pattern until run→PR mapping is stored in SQL). For pipeline-driven per-PR decoration, use the [pipeline task](AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) instead. |
| `StatusTargetUrl` | Optional URL for the PR status “open” link (e.g. operator run detail). |

## Runtime wiring

- **Worker** role registers `AuthorityRunCompletedAzureDevOpsIntegrationEventHandler` and `IAzureDevOpsPullRequestDecorator` (`ArchLucid.Integrations.AzureDevOps`).
- Consumes the same integration event type as other subscribers: **`com.archlucid.authority.run.completed`** ([schema](../../schemas/integration-events/authority-run-completed.v1.schema.json)).

**Audit:** This path does **not** emit a dedicated “PR decorated” audit row from the buyer’s repo; ArchLucid only records the authority run lifecycle it already owned. Pipeline-side decoration is **invisible** to ArchLucid by design (no API call from the buyer pipeline to ArchLucid for the ADO REST leg — the pipeline only calls `GET /v1/compare` with your API key).

## Security

- Do **not** log PATs or full Authorization headers.
- Treat PAT as a secret; rotate on compromise.
- No SMB (port 445) for artifact paths; Azure DevOps is HTTPS-only.

## Related

- [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../INTEGRATION_EVENTS_AND_WEBHOOKS.md)
- [REFERENCE_SAAS_STACK_ORDER.md](../REFERENCE_SAAS_STACK_ORDER.md)
- [ADR 0024 — Azure DevOps pipeline task parity](../adr/0024-azure-devops-pipeline-task-parity-with-github-action.md)
