> **Scope:** Buyer-facing end-to-end recipes bridging ArchLucid to enterprise workflows (Azure DevOps PR review, CloudEvents consumers, customer-owned Power Automate / Logic Apps); not a SKU matrix, endpoint inventory, or substitute for **V1** first-party **Jira** / **ServiceNow** connectors ([`V1_SCOPE.md`](V1_SCOPE.md) §2.13).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

# ITSM bridge — V1 recipe hub

**Audience:** Platform engineers and integrators who need a **single map** from ArchLucid to PR decoration, event-driven automation, or no-code bridges — alongside **or instead of** first-party **Jira** / **ServiceNow** connectors committed for **V1**.

**Non-goals:** This page does not replace [INTEGRATION_CATALOG.md](../go-to-market/INTEGRATION_CATALOG.md), [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md), or the OpenAPI contract. **First-party** **Jira** and **ServiceNow** are **in V1 scope** ([`V1_SCOPE.md`](V1_SCOPE.md) §2.13). These recipes are **customer-operated** alternatives when you prefer Logic Apps / Power Automate or need coverage **before** connector enablement. **Confluence** first-party remains **V1.1** ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6).

---

## Recipe 1 — Azure DevOps: PR comment + status (manifest delta)

**Goal:** On each pipeline run for an Azure Repos pull request, show the same **`GET /v1/compare`** Markdown as the GitHub “sticky PR comment” pattern: **one** thread updated via marker `<!-- archlucid:manifest-delta -->`, plus an informational PR status.

**Grounding docs and templates:**

| What | Where |
|------|--------|
| Runbook (auth modes, marker contract, example YAML) | [AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md](../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) |
| Pipeline task + scripts (`task.yml`, `post-pr-thread.mjs`, `example.azure-pipelines.yml`) | Repo folder [`integrations/azure-devops-task-manifest-delta-pr-comment/`](../../integrations/azure-devops-task-manifest-delta-pr-comment/) |
| Job-summary-only variant (Markdown on the **run** summary, same `fetch-manifest-delta.mjs`) | [AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md](../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA.md) · [`integrations/azure-devops-task-manifest-delta/`](../../integrations/azure-devops-task-manifest-delta/) |
| CI/CD overview (GitHub + Azure DevOps pointers) | [CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) |

**Conceptual flow:** Pipeline → `fetch-manifest-delta.mjs` → ArchLucid **`GET /v1/compare`** → `post-pr-thread.mjs` upserts the PR thread → optional PR status POST. Both runs must be **committed** with golden manifests in the same tenant scope, or compare returns **404** (see [API_CONTRACTS.md](API_CONTRACTS.md) / compare sections).

**Optional (Azure-native, minimal pipeline change):** Server-side PR decoration via Service Bus is documented separately — [AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md](../integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md) (see also [INTEGRATION_CATALOG.md §2](../go-to-market/INTEGRATION_CATALOG.md) **Azure DevOps Repos (Service Bus)** row).

---

## Recipe 2 — Generic CloudEvents consumer (HTTP webhook or Azure Service Bus)

**Goal:** Receive ArchLucid integration events in **your** subscription (custom worker, Logic App, Function, or partner bus), with a stable type string and validatable JSON payloads.

**Grounding docs:**

| What | Where |
|------|--------|
| CloudEvents envelope on HTTP, HMAC, Service Bus properties, outbox, worker processor | [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md) |
| Machine-readable index (`eventType`, `schemaFile`, `transport`, descriptions) | [`schemas/integration-events/catalog.json`](../../schemas/integration-events/catalog.json) |
| Per-event JSON Schemas under `schemas/integration-events/*.schema.json` | Same folder as the catalog (see table in [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md) § JSON Schema catalog) |
| Narrative event list (cross-links) | [INTEGRATION_EVENT_CATALOG.md](INTEGRATION_EVENT_CATALOG.md) |

**Outline (customer-owned):**

1. **Choose transport** — Hosted **HTTP webhook** (CloudEvents wrapper when enabled) and/or **Azure Service Bus** topic subscription per [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md).
2. **Route by type** — Use `event_type` / `Subject` / CloudEvents `type` exactly as emitted; validate candidates against **`catalog.json`**.
3. **Validate payload** — For strict checks, validate JSON **data** against the referenced `schemaFile` from the catalog (additive fields may appear; see doc notes).
4. **Handle idempotency** — Use `MessageId` / event `id` where provided; align with outbox and retry semantics described in [INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md).
5. **Correlate operations** — For downstream ITSM tickets, persist ArchLucid **`runId`** / alert keys from the payload; use the same **correlation** discipline as HTTP APIs ([API_CONTRACTS.md](API_CONTRACTS.md) § Correlation ID).

---

## Recipe 3 — No-code bridges (Logic Apps–first or Power Automate–first)

**Goal:** Bridge CloudEvents from ArchLucid to **Atlassian** or **ServiceNow** HTTP APIs using **Microsoft** automation **you** deploy and maintain — not first-party ArchLucid connectors.

**Honest framing:** Step-by-step flows live under [docs/integrations/recipes/](../integrations/recipes/README.md). Teams on **Azure Logic Apps Standard** should start with the **Logic Apps–first** ordering below; Power Automate–first ordering follows for Microsoft 365–centric tenants. These recipes complement **V1** first-party **Jira** / **ServiceNow** ([`V1_SCOPE.md`](V1_SCOPE.md) §2.13); **Confluence** first-party remains **V1.1** ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6).

### Azure Logic Apps–first (recommended when Logic Apps Standard is your integration plane)

| Entry | Use |
|-------|-----|
| Index + platform matrix | [recipes/README.md](../integrations/recipes/README.md) |
| **Azure DevOps (Logic Apps)** | [recipe-azure-logic-apps-webhook-to-ado-work-item.md](../integrations/recipes/recipe-azure-logic-apps-webhook-to-ado-work-item.md) |
| **ServiceNow incident (Logic Apps)** | [SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md](../integrations/recipes/SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md) |
| **Confluence (Logic Apps)** | [CONFLUENCE_PAGE_VIA_LOGIC_APPS.md](../integrations/recipes/CONFLUENCE_PAGE_VIA_LOGIC_APPS.md) |

### Power Automate–first (Microsoft 365 / Power Platform–centric)

| Entry | Use |
|-------|-----|
| Index + platform matrix | [recipes/README.md](../integrations/recipes/README.md) |
| **ServiceNow (Power Automate)** | [SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md](../integrations/recipes/SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md) |
| **Jira (Power Automate)** | [JIRA_ISSUE_VIA_POWER_AUTOMATE.md](../integrations/recipes/JIRA_ISSUE_VIA_POWER_AUTOMATE.md) |

Developer-oriented alternatives (Azure Functions, HMAC, custom code) remain in [`templates/integrations/`](../../templates/integrations/) as referenced from [recipes/README.md](../integrations/recipes/README.md).

---

## Related

- [INTEGRATION_CATALOG.md](../go-to-market/INTEGRATION_CATALOG.md) — available vs roadmap connectors
- [TENANT_TIER_AND_ROUTE_ENUMERATION.md](TENANT_TIER_AND_ROUTE_ENUMERATION.md) — tier-gated **404** and anti-enumeration for API probes
